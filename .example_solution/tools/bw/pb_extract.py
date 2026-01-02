#!/usr/bin/env python3
# .roo/tools/bw/pb_extract.py
from __future__ import annotations
import re, json, hashlib
from pathlib import Path
from typing import List, Dict, Tuple, Optional
from lxml import etree as ET

PB_EVENT_RE = re.compile(r"^\s*event\s+([a-zA-Z0-9_]+)", re.IGNORECASE | re.MULTILINE)
PB_FUNC_RE  = re.compile(r"^\s*(public|protected)?\s*(function|subroutine)\s+[a-zA-Z0-9_]+(?:\s+[a-zA-Z0-9_]+)?\s+([a-zA-Z0-9_\.]+)\s*\(", re.IGNORECASE | re.MULTILINE)
PB_CALL_RE  = re.compile(r"([a-zA-Z_][a-zA-Z0-9_\.]+)\s*\.\s*([a-zA-Z_][a-zA-Z0-9_]*)\s*\(", re.IGNORECASE)
PB_HTTP_RE  = re.compile(r'createobject\(\s*"(httpclient|restclient)"\s*\)|sendrequest\s*\(|setrequestheader\s*\(', re.IGNORECASE)
PB_SOAP_RE  = re.compile(r"(soapconnection|createsoapconnection)\s*|\.soap\w+\(", re.IGNORECASE)
PB_EXT_RE   = re.compile(r"\bfunction\b.*\bexternal\b", re.IGNORECASE)
PB_SQL_HINT = re.compile(r"\b(select|insert|update|delete|merge|call)\b", re.IGNORECASE)
PB_DW_OP_RE = re.compile(r"\b(dw_[a-zA-Z0-9_]+)\.(retrieve|update|insertrow|deleterow|settrans|settransobject)", re.IGNORECASE)
IMG_REF_RE  = re.compile(r'["\']([^"\']+\.(?:png|ico))["\']', re.IGNORECASE)

def _collect_image_refs_from_text(text: str) -> List[str]:
    refs = set()

    # Generic: any "something.png" or "something.ico" inside quotes
    for m in IMG_REF_RE.findall(text or ""):
        refs.add(m.strip())

    # Property-like patterns often seen in exported SRW/SRU text
    for m in re.findall(r"(?i)\b(PictureName|Icon|SmallIcon|LargeIcon|Picture)\s*=\s*['\"]([^'\"]+)", text or ""):
        refs.add(m[1].strip())

    # Also accept backslash separators (RibbonBar-style)
    return sorted(refs)

def _collect_image_refs_from_ribbon_xml(rootxml: ET._Element) -> List[str]:
    refs = set()
    # Attributes where PB ribbons store icons/images
    attr_names = ["PictureName", "Picture", "SmallIcon", "LargeIcon", "Icon", "PictureName"]
    for el in rootxml.xpath(".//*"):
        if not hasattr(el, "attrib"): 
            continue
        for a in attr_names:
            v = el.get(a)
            if v and (".png" in v.lower() or ".ico" in v.lower()):
                refs.add(v.strip())
    return sorted(refs)


def sha256_file(p: Path) -> str:
    h = hashlib.sha256()
    with p.open("rb") as f:
        for chunk in iter(lambda: f.read(1024*1024), b""): h.update(chunk)
    return h.hexdigest()

def discover_pb_sources(root: Path) -> Dict[str, List[Path]]:
    files: Dict[str, List[Path]] = {
        "srw": [], "sru": [], "srd": [], "xml": [], "ini": [], "json": [], "pbt": [], "pbw": [], "pbg": []
    }
    for p in root.rglob("*"):
        if not p.is_file(): continue
        ext = p.suffix.lower()
        if ext in (".srw",".sru",".srd",".xml",".ini",".json",".pbt",".pbw",".pbg"):
            key = ext.lstrip(".")
            if key in files: files[key].append(p)
    return files

def _read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8", errors="ignore")

def _pb_object_name(path: Path, header_text: str) -> Tuple[str,str]:
    # Heuristic: file stem often equals object name; prefix by typish
    stem = path.stem.lower()
    # infer object type by extension/prefix
    if path.suffix.lower()==".srw" or stem.startswith("w_"): otype="Window"
    elif stem.startswith("n_") or path.suffix.lower()==".sru": otype="NVO"
    elif stem.startswith("m_"): otype="Menu"
    else: otype="Object"
    return otype, path.stem

