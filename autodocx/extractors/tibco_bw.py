# autodocx/extractors/tibco_bw.py
from __future__ import annotations
from pathlib import Path
from typing import Iterable, List, Dict, Any, Optional, Set, Tuple
import re
import os
import hashlib
from lxml import etree

from autodocx.types import Signal

# try to import redact but tolerate it missing
try:
    from autodocx.utils.redaction import redact
except Exception:
    # fallback no-op redact if redaction module fails for any reason
    def redact(s: str) -> str:
        try:
            return str(s)
        except Exception:
            return "*****REDACTED*****"


# import the universal mapper
try:
    from autodocx.utils.roles import map_connectors_to_roles
except Exception:
    # fallback: no-op mapper
    def map_connectors_to_roles(connectors):
        return []


# Helpers -----------------------------------------------------------------

NS = {
    "bpws": "http://docs.oasis-open.org/wsbpel/2.0/process/executable",
    "tibex": "http://www.tibco.com/bpel/2007/extensions",
    "scaext": "http://xsd.tns.tibco.com/amf/models/sca/extensions",
    "rest": "http://xsd.tns.tibco.com/bw/models/binding/rest",
}
IDENTIFIER_SUFFIXES = ("id", "key", "code", "number", "guid", "token")
BPWS_NS = {"bpws": "http://docs.oasis-open.org/wsbpel/2.0/process/executable"}

KNOWN_CONNECTOR_TOKENS = (
    "http",
    "rest",
    "soap",
    "jdbc",
    "sql",
    "jms",
    "mapper",
    "callprocess",
    "process",
    "bw",
    "timer",
    "file",
)

BW_PALETTE_HINTS: Tuple[Tuple[str, Dict[str, Any]], ...] = (
    ("httpreceiver", {"connector": "http", "role_hints": ["interface.receive"]}),
    ("httpresponse", {"connector": "http", "role_hints": ["interface.reply"]}),
    ("sendhttp", {"connector": "http", "role_hints": ["interface.reply"]}),
    ("rest", {"connector": "rest", "role_hints": ["interface.receive", "interface.reply"]}),
    ("soap", {"connector": "soap", "role_hints": ["interface.receive", "interface.reply"]}),
    ("callprocess", {"connector": "process", "role_hints": ["invoke.process"]}),
    ("invoke", {"connector": "process", "role_hints": ["invoke.process"]}),
    ("mapper", {"connector": "mapper", "role_hints": ["transform.mapper"]}),
    ("jdbc", {"connector": "jdbc", "role_hints": ["data.jdbc"]}),
    ("sql", {"connector": "jdbc", "role_hints": ["data.jdbc"]}),
    ("jms", {"connector": "jms", "role_hints": ["messaging.jms"]}),
    ("timer", {"connector": "timer", "role_hints": ["schedule.timer"]}),
    ("log", {"connector": "log", "role_hints": ["ops.log"]}),
    ("error", {"connector": "error", "role_hints": ["error.throw"]}),
)


def _apply_palette_hints(step: Dict[str, Any], tag: str) -> None:
    lowered = tag.lower()
    for token, meta in BW_PALETTE_HINTS:
        if token in lowered:
            connector = meta.get("connector")
            if connector and not step.get("connector"):
                step["connector"] = connector
            role_hints = meta.get("role_hints") or []
            if role_hints:
                holder = step.setdefault("role_hints", [])
                for role in role_hints:
                    if role not in holder:
                        holder.append(role)
            break


def _step_matches_known(step: Dict[str, Any]) -> bool:
    source = f"{step.get('connector') or ''} {step.get('type') or ''}".lower()
    return any(token in source for token in KNOWN_CONNECTOR_TOKENS)


