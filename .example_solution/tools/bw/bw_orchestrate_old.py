#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
bw_orchestrate.py — repo-aware, business-specific BW documentation generator

Key outputs:
- out/sir/<Process>.json                 (SIR per process)
- out/sir/_interdeps.json                (calls, shared identifiers, shared JDBC, families)
- out/docs/<Process>.md                  (specific, evidence-tagged)
- out/docs/Family_<family>.md            (domain family doc, e.g., all "movie*" working together)
- out/docs/REPO_OVERVIEW.md              (holistic view across families)
- out/docs/assets/graphs/<Process>.svg  (optional; Graphviz; Windows-safe temp handling)

Windows Graphviz fix: close mkstemp() file descriptor before invoking 'dot'. See:
- https://bugs.python.org/issue14243  (NamedTemporaryFile on Windows) 
- tempfile docs on mkstemp() usage. 
"""

from __future__ import annotations
import datetime
import hashlib
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import zipfile
from pathlib import Path
from typing import Dict, List, Tuple, Optional, Set, Union
import click
import yaml
import networkx as nx
from lxml import etree as ET
from tqdm import tqdm

# -----------------------
# Paths & constants
# -----------------------
REPO_ROOT = Path(__file__).resolve().parents[3] if len(Path(__file__).resolve().parents) >= 4 else Path.cwd()
SCHEMA_PATH = REPO_ROOT / ".roo" / "schemas" / "sir.schema.json"
OUT_SIR = REPO_ROOT / "out" / "sir"
OUT_GRAPHS = REPO_ROOT / "out" / "graphs"
OUT_DOCS = REPO_ROOT / "out" / "docs"
OUT_LOGS = REPO_ROOT / "out" / "logs"
OUT_TMP = REPO_ROOT / "out" / "tmp" / "archives"
CHECK_ENV = REPO_ROOT / ".roo" / "tools" / "bw" / "check_env.py"
PROMPT_DEFAULT = REPO_ROOT / ".roo" / "prompts" / "bw_explain.md"
DEFAULT_ENV_FILE = REPO_ROOT / ".env"
PALETTE_MAP_FILE = REPO_ROOT / ".roo" / "tools" / "bw" / "palette_roles.json"

TOOL_VERSION = "bw-orchestrator-1.3.0"

BPWS_NS = {"bpws": "http://docs.oasis-open.org/wsbpel/2.0/process/executable"}
NS = {
    "xmi": "http://www.omg.org/XMI",
    "bpws": BPWS_NS["bpws"],
}

# Known types to improve "known_types_coverage"
KNOWN_TYPES = {
    "pb:ui_event","pb:service_call","pb:method_call","pb:datawindow_op",
    "pb:db_exec","pb:http_request","pb:soap_call","pb:external_function","pb:file_io"
    "http:ReceiveHTTP", "http:SendHTTP", "rest:Invoke", "rest:Receive", "rest:Reply",
    "soap:Invoke", "soap:Receive", "soap:Reply",
    "mapper:Mapper",
    "jdbc:Insert", "jdbc:Update", "jdbc:Select", "jdbc:Delete", "jdbc:CallProcedure",
    "file:Read", "file:Write",
    "timer:TimerEvent",
    "jms:Send", "jms:Receive", "jms:Publish", "jms:Subscribe",
    "process:CallProcess", "bw:CallProcess", "bw:Throw", "bw:Catch", "bw:Choice", "bw:Group", "bw:ForEach"
}

# Fallback roles (overridden by palette map file if present)
DEFAULT_PREFIX_ROLES = {
    "http": ["interface.receive", "interface.reply"],
    "rest": ["interface.receive", "interface.reply"],
    "soap": ["interface.receive", "interface.reply"],
    "process": ["invoke.process"], "bw": ["invoke.process"],
    "jdbc": ["data.jdbc"], "jms": ["messaging.jms"],
    "mapper": ["transform.mapper"], "timer": ["schedule.timer"],
    "log": ["ops.log"], "error": ["error.throw"],
    "file": [], "json": [], "xml": []
}

# Family detection patterns
NAME_FAMILY_PATTERNS = [
    r"^(?P<fam>[A-Za-z0-9]+(?:\.[A-Za-z0-9]+)+)\.(?P<leaf>[^.]+)$",   # like moviecatalogsearch.module.Process
    r"^(?P<fam>[A-Za-z0-9]+)\.(?P<leaf>[^.]+)$"                       # fallback
]

IDENTIFIER_TOKENS = ["SSN","CustomerId","CustomerID","AccountNumber","FICOScore","Rating","CorrelationId","MovieId","Title","Id","ID"]

# -----------------------
# Env helpers
# -----------------------
def load_env_file(env_file: Path, override: bool = False) -> bool:
    if not env_file or not Path(env_file).exists():
        return False
    try:
        import dotenv  # type: ignore
        dotenv.load_dotenv(dotenv_path=str(env_file), override=override)
        print(f"[env] file: {env_file} LOADED")
        return True
    except Exception:
        pass
    # Fallback manual parser
    try:
        for raw in Path(env_file).read_text(encoding="utf-8").splitlines():
            line = raw.strip()
            if not line or line.startswith("#"):
                continue
            if line.lower().startswith("export "):
                line = line[7:].strip()
            if "=" not in line:
                continue
            k,v = line.split("=",1)
            v = v.strip().strip('"').strip("'")
            if override or k not in os.environ:
                os.environ[k.strip()] = v
        print(f"[env] file: {env_file} LOADED")
        return True
    except Exception:
        print(f"[env] file: {env_file} NOT LOADED")
        return False

CONFIG_DEFAULT = (REPO_ROOT / ".roo" / "config.yaml")

def load_llm_config(cfg_path: Optional[Path] = None) -> Dict[str, str]:
    """Load provider/model from .roo/config.yaml; env overrides still win."""
    cfg = {}
    path = (cfg_path or CONFIG_DEFAULT)
    try:
        if path.exists():
            cfg = yaml.safe_load(path.read_text(encoding="utf-8")) or {}
    except Exception:
        cfg = {}
    # env overrides
    if os.getenv("OPENAI_MODEL"):
        cfg["model"] = os.getenv("OPENAI_MODEL")
    if os.getenv("OPENAI_PROVIDER"):
        cfg["provider"] = os.getenv("OPENAI_PROVIDER")
    # sensible defaults
    cfg.setdefault("provider", "openai")
    cfg.setdefault("model", "gpt-5-chat-latest")
    return cfg

# -----------------------
# IO helpers
# -----------------------
def atomic_write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.NamedTemporaryFile("w", delete=False, encoding="utf-8") as tmp:
        tmp.write(text)
        tmp_path = Path(tmp.name)
    tmp_path.replace(path)

# --- Front-matter helpers for dox_draft_plan.md ------------------------------


def _read_plan_yaml(plan_source: Union[Path, str]) -> Tuple[Dict, str]:
    """
    Returns (header_dict, body_str).

    Accepts either:
    - Path to a Markdown file with YAML front matter
    - Raw Markdown text (string) that includes a YAML front matter block

    If no front matter present, returns ({}, full_text or "").
    """
    # Load text based on type
    if isinstance(plan_source, Path):
        if not plan_source.exists():
            return {}, ""
        raw = plan_source.read_text(encoding="utf-8-sig")
    elif isinstance(plan_source, str):
        raw = plan_source
    else:
        return {}, ""

    # Parse YAML front matter from text (same logic as before)
    if raw.lstrip().startswith("---"):
        parts = raw.split("\n", 2)
        blocks = raw.split("\n---\n", 1)
        if raw.startswith("---\n") and len(blocks) == 2:
            header_txt = blocks[0][4:]  # remove leading '---\n'
            body = blocks[1]
        else:
            fm = []
            lines = raw.splitlines(True)
            idx = 1
            while idx < len(lines) and not lines[idx].startswith("---"):
                fm.append(lines[idx])
                idx += 1
            if idx < len(lines) and lines[idx].startswith("---"):
                header_txt = "".join(fm)
                body = "".join(lines[idx+1:])
            else:
                return {}, raw
        try:
            header = yaml.safe_load(header_txt) or {}
        except Exception:
            header = {}
        return header, body

    return {}, raw


def _write_plan_yaml(plan_path: Path, header: Dict, body: str) -> None:
    """
    Write a documentation plan as a Markdown file with YAML front matter.

    - Ensures 'header' is a dict
    - Ensures presence of 'plan_version' and 'docs' keys
    - Writes a front matter block starting with '---' and ending with '---'
    - Writes the Markdown body after a blank line
    """
    # Normalize header to a dict
    if not isinstance(header, dict):
        header = {"plan_version": 1, "docs": []}
    header.setdefault("plan_version", 1)
    header.setdefault("docs", [])

    # Dump YAML and assemble the full Markdown text
    yml = yaml.safe_dump(header, sort_keys=False).strip()
    text = f"---\n{yml}\n---\n\n{body or ''}"
    # Ensure file ends with a newline (good practice)
    if not text.endswith("\n"):
        text += "\n"

    # Atomic write
    atomic_write_text(plan_path, text)

def _default_repo_plan() -> Dict:
    return {
        "plan_version": 1,
        "docs": [
            {
                "title": "Custom Application Properties Documentation",
                "filename": "custom-app-props.md",
                "inputs": [
                    "com.tibco.bw.custom.app.props.CustomAppProps.md",
                    "Family_com.tibco.bw.custom.app.props.md"
                ]
            },
            {
                "title": "Credit Application Service Documentation",
                "filename": "creditapp-service.md",
                "inputs": [
                    "creditapp.module.EquifaxScore.md",
                    "creditapp.module.ExperianScore.md",
                    "creditapp.module.MainProcess.md",
                    "Family_creditapp.module.md"
                ]
            },
            {
                "title": "Credit Check Backend Service Documentation",
                "filename": "creditcheck-backend.md",
                "inputs": [
                    "creditcheckservice.LookupDatabase.md",
                    "creditcheckservice.Process.md",
                    "Family_creditcheckservice.md"
                ]
            },
            {
                "title": "Experian Service Documentation",
                "filename": "experian-service.md",
                "inputs": [
                    "experianservice.module.Process.md",
                    "Family_experianservice.module.md"
                ]
            },
            {
                "title": "Logging Service Documentation",
                "filename": "logging-service.md",
                "inputs": [
                    "loggingservice.LogProcess.md",
                    "Family_loggingservice.md"
                ]
            },
            {
                "title": "Movie Catalog Search Service Documentation",
                "filename": "moviecatalogsearch-service.md",
                "inputs": [
                    "moviecatalogsearch.module.GetRatings.md",
                    "moviecatalogsearch.module.Process.md",
                    "moviecatalogsearch.module.SearchMovies.md",
                    "moviecatalogsearch.module.SortMovies.md",
                    "moviecatalogsearch.module.SortMovieSingle.md",
                    "moviecatalogsearch.module.SortSingleMovie.md",
                    "Family_moviecatalogsearch.module.md"
                ]
            },
            {
                "title": "Movie Search Service Documentation",
                "filename": "moviesearch-service.md",
                "inputs": [
                    "moviesearch.module.Process.md",
                    "moviesearch.module.SearchOmdb.md",
                    "Family_moviesearch.module.md"
                ]
            },
            {
                "title": "Execution Event Subscriber Documentation",
                "filename": "execution-event-subscriber.md",
                "inputs": [
                    "tibco.bw.sample.application.execution.event.subscriber.md",
                    "Family_tibco.bw.sample.application.execution.event.md"
                ]
            },
            {
                "title": "Unclassified / Utilities Documentation",
                "filename": "unclassified-utilities.md",
                "inputs": [
                    "TIBCO BW Custom DataSource Factory.md",
                    "Family_unclassified.md"
                ]
            }
        ],
        "meta": {"remaining_execs": 9}
    }



def sha256_file(p: Path) -> str:
    h = hashlib.sha256()
    with p.open("rb") as f:
        for chunk in iter(lambda: f.read(1024*1024), b""):
            h.update(chunk)
    return h.hexdigest()

# -----------------------
# Discovery (+ archives)
# -----------------------
def discover_process_files(root: Path) -> List[Path]:
    exts = (".bwp", ".process", ".xml")
    out: List[Path] = []
    for ext in exts:
        out.extend(root.rglob(f"*{ext}"))
    return out

def unpack_archives(root: Path) -> List[Path]:
    OUT_TMP.mkdir(parents=True, exist_ok=True)
    extracted_dirs: List[Path] = []
    for ext in (".ear", ".par", ".zip"):
        for arc in root.rglob(f"*{ext}"):
            try:
                if not zipfile.is_zipfile(arc):
                    continue
                hid = hashlib.sha256(str(arc.resolve()).encode("utf-8")).hexdigest()[:12]
                dest = OUT_TMP / f"{arc.stem}_{hid}"
                if not dest.exists():
                    with zipfile.ZipFile(arc, "r") as zf:
                        zf.extractall(dest)
                extracted_dirs.append(dest)
            except Exception:
                continue
    return extracted_dirs

# -----------------------
# Debug counters
# -----------------------
def compute_debug_counts(xml_root: ET._Element) -> Dict[str,int]:
    def count(xpath: str, ns=None) -> int:
        try: return len(xml_root.xpath(xpath, namespaces=ns))
        except: return 0
    return {
        "n_type_ns": count(".//*[@type and contains(@type,':')]"),
        "n_xmi_act": count(".//*[@xmi:type and contains(@xmi:type,'Activity')]", NS),
        "n_trans_nodes": count(".//*[@xmi:type and contains(@xmi:type,'Transition')]", NS) +
                         count(".//*[local-name()='transition']") +
                         count(".//*[contains(local-name(),'Transition')]"),
        "n_bpws_sequence": count(".//bpws:sequence", NS),
        "n_bpws_flow": count(".//bpws:flow", NS),
        "n_bpws_source": count(".//bpws:source", NS),
        "n_bpws_target": count(".//bpws:target", NS),
    }

# -----------------------
# Parse + SIR
# -----------------------
def find_start_activity(activities: List[Dict], transitions: List[Dict]):
    names = {a["name"] for a in activities}
    to_set = {t["to"] for t in transitions}
    cand = list(names - to_set)
    return cand[0] if cand else (activities[0]["name"] if activities else None)

def _get_tag_short(el: ET._Element) -> str:
    qname = ET.QName(el.tag); local = qname.localname; ns = qname.namespace
    prefix = None
    if hasattr(el, "nsmap") and el.nsmap:
        for k,v in el.nsmap.items():
            if v == ns: prefix = k; break
    return f"{prefix}:{local}" if prefix else local

def _descendants_excluding(root: ET._Element, exclude: List[Tuple[str,str]]) -> List[ET._Element]:
    excluded_nodes: List[ET._Element] = []
    for ns,local in exclude:
        try:
            excluded_nodes += root.xpath(f".//*[local-name()='{local}' and namespace-uri()='{ns}']")
        except: pass
    if not excluded_nodes:
        return list(root.xpath(".//*"))
    exset = set()
    for n in excluded_nodes:
        exset.add(n)
        for d in n.xpath(".//*"): exset.add(d)
    alln = list(root.xpath(".//*"))
    return [n for n in alln if n not in exset]

def extract_activities_and_transitions(xml_root: ET._Element) -> Tuple[List[Dict], List[Dict]]:
    activities: List[Dict] = []
    transitions: List[Dict] = []
    id_to_node: Dict[str, Dict[str,str]] = {}

    EXCL = [
        ("http://www.tibco.com/bpel/2007/extensions", "Types"),
        ("http://schemas.xmlsoap.org/wsdl/", "definitions"),
        ("http://www.w3.org/2001/XMLSchema", "schema"),
    ]
    body_nodes = _descendants_excluding(xml_root, EXCL)

    xmi_act_nodes = xml_root.xpath(".//*[@xmi:type and contains(@xmi:type,'Activity')]", namespaces=NS)
    type_ns_nodes = xml_root.xpath(".//*[@type and contains(@type,':')]")

    for el in set(list(xmi_act_nodes) + list(type_ns_nodes)):
        name = el.get("name") or el.get("displayName")
        typ = el.get("type") or el.get("Type") or el.get("{http://www.omg.org/XMI}type") or _get_tag_short(el)
        xid = el.get("{http://www.omg.org/XMI}id") or el.get("xmi:id")
        if name and typ: activities.append({"name": name, "type": typ})
        if xid: id_to_node[xid] = {"name": name, "type": typ}

    for el in body_nodes:
        if not isinstance(el.tag, str): continue
        qn = ET.QName(el.tag)
        if qn.namespace == BPWS_NS["bpws"] and qn.localname in {"variables","links","sources","targets"}:
            continue
        nm = el.get("name") or el.get("displayName")
        if nm:
            typ = el.get("type") or el.get("{http://www.omg.org/XMI}type") or _get_tag_short(el)
            activities.append({"name": nm, "type": typ})

    # dedupe by name
    ded = {}
    for a in activities:
        if a.get("name") and a["name"] not in ded: ded[a["name"]] = a
    activities = list(ded.values())

    # fill id_to_node names
    for xid,node in list(id_to_node.items()):
        if not node.get("name"):
            try: parent = xml_root.xpath(f"//*[@xmi:id='{xid}']", namespaces=NS)
            except: parent = []
            if parent:
                nm = parent[0].get("name") or parent[0].get("displayName")
                if nm: id_to_node[xid]["name"] = nm

    # transitions (various shapes)
    tn = []
    tn += xml_root.xpath(".//*[local-name()='transitions']/*[local-name()='transition']")
    tn += xml_root.xpath(".//*[local-name()='Transition']")
    tn += xml_root.xpath(".//*[@xmi:type and contains(@xmi:type,'Transition')]", namespaces=NS)
    tn = list({id(n): n for n in tn}.values())

    def resolve_endpoint(val: Optional[str]) -> Optional[str]:
        if not val: return None
        node = id_to_node.get(val)
        if node and node.get("name"): return node["name"]
        return val

    for t in tn:
        frm = t.get("from") or t.get("From"); to = t.get("to") or t.get("To")
        frm_ref = t.get("fromRef") or t.get("FromRef") or t.get("source") or t.get("Source")
        to_ref  = t.get("toRef") or t.get("ToRef") or t.get("target") or t.get("Target")
        f = resolve_endpoint(frm) or resolve_endpoint(frm_ref)
        tt = resolve_endpoint(to) or resolve_endpoint(to_ref)
        if f and tt: transitions.append({"from": f, "to": tt})

    # bpel sequence ordering
    for seq in xml_root.xpath(".//bpws:sequence", namespaces=NS):
        children = [c for c in list(seq) if isinstance(c.tag, str)]
        child_names: List[str] = []
        for idx, el in enumerate(children):
            qn = ET.QName(el.tag)
            if qn.namespace == BPWS_NS["bpws"] and qn.localname in {"links","sources","targets","variables"}:
                continue
            nm = el.get("name") or el.get("displayName") or f"{_get_tag_short(el)}-{idx}"
            if nm not in ded:
                activities.append({"name": nm, "type": _get_tag_short(el)})
                ded[nm] = {"name": nm, "type": _get_tag_short(el)}
            child_names.append(nm)
        for i in range(len(child_names)-1):
            transitions.append({"from": child_names[i], "to": child_names[i+1]})

    # bpel flow links (target<-source) for visualization
    for flow in xml_root.xpath(".//bpws:flow", namespaces=NS):
        links = [ln.get("name") for ln in flow.xpath(".//bpws:link", namespaces=NS) if ln.get("name")]
        for link in links:
            tgt_h = flow.xpath(f".//*[@name][.//bpws:target[@linkName='{link}']]", namespaces=NS)
            src_h = flow.xpath(f".//*[@name][.//bpws:source[@linkName='{link}']]", namespaces=NS)
            for tgt in tgt_h:
                tgt_name = tgt.get("name")
                if tgt_name and tgt_name not in ded:
                    activities.append({"name": tgt_name, "type": _get_tag_short(tgt)})
                    ded[tgt_name] = {"name": tgt_name, "type": _get_tag_short(tgt)}
                for src in src_h:
                    src_name = src.get("name")
                    if src_name and src_name not in ded:
                        activities.append({"name": src_name, "type": _get_tag_short(src)})
                        ded[src_name] = {"name": src_name, "type": _get_tag_short(src)}
                    if tgt_name and src_name:
                        transitions.append({"from": tgt_name, "to": src_name})

    # final dedupe
    uniq_t = {(t["from"], t["to"]) for t in transitions if t.get("from") and t.get("to")}
    transitions = [{"from": a, "to": b} for (a,b) in uniq_t]
    activities = list({a["name"]: a for a in activities if a.get("name") and a.get("type")}.values())
    return activities, transitions

def parse_process_to_sir(xml_path: Path) -> Tuple[Dict, ET._Element]:
    parser = ET.XMLParser(recover=True, remove_blank_text=True)
    root = ET.parse(str(xml_path), parser).getroot()
    tag = (root.tag.lower() if isinstance(root.tag, str) else "")
    source_format = "process_xml" if "process" in tag else "bwp_xml"
    proc_name = root.get("name") or root.get("Name") or xml_path.stem

    activities, transitions = extract_activities_and_transitions(root)
    sir = {
        "process_name": proc_name,
        "project_name": xml_path.parents[1].name if len(xml_path.parents) >= 2 else "",
        "source_file": str(xml_path.as_posix()),
        "source_file_format": source_format,
        "hash_sha256": sha256_file(xml_path),
        "start_activity": find_start_activity(activities, transitions),
        "activities": activities,
        "transitions": transitions,
        "resources": {},
        "metadata": {
            "extracted_at": datetime.datetime.now(datetime.timezone.utc).isoformat(),
            "tool_version": TOOL_VERSION,
            "notes": []
        }
    }
    return sir, root

# -----------------------
# Palette roles (BW6)
# -----------------------
def load_palette_roles(file_path: Path) -> Dict[str, List[str]]:
    try:
        if file_path.exists():
            data = json.loads(file_path.read_text(encoding="utf-8"))
            pr = (data or {}).get("prefix_roles", {})
            # normalize keys to lowercase
            return {k.lower(): v for k,v in pr.items()}
    except Exception:
        pass
    return DEFAULT_PREFIX_ROLES

# -----------------------
# Business scaffold (specifics)
# -----------------------
def name_family(process_name: str) -> Optional[str]:
    for pat in NAME_FAMILY_PATTERNS:
        m = re.match(pat, process_name)
        if m and m.group("fam"):
            return m.group("fam").lower()
    return "unclassified"



def _infer_roles_by_prefix(activity_type: str, palette_roles: Dict[str, List[str]]) -> Set[str]:
    roles: Set[str] = set()
    if not activity_type: return roles
    t = activity_type.lower()
    if ":" in t:
        prefix = t.split(":",1)[0]
        for r in palette_roles.get(prefix, []):
            roles.add(r)
    return roles

def _jdbc_targets(enrichment: Dict) -> List[str]:
    out = []
    for j in (enrichment or {}).get("jdbc_sql", []):
        ds = j.get("datasource") or ""
        tbl = j.get("table") or ""
        sql = (j.get("sql") or "")[:120].replace("\n"," ")
        label = f"{ds}:{tbl}" if ds or tbl else (sql if sql else "JDBC")
        out.append(label)
    # quick dedupe
    ded=[]; seen=set()
    for s in out:
        if s not in seen: ded.append(s); seen.add(s)
    return ded[:16]

def _scan_identifiers(enrichment: Dict) -> List[str]:
    out = set()
    for m in (enrichment or {}).get("mapper_hints", []):
        path = (m.get("path") or "") + " " + (m.get("name") or "")
        for tok in IDENTIFIER_TOKENS:
            if tok in path: out.add(tok)
    return sorted(list(out))[:16]

def _scan_http_endpoints(xml_root: ET._Element) -> List[Dict]:
    """
    Very lightweight scan for HTTP/REST binding hints:
    - Look for attributes that look like http method and path (GET|POST and /path)
    - Pull bpws:receive 'operation'/'partnerLink' if present.
    """
    hints = []
    # Generic method/path attribute probes
    for el in xml_root.xpath(".//*"):
        if not isinstance(el.tag, str): continue
        # collect attributes that look like method or path
        method = None; path = None
        for attr,val in el.attrib.items():
            v = str(val)
            if re.fullmatch(r"(GET|POST|PUT|DELETE|PATCH|OPTIONS|HEAD)", v, flags=re.I):
                method = v.upper()
            if v.startswith("/") or "{" in v or "/{" in v:
                # common REST-ish path
                path = v
        if method or path:
            hints.append({"kind":"REST","method":method or "Unknown","endpoint":path or "Unknown","evidence": el.tag})
    # bpws receive
    receives = xml_root.xpath(".//bpws:receive", namespaces=NS)
    for rcv in receives:
        op = rcv.get("operation") or "Unknown"
        pl = rcv.get("partnerLink") or ""
        hints.append({"kind":"Service","method":"Unknown","endpoint":op, "operation":op, "evidence":"bpws:receive"})
    # dedupe by (method,endpoint)
    seen=set(); out=[]
    for h in hints:
        key=(h.get("method"),h.get("endpoint"))
        if key not in seen:
            out.append(h); seen.add(key)
    return out[:8]

def build_business_scaffold(sir: Dict, xml_root: ET._Element, palette_roles: Dict[str,List[str]]) -> Dict:
    activities = sir.get("activities", [])
    transitions = sir.get("transitions", [])

    # Roles by prefix map
    role_sets: Dict[str, Set[str]] = {}
    for a in activities:
        t = a.get("type","")
        for r in _infer_roles_by_prefix(t, palette_roles):
            role_sets.setdefault(r, set()).add(a["name"])

    # Simple graph for reachability
    G = nx.DiGraph()
    for a in activities:
        G.add_node(a["name"], type=a.get("type",""))
    for t in transitions:
        if t.get("from") in G and t.get("to") in G:
            G.add_edge(t["from"], t["to"])
    start = sir.get("start_activity")
    reachable = set()
    if start and start in G:
        reachable.update(nx.descendants(G, start))
        reachable.add(start)

    # Interfaces (REST/HTTP best-effort)
    interfaces = _scan_http_endpoints(xml_root)

    # Invokes
    invocations = []
    if role_sets.get("invoke.process"):
        for n in sorted(role_sets["invoke.process"]):
            invocations.append({"kind":"Process","target":n,"operation":None,"resource":None,"evidence":n})
    if role_sets.get("data.jdbc"):
        invocations.append({"kind":"JDBC","target":"Unknown","operation":None,"resource":None,"evidence": list(role_sets["data.jdbc"])[0]})
    if role_sets.get("messaging.jms"):
        invocations.append({"kind":"JMS","target":"Unknown","operation":None,"resource":None,"evidence": list(role_sets["messaging.jms"])[0]})

    # IO summary (identifiers via mapper hints if present later)
    io_summary = {"inputs": [], "outputs": [], "identifiers": []}

    # Errors + logging
    errors=[]; logging=[]
    if role_sets.get("error.throw"):
        errors.append({"type":"ThrownError","mapped_to":"Unknown","evidence": list(role_sets["error.throw"])[0]})
    if role_sets.get("ops.log"):
        logging.append({"level":"Unknown","message_hint":"Log activity present","evidence": list(role_sets["ops.log"])[0]})

    # Traceability crumbs
    trace=[]
    if role_sets.get("interface.receive"):
        trace.append(f"interface.receive:{next(iter(role_sets['interface.receive']))}")
    if role_sets.get("interface.reply"):
        trace.append(f"interface.reply:{next(iter(role_sets['interface.reply']))}")
    if role_sets.get("invoke.process"):
        trace.append(f"invoke.process:{next(iter(role_sets['invoke.process']))}")
    if role_sets.get("data.jdbc"):
        trace.append(f"data.jdbc:{next(iter(role_sets['data.jdbc']))}")

    scaffold = {
        "interfaces": interfaces,
        "invocations": invocations,
        "dependencies": {
            "processes": [i["target"] for i in invocations if i["kind"]=="Process"],
            "services": ["JMS"] if role_sets.get("messaging.jms") else [],
            "datastores": []  # filled later by enricher
        },
        "io_summary": io_summary,
        "errors": errors,
        "logging": logging,
        "traceability": trace
    }
    return scaffold

# -----------------------
# Packaging (TIBCO.xml) — discover modules
# -----------------------
def read_packaging_descriptors(root: Path) -> Dict:
    out = {"modules":[]}
    for cand in root.rglob("TIBCO.xml"):
        try:
            xml = ET.parse(str(cand), parser=ET.XMLParser(recover=True)).getroot()
            sym = xml.xpath("//*[local-name()='module']/*[local-name()='symbolicName']/text()")
            props = xml.xpath("//*[local-name()='properties']/*[local-name()='property']")
            prop_map={}
            for p in props:
                name = p.xpath("./*[local-name()='name']/text()")
                typ  = p.xpath("./*[local-name()='type']/text()")
                if name: prop_map[name[0]] = (typ[0] if typ else "")
            if sym:
                out["modules"].append({"symbolicName": sym[0], "properties": prop_map, "file": str(cand.as_posix())})
        except Exception:
            continue
    return out

# -----------------------
# Interdependencies (project-wide)
# -----------------------
def build_interdependency_graph(all_sirs: List[Dict], packaging: Dict) -> Dict:
    node_index = {}
    fam_groups: Dict[str, List[str]] = {}
    name_to_sir = {s["process_name"]: s for s in all_sirs}

    # Build nodes
    for s in all_sirs:
        sc = (s.get("enrichment") or {}).get("business_scaffold", {})
        fam = name_family(s["process_name"]) or (s.get("project_name") or "").lower() or None
        if fam: fam_groups.setdefault(fam, []).append(s["process_name"])
        identifiers = sc.get("io_summary", {}).get("identifiers", []) or []
        jdbc = sc.get("dependencies", {}).get("datastores", []) or []
        node_index[s["process_name"]] = {
            "family": fam,
            "identifiers": set(identifiers),
            "jdbc_targets": set(jdbc),
            "module": None
        }

    # Attach modules via prefix match
    modules = packaging.get("modules", [])
    for m in modules:
        sym = (m.get("symbolicName") or "").lower()
        for pname,node in node_index.items():
            if sym and pname.lower().startswith(sym):
                node["module"] = sym

    edges = []

    # 1) calls
    for s in all_sirs:
        sc = (s.get("enrichment") or {}).get("business_scaffold", {})
        for inv in sc.get("invocations", []) or []:
            if inv.get("kind") == "Process":
                tgt = inv.get("target")
                if tgt and tgt in name_to_sir:
                    edges.append({"from": s["process_name"], "to": tgt, "kind": "calls"})

    # 2) shared identifiers / jdbc
    names = list(node_index.keys())
    for i in range(len(names)):
        for j in range(i+1, len(names)):
            a_name, b_name = names[i], names[j]
            a, b = node_index[a_name], node_index[b_name]
            if a["identifiers"] and b["identifiers"] and (a["identifiers"] & b["identifiers"]):
                edges.append({"from": a_name, "to": b_name, "kind": "shared_identifier"})
                edges.append({"from": b_name, "to": a_name, "kind": "shared_identifier"})
            if a["jdbc_targets"] and b["jdbc_targets"] and (a["jdbc_targets"] & b["jdbc_targets"]):
                edges.append({"from": a_name, "to": b_name, "kind": "shared_jdbc"})
                edges.append({"from": b_name, "to": a_name, "kind": "shared_jdbc"})

    # 3) family clustering edges
    for fam, procs in fam_groups.items():
        for i in range(len(procs)):
            for j in range(i+1, len(procs)):
                a_name, b_name = procs[i], procs[j]
                edges.append({"from": a_name, "to": b_name, "kind": "family"})
                edges.append({"from": b_name, "to": a_name, "kind": "family"})


    # Convert node_index to pure JSON types (sets -> sorted lists)
    node_index_json: Dict[str, Dict[str, object]] = {}
    for name, node in node_index.items():
        ids = node.get("identifiers") or set()
        jdb = node.get("jdbc_targets") or set()
        node_index_json[name] = {
            "family": node.get("family"),
            "identifiers": sorted(list(ids)) if isinstance(ids, set) else list(ids or []),
            "jdbc_targets": sorted(list(jdb)) if isinstance(jdb, set) else list(jdb or []),
            "module": node.get("module")
        }
    return {
        "nodes": node_index_json,
        "edges": edges,
        "groups": fam_groups,
        "modules": modules
    }

def slice_interdeps_for_process(interdeps: Dict, proc_name: str) -> Dict:
    calls, called_by, shared_ids, shared_jdbc, related = [],[],[],[],[]
    for e in interdeps.get("edges", []):
        if e["kind"]=="calls" and e["from"]==proc_name: calls.append(e["to"])
        if e["kind"]=="calls" and e["to"]==proc_name: called_by.append(e["from"])
        if e["kind"]=="shared_identifier" and e["from"]==proc_name: shared_ids.append(e["to"])
        if e["kind"]=="shared_jdbc" and e["from"]==proc_name: shared_jdbc.append(e["to"])
        if e["kind"]=="family" and e["from"]==proc_name: related.append(e["to"])
    def dd(lst):
        seen=set(); out=[]
        for x in lst:
            if x not in seen: out.append(x); seen.add(x)
        return out
    return {
        "related": dd(related), "calls": dd(calls), "called_by": dd(called_by),
        "shared_identifiers_with": dd(shared_ids), "shared_datastores_with": dd(shared_jdbc)
    }

# -----------------------
# Extrapolations (educated guesses; clearly marked)
# -----------------------
def extrapolate_context(sir: Dict, interdeps: Dict) -> List[Dict]:
    hyps=[]
    name = sir["process_name"].lower()
    fam = name_family(sir["process_name"]) or ""
    sl = slice_interdeps_for_process(interdeps, sir["process_name"])
    # Verb pipeline within family
    signals=0
    if "search" in name and any("get" in p.lower() for p in sl["related"]): signals+=1
    if any("sort" in p.lower() for p in sl["related"]): signals+=1
    if signals>=2:
        hyps.append({
            "hypothesis": "Family likely implements Search → Get → Sort pipeline for the same domain.",
            "rationale": "Peer names include verbs 'Search', 'Get', 'Sort' within same family.",
            "evidence_refs": sl["related"][:6], "hypothesis_score": 0.5
        })
    # Shell+delegate
    sc = (sir.get("enrichment") or {}).get("business_scaffold", {})
    if sc.get("interfaces") and any(inv.get("kind")=="Process" for inv in sc.get("invocations",[])):
        hyps.append({
            "hypothesis": "This process exposes an interface and delegates core logic to a helper subprocess.",
            "rationale": "Presence of interface + CallProcess on main path.",
            "evidence_refs": [inv.get("target") for inv in sc.get("invocations",[]) if inv.get("kind")=="Process"][:6],
            "hypothesis_score": 0.4
        })
    # Shared identifiers imply shared entity
    ids = sc.get("io_summary",{}).get("identifiers",[])
    if ids and sl["shared_identifiers_with"]:
        hyps.append({
            "hypothesis": f"Processes share domain key(s) ({', '.join(ids[:3])}); they likely touch the same entity lifecycle.",
            "rationale": "Shared identifier(s) detected across processes.",
            "evidence_refs": sl["shared_identifiers_with"][:6], "hypothesis_score": 0.35
        })
    return hyps

# -----------------------
# Validation + scoring
# -----------------------
def validate_sir(sir: Dict) -> Tuple[bool,str]:
    try:
        import jsonschema
        schema = json.loads(SCHEMA_PATH.read_text(encoding="utf-8"))
        jsonschema.validate(sir, schema)
        return True, "jsonschema: ok"
    except ModuleNotFoundError:
        # minimal check
        req = ["process_name","source_file","hash_sha256","activities","transitions","metadata"]
        for k in req:
            if k not in sir: return False, f"missing {k}"
        if len(str(sir.get("hash_sha256",""))) != 64: return False, "bad hash"
        return True, "minimal: ok"
    except Exception as e:
        return False, f"schema error: {e}"

def score_sir(sir: Dict, scaffold: Optional[Dict], interdep_slice: Optional[Dict], hyps: List[Dict]) -> Tuple[float, Dict[str,float], List[str]]:
    notes=[]
    base = 0.5 if sir.get("activities") else 0.0
    total = len(sir.get("activities",[]))
    known = sum(1 for a in sir.get("activities",[]) if a.get("type") in KNOWN_TYPES)
    coverage = (known/total) if total else 0.0
    if coverage<1.0 and total:
        notes.append(f"Unknown activity types: {total-known}/{total}.")
    names={a["name"] for a in sir.get("activities",[])}
    ok_trans = all(t["from"] in names and t["to"] in names for t in sir.get("transitions",[]))
    if not ok_trans:
        notes.append("One or more transitions reference missing activities.")
    G=nx.DiGraph(); G.add_nodes_from(names); G.add_edges_from([(t["from"],t["to"]) for t in sir.get("transitions",[]) if t["from"] in names and t["to"] in names])
    cyclic = not nx.is_directed_acyclic_graph(G) if G.number_of_nodes() else False
    if cyclic: notes.append("Cycle detected (may be intentional).")

    role_cov=0.0; signals=set()
    if scaffold:
        for key in ("interfaces","invocations"):
            for item in scaffold.get(key,[]):
                ev=item.get("evidence"); 
                if ev: signals.add(ev)
        for tr in scaffold.get("traceability",[]):
            if ":" in tr: signals.add(tr.split(":",1)[1])
        role_cov = min(1.0, len(signals)/(total or 1))

    claims=evid=0
    if scaffold:
        claims += len(scaffold.get("interfaces",[]))
        claims += len(scaffold.get("invocations",[]))
        claims += len(scaffold.get("dependencies",{}).get("datastores",[]))
        claims += len(scaffold.get("errors",[]))
        claims += len(scaffold.get("logging",[]))
        evid += sum(1 for i in scaffold.get("interfaces",[]) if i.get("evidence"))
        evid += sum(1 for i in scaffold.get("invocations",[]) if i.get("evidence"))
        evid += len(scaffold.get("dependencies",{}).get("datastores",[]))
        evid += len(scaffold.get("errors",[]))
        evid += len(scaffold.get("logging",[]))
    evidence_strength = (evid/claims) if claims else 0.0
    inferred_fraction = min(1.0, 0.25*len(hyps)) if hyps else 0.0

    score = min(1.0, base + 0.3*coverage + (0.2 if ok_trans else 0.0) + 0.1*role_cov + 0.1*evidence_strength - 0.05*inferred_fraction)
    subs = {
        "parsed": round(base,3), "known_types_coverage": round(coverage,3),
        "transition_integrity": 1.0 if ok_trans else 0.0,
        "role_coverage": round(role_cov,3),
        "evidence_strength": round(evidence_strength,3),
        "inferred_fraction": round(inferred_fraction,3)
    }
    return score, subs, notes

# -----------------------
# Graphviz (Windows-safe temp handling)
# -----------------------
def graphviz_svg(sir: Dict, svg_path: Path, dot_cmd: Optional[str] = None) -> None:
    """
    Windows-safe: ensure the .dot temp file handle is CLOSED before calling dot.exe,
    or Windows will report 'Permission denied' (file in use).
    """
    dot_lines = ["digraph G {", "rankdir=LR;", 'node [shape=box, style="rounded"];']
    start = sir.get("start_activity")
    for a in sir.get("activities", []):
        label = f'{a["name"]}\\n({a["type"]})'
        extra = 'color="green", penwidth=2' if a["name"] == start else ""
        dot_lines.append(f'"{a["name"]}" [label="{label}" {extra}];')
    for t in sir.get("transitions", []):
        dot_lines.append(f'"{t["from"]}" -> "{t["to"]}";')
    dot_lines.append("}")

    # Use mkstemp, CLOSE the fd, then write with Path to avoid open-handle conflicts on Windows.
    fd, tmp_name = tempfile.mkstemp(suffix=".dot")
    try:
        os.close(fd)  # critical on Windows
        tmp = Path(tmp_name)
        tmp.write_text("\n".join(dot_lines), encoding="utf-8")
        svg_path.parent.mkdir(parents=True, exist_ok=True)
        exe = dot_cmd or os.environ.get("DOT_PATH") or "dot"
        subprocess.run([exe, "-Tsvg", str(tmp), "-o", str(svg_path)], check=True)
    finally:
        try:
            Path(tmp_name).unlink(missing_ok=True)
        except Exception:
            pass

# -----------------------
# LLM (role-aware) + repo summaries (stub if no API key)
# -----------------------
ROLE_AWARE_PROMPT = """You are given a deterministic SIR plus enrichment (business_scaffold, interdependencies_slice, extrapolations).
Write descriptive, specific, business-facing sentences using the provided facts and context. Cite evidence strings inline.
Make it helpful, and avoid boilerplate phrasing. Mark educated guesses in a separate section.