def _make_process_name(pbl_hint: str, obj: str, member: str) -> str:
    # format to cluster families by leading segments
    fam = pbl_hint.lower() if pbl_hint else "pb"
    return f"{fam}.{obj}.{member}"

def _parse_calls(text: str) -> Dict[str, List[str]]:
    calls = []
    for m in PB_CALL_RE.finditer(text):
        qual, method = m.group(1), m.group(2)
        calls.append(f"{qual}.{method}")
    dw_ops = [f"{m.group(1)}.{m.group(2).lower()}" for m in PB_DW_OP_RE.finditer(text)]
    hints = {
        "http": bool(PB_HTTP_RE.search(text)),
        "soap": bool(PB_SOAP_RE.search(text)),
        "external": bool(PB_EXT_RE.search(text)),
        "sql_like": bool(PB_SQL_HINT.search(text)),
        "dw_ops": dw_ops,
        "calls": calls
    }
    return hints

def parse_srd_tables(text: str) -> Tuple[str, List[str]]:
    # crude SQL SELECT extractor
    sql = ""
    tables: List[str] = []
    m = re.search(r"(?is)select\b.+?\bfrom\b.+?(?:\bwhere\b|\bgroup\b|\border\b|;|$)", text)
    if m:
        sql = m.group(0).strip()
        # find table tokens after FROM and JOIN
        for t in re.findall(r"(?i)\bfrom\s+([a-z0-9_\.]+)|\bjoin\s+([a-z0-9_\.]+)", sql):
            tables.extend([x for x in t if x])
    tables = list(dict.fromkeys(tables))
    return sql, tables

def _activity(name: str, typ: str, path: Path, line: int = 1) -> Dict:
    return {
        "name": name, "type": typ,
        "line": max(1, line),
        "evidence": [{"path": str(path.as_posix()), "selector": f"line:{line}"}]
    }