def _compute_debug_counts(xml_root: etree._Element) -> Dict[str, int]:
    def _count(xpath: str, ns: Optional[Dict[str, str]] = None) -> int:
        try:
            return len(xml_root.xpath(xpath, namespaces=ns))
        except Exception:
            return 0

    return {
        "n_type_ns": _count(".//*[@type and contains(@type,':')]"),
        "n_xmi_act": _count(".//*[@xmi:type and contains(@xmi:type,'Activity')]", BPWS_NS),
        "n_transitions": _count(".//*[local-name()='transition']") + _count(".//*[contains(local-name(),'Transition')]"),
        "n_bpws_sequence": _count(".//bpws:sequence", BPWS_NS),
        "n_bpws_flow": _count(".//bpws:flow", BPWS_NS),
        "n_bpws_source": _count(".//bpws:source", BPWS_NS),
        "n_bpws_target": _count(".//bpws:target", BPWS_NS),
    }


def _get_tag_short(el: etree._Element) -> str:
    try:
        qname = etree.QName(el)
        local = qname.localname
        ns = qname.namespace
    except Exception:
        return _strip_tag(getattr(el, "tag", ""))
    prefix = None
    if ns and getattr(el, "nsmap", None):
        for key, value in (el.nsmap or {}).items():
            if value == ns:
                prefix = key
                break
    return f"{prefix}:{local}" if prefix else local


def _extract_activities_and_transitions(xml_root: etree._Element) -> Tuple[List[Dict[str, str]], List[Dict[str, str]]]:
    activities: List[Dict[str, str]] = []
    transitions: List[Dict[str, str]] = []
    names_seen: Set[str] = set()
    id_to_name: Dict[str, str] = {}

    for el in xml_root.xpath(".//*[@name or @displayName]"):
        name = (el.get("name") or el.get("displayName") or "").strip()
        if not name:
            continue
        typ = el.get("type") or el.get("Type") or el.get("{http://www.omg.org/XMI}type") or _get_tag_short(el)
        if name not in names_seen:
            activities.append({"name": name, "type": typ})
            names_seen.add(name)
        ident = el.get("{http://www.omg.org/XMI}id") or el.get("xmi:id") or el.get("id")
        if ident and name:
            id_to_name[ident] = name

    for el in xml_root.xpath(".//*[local-name()='transition'] | .//*[contains(local-name(),'Transition')]"):
        frm = (
            el.get("from")
            or el.get("From")
            or el.get("source")
            or el.get("Source")
            or el.get("fromRef")
            or el.get("sourceRef")
        )
        to = (
            el.get("to")
            or el.get("To")
            or el.get("target")
            or el.get("Target")
            or el.get("toRef")
            or el.get("targetRef")
        )
        if not frm and el.find(".//*[local-name()='from']") is not None:
            frm = el.find(".//*[local-name()='from']").get("value")
        if not to and el.find(".//*[local-name()='to']") is not None:
            to = el.find(".//*[local-name()='to']").get("value")
        if frm:
            frm = id_to_name.get(frm, frm)
        if to:
            to = id_to_name.get(to, to)
        if frm and to:
            transitions.append({"from": frm, "to": to})

    return activities, transitions


def _find_start_activity(activities: List[Dict[str, str]], transitions: List[Dict[str, str]]) -> Optional[str]:
    activity_names = [a.get("name") for a in activities if a.get("name")]
    destinations = {t.get("to") for t in transitions if t.get("to")}
    for name in activity_names:
        if name not in destinations:
            return name
    return activity_names[0] if activity_names else None

def _strip_tag(t: str) -> str:
    return t.split("}")[-1] if "}" in t else t

def _make_snippet(path: Path, lineno: int, context: int = 3) -> Dict[str, Any]:
    """
    Read file and return snippet around lineno.
    Returns dict {path, lines, snippet} with redact applied.
    """
    try:
        lines = path.read_text(encoding="utf-8", errors="ignore").splitlines()
        idx = max(0, lineno - 1)
        start = max(0, idx - context)
        end = min(len(lines), idx + context + 1)
        snippet_text = "\n".join(lines[start:end])
        snippet_text = redact(snippet_text)
        return {"path": str(path), "lines": f"{start+1}-{end}", "snippet": snippet_text}
    except Exception as e:
        return {"path": str(path), "lines": "", "snippet": f"(unreadable snippet: {e})"}