Example Output:
{
  "process_name":"...creditcheckservice...",
  "one_line_summary":"...This process implements a REST service called “Credit Score” that accepts a request (such as a person’s SSN), looks up credit information, and returns the credit score data — logging successes and failures along the way....",
  "what_it_does":[ "...Exposes a REST endpoint (/creditscore) that accepts incoming requests (POST)....", "...When a request arrives, the process extracts the relevant input (for example, the SSN)....", "...It calls an internal lookup step (a separate “LookupDatabase” component) to retrieve credit information such as FICOScore, Rating, and number of inquiries....", "...It builds a response from the lookup results and sends that response back to the caller...", "...If something goes wrong, it logs the error and returns an appropriate fault/HTTP error response...."],
  "why_it_matters":[ "...The service lets other systems ask for a customer’s credit information and get back a structured result (score, rating, inquiries)....", "...It includes basic logging so operators can see when a request succeeded or failed and why...."],
  "interfaces":[{"kind":"REST|Service","endpoint":"...","method":"...","evidence":"..."}],
  "invokes":[{"kind":"Process|JDBC|JMS|Other","target":"...","evidence":"..."}],
  "key_inputs":[ "...SSN..."], "key_outputs":[ "...creditScore..."],
  "errors_and_logging":{"errors":[ "..."], "logging":[ "..."]},
  "nontechnical_notes":[ "...This process is part of the credit check workflow...."],
  "traceability":[ "..."],
  "interdependencies":{"related":["...creditcheckservice.Process..."],"calls":["...creditcheckservice.LookupDatabase..."],"called_by":[...],"shared_identifiers_with":[...],"shared_datastores_with":[...]},
  "extrapolations":[{"hypothesis":"...","rationale":"...","hypothesis_score":0.0}]
}