def build_sirs(root: Path) -> List[Dict]:
    found = discover_pb_sources(root)
    # Build a map of DataWindow name -> tables (from .srd)
    dw_sql: Dict[str, Dict] = {}
    for srd in found["srd"]:
        t = _read_text(srd)
        sql, tables = parse_srd_tables(t)
        dw_sql[srd.stem.lower()] = {"sql": sql, "tables": tables}
    sirs: List[Dict] = []

    # Parse RibbonBar XML (optional workflows from UI)
    for x in found["xml"]:
        try:
            # inside: for x in found["xml"]:
            rootxml = ET.parse(str(x), parser=ET.XMLParser(recover=True)).getroot()
            img_refs = _collect_image_refs_from_ribbon_xml(rootxml)
        except Exception:
            continue
        # items with Clicked/Selected attributes -> ue_* event names
        img_refs = _collect_image_refs_from_ribbon_xml(rootxml)
        pbl_hint = x.parent.name if x.parent else "ribbon"
        for el in rootxml.xpath(".//*[@Clicked or @Selected]"):
            ev = el.get("Clicked") or el.get("Selected")
            if ev:
                otype, objname = "Window", x.stem  # heuristic
                proc = _make_process_name("ribbon", objname, ev)
                activities = [
                    _activity(f"{objname}.{ev}", "pb:ui_event", x, 1)
                ]
                transitions = []
                sir = {
                    "process_name": proc,
                    "project_name": root.name,
                    "source_file": str(x.as_posix()),
                    "source_file_format": "powerbuilder_xml",
                    "hash_sha256": sha256_file(x),
                    "start_activity": activities[0]["name"],
                    "activities": activities,
                    "transitions": transitions,
                    "resources": {},
                    "metadata": {
                        "extracted_at": __import__("datetime").datetime.now(__import__("datetime").timezone.utc).isoformat(),
                        "tool_version": "bw-orchestrator-1.3.0",
                        "notes": ["ribbon:ui_event only"]
                    },
                    "enrichment": {
                        "pb_meta": {
                            "object_type": otype,
                            "ribbon": True,
                            "pbl_hint": pbl_hint,
                            "image_refs": img_refs
                        }
                    }
                }
                sirs.append(sir)

    # Parse SRW/SRU for events and public methods
    for path in found["srw"] + found["sru"]:
        text = _read_text(path)
        otype, objname = _pb_object_name(path, text)
        pbl_hint = path.parent.name  # library folder heuristic
        # events
        evs = [m.group(1) for m in PB_EVENT_RE.finditer(text)]
        # public functions/subroutines treated as service entry points
        fns = [m.group(3) for m in PB_FUNC_RE.finditer(text)]
        # Build SIRs for events
        for ev in evs:
            proc = _make_process_name(pbl_hint, objname, ev)
            hints = _parse_calls(text)
            acts = [_activity(f"{objname}.{ev}", "pb:ui_event", path, 1)]
            # append steps based on hints
            for dwop in hints["dw_ops"]:
                acts.append(_activity(dwop, "pb:datawindow_op", path, 1))
            for c in hints["calls"][:10]:
                acts.append(_activity(c, "pb:method_call", path, 1))
            if hints["sql_like"]:
                acts.append(_activity("embedded_sql", "pb:db_exec", path, 1))
            if hints["http"]:
                acts.append(_activity("http_request", "pb:http_request", path, 1))
            if hints["soap"]:
                acts.append(_activity("soap_call", "pb:soap_call", path, 1))
            if hints["external"]:
                acts.append(_activity("external_function", "pb:external_function", path, 1))

            # transitions in sequence
            trans = []
            for i in range(len(acts)-1):
                trans.append({"from": acts[i]["name"], "to": acts[i+1]["name"]})

            sir = {
                "process_name": proc,
                "project_name": path.parents[1].name if len(path.parents) >= 2 else "",
                "source_file": str(path.as_posix()),
                "source_file_format": "powerbuilder_srw" if path.suffix.lower()==".srw" else "powerbuilder_sru",
                "hash_sha256": sha256_file(path),
                "start_activity": acts[0]["name"],
                "activities": acts,
                "transitions": trans,
                "resources": {},
                "metadata": {
                    "extracted_at": __import__("datetime").datetime.now(__import__("datetime").timezone.utc).isoformat(),
                    "tool_version": "bw-orchestrator-1.3.0",
                    "notes": []
                },
                "enrichment": {
                    "pb_meta": {"object_type": otype, "pbl_hint": pbl_hint}
                }
            }
            sirs.append(sir)

        # Build SIRs for public methods (service-style)
        for fn in fns:
            proc = _make_process_name(pbl_hint, objname, fn)
            hints = _parse_calls(text)
            acts = [_activity(f"{objname}.{fn}", "pb:service_call", path, 1)]
            for dwop in hints["dw_ops"]:
                acts.append(_activity(dwop, "pb:datawindow_op", path, 1))
            for c in hints["calls"][:10]:
                acts.append(_activity(c, "pb:method_call", path, 1))
            if hints["sql_like"]:
                acts.append(_activity("embedded_sql", "pb:db_exec", path, 1))
            if hints["http"]:
                acts.append(_activity("http_request", "pb:http_request", path, 1))
            if hints["soap"]:
                acts.append(_activity("soap_call", "pb:soap_call", path, 1))
            if hints["external"]:
                acts.append(_activity("external_function", "pb:external_function", path, 1))
            trans = [{"from": acts[i]["name"], "to": acts[i+1]["name"]} for i in range(len(acts)-1)]
            sir = {
                "process_name": proc,
                "project_name": path.parents[1].name if len(path.parents) >= 2 else "",
                "source_file": str(path.as_posix()),
                "source_file_format": "powerbuilder_sru" if path.suffix.lower()==".sru" else "powerbuilder_srw",
                "hash_sha256": sha256_file(path),
                "start_activity": acts[0]["name"],
                "activities": acts,
                "transitions": trans,
                "resources": {},
                "metadata": {
                    "extracted_at": __import__("datetime").datetime.now(__import__("datetime").timezone.utc).isoformat(),
                    "tool_version": "bw-orchestrator-1.3.0",
                    "notes": []
                },
                "enrichment": {
                    "pb_meta": {"object_type": otype, "pbl_hint": pbl_hint}
                }
            }
            sirs.append(sir)

    # Attach DataWindow datastore hints (dependencies) best-effort
    for s in sirs:
        pbm = (s.get("enrichment") or {}).get("pb_meta", {})
        deps = set()
        for a in s.get("activities", []):
            m = re.match(r"(dw_[a-z0-9_]+)\.(\w+)", a["name"], flags=re.IGNORECASE)
            if m:
                dw = m.group(1).lower().replace("dw_", "")
                meta = dw_sql.get(dw)
                if meta and meta.get("tables"):
                    for tbl in meta["tables"]:
                        deps.add(f"sqlca:{tbl}")
        if deps:
            s.setdefault("enrichment", {}).setdefault("business_scaffold", {}).setdefault("dependencies", {})["datastores"] = sorted(deps)
    return sirs