def _find_repo_root(path: Path) -> Path:
    """
    Heuristic: climb parents until you find a .git or a 'Workflows' folder (common in your repos).
    Fallback: current working directory.
    """
    p = path.resolve()
    for parent in [p] + list(p.parents):
        if (parent / ".git").exists():
            return parent
        if any((parent / child).is_dir() and child.lower() == "workflows" for child in parent.iterdir() if child.is_dir()):
            return parent
    # fallback: cwd
    return Path.cwd()

def _safe_name_for_file(s: str) -> str:
    return re.sub(r"[^A-Za-z0-9_.-]+", "_", s).strip("_")[:200]

def _sql_summary(sql_text: str) -> Dict[str, str]:
    summary = {"verb": "", "table": ""}
    if not sql_text:
        return summary
    text = sql_text.strip()
    parts = text.split(None, 1)
    verb = parts[0].upper()
    summary["verb"] = verb
    table = ""
    if verb == "SELECT":
        match = re.search(r"\bFROM\s+([A-Za-z0-9_.\[\]\"`]+)", text, re.IGNORECASE)
        if match:
            table = match.group(1)
    elif verb in {"INSERT", "DELETE"}:
        match = re.search(r"\bINTO\s+([A-Za-z0-9_.\[\]\"`]+)", text, re.IGNORECASE)
        if not match:
            match = re.search(r"\bFROM\s+([A-Za-z0-9_.\[\]\"`]+)", text, re.IGNORECASE)
        if match:
            table = match.group(1)
    elif verb == "UPDATE":
        match = re.search(r"\bUPDATE\s+([A-Za-z0-9_.\[\]\"`]+)", text, re.IGNORECASE)
        if match:
            table = match.group(1)
    if table:
        summary["table"] = table.strip("[]\"`")
    return summary

def _make_relationship(source: str, target_kind: str, target_ref: str, target_display: str, operation_type: str, detail: str, evidence: Dict[str, Any]) -> Dict[str, Any]:
    base = f"{source}:{target_kind}:{target_ref}:{detail}"
    rel_id = hashlib.sha1(base.encode("utf-8")).hexdigest()[:12]
    crud_map = {
        "reads": "read",
        "writes": "update",
        "deletes": "delete",
        "invokes": "execute",
        "calls": "execute",
    }
    return {
        "id": f"{target_kind}_{rel_id}",
        "source": {"type": "activity", "name": source, "step_id": source},
        "target": {"kind": target_kind, "ref": target_ref, "display": target_display},
        "operation": {
            "type": operation_type,
            "verb": operation_type,
            "crud": crud_map.get(operation_type, "read"),
            "protocol": target_kind,
            "detail": detail,
        },
        "connector": target_kind,
        "direction": "outbound",
        "context": {},
        "roles": [],
        "evidence": [evidence],
        "confidence": 0.7,
    }


def _compose_rest_url(base: Optional[str], resource: Optional[str], path_value: Optional[str]) -> str:
    base_clean = (base or "").strip().rstrip("/")
    resource_clean = (resource or "").strip("/")
    rel_clean = (path_value or "").strip("/")
    suffix_parts = [segment for segment in (resource_clean, rel_clean) if segment]
    if base_clean:
        if suffix_parts:
            return "/".join([base_clean] + suffix_parts)
        return base_clean
    if suffix_parts:
        return "/" + "/".join(suffix_parts)
    return ""


def _collect_partner_links(root: etree._Element) -> Dict[str, Dict[str, Any]]:
    catalog: Dict[str, Dict[str, Any]] = {}
    try:
        partners = root.xpath(".//bpws:partnerLink", namespaces=NS)
    except Exception:
        partners = []
    for node in partners or []:
        name = node.get("name")
        if not name:
            continue
        meta: Dict[str, Any] = {
            "name": name,
            "connector": (node.get("partnerLinkType") or "").split(":")[-1],
            "kind": "http",
            "display": node.get("name") or "",
        }
        binding = None
        try:
            binding = node.xpath(".//scaext:binding", namespaces=NS)
            binding = binding[0] if binding else None
        except Exception:
            binding = None
        if binding is not None:
            meta["doc_base_path"] = binding.get("docBasePath")
            meta["doc_resource_path"] = binding.get("docResourcePath") or binding.get("docResourcepath")
            meta["path"] = binding.get("path")
            meta["connector"] = binding.get("connector") or meta.get("connector")
            meta["display"] = binding.get("name") or meta.get("display")
            op_node = None
            try:
                ops = binding.xpath(".//*[local-name()='operation']")
                op_node = ops[0] if ops else None
            except Exception:
                op_node = None
            if op_node is not None:
                meta["http_method"] = (op_node.get("httpMethod") or "").upper()
                meta["operation_name"] = op_node.get("operationName") or op_node.get("nickname")
        meta["url"] = _compose_rest_url(meta.get("doc_base_path"), meta.get("doc_resource_path"), meta.get("path"))
        catalog[name] = meta
    return catalog