Output JSON:
{
  "process_name": "<from SIR.process_name>",
  "one_line_summary": "...",
  "what_it_does": ["... 4–7 grounded bullets ..."],
  "why_it_matters": ["... 2–3 business bullets ..."],
  "interfaces": [{"kind":"REST|SOAP|JMS|Timer|Other","endpoint":"...","method":"...","operation":"...","evidence":"..."}],
  "invokes": [{"kind":"Process|JDBC|SOAP|REST|JMS|Other","target":"...","operation":"...","evidence":"..."}],
  "key_inputs": ["..."],
  "key_outputs": ["..."],
  "errors_and_logging": {"errors": ["..."], "logging": ["..."]},
  "traceability": ["...evidence strings..."],
  "interdependencies": {
    "related": ["..."],
    "calls": ["..."],
    "called_by": ["..."],
    "shared_identifiers_with": ["..."],
    "shared_datastores_with": ["..."]
  },
  "extrapolations": [
    {"hypothesis":"...", "rationale":"...", "hypothesis_score":0.0}
  ]
}

"""

def explain_stub_from_scaffold(sir: Dict) -> Dict:
    sc = (sir.get("enrichment") or {}).get("business_scaffold", {}) or {}
    sl = (sir.get("enrichment") or {}).get("interdependencies_slice", {}) or {}
    ex = (sir.get("enrichment") or {}).get("extrapolations", []) or []

    name = sir["process_name"]
    one = compose_one_line_summary_from_scaffold(sir)

    what = []
    if sc.get("interfaces"):
        itf = sc["interfaces"][0]
        what.append(f"Receives a {itf.get('kind','Service')} request (endpoint: {itf.get('endpoint','Unknown')}, method: {itf.get('method','Unknown')}).")
    ids = sc.get("io_summary", {}).get("identifiers") or []
    if ids:
        what.append(f"Extracts identifiers ({', '.join(ids[:3])}).")
    for inv in sc.get("invocations", []):
        if inv["kind"] == "Process":
            what.append(f"Invokes subprocess {inv['target']}.")
        elif inv["kind"] == "JDBC":
            what.append("Queries datastore via JDBC.")
        elif inv["kind"] == "JMS":
            what.append("Interacts with JMS messaging.")
    if sc.get("io_summary", {}).get("outputs"):
        what.append("Returns mapped outputs.")
    if sc.get("errors"):
        what.append("Handles error paths and logs outcomes.")

    why = ["Enables downstream systems to obtain a specific, structured result.",
           "Provides traceability via explicit logging and error handling paths."]

    return {
        "process_name": name,
        "one_line_summary": one,
        "what_it_does": what[:7],
        "why_it_matters": why,
        "interfaces": sc.get("interfaces", []),
        "invokes": sc.get("invocations", []),
        "key_inputs": sc.get("io_summary", {}).get("inputs", []) or sc.get("io_summary", {}).get("identifiers", []),
        "key_outputs": sc.get("io_summary", {}).get("outputs", []),
        "errors_and_logging": {
            "errors": [e.get("type","Error") for e in sc.get("errors", [])],
            "logging": [l.get("message_hint","Log present") for l in sc.get("logging", [])]
        },
        "traceability": sc.get("traceability", []),
        "interdependencies": {
            "related": sl.get("related", []),
            "calls": sl.get("calls", []),
            "called_by": sl.get("called_by", []),
            "shared_identifiers_with": sl.get("shared_identifiers_with", []),
            "shared_datastores_with": sl.get("shared_datastores_with", [])
        },
        "extrapolations": [{"hypothesis": h["hypothesis"], "rationale": h["rationale"], "hypothesis_score": h.get("hypothesis_score", 0.3)} for h in ex][:5]
    }



def explain_llm_role_aware(sir: Dict, prompt_path: Path) -> Dict:
    prompt_txt=None
    if prompt_path and Path(prompt_path).exists():
        try: prompt_txt = Path(prompt_path).read_text(encoding="utf-8")
        except: prompt_txt=None
    if not prompt_txt: prompt_txt = ROLE_AWARE_PROMPT
    try:
        import openai  # type: ignore
        cfg = load_llm_config()
        client = openai.OpenAI()
        model = cfg.get("model", "gpt-5-chat-latest")
        resp = client.chat.completions.create(
            model=model,
            temperature=0,
            max_tokens=1400,
            messages=[
                {"role": "system", "content": "Write exceptionally concrete, business-friendly explanations."},
                {"role": "user", "content": prompt_txt},
                {"role": "user", "content": json.dumps(sir)}
            ],
        )
        content = resp.choices[0].message.content.strip()
        return json.loads(content)
    except Exception:
        return explain_stub_from_scaffold(sir)


# Rendering (process & family & repo overview)
# -----------------------
def render_markdown(sir: Dict, expl: Dict, score: float, subs: Dict[str,float], notes: List[str]) -> str:
    fm = {
        "interface_name": sir["process_name"],
        "process_file": sir["source_file"],
        "source_hash": sir["hash_sha256"],
        "extracted_at": sir["metadata"]["extracted_at"],
        "tool_version": sir["metadata"]["tool_version"],
        "explanation_source": "llm" if os.environ.get("OPENAI_API_KEY") else "stub",
        "documentation_status": "auto_generated",
        "review_status": "pending",
        "confidence_score": round(score,3),
        "confidence_subscores": subs,
        "detected_format": sir.get("source_file_format"),
        "start_activity": sir.get("start_activity"),
    }
    enr = sir.get("enrichment") or {}
    if enr:
        fm["enrichment_summary"] = {k: (len(v) if isinstance(v,list) else (len(v.keys()) if isinstance(v,dict) else 1)) for k,v in enr.items()}
    yaml_block = yaml.safe_dump(fm, sort_keys=False)
    lines = ["---", yaml_block.strip(), "---", ""]
    if expl.get("one_line_summary"):
        lines += [f"## {sir['process_name']} — One-line summary", expl["one_line_summary"].strip(), ""]
    if expl.get("what_it_does"):
        lines.append("## What it does (specific steps)")
        for b in expl["what_it_does"]: lines.append(f"- {b}")
        lines.append("")
    if expl.get("why_it_matters"):
        lines.append("## Why this matters (business view)")
        for b in expl["why_it_matters"]: lines.append(f"- {b}")
        lines.append("")
    if expl.get("interfaces"):
        lines.append("## Interfaces exposed")
        for itf in expl["interfaces"]:
            lines.append(f"- {itf.get('kind','Service')}: {itf.get('method','Unknown')} {itf.get('endpoint','Unknown')} (evidence: {itf.get('evidence','')})")
        lines.append("")
    if expl.get("invokes"):
        lines.append("## Invokes / Dependencies")
        for inv in expl["invokes"]:
            lines.append(f"- {inv.get('kind','Component')} → {inv.get('target','Unknown')} (evidence: {inv.get('evidence','')})")
        deps = (sir.get("enrichment") or {}).get("business_scaffold", {}).get("dependencies", {})
        if deps:
            d_line=[]
            if deps.get("processes"): d_line.append(f"processes: {', '.join(deps['processes'])}")
            if deps.get("services"): d_line.append(f"services: {', '.join(deps['services'])}")
            if deps.get("datastores"): d_line.append(f"datastores: {', '.join(deps['datastores'])}")
            if d_line: lines.append(f"_Dependencies_: {' | '.join(d_line)}")
        lines.append("")
    inter = expl.get("interdependencies") or {}
    if any(inter.get(k) for k in ("related","calls","called_by","shared_identifiers_with","shared_datastores_with")):
        lines.append("## Interdependency Map")
        if inter.get("related"): lines.append(f"- Related family processes: {', '.join(inter['related'])}")
        if inter.get("calls"): lines.append(f"- Calls: {', '.join(inter['calls'])}")
        if inter.get("called_by"): lines.append(f"- Called by: {', '.join(inter['called_by'])}")
        if inter.get("shared_identifiers_with"): lines.append(f"- Shares identifiers with: {', '.join(inter['shared_identifiers_with'])}")
        if inter.get("shared_datastores_with"): lines.append(f"- Shares datastores with: {', '.join(inter['shared_datastores_with'])}")
        lines.append("")
    ki = expl.get("key_inputs") or []; ko = expl.get("key_outputs") or []
    if ki or ko:
        lines.append("## Key inputs & outputs")
        if ki: lines.append(f"- Inputs: {', '.join(ki)}")
        if ko: lines.append(f"- Outputs: {', '.join(ko)}")
        lines.append("")
    eal = expl.get("errors_and_logging") or {}
    if eal.get("errors") or eal.get("logging"):
        lines.append("## Errors & Logging")
        if eal.get("errors"): lines.append(f"- Errors: {', '.join(eal['errors'])}")
        if eal.get("logging"): lines.append(f"- Logging: {', '.join(eal['logging'])}")
        lines.append("")
    if expl.get("extrapolations"):
        lines.append("## Extrapolations (educated guesses)")
        for h in expl["extrapolations"]:
            lines.append(f"- (Hypothesis {h.get('hypothesis_score',0.3):.2f}) {h.get('hypothesis')}")
            if h.get("rationale"): lines.append(f"  - Rationale: {h['rationale']}")
        lines.append("_These are clearly marked as hypotheses; treat as directional until reviewed._")
        lines.append("")
    # Technical appendix
    lines.append("## Technical appendix")
    lines.append(f"The process **{sir['process_name']}** consists of {len(sir.get('activities', []))} activities connected by transitions.")
    lines.append("")
    lines.append("### Activities")
    for a in sir.get("activities", []): lines.append(f"- {a['name']} ({a['type']})")
    lines.append("")
    # Related Documents block
    related_docs = []
    fam = (sir.get("enrichment") or {}).get("interdependencies_slice", {}).get("related", [])
    if fam:
        related_docs.extend(fam)
    calls = (sir.get("enrichment") or {}).get("interdependencies_slice", {}).get("calls", [])
    if calls:
        related_docs.extend(calls)
    if related_docs:
        lines.append("## Related Documents")
        for r in sorted(set(related_docs)):
            lines.append(f"- [{r}]({r}.md)")
        lines.append("")
    return "\n".join(lines)

def render_family_markdown(fam: str, members: List[str], interdeps: Dict, sirs_by_name: Dict[str,Dict]) -> str:
    title = f"Family {fam} — domain view"
    lines = [f"# {title}", ""]
    lines.append("## Members")
    for m in sorted(members):
        lines.append(f"- {m}")
    lines.append("")
    # Build concise facts
    endpoints=[]; ids=set(); stores=set(); calls=[]
    for m in members:
        s = sirs_by_name.get(m, {})
        sc = (s.get("enrichment") or {}).get("business_scaffold", {})
        for itf in sc.get("interfaces",[]):
            endpoints.append(f"{m}: {itf.get('method','Unknown')} {itf.get('endpoint','Unknown')}")
        for i in sc.get("io_summary",{}).get("identifiers",[]):
            ids.add(i)
        for d in sc.get("dependencies",{}).get("datastores",[]):
            stores.add(d)
    for e in interdeps.get("edges",[]):
        if e["kind"]=="calls" and e["from"] in members and e["to"] in members:
            calls.append(f'{e["from"]} → {e["to"]}')
    if endpoints:
        lines.append("## Exposed Endpoints (detected)")
        for ep in sorted(set(endpoints)): lines.append(f"- {ep}")
        lines.append("")
    if calls:
        lines.append("## Intra-family call flow (detected)")
        for c in sorted(set(calls)): lines.append(f"- {c}")
        lines.append("")
    if ids or stores:
        lines.append("## Shared data")
        if ids: lines.append(f"- Identifiers: {', '.join(sorted(ids))}")
        if stores: lines.append(f"- Datastores: {', '.join(sorted(stores))}")
        lines.append("")
    lines.append("_This is an automated domain-level synthesis; verify endpoints and flows where needed._")
    lines.append("")
    return "\n".join(lines)

def render_repo_overview(families: Dict[str,List[str]], interdeps: Dict, sirs_by_name: Dict[str,Dict]) -> str:
    lines = ["# Repository Overview", ""]
    lines.append(f"Generated at {datetime.datetime.now().isoformat()} (tool {TOOL_VERSION}).")
    lines.append("")
    lines.append("## Domain families")
    for fam, members in sorted(families.items(), key=lambda kv: (kv[0], len(kv[1]))):
        lines.append(f"- **{fam}**: {len(members)} process(es)")
    lines.append("")
    # Top endpoints across repo
    endpoints=[]
    for name,s in sirs_by_name.items():
        sc = (s.get("enrichment") or {}).get("business_scaffold", {})
        for itf in sc.get("interfaces",[]):
            endpoints.append(f'{name}: {itf.get("method","Unknown")} {itf.get("endpoint","Unknown")}')
    if endpoints:
        lines.append("## Exposed endpoints across repository (detected)")
        for ep in sorted(set(endpoints)): lines.append(f"- {ep}")
        lines.append("")
    # Cross-family coupling (calls)
    cross_calls=[]
    for e in interdeps.get("edges",[]):
        if e["kind"]=="calls":
            fa = (interdeps["nodes"][e["from"]]["family"] if interdeps["nodes"].get(e["from"]) else None)
            fb = (interdeps["nodes"][e["to"]]["family"] if interdeps["nodes"].get(e["to"]) else None)
            if fa and fb and fa!=fb:
                cross_calls.append(f'{e["from"]} ({fa}) → {e["to"]} ({fb})')
    if cross_calls:
        lines.append("## Cross-family service calls (detected)")
        for c in sorted(set(cross_calls)): lines.append(f"- {c}")
        lines.append("")
    return "\n".join(lines)


# ---- Front-matter helpers for plan handling (YAML at top of .md) ----
_FRONT_MATTER_RE = re.compile(r"^\ufeff?\s*---\s*\r?\n(?P<yml>.*?\r?\n)---\s*(?:\r?\n|$)", re.DOTALL)


def _parse_plan_front_matter(md_text: str):
    m = _FRONT_MATTER_RE.match(md_text)
    if not m:
        return {}, md_text
    yml = m.group("yml")
    body = md_text[m.end():]
    try:
        header = yaml.safe_load(yml) or {}
    except Exception:
        header = {}
    return header, body

def _write_plan_front_matter(path: Path, header: dict, body: str) -> None:
    """
    Same semantics as _write_plan_yaml, but kept as a separate helper to preserve
    downstream call sites that already use this name.
    """
    # Normalize header to a dict
    if not isinstance(header, dict):
        header = {"plan_version": 1, "docs": []}
    header.setdefault("plan_version", 1)
    header.setdefault("docs", [])

    yml = yaml.safe_dump(header, sort_keys=False).strip()
    text = f"---\n{yml}\n---\n\n{body or ''}"
    if not text.endswith("\n"):
        text += "\n"

    atomic_write_text(path, text)

def _rescue_docs_from_body(md_text: str):
    """
    Find a fenced ```markdown block containing YAML with a top-level 'docs:' list
    and return that list. Be liberal in what we accept.
    """
    code_blocks = re.findall(r"```(?:markdown|md)?\s+---\s+(.*?)\s+---\s*```", md_text, flags=re.DOTALL | re.IGNORECASE)
    for blk in code_blocks:
        try:
            data = yaml.safe_load(blk) or {}
            docs = data.get("docs")
            if isinstance(docs, list) and docs:
                return docs
        except Exception:
            continue
    return None

def _default_pb_plan(all_sirs: List[Dict]) -> Dict:
    # Build a PB-centric plan from discovered families and a few representative processes
    from collections import Counter, defaultdict
    fams = Counter()
    samples = defaultdict(list)
    for s in all_sirs:
        fam = name_family(s.get("process_name","")) or "unclassified"
        fams[fam] += 1
        if len(samples[fam]) < 6:
            samples[fam].append(s["process_name"] + ".md")

    top_fams = [f for f,_ in fams.most_common(6)]
    docs = []
    docs.append({
        "title": "PowerBuilder Executive Overview",
        "filename": "pb-executive-overview.md",
        "inputs": ["REPO_OVERVIEW.md"] + [f"Family_{f}.md" for f in top_fams]
    })
    for f in top_fams:
        docs.append({
            "title": f"PB Family — {f}",
            "filename": f"pb-family-{f}.md",
            "inputs": [f"Family_{f}.md"] + samples[f]
        })
    return {"plan_version": 1, "docs": docs, "meta": {"remaining_execs": 6}}

# ---------- end planning helpers ----------

def _read_text(path: Path) -> str:
    # 'utf-8-sig' strips a BOM if present (safe on Windows-created files)
    return path.read_text(encoding="utf-8-sig")

def _write_text(path: Path, text: str) -> None:
    atomic_write_text(path, text)


def _render_front_matter(header: Dict, body: str) -> str:
    yml = yaml.safe_dump(header, sort_keys=False).strip()
    return f"---\n{yml}\n---\n\n{body}"

def _normalize_front_matter(text: str) -> str:
    """
    Normalize LLM output into valid YAML front matter + Markdown body.
    Always returns a string starting with a valid YAML block.
    """
    import json as _json
    header = {"plan_version": 1, "docs": []}
    body = text

    # Try JSON
    try:
        if text.strip().startswith("{") or text.strip().startswith("["):
            data = _json.loads(text)
            if isinstance(data, dict):
                header = data
                body = ""
            else:
                header = {"plan_version": 1, "docs": data}
                body = ""
    except Exception:
        # Try YAML
        try:
            data = yaml.safe_load(text)
            if isinstance(data, dict):
                header = data
                body = ""
        except Exception:
            # leave fallback
            pass

    # Ensure header is dict
    if not isinstance(header, dict):
        header = {"plan_version": 1, "docs": []}

    return _render_front_matter(header, body)

def _load_config_model() -> Optional[str]:
    """
    Honor config.yaml or .roo/config.yaml if present; else OPENAI_MODEL; else default.
    """
    cfg_paths = [
        REPO_ROOT / "config.yaml",
        REPO_ROOT / ".roo" / "config.yaml",
    ]
    for p in cfg_paths:
        if p.exists():
            try:
                cfg = yaml.safe_load(p.read_text(encoding="utf-8-sig")) or {}
                m = (cfg.get("model") or cfg.get("openai", {}).get("model"))
                if m:
                    return str(m)
            except Exception:
                pass
    return os.environ.get("OPENAI_MODEL")

def _collect_doc_corpus(docs_out: Path) -> Tuple[str, List[str]]:
    # unchanged if you already had a function with this name
    buf = []
    files = []
    for p in sorted(docs_out.glob("*.md")):
        try:
            t = _read_text(p)
            files.append(p.name)
            # keep it reasonably sized
            buf.append(f"\n\n# FILE: {p.name}\n\n")
            buf.append(t[:20000])
        except Exception:
            continue
    return "".join(buf), files

def _llm_make_repo_plan(corpus: str, visible_files: List[str], out_dir: Path) -> str:
    """
    As before, but use model from config if available.
    """
    plan_path = out_dir / "dox_draft_plan.md"
    try:
        import openai
        client = openai.OpenAI()
        model = _load_config_model() or "gpt-5-chat-latest"
        prompt = (
            "You are a senior tech writer. Read the corpus and propose a YAML front-matter "
            "plan with key 'docs' (list of deliverables). Each item: {title, filename, inputs[list]}. "
            "Keep the rest of the file as a Markdown body explaining objectives. "
            "Return the document with a YAML front matter block delimited by '---' lines."
        )
        resp = client.chat.completions.create(
            model=model,
            temperature=0,
            max_tokens=1800,
            messages=[
                {"role": "system", "content": prompt},
                {"role": "user", "content": f"Visible files: {visible_files[:60]}\n\nCorpus:\n{corpus[:45000]}"},
            ],
        )
        text = resp.choices[0].message.content.strip()
        # Ensure front matter exists
        if not _FRONT_MATTER_RE.match(text):
            # wrap as front matter if model forgot
            header = {"plan_version": 1, "docs": []}
            text = _render_front_matter(header, text)
        _write_text(plan_path, text)
        return text
    except Exception as e:
        # Fallback minimal plan
        minimal = _render_front_matter(
            {"plan_version": 1, "docs": []},
            "Plan generation failed; please re-run. Error: {}".format(e),
        )
        _write_text(plan_path, minimal)
        return minimal



# ---- Better one-line summaries (business-forward, specific) ----
def _nl_join(items):
    return ", ".join(items[:3]) + ("…" if len(items) > 3 else "")

def compose_one_line_summary_from_scaffold(sir: Dict) -> str:
    name = sir.get("process_name") or "This process"
    sc = (sir.get("enrichment") or {}).get("business_scaffold", {}) or {}
    interfaces = sc.get("interfaces") or []
    inv = sc.get("invocations") or []
    deps = sc.get("dependencies") or {}
    io = sc.get("io_summary") or {}
    ids = io.get("identifiers") or []
    inputs = io.get("inputs") or []
    outputs = io.get("outputs") or []
    has_log = bool(sc.get("logging"))
    has_err = bool(sc.get("errors"))

    # pick first, most representative interface
    if interfaces:
        itf = interfaces[0]
        kind = (itf.get("kind") or "Service").upper()
        ep = itf.get("endpoint") or "Unknown endpoint"
        method = itf.get("method") or "Unknown method"
        who = f'{kind} service "{name}"'
        # subject matter (what it does) from invocations + datastores
        acts = []
        if any(x.get("kind") == "JDBC" for x in inv) or deps.get("datastores"):
            acts.append("queries enterprise data stores")
        if any(x.get("kind") == "JMS" for x in inv) or "JMS" in (deps.get("services") or []):
            acts.append("publishes/consumes messages")
        subprocs = [x.get("target") for x in inv if x.get("kind") == "Process" and x.get("target")]
        if subprocs:
            acts.append(f"delegates to subprocesses ({_nl_join(subprocs)})")
        subject = "; ".join(acts) if acts else "performs domain logic"

        # inputs / outputs hints
        in_hint = _nl_join(inputs or ids) if (inputs or ids) else None
        out_hint = _nl_join(outputs) if outputs else None
        io_clause = []
        if in_hint:
            io_clause.append(f'given inputs such as {in_hint}')
        if out_hint:
            io_clause.append(f'returns {out_hint}')
        io_txt = ", ".join(io_clause) if io_clause else "returns a structured response"

        # ops hints
        ops = []
        if has_log:
            ops.append("logging")
        if has_err:
            ops.append("error handling")
        ops_txt = f", with {' and '.join(ops)}" if ops else ""

        return f'This process implements a {who} at {method} {ep} that {subject} {io_txt}{ops_txt}.'
    else:
        # No explicit interface → fall back to data/flows
        acts = []
        if any(x.get("kind") == "JDBC" for x in inv) or deps.get("datastores"):
            acts.append("reads/writes via JDBC")
        if any(x.get("kind") == "JMS" for x in inv) or "JMS" in (deps.get("services") or []):
            acts.append("exchanges messages (JMS)")
        subprocs = [x.get("target") for x in inv if x.get("kind") == "Process" and x.get("target")]
        if subprocs:
            acts.append(f"coordinates subprocesses ({_nl_join(subprocs)})")
        subject = "; ".join(acts) if acts else "performs domain logic"
        return f'{name} {subject} and returns a structured response.'



# -----------------------
# CLI
# -----------------------
@click.group()
def cli():

        """PB Auto-Documentation Orchestrator (repo-aware)"""

@cli.command("run-pb")
@click.option("--env-file", type=click.Path(path_type=Path), default=DEFAULT_ENV_FILE)
@click.option("--root", type=click.Path(path_type=Path), required=True)
@click.option("--sir-out", type=click.Path(path_type=Path), default=OUT_SIR)
@click.option("--graphs-out", type=click.Path(path_type=Path), default=OUT_GRAPHS)
@click.option("--docs-out", type=click.Path(path_type=Path), default=OUT_DOCS)
@click.option("--skip-graphs", is_flag=True)
@click.option("--skip-llm", is_flag=True)
@click.option("--fail-fast", is_flag=True)
@click.option("--dot-path", type=str, default="")
def run_pb(env_file, root, sir_out, graphs_out, docs_out, skip_graphs, skip_llm, fail_fast, dot_path):
    """PowerBuilder: discover → extract → validate → enrich → interdeps → explain → render."""
    load_env_file(Path(env_file), override=False)

    # import PB modules
    sys.path.append(str((Path(__file__).resolve().parent)))
    from pb_extract import build_sirs  # type: ignore
    try:
        from pb_enrich import enrich_project_artifacts_pb, enrich_sir_pb  # type: ignore
    except Exception:
        enrich_project_artifacts_pb = None
        enrich_sir_pb = None

    root = Path(root)
    all_sirs = []
    # Project-level enrichment
    project_enrichment = {}
    if enrich_project_artifacts_pb:
        try:
            project_enrichment = enrich_project_artifacts_pb(root) or {}
            if project_enrichment:
                OUT_SIR.mkdir(parents=True, exist_ok=True)
                atomic_write_text(OUT_SIR / "_project_enrichment_pb.json", json.dumps(project_enrichment, indent=2))
        except Exception as e:
            print(f"[WARN] PB project enrichment failed: {e}")
            if fail_fast: sys.exit(4)

    # Extract PB SIRs
    sirs = build_sirs(root)
    for s in sirs:
        ok, msg = validate_sir(s)
        if not ok:
            print(f"[SKIP] PB SIR invalid: {s.get('process_name')} ({msg})")
            continue
        # Per-SIR enrichment
        if enrich_sir_pb:
            try:
                s.setdefault("enrichment", {}).update(enrich_sir_pb(s))
            except Exception as e:
                print(f"[WARN] PB per-SIR enrich failed for {s.get('process_name')}: {e}")
        OUT_SIR.mkdir(parents=True, exist_ok=True)
        atomic_write_text(sir_out / (s["process_name"] + ".json"), json.dumps(s, indent=2))
        # Graph
        try:
            if not skip_graphs:
                graphviz_svg(s, graphs_out / (s["process_name"] + ".svg"), dot_cmd=dot_path or None)
        except Exception as ge:
            print(f"[WARN] Graphviz failed (PB): {s['process_name']}: {ge}")
        all_sirs.append(s)

    # Interdependencies + explain/render + family/repo docs (reuse existing)
    interdeps = build_interdependency_graph(all_sirs, packaging={"modules":[]})
    atomic_write_text(OUT_SIR / "_interdeps.json", json.dumps(interdeps, indent=2))

    for s in all_sirs:
        s.setdefault("enrichment", {})["interdependencies_slice"] = slice_interdeps_for_process(interdeps, s["process_name"])
        hyps = extrapolate_context(s, interdeps)
        s["enrichment"]["extrapolations"] = hyps
        use_llm = (not skip_llm) and bool(os.environ.get("OPENAI_API_KEY"))
        expl = explain_llm_role_aware(s, PROMPT_DEFAULT) if use_llm else explain_stub_from_scaffold(s)
        score, subs, notes = score_sir(s, s["enrichment"].get("business_scaffold"), s["enrichment"].get("interdependencies_slice"), hyps)
        md = render_markdown(s, expl, score, subs, notes)
        OUT_DOCS.mkdir(parents=True, exist_ok=True)
        atomic_write_text(OUT_DOCS / (s["process_name"] + ".md"), md)

    fams = interdeps.get("groups", {})
    name_to_sir = {s["process_name"]: s for s in all_sirs}
    for fam, members in fams.items():
        doc = render_family_markdown(fam, members, interdeps, name_to_sir)
        atomic_write_text(OUT_DOCS / (f"Family_{fam}.md"), doc)
    overview = render_repo_overview(fams, interdeps, name_to_sir)
    atomic_write_text(OUT_DOCS / "REPO_OVERVIEW.md", overview)

        # === Repo-wide planning and fulfillment (iterative until done) ===
    try:
        if not skip_llm and os.environ.get("OPENAI_API_KEY"):
            out_dir = OUT_DOCS.parent  # .../out
            corpus, visible_files = _collect_doc_corpus(docs_out)
            click.echo("[plan] Building dox_draft_plan.md from repo docs...")
            plan_text = _llm_make_repo_plan(corpus, visible_files, out_dir)

            plan_path = out_dir / "dox_draft_plan.md"

            # 1) Try reading as proper front matter
            try:
                header, body = _read_plan_yaml(plan_path)
            except Exception:
                header, body = {}, ""

            # 2) If still empty, try parsing raw text front matter
            if not header or not header.get("docs"):
                raw = plan_path.read_text(encoding="utf-8-sig")
                try:
                    header2, body2 = _parse_plan_front_matter(raw)
                except Exception:
                    header2, body2 = {}, ""
                if header2 and header2.get("docs"):
                    header, body = header2, body2

            # 3) If still no docs, try rescuing from a fenced block in the body
            if not header or not header.get("docs"):
                raw = plan_path.read_text(encoding="utf-8-sig")
                rescued = _rescue_docs_from_body(raw)
                if rescued:
                    header = {"plan_version": 1, "docs": rescued, "meta": {"remaining_execs": 9}}
                    body = ""

            # 4) Final fallback: use the default plan
            if not header or not header.get("docs"):
                header = _default_repo_plan(all_sirs)
                body = ""

            # Persist normalized plan
            _write_plan_front_matter(plan_path, header, body)

            docs_spec = header.get("docs") or []
            docs_per_round = int(os.environ.get("DOCS_PER_ROUND", "3"))
            pending = [d for d in docs_spec if d and d.get("status") != "done"]
            needed = max(1, (len(pending) + docs_per_round - 1) // docs_per_round)

            meta = header.setdefault("meta", {})
            if not isinstance(meta.get("remaining_execs"), int) or meta["remaining_execs"] < needed:
                meta["remaining_execs"] = needed
                _write_plan_front_matter(plan_path, header, body)

            runner = REPO_ROOT / ".roo" / "tools" / "bw" / "dox_follow_plan.py"
            if not runner.exists():
                click.echo(f"[plan][WARN] Runner not found: {runner}")
            else:
                while True:
                    raw = plan_path.read_text(encoding="utf-8-sig")
                    header, body = _read_plan_yaml(plan_path)

                    docs_spec = header.get("docs") or []
                    meta = header.get("meta") or {}
                    rem_execs = int(meta.get("remaining_execs", 0))
                    remaining = [d for d in docs_spec if d and d.get("status") != "done"
                                 and not (docs_out / d.get("filename", "")).exists()]
                    if not remaining or rem_execs <= 0:
                        break

                    click.echo(f"[plan] Fulfilling docs (left={len(remaining)}, remaining_execs={rem_execs})...")
                    subprocess.run(
                        [sys.executable, str(runner), "--plan", str(plan_path),
                         "--out", str(docs_out), "--docs-per-round", str(docs_per_round)],
                        check=False
                    )
        else:
            click.echo("[plan] Skipped (no LLM or --skip-llm).")
    except Exception as e:
        click.echo(f"[plan][WARN] Planning/fulfillment step failed: {e}")


    # Optional plan/rollup/mkdocs reuse (same as BW run)
    try:
        subprocess.run([sys.executable, str(REPO_ROOT / ".roo" / "tools" / "bw" / "docs_reorganize.py")], check=False)
    except Exception as e:
        print(f"[reorg][WARN] {e}")
    try:
        rollup = REPO_ROOT / ".roo" / "tools" / "bw" / "rollup_confidence.py"
        if rollup.exists():
            subprocess.run([sys.executable, str(rollup), "--docs", str(OUT_DOCS),
                            "--sir", str(REPO_ROOT / "out" / "sir" / "_interdeps.json"),
                            "--plan", str(REPO_ROOT / "out" / "dox_draft_plan.md")], check=False)
    except Exception as e:
        print(f"[rollup][WARN] {e}")
    try:
        boot = REPO_ROOT / ".roo" / "tools" / "bw" / "mkdocs_bootstrap.py"
        if boot.exists():
            subprocess.run([sys.executable, str(boot), "--force-config", "--force-home"], check=False)
    except Exception as e:
        print(f"[mkdocs][WARN] {e}")
    print(f"[PB] Pipeline complete. Processes documented: {len(all_sirs)}. Families: {len(fams)}")





    """BW Auto-Documentation Orchestrator (repo-aware)"""

@cli.command()
@click.option("--env-file", type=click.Path(path_type=Path), default=DEFAULT_ENV_FILE, help="Path to .env file.")
@click.option("--root", type=click.Path(path_type=Path), required=True, help="Input root with BW artifacts.")
@click.option("--sir-out", type=click.Path(path_type=Path), default=OUT_SIR)
@click.option("--graphs-out", type=click.Path(path_type=Path), default=OUT_GRAPHS)
@click.option("--docs-out", type=click.Path(path_type=Path), default=OUT_DOCS)
@click.option("--prompt", type=click.Path(path_type=Path), default=PROMPT_DEFAULT)
@click.option("--pattern", type=str, default="", help="Substring filter for file paths.")
@click.option("--max", "max_count", type=int, default=0, help="Max processes to handle (0 = all).")
@click.option("--skip-graphs", is_flag=True, help="Skip Graphviz rendering.")
@click.option("--skip-llm", is_flag=True, help="Skip LLM (use stub explanations).")
@click.option("--fail-fast", is_flag=True, help="Stop on first failure.")
@click.option("--dot-path", type=str, default="", help="Explicit path to dot.exe (Graphviz).")
@click.option("--no-project-enrich", is_flag=True, help="Disable project-level enrichment scan.")
@click.option("--include-archives", is_flag=True, help="Unpack and scan .ear/.par/.zip archives under --root.")
@click.option("--debug-discovery", is_flag=True, help="Print discovery counters.")
def run(env_file, root, sir_out, graphs_out, docs_out, prompt, pattern, max_count, skip_graphs, skip_llm, fail_fast, dot_path, no_project_enrich, include_archives, debug_discovery):
    """Run: env → discover → extract → validate → enrich → scaffold → interdeps → explain → render (process/family/repo)."""
    load_env_file(Path(env_file), override=False)

    # Env check (best-effort)
    try:
        if CHECK_ENV.exists():
            cmd = [sys.executable, str(CHECK_ENV), "--env-file", str(env_file)]
            subprocess.run(cmd, check=bool(fail_fast))
    except Exception as e:
        print(f"[WARN] Env check warned/failed: {e}")
        if fail_fast: sys.exit(2)

    roots=[Path(root)]
    if include_archives:
        try:
            extracted = unpack_archives(Path(root))
            roots.extend(extracted)
            print(f"Archive extraction: {len(extracted)} extracted directories under {OUT_TMP}")
        except Exception as e:
            print(f"[WARN] Archive unpack failed: {e}")
            if fail_fast: sys.exit(3)

    # Discover
    candidates=[]
    for r in roots:
        if not r.exists():
            if debug_discovery: print(f"[DEBUG] Root does not exist: {r}")
            continue
        found = discover_process_files(r)
        candidates.extend(found)
        if debug_discovery:
            c_xml=len([p for p in found if p.suffix.lower()==".xml"])
            c_bwp=len([p for p in found if p.suffix.lower()==".bwp"])
            c_proc=len([p for p in found if p.suffix.lower()==".process"])
            print(f"[DEBUG] Root {r} → found {len(found)} files (xml={c_xml}, bwp={c_bwp}, process={c_proc})")

    # De-duplicate & filter
    seen=set(); files=[]
    for p in candidates:
        key = str(p.resolve())
        if key not in seen:
            seen.add(key); files.append(p)
    if pattern: files = [f for f in files if pattern in str(f)]
    if max_count and max_count>0: files = files[:max_count]
    print(f"Discovered {len(files)} files with matching extensions under {root} (incl. archives={include_archives}).")

    # Project enrichment (optional external scan hook)
    project_enrichment={}
    if not no_project_enrich:
        try:
            # Optional hook: no-op if bw_enrich not provided in your repo
            sys.path.append(str((Path(__file__).resolve().parent)))
            try:
                from bw_enrich import enrich_project_artifacts, enrich_process  # type: ignore
            except Exception:
                enrich_project_artifacts = None
                enrich_process = None
            if enrich_project_artifacts:
                project_enrichment = enrich_project_artifacts(Path(root)) or {}
                if project_enrichment:
                    OUT_SIR.mkdir(parents=True, exist_ok=True)
                    atomic_write_text(OUT_SIR / "_project_enrichment.json", json.dumps(project_enrichment, indent=2))
        except Exception as e:
            print(f"[WARN] Project enrichment failed: {e}")
            if fail_fast: sys.exit(4)
    else:
        enrich_process = None  # type: ignore

    # Packaging descriptors
    packaging = read_packaging_descriptors(Path(root))
    if packaging.get("modules"):
        OUT_SIR.mkdir(parents=True, exist_ok=True)
        atomic_write_text(OUT_SIR / "_packaging.json", json.dumps(packaging, indent=2))

    # load palette roles
    palette_roles = load_palette_roles(PALETTE_MAP_FILE)

    # Process loop
    processed=0
    all_sirs=[]
    pbar = tqdm(files, desc="Parse/Enrich")
    for f in pbar:
        try:
            # Parse XML → SIR
            try:
                sir, xml_root = parse_process_to_sir(f)
            except Exception as pe:
                pbar.write(f"[SKIP] Not XML or unreadable: {f} ({pe})")
                continue

            # Debug counters
            if debug_discovery:
                dbg = compute_debug_counts(xml_root)
                pbar.write("[DEBUG] Heuristics for {} => type_ns={} | xmi_act={} | trans_nodes={} | bpws_sequence={} | bpws_flow={} | bpws_source={} | bpws_target={}".format(
                    f, dbg["n_type_ns"], dbg["n_xmi_act"], dbg["n_trans_nodes"], dbg["n_bpws_sequence"], dbg["n_bpws_flow"], dbg["n_bpws_source"], dbg["n_bpws_target"]
                ))

            if not sir.get("activities") and not sir.get("transitions"):
                pbar.write(f"[SKIP] No BW activities/transitions detected: {f}")
                continue

            # Validate
            ok,msg = validate_sir(sir)
            if not ok:
                pbar.write(f"[SKIP] Invalid SIR for {f.name}: {msg}")
                continue

            # Build scaffold (specific)
            scaffold = build_business_scaffold(sir, xml_root, palette_roles)
            sir.setdefault("enrichment", {})["business_scaffold"] = scaffold

            # Add identifiers & JDBC from optional enricher, if present
            if 'enrich_process' in globals() and globals()['enrich_process']:
                try:
                    enr_proc = enrich_process(xml_root, f)  # type: ignore
                except Exception as ee:
                    enr_proc = {}
                    pbar.write(f"[WARN] Enrichment failed for {f.name}: {ee}")
                if enr_proc:
                    sir["enrichment"].update(enr_proc)
                    # fold jdbc/sql targets & identifiers
                    jdbc_targets = _jdbc_targets(sir["enrichment"])
                    if jdbc_targets:
                        sir["enrichment"]["business_scaffold"]["dependencies"]["datastores"] = jdbc_targets
                    ids = _scan_identifiers(sir["enrichment"])
                    if ids:
                        sir["enrichment"]["business_scaffold"]["io_summary"]["identifiers"] = ids

            # Persist SIR
            OUT_SIR.mkdir(parents=True, exist_ok=True)
            atomic_write_text(sir_out / (sir["process_name"] + ".json"), json.dumps(sir, indent=2))

            # Graph SVG (optional)
            svg_path = graphs_out / (sir["process_name"] + ".svg")
            try:
                graphviz_svg(sir, svg_path, dot_cmd=dot_path or None)
            except Exception as ge:
                pbar.write(f"[WARN] Graphviz failed for {f.name}: {ge}")
            # Skipping Graphviz SVG generation for low-level docs (only final docs get diagrams)
                svg_path = graphs_out / (sir["process_name"] + ".svg")
                try:
                    graphviz_svg(sir, svg_path, dot_cmd=dot_path or None)
                except Exception as ge:
                    pbar.write(f"[WARN] Graphviz failed for {f.name}: {ge}")

            all_sirs.append(sir)
            processed += 1

        except Exception as e:
            pbar.write(f"[ERROR] {f}: {e}")
            if fail_fast: sys.exit(1)

    # Interdependencies over all SIRs
    interdeps = build_interdependency_graph(all_sirs, packaging)
    atomic_write_text(OUT_SIR / "_interdeps.json", json.dumps(interdeps, indent=2))

    # Add interdep slice + extrapolations + scoring + render per-process
    name_to_sir = {s["process_name"]: s for s in all_sirs}
    for s in tqdm(all_sirs, desc="Explain/Render"):
        s.setdefault("enrichment", {})["interdependencies_slice"] = slice_interdeps_for_process(interdeps, s["process_name"])
        hyps = extrapolate_context(s, interdeps)
        s["enrichment"]["extrapolations"] = hyps
        # choose LLM or stub
        use_llm = (not skip_llm) and bool(os.environ.get("OPENAI_API_KEY"))
        expl = explain_llm_role_aware(s, prompt) if use_llm else explain_stub_from_scaffold(s)
        score, subs, notes = score_sir(s, s["enrichment"].get("business_scaffold"), s["enrichment"].get("interdependencies_slice"), hyps)
        md = render_markdown(s, expl, score, subs, notes)
        OUT_DOCS.mkdir(parents=True, exist_ok=True)
        atomic_write_text(OUT_DOCS / (s["process_name"] + ".md"), md)

    # Family docs & repo overview
    fams = interdeps.get("groups", {})
    for fam, members in fams.items():
        doc = render_family_markdown(fam, members, interdeps, name_to_sir)
        atomic_write_text(OUT_DOCS / (f"Family_{fam}.md"), doc)
    overview = render_repo_overview(fams, interdeps, name_to_sir)
    atomic_write_text(OUT_DOCS / "REPO_OVERVIEW.md", overview)

    # Index
    try:
        index = [{"doc": p.name} for p in docs_out.glob("*.md")]
        atomic_write_text(docs_out / "_index.json", json.dumps(index, indent=2))
    except Exception as e:
        click.echo(f"[WARN] Could not write docs index: {e}")
    
    # === Repo-wide planning and fulfillment (iterative until done) ===
    try:
        if not skip_llm and os.environ.get("OPENAI_API_KEY"):
            out_dir = OUT_DOCS.parent  # .../out
            corpus, visible_files = _collect_doc_corpus(docs_out)
            click.echo("[plan] Building dox_draft_plan.md from repo docs...")
            plan_text = _llm_make_repo_plan(corpus, visible_files, out_dir)

            plan_path = out_dir / "dox_draft_plan.md"
            
            # 1) Try reading as proper front matter
            try:
                header, body = _read_plan_yaml(plan_path)
            except Exception:
                header, body = {}, ""
            
            # 2) If still empty, try parsing raw text front matter
            if not header or not header.get("docs"):
                raw = plan_path.read_text(encoding="utf-8-sig")
                try:
                    header2, body2 = _parse_plan_front_matter(raw)
                except Exception:
                    header2, body2 = {}, ""
                if header2 and header2.get("docs"):
                    header, body = header2, body2
            
            # 3) If still no docs, try rescuing from a fenced block in the body
            if not header or not header.get("docs"):
                raw = plan_path.read_text(encoding="utf-8-sig")
                rescued = _rescue_docs_from_body(raw)
                if rescued:
                    header = {"plan_version": 1, "docs": rescued, "meta": {"remaining_execs": 9}}
                    body = ""
            
            # 4) Final fallback: use the default plan
            if not header or not header.get("docs"):
                header = _default_repo_plan()
                body = ""
            
            # Persist the normalized plan so downstream always sees a valid YAML front matter with docs
            _write_plan_front_matter(plan_path, header, body)
            
            docs_spec = header.get("docs") or []
            docs_per_round = int(os.environ.get("DOCS_PER_ROUND", "3"))
            pending = [d for d in docs_spec if d and d.get("status") != "done"]
            needed = max(1, (len(pending) + docs_per_round - 1) // docs_per_round)
            
            meta = header.setdefault("meta", {})
            if not isinstance(meta.get("remaining_execs"), int) or meta["remaining_execs"] < needed:
                meta["remaining_execs"] = needed
                _write_plan_front_matter(plan_path, header, body)
            
            runner = REPO_ROOT / ".roo" / "tools" / "bw" / "dox_follow_plan.py"
            if not runner.exists():
                click.echo(f"[plan][WARN] Runner not found: {runner}")
            else:
                while True:
                    raw = plan_path.read_text(encoding="utf-8-sig")
                    header, body = _read_plan_yaml(plan_path)

                    docs_spec = header.get("docs") or []
                    meta = header.get("meta") or {}
                    rem_execs = int(meta.get("remaining_execs", 0))
                    remaining = [d for d in docs_spec if d and d.get("status") != "done"
                                 and not (docs_out / d.get("filename", "")).exists()]
                    if not remaining or rem_execs <= 0:
                        break

                    click.echo(f"[plan] Fulfilling docs (left={len(remaining)}, remaining_execs={rem_execs})...")
                    subprocess.run(
                        [sys.executable, str(runner), "--plan", str(plan_path),
                         "--out", str(docs_out), "--docs-per-round", str(docs_per_round)],
                        check=False
                    )
        else:
            click.echo("[plan] Skipped (no LLM or --skip-llm).")
    except Exception as e:
        click.echo(f"[plan][WARN] Planning/fulfillment step failed: {e}")

    # === ALWAYS run these steps after planning/fulfillment ===

    # Reorganize docs into subfolders before MkDocs
    try:
        subprocess.run([sys.executable, str(REPO_ROOT / ".roo" / "tools" / "bw" / "docs_reorganize.py")],
                       check=False)
        click.echo("[reorg] Docs reorganized.")
    except Exception as e:
        click.echo(f"[reorg][WARN] Reorg step failed: {e}")

    # Confidence rollup (append to Family/plan docs)
    try:
        rollup = REPO_ROOT / ".roo" / "tools" / "bw" / "rollup_confidence.py"
        if rollup.exists():
            subprocess.run(
                [sys.executable, str(rollup),
                 "--docs", str(OUT_DOCS),
                 "--sir", str(REPO_ROOT / "out" / "sir" / "_interdeps.json"),
                 "--plan", str(REPO_ROOT / "out" / "dox_draft_plan.md")],
                check=False
            )
            click.echo("[rollup] Confidence rollup appended to high-level docs.")
        else:
            click.echo(f"[rollup][WARN] Script not found: {rollup}")
    except Exception as e:
        click.echo(f"[rollup][WARN] Confidence rollup step failed: {e}")

    # MkDocs bootstrap
    try:
        boot = REPO_ROOT / ".roo" / "tools" / "bw" / "mkdocs_bootstrap.py"
        if boot.exists():
            # Use --force-config to ensure mkdocs.yml matches your target settings
            subprocess.run([sys.executable, str(boot),
                            "--force-config", "--force-home"],
                           check=False)
            click.echo("[mkdocs] Bootstrapped and homepage set.")
        else:
            click.echo(f"[mkdocs][WARN] Bootstrap script not found at {boot}")
    except Exception as e:
        click.echo(f"[mkdocs][WARN] mkdocs bootstrap failed: {e}")

    click.echo(f"Pipeline complete. Processes documented: {processed}. Families: {len(fams)}")


if __name__ == "__main__":
    cli()