def _http_operation_kind(method: Optional[str]) -> str:
    method = (method or "").upper()
    if method == "GET":
        return "reads"
    if method in {"POST", "PUT", "PATCH"}:
        return "writes"
    if method == "DELETE":
        return "deletes"
    return "calls"


def _friendly_step_label(step_name: str, meta: Dict[str, Any], operation: Optional[str]) -> str:
    method = (meta.get("http_method") or operation or "").upper()
    target = meta.get("display") or meta.get("path") or meta.get("url") or meta.get("name") or "Endpoint"
    base = step_name or target
    friendly = base
    if target and target.lower() not in base.lower():
        friendly = f"{base} \u2192 {target}"
    if method:
        friendly = f"{method} {friendly}"
    return friendly


def _derive_identifier_tokens(values: List[str]) -> List[str]:
    tokens: Set[str] = set()
    for value in values:
        if not value:
            continue
        for token in re.findall(r"[A-Za-z0-9_]+", str(value)):
            lower = token.lower()
            if any(lower.endswith(suffix) for suffix in IDENTIFIER_SUFFIXES):
                tokens.add(token)
    return sorted(tokens)

# Main extractor ----------------------------------------------------------

class TibcoBWExtractor:
    name = "tibco_bw_lxml"
    patterns = ["**/*.bwp", "**/*.process", "**/*.xml"]

    def detect(self, repo: Path) -> bool:
        # quick check for likely process files
        return any(repo.glob("**/*.bwp")) or any(repo.glob("**/*.process")) or any(repo.glob("**/*.xml"))

    def discover(self, repo: Path) -> Iterable[Path]:
        """
        Yield BW-relevant process files. Be robust and explicit:
        - Always include *.bwp and *.process
        - Include *.xml only if scanning the head shows BW markers
        """
        seen = set()
    
        # Always take .bwp and .process
        for p in repo.rglob("*.bwp"):
            if p.is_file() and p not in seen:
                seen.add(p)
                yield p
        for p in repo.rglob("*.process"):
            if p.is_file() and p not in seen:
                seen.add(p)
                yield p
    
        # Heuristic include .xml only if head suggests BW content
        for p in repo.rglob("*.xml"):
            if p.is_file() and p not in seen:
                try:
                    head = p.read_text(encoding="utf-8", errors="ignore")[:8192].lower()
                    if any(k in head for k in ("http://www.tibco.com/bpel", "xmlns:bpws", "xmlns:tibex", "bw.model", "<process")):
                        seen.add(p)
                        yield p
                except Exception:
                    # If unreadable, skip to reduce noise
                    continue


    def extract(self, path: Path) -> Iterable[Signal]:
        signals: List[Signal] = []
        try:
            parser = etree.XMLParser(remove_comments=True, recover=True)
            tree = etree.parse(str(path), parser)
            root = tree.getroot()
        except Exception as e:
            signals.append(Signal(
                kind="doc",
                props={"name": path.stem, "file": str(path), "note": f"XML parse error: {e}"},
                evidence=[{"path": str(path), "lines": "1-1", "snippet": "(parse error)"}],
                subscores={"parsed": 0.1}
            ))
            return signals

        # Determine process name
        proc_name = root.get("name") or root.get("id") or path.stem
        partner_links = _collect_partner_links(root)

        if os.getenv("AUTODOCX_DEBUG_BW", "0") == "1":
            print(f"[tibco_bw] extracting: {path} as process '{proc_name}'")
        

        triggers: List[Dict[str, Any]] = []
        steps: List[Dict[str, Any]] = []
        calls: List[str] = []
        sqls: List[Dict[str, Any]] = []
        relationships: List[Dict[str, Any]] = []
        evidence_list: List[Dict[str, Any]] = []
        datasource_tables: Set[str] = set()
        identifier_hints: Set[str] = set()
        service_dependencies: Set[str] = set()

        # Surface partner link bindings as interface triggers before walking activities
        for pname, meta in partner_links.items():
            url = meta.get("url") or meta.get("path")
            method = (meta.get("http_method") or "").upper() or None
            if url:
                triggers.append(
                    {
                        "name": pname,
                        "type": meta.get("kind") or meta.get("connector") or "http",
                        "path": url,
                        "method": method,
                        "evidence": {"file": str(path)},
                    }
                )
                relationships.append(
                    _make_relationship(
                        source=proc_name,
                        target_kind=meta.get("kind") or "http",
                        target_ref=url,
                        target_display=url,
                        operation_type="receives",
                        detail=f"{method or 'ANY'} {url}",
                        evidence={"file": str(path)},
                    )
                )
                identifier_hints.update(_derive_identifier_tokens([url, pname]))

        # Iterate all elements
        for el in tree.iter():
            tag = _strip_tag(el.tag).lower()
            lineno = el.sourceline or 0
            xpath = tree.getpath(el) if hasattr(tree, "getpath") else f"/{tag}"
            anchor = {"path": str(path), "lines": f"{lineno}-{lineno}", "snippet": _make_snippet(path, lineno)["snippet"]}

            # Record generic evidence
            evidence_list.append(anchor)

            # Activity-like detection
            name = el.get("name") or el.get("id") or _strip_tag(el.tag)
            # Record steps for many BW activity tags (best-effort)
            if any(k in tag for k in ("activity", "invoke", "callprocess", "jdbc", "mapper", "httpreceiver", "http")):
                step = {"name": name, "type": tag, "evidence": anchor}
                _apply_palette_hints(step, tag)
                datasource = el.get("datasource") or el.get("datasourceName")
                if datasource:
                    step["datasource"] = datasource
                partner_name = el.get("partnerLink")
                partner_meta = partner_links.get(partner_name) if partner_name else None
                if partner_meta:
                    method = (partner_meta.get("http_method") or el.get("operation") or "").upper()
                    url = partner_meta.get("url") or partner_meta.get("path")
                    step["connector"] = partner_meta.get("connector") or "http"
                    _apply_palette_hints(step, step["connector"])
                    if method:
                        step["method"] = method
                    if url:
                        step["url_or_path"] = url
                    step["connection_display"] = partner_meta.get("display")
                    friendly = _friendly_step_label(step.get("name") or "", partner_meta, el.get("operation"))
                    if friendly:
                        step["friendly_display"] = friendly
                    detail = " ".join(bit for bit in (method, url or partner_meta.get("operation_name")) if bit)
                    relationships.append(
                        _make_relationship(
                            source=name,
                            target_kind=partner_meta.get("kind") or "http",
                            target_ref=url or partner_meta.get("operation_name") or partner_name,
                            target_display=partner_meta.get("display") or url or partner_name,
                            operation_type=_http_operation_kind(method),
                            detail=detail or (partner_meta.get("display") or ""),
                            evidence=anchor,
                        )
                    )
                    if partner_meta.get("url"):
                        service_dependencies.add(partner_meta["url"])
                # Try to find connector detail for JDBC/HTTP
                # JDBC: look for child elements or attributes containing 'sql' or 'statement'
                sql_text = None
                sql_node = el.find(".//sqlStatement")
                if sql_node is not None and (sql_node.text or "").strip():
                    sql_text = (sql_node.text or "").strip()
                else:
                    for attr in ("statement", "sql"):
                        if el.get(attr):
                            sql_text = el.get(attr)
                            break
                if sql_text:
                    step["connector"] = "jdbc"
                    _apply_palette_hints(step, "jdbc")
                    summary = _sql_summary(sql_text)
                    verb = summary.get("verb") or "SQL"
                    table = summary.get("table") or "SQL statement"
                    if summary.get("table"):
                        datasource_tables.add(summary["table"])
                    detail = f"{verb.title()} {table}"
                    step["friendly_display"] = detail
                    relationships.append(
                        _make_relationship(
                            source=name,
                            target_kind="sql",
                            target_ref=table,
                            target_display=table,
                            operation_type="writes" if verb in {"INSERT", "UPDATE", "DELETE"} else "reads",
                            detail=detail,
                            evidence=anchor,
                        )
                    )
                    sqls.append({"sql": sql_text.strip(), "evidence": anchor})
                # HTTP receiver hints
                # common attribute names: endpointUri, endpointURI, path, apipath
                for attr in ("endpointUri", "endpointURI", "path", "apipath", "url"):
                    v = el.get(attr)
                    if v and isinstance(v, str) and v.strip():
                        step["connector"] = "http"
                        _apply_palette_hints(step, "http")
                        path_hint = v.strip()
                        method = (el.get("method") or "POST").upper()
                        step["url_or_path"] = path_hint
                        step["method"] = method
                        step["friendly_display"] = f"{method} {path_hint}"
                        relationships.append(
                            _make_relationship(
                                source=name,
                                target_kind="http",
                                target_ref=path_hint,
                                target_display=path_hint,
                                operation_type="reads",
                                detail=f"{method} {path_hint}",
                                evidence=anchor,
                            )
                        )
                        triggers.append({"name": proc_name, "type": "http", "path": path_hint, "method": method, "evidence": anchor})
                        break

                steps.append(step)
                mapper_paths = []
                for attr_name, attr_value in el.attrib.items():
                    lower_name = attr_name.lower()
                    if any(token in lower_name for token in ("path", "xpath", "field")) and attr_value:
                        mapper_paths.append(attr_value)
                for child in el:
                    text = (child.text or "").strip()
                    if text and "/" in text:
                        mapper_paths.append(text)
                for token in _derive_identifier_tokens(mapper_paths):
                    identifier_hints.add(token)

                if "mapper" in tag and mapper_paths:
                    step["mapper_paths"] = mapper_paths

                if any(keyword in tag for keyword in ("jms", "queue", "topic")):
                    destination = el.get("destination") or el.get("queue") or el.get("topic")
                    if destination:
                        step["connector"] = "jms"
                        _apply_palette_hints(step, "jms")
                        step["destination"] = destination
                        service_dependencies.add(destination)
                        relationships.append(
                            _make_relationship(
                                source=name,
                                target_kind="jms",
                                target_ref=destination,
                                target_display=destination,
                                operation_type="calls",
                                detail=f"JMS {destination}",
                                evidence=anchor,
                            )
                        )

            # CallProcess detection: target process may be attribute or child element text
            if "callprocess" in tag or "call-process" in tag or "callprocessactivity" in tag:
                tgt = el.get("target") or el.get("process") or ""
                if not tgt:
                    # look for child with targetProcess or similar
                    for c in el.iterchildren():
                        if "process" in _strip_tag(c.tag).lower():
                            tgt = (c.text or "").strip()
                            if tgt:
                                break
                if tgt:
                    calls.append(tgt)
                    process_call_label = tgt
                    process_calls = _derive_identifier_tokens([tgt])
                    for token in process_calls:
                        identifier_hints.add(token)
                    relationships.append(
                        _make_relationship(
                            source=name,
                            target_kind="workflow",
                            target_ref=tgt,
                            target_display=tgt,
                            operation_type="invokes",
                            detail=f"Invoke {tgt}",
                            evidence=anchor,
                        )
                    )
                    step["called_process"] = tgt
                else:
                    # fallback: try to find word "process" in snippet
                    sn = (el.text or "") or ""
                    m = re.search(r"(?i)processName\s*[:=]\s*['\"]?([A-Za-z0-9_.:-]+)['\"]?", sn)
                    if m:
                        calls.append(m.group(1))

            # JDBC generic detection (SQL inside)
            if "jdbc" in tag or "sql" in tag or el.find(".//sqlStatement") is not None:
                sql_text = None
                if el.find(".//sqlStatement") is not None:
                    sql_text = (el.find(".//sqlStatement").text or "").strip()
                if not sql_text:
                    for attr in ("statement", "sql", "query"):
                        v = el.get(attr)
                        if v and isinstance(v, str) and re.search(r"\b(select|insert|update|delete)\b", v, re.I):
                            sql_text = v
                            break
                if sql_text:
                    sql_item = {"sql": sql_text.strip(), "evidence": anchor}
                    sqls.append(sql_item)

        # dedupe calls
        calls = sorted(set(calls))

        # Build role tags using the universal mapper
        # Collect candidate strings (connectors, types, names) and map them once
        connectors_to_map = []
        for s in steps:
            if s.get("connector"):
                connectors_to_map.append(s.get("connector"))
            if s.get("type"):
                connectors_to_map.append(s.get("type"))
            if s.get("name"):
                connectors_to_map.append(s.get("name"))

        for t in triggers:
            connectors_to_map.append(t.get("type") or "")
            connectors_to_map.append(t.get("path") or "")

        # map_connectors_to_roles returns a list of role strings (deduped/sorted)
        roles = set(map_connectors_to_roles(connectors_to_map))

        debug_counts = _compute_debug_counts(root)
        activities, transition_edges = _extract_activities_and_transitions(root)
        start_activity = _find_start_activity(activities, transition_edges)
        total_steps = len(steps)
        known_steps = sum(1 for step in steps if _step_matches_known(step))
        roleful_steps = sum(1 for step in steps if step.get("role_hints"))
        evidence_steps = sum(1 for step in steps if step.get("evidence"))
        known_cov = (known_steps / total_steps) if total_steps else 0.0
        role_cov = (roleful_steps / total_steps) if total_steps else 0.0
        evidence_strength = (evidence_steps / total_steps) if total_steps else 0.0
        activity_names = {a["name"] for a in activities if a.get("name")}
        transitions_ok = all(
            (edge.get("from") in activity_names and edge.get("to") in activity_names) for edge in transition_edges
        ) if transition_edges else True
        transition_integrity = 1.0 if transitions_ok else 0.0

        subs = {
            "parsed": 0.9 if steps else 0.2,
            "schema_evidence": 0.4 if sqls else 0.1,
            "endpoint_or_op_coverage": 0.8 if triggers else 0.15,
            "known_types_coverage": round(known_cov, 3),
            "role_coverage": round(role_cov, 3),
            "evidence_strength": round(evidence_strength, 3),
            "transition_integrity": round(transition_integrity, 3),
            "inferred_fraction": 0.0,
        }

        # Compose workflow Signal
        wf_props = {
            "name": proc_name,
            "file": str(path),
            "engine": "tibco_bw",
            "wf_kind": "tibco_bw",
            "triggers": triggers,
            "steps": steps,
            "calls_flows": calls,
            "sql_statements": sqls,
            "roles": sorted(list(roles)),
            "relationships": relationships,
            "start_activity": start_activity,
            "control_edges": transition_edges,
            "debug_counts": debug_counts,
            "datasource_tables": sorted(datasource_tables),
            "identifier_hints": sorted(identifier_hints),
            "process_calls": calls,
            "service_dependencies": sorted(service_dependencies),
        }
        wf_props.update(self._augment_narrative_props(proc_name, triggers, steps, sqls, path))

        # evidence rollup: unique anchors (limit to N)
        unique_evidence = []
        seen = set()
        for e in evidence_list:
            k = (e.get("path"), e.get("lines"))
            if k not in seen:
                seen.add(k)
                unique_evidence.append(e)
            if len(unique_evidence) >= 12:
                break

        signals.append(Signal(
            kind="workflow",
            props=wf_props,
            evidence=unique_evidence,
            subscores=subs
        ))

        # Additional derived Signals: db and route
        for s in sqls:
            signals.append(Signal(
                kind="db",
                props={"table": "", "sql_sample": s.get("sql")[:200], "file": str(path)},
                evidence=[s.get("evidence")],
                subscores={"parsed": 0.6, "schema_evidence": 0.4}
            ))

        for t in triggers:
            if t.get("type") == "http":
                method = (t.get("method") or "").upper() or "POST"
                signals.append(Signal(
                    kind="route",
                    props={"service": proc_name, "method": method, "path": t.get("path") or "", "file": str(path)},
                    evidence=[t.get("evidence")],
                    subscores={"parsed": 0.9, "endpoint_or_op_coverage": 0.8}
                ))

        return signals

    # ---- narrative helpers -------------------------------------------------

    def _augment_narrative_props(
        self,
        proc_name: str,
        triggers: List[Dict[str, Any]],
        steps: List[Dict[str, Any]],
        sqls: List[Dict[str, Any]],
        path: Path,
    ) -> Dict[str, Any]:
        additions: Dict[str, Any] = {}
        user_story = self._infer_user_story(proc_name, triggers, steps)
        if user_story:
            additions["user_story"] = user_story
        inputs_example = self._infer_inputs_example(triggers, steps)
        if inputs_example:
            additions["inputs_example"] = inputs_example
        outputs_example = self._infer_outputs_example(steps)
        if outputs_example:
            additions["outputs_example"] = outputs_example
        ui_views = self._collect_ui_views(steps)
        if ui_views:
            additions["ui_views"] = ui_views
        screenshots = self._find_local_screenshots(path, proc_name)
        if screenshots:
            additions["screenshots"] = screenshots
        data_samples = self._summarize_sql_samples(sqls)
        if data_samples:
            additions["data_samples"] = data_samples
        return additions

    def _infer_user_story(self, proc_name: str, triggers: List[Dict[str, Any]], steps: List[Dict[str, Any]]) -> str:
        action = ""
        if triggers:
            trig = triggers[0]
            if trig.get("type") == "http":
                action = f"receives HTTP {trig.get('method') or 'requests'} at {trig.get('path')}"
            else:
                action = f"is triggered by {trig.get('type')}"
        if not action:
            action = "is triggered by upstream processes"
        primary_steps = []
        for step in steps[:4]:
            connector = step.get("connector") or step.get("type")
            if connector:
                primary_steps.append(f"{step.get('name') or connector} via {connector}")
            elif step.get("name"):
                primary_steps.append(step["name"])
        if not primary_steps:
            primary_steps.append("performs orchestration and data processing")
        return f"{proc_name} {action}, then {' -> '.join(primary_steps)}."

    def _infer_inputs_example(self, triggers: List[Dict[str, Any]], steps: List[Dict[str, Any]]) -> Dict[str, Any]:
        if triggers:
            trig = triggers[0]
            return {
                "source": trig.get("type") or "trigger",
                "details": trig.get("path") or trig.get("name"),
            }
        for step in steps:
            if step.get("connector") == "jdbc" and step.get("url_or_path"):
                return {"source": "jdbc", "details": step["url_or_path"]}
        return {}

    def _infer_outputs_example(self, steps: List[Dict[str, Any]]) -> Dict[str, Any]:
        responders = []
        for step in steps:
            connector = (step.get("connector") or "").lower()
            if "http" in connector or "response" in (step.get("name") or "").lower():
                responders.append(step.get("name") or connector)
        if responders:
            return {"responders": responders[:5]}
        return {}

    def _collect_ui_views(self, steps: List[Dict[str, Any]]) -> List[str]:
        views = []
        for step in steps:
            name = (step.get("name") or "").lower()
            stype = (step.get("type") or "").lower()
            if any(keyword in name for keyword in ("ui", "screen", "page")) or any(keyword in stype for keyword in ("ui", "html")):
                views.append(step.get("name") or step.get("type"))
        return views[:5]

    def _find_local_screenshots(self, path: Path, proc_name: str) -> List[str]:
        screenshots: List[str] = []
        search_root = path.parent
        slug = _safe_name_for_file(proc_name).lower()
        for candidate in search_root.glob("*.png"):
            if slug in candidate.stem.lower():
                screenshots.append(str(candidate))
        return screenshots[:3]

    def _summarize_sql_samples(self, sqls: List[Dict[str, Any]]) -> List[Dict[str, str]]:
        samples: List[Dict[str, str]] = []
        for sql in sqls[:3]:
            stmt = sql.get("sql") or ""
            preview = redact(stmt[:400])
            if preview:
                samples.append({"statement_preview": preview})
        return samples
