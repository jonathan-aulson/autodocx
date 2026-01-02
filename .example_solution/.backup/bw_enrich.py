#!/usr/bin/env python3
"""
bw_enrich.py
Evidence-first enrichment for BW SIR:
- REST (OpenAPI/Swagger JSON)
- SOAP WSDL operations
- XSD glossary (annotations, enums)
- SQL/JDBC statements from process activities
- JMS destinations (queue/topic)
- Timer cadences (cron/interval)
- Transition conditions/labels (decision context)
- Mapper hints (from/to XPaths, constants, functions used)

All functions return dicts suitable to merge under sir["enrichment"] and/or sir["resources"].
They never infer beyond what is observed, and include evidence pointers.

Usage patterns:
- Project-level artifacts (REST/WSDL/XSD):
    enr = enrich_project_artifacts(project_root)
- Per-process enrichments (SQL/JMS/Timer/Transitions/Mappers):
    enr_proc = enrich_process(root_element, xml_path)

Requires: lxml, re, json, pathlib
"""

from __future__ import annotations
import json
import re
from pathlib import Path
from typing import Dict, List, Tuple, Any
from lxml import etree as ET

# Namespaces commonly used
NS = {
    "wsdl": "http://schemas.xmlsoap.org/wsdl/",
    "xs": "http://www.w3.org/2001/XMLSchema",
    # WSDL 2.0 (less common)
    "wsdl2": "http://www.w3.org/ns/wsdl",
}

# Helpers
def evidence(file: Path, xpath: str = "", json_ptr: str = "", snippet: str = "") -> Dict[str, str]:
    out = {"file": str(file.as_posix())}
    if xpath:
        out["xpath"] = xpath
    if json_ptr:
        out["json_pointer"] = json_ptr
    if snippet:
        # Keep short to avoid bloating docs; caller should truncate if needed
        out["snippet"] = snippet.strip()[:400]
    return out

def text_or_none(el: Any) -> str | None:
    if el is None:
        return None
    t = el.text if hasattr(el, "text") else None
    return (t or "").strip() or None

def first(el: Any, default=None):
    try:
        return el[0]
    except Exception:
        return default

# 1) REST: OpenAPI/Swagger JSON (Service Descriptors)
def extract_openapi_from_json(json_path: Path) -> List[Dict[str, Any]]:
    """
    Returns a list of endpoint dicts:
      { path, method, operationId, summary, tags, evidence }
    Only if file has 'openapi' or 'swagger' top-level keys.
    """
    endpoints: List[Dict[str, Any]] = []
    try:
        data = json.loads(json_path.read_text(encoding="utf-8"))
    except Exception:
        return endpoints
    if not isinstance(data, dict) or not (("openapi" in data) or ("swagger" in data)):
        return endpoints
    paths = data.get("paths", {})
    if not isinstance(paths, dict):
        return endpoints
    for pth, methods in paths.items():
        if not isinstance(methods, dict):
            continue
        for mth, op in methods.items():
            if not isinstance(op, dict):
                continue
            op_id = op.get("operationId")
            summary = op.get("summary")
            tags = op.get("tags", [])
            endpoints.append({
                "path": pth,
                "method": mth.upper(),
                "operationId": op_id,
                "summary": summary,
                "tags": tags if isinstance(tags, list) else [],
                "evidence": evidence(json_path, json_ptr=f"/paths/{pth}/{mth}")
            })
    return endpoints

def enrich_openapi_in_project(project_root: Path) -> Dict[str, Any]:
    """
    Scan for JSON files likely to be Service Descriptors or OpenAPI files.
    Heuristic: any *.json under 'Service Descriptors' or whole project.
    """
    files = list(project_root.rglob("*.json"))
    endpoints: List[Dict[str, Any]] = []
    for f in files:
        if "Service Descriptors" in str(f) or True:
            endpoints.extend(extract_openapi_from_json(f))
    return {"rest_endpoints": endpoints} if endpoints else {}

# 2) WSDL operations (SOAP)
def enrich_wsdl_in_project(project_root: Path) -> Dict[str, Any]:
    """
    Parse *.wsdl files, list operations:
      { service?, portType, operation, input?, output?, documentation?, evidence }
    """
    items: List[Dict[str, Any]] = []
    for wsdl_path in project_root.rglob("*.wsdl"):
        try:
            root = ET.parse(str(wsdl_path), ET.XMLParser(recover=True)).getroot()
        except Exception:
            continue
        # WSDL 1.1
        for op in root.xpath("//wsdl:portType/wsdl:operation", namespaces=NS):
            name = op.get("name")
            doc = op.find("wsdl:documentation", namespaces=NS)
            doc_txt = text_or_none(doc)
            inp = op.find("wsdl:input", namespaces=NS)
            outp = op.find("wsdl:output", namespaces=NS)
            items.append({
                "portType": op.getparent().get("name") if op.getparent() is not None else None,
                "operation": name,
                "input_message": inp.get("message") if inp is not None else None,
                "output_message": outp.get("message") if outp is not None else None,
                "documentation": doc_txt,
                "evidence": evidence(wsdl_path, xpath=f"//wsdl:portType/wsdl:operation[@name='{name}']"),
            })
        # Optionally, WSDL 1.1 service/port names
        for svc in root.xpath("//wsdl:service", namespaces=NS):
            items.append({
                "service": svc.get("name"),
                "evidence": evidence(wsdl_path, xpath=f"//wsdl:service[@name='{svc.get('name')}']"),
            })
        # WSDL 2.0 (fallback)
        for op in root.xpath("//wsdl2:operation", namespaces=NS):
            name = op.get("name")
            items.append({
                "operation": name,
                "evidence": evidence(wsdl_path, xpath=f"//wsdl2:operation[@name='{name}']"),
            })
    return {"wsdl_operations": items} if items else {}

# 3) XSD glossary (annotations, enums, entity names)
def enrich_xsd_in_project(project_root: Path) -> Dict[str, Any]:
    """
    Extracts:
      - elements: name, type
      - complexTypes: name
      - simpleType enums
      - annotations/documentation
    """
    elements: List[Dict[str, Any]] = []
    complex_types: List[Dict[str, Any]] = []
    enums: List[Dict[str, Any]] = []
    docs: List[Dict[str, Any]] = []

    for xsd_path in project_root.rglob("*.xsd"):
        try:
            root = ET.parse(str(xsd_path), ET.XMLParser(recover=True)).getroot()
        except Exception:
            continue
        # Elements
        for el in root.xpath("//xs:element", namespaces=NS):
            nm = el.get("name")
            tp = el.get("type")
            if nm:
                elements.append({
                    "element": nm,
                    "type": tp,
                    "evidence": evidence(xsd_path, xpath=f"//xs:element[@name='{nm}']")
                })
        # complexTypes
        for ct in root.xpath("//xs:complexType", namespaces=NS):
            nm = ct.get("name")
            if nm:
                complex_types.append({
                    "complexType": nm,
                    "evidence": evidence(xsd_path, xpath=f"//xs:complexType[@name='{nm}']")
                })
        # simpleType enums
        for st in root.xpath("//xs:simpleType", namespaces=NS):
            nm = st.get("name")
            for en in st.xpath(".//xs:enumeration", namespaces=NS):
                val = en.get("value")
                if val is not None:
                    enums.append({
                        "type": nm,
                        "value": val,
                        "evidence": evidence(xsd_path, xpath=f"//xs:simpleType[@name='{nm}']//xs:enumeration[@value='{val}']")
                    })
        # annotations
        for ann in root.xpath("//xs:annotation/xs:documentation", namespaces=NS):
            txt = text_or_none(ann)
            if txt:
                docs.append({
                    "documentation": txt,
                    "evidence": evidence(xsd_path, xpath="//xs:annotation/xs:documentation")
                })

    out: Dict[str, Any] = {}
    if elements: out["xsd_elements"] = elements
    if complex_types: out["xsd_complex_types"] = complex_types
    if enums: out["xsd_enums"] = enums
    if docs: out["xsd_documentation"] = docs
    return out

# 4) Per-process enrichments
SQL_KEYWORDS = re.compile(r"\b(select|insert|update|delete|merge|call)\b", re.IGNORECASE)

def _collect_text_nodes(el: ET._Element) -> List[str]:
    texts = []
    # element text
    if el.text and el.text.strip():
        texts.append(el.text.strip())
    # attributes that may carry SQL or config
    for k, v in el.attrib.items():
        if isinstance(v, str) and v.strip():
            texts.append(v.strip())
    # recurse
    for ch in el:
        texts.extend(_collect_text_nodes(ch))
    return texts

def enrich_jdbc_sql(root: ET._Element, xml_path: Path) -> Dict[str, Any]:
    """
    Find JDBC activities and capture any SQL-like statement text.
    XPath patterns:
      //*[local-name()='activity' and starts-with(@type,'jdbc:')]
        - search descendant text/attributes containing SQL keywords
        - also try child elements named 'sql' or 'statement'
    """
    items: List[Dict[str, Any]] = []
    for act in root.xpath("//*[local-name()='activity' and starts-with(@type,'jdbc:')]"):
        name = act.get("name") or act.get("Name")
        # collect plausible SQL snippets
        snippets = set()
        # direct children named sql/statement
        for s in act.xpath(".//*[local-name()='sql' or local-name()='statement']"):
            txt = text_or_none(s)
            if txt and SQL_KEYWORDS.search(txt):
                snippets.add(txt)
        # scan all text/attrs under the activity
        for t in _collect_text_nodes(act):
            if SQL_KEYWORDS.search(t):
                snippets.add(t)
        for sn in list(snippets)[:3]:  # cap to avoid large outputs
            items.append({
                "activity": name,
                "type": act.get("type"),
                "sql": sn,
                "evidence": evidence(xml_path, xpath=f"//*[local-name()='activity' and @name='{name}']")
            })
    return {"jdbc_sql": items} if items else {}

def enrich_jms_destinations(root: ET._Element, xml_path: Path) -> Dict[str, Any]:
    """
    JMS activities: //*[local-name()='activity' and starts-with(@type,'jms:')]
    Pull destination names from common properties:
      queueName, topicName, destination, destinationName
    """
    items: List[Dict[str, Any]] = []
    for act in root.xpath("//*[local-name()='activity' and starts-with(@type,'jms:')]"):
        name = act.get("name") or act.get("Name")
        # try attributes
        attrs = act.attrib
        dest = attrs.get("queueName") or attrs.get("topicName") or attrs.get("destination") or attrs.get("destinationName")
        # try child elements
        if not dest:
            for tag in ["queueName", "topicName", "destination", "destinationName"]:
                node = first(act.xpath(f".//*[local-name()='{tag}']"))
                if node is not None and text_or_none(node):
                    dest = text_or_none(node)
                    break
        if dest:
            items.append({
                "activity": name,
                "type": act.get("type"),
                "destination": dest,
                "evidence": evidence(xml_path, xpath=f"//*[local-name()='activity' and @name='{name}']")
            })
    return {"jms_destinations": items} if items else {}

def enrich_timer_cadence(root: ET._Element, xml_path: Path) -> Dict[str, Any]:
    """
    Timer activities: //*[local-name()='activity' and @type='timer:TimerEvent']
    Look for cronExpression, repeatInterval, fixedRate, delay, startTime.
    """
    items: List[Dict[str, Any]] = []
    for act in root.xpath("//*[local-name()='activity' and @type='timer:TimerEvent']"):
        name = act.get("name") or act.get("Name")
        fields = {}
        for tag in ["cronExpression", "repeatInterval", "fixedRate", "delay", "startTime", "timeZone"]:
            # attr first
            val = act.get(tag)
            if not val:
                node = first(act.xpath(f".//*[local-name()='{tag}']"))
                val = text_or_none(node)
            if val:
                fields[tag] = val
        if fields:
            items.append({
                "activity": name,
                "fields": fields,
                "evidence": evidence(xml_path, xpath=f"//*[local-name()='activity' and @name='{name}']")
            })
    return {"timers": items} if items else {}

def enrich_transition_conditions(root: ET._Element, xml_path: Path) -> Dict[str, Any]:
    """
    Transitions with conditions/labels:
      //*[local-name()='transition'] -> @from, @to, @condition, @label
      Also look for child <condition> text and attributes like isErrorPath.
    """
    items: List[Dict[str, Any]] = []
    for t in root.xpath("//*[local-name()='transition']"):
        frm = t.get("from") or t.get("From")
        to = t.get("to") or t.get("To")
        cond = t.get("condition") or t.get("Condition")
        label = t.get("label") or t.get("Label") or t.get("name") or t.get("Name")
        # child <condition> element
        if not cond:
            cnode = first(t.xpath(".//*[local-name()='condition']"))
            cond = text_or_none(cnode)
        # error-path hint
        is_error = False
        for attr in ["isErrorPath", "IsErrorPath", "error", "Error"]:
            if t.get(attr) in ("true", "True", "1"):
                is_error = True
                break
        items.append({
            "from": frm,
            "to": to,
            "condition": cond,
            "label": label,
            "is_error_path": is_error,
            "evidence": evidence(xml_path, xpath=f"//*[local-name()='transition' and @from='{frm}' and @to='{to}']")
        })
    # Filter empty if nothing meaningful found
    meaningful = [i for i in items if any([i.get("condition"), i.get("label"), i.get("is_error_path")])]
    return {"transition_conditions": meaningful} if meaningful else {}

MAPPER_FN_HINTS = re.compile(r"\b(concat|substring|upper-case|lower-case|format|replace|tokenize)\s*\(", re.IGNORECASE)
XPATH_HINT = re.compile(r"(/|\.\.|::)")

def enrich_mapper_hints(root: ET._Element, xml_path: Path) -> Dict[str, Any]:
    """
    Mapper activity type: mapper:Mapper.
    We heuristically collect:
      - XPath-lookalike strings (from/to)
      - Constants (string/numeric literals)
      - Functions used (concat, substring, etc.)
    Patterns:
      - //*[local-name()='activity' and @type='mapper:Mapper']//*[local-name()='map' or local-name()='mapping' or local-name()='expression' or local-name()='assign']
    """
    items: List[Dict[str, Any]] = []
    for act in root.xpath("//*[local-name()='activity' and @type='mapper:Mapper']"):
        name = act.get("name") or act.get("Name")
        paths: List[str] = []
        consts: List[str] = []
        fns: List[str] = []
        # scan mapping-like nodes
        for node in act.xpath(".//*[local-name()='map' or local-name()='mapping' or local-name()='expression' or local-name()='assign' or local-name()='function']"):
            # attributes possibly containing XPaths
            for k, v in node.attrib.items():
                if not v or not isinstance(v, str):
                    continue
                v = v.strip()
                if XPATH_HINT.search(v):
                    paths.append(v)
                # function detection
                for m in MAPPER_FN_HINTS.findall(v):
                    fns.append(m)
            # inner text could also have expressions
            txt = text_or_none(node)
            if txt:
                if XPATH_HINT.search(txt):
                    paths.append(txt)
                for m in MAPPER_FN_HINTS.findall(txt):
                    fns.append(m)
                # constants: quoted strings
                for s in re.findall(r"'([^']{1,80})'|\"([^\"]{1,80})\"", txt):
                    const = s[0] or s[1]
                    if const:
                        consts.append(const)
        if paths or consts or fns:
            items.append({
                "activity": name,
                "paths": list(dict.fromkeys(paths))[:10],      # dedupe + cap
                "functions": sorted(set([f.lower() for f in fns]))[:10],
                "constants": list(dict.fromkeys(consts))[:10],
                "evidence": evidence(xml_path, xpath=f"//*[local-name()='activity' and @name='{name}']")
            })
    return {"mapper_hints": items} if items else {}

# Public combinators

def enrich_project_artifacts(project_root: Path) -> Dict[str, Any]:
    """
    Project-wide enrichment: REST (OpenAPI), WSDL, XSD.
    """
    rest = enrich_openapi_in_project(project_root)
    wsdl = enrich_wsdl_in_project(project_root)
    xsd = enrich_xsd_in_project(project_root)
    out: Dict[str, Any] = {}
    out.update(rest)
    out.update(wsdl)
    out.update(xsd)
    return out

def enrich_process(root: ET._Element, xml_path: Path) -> Dict[str, Any]:
    """
    Per-process enrichment: SQL/JMS/Timers/Transition conditions/Mappers.
    """
    out: Dict[str, Any] = {}
    out.update(enrich_jdbc_sql(root, xml_path))
    out.update(enrich_jms_destinations(root, xml_path))
    out.update(enrich_timer_cadence(root, xml_path))
    out.update(enrich_transition_conditions(root, xml_path))
    out.update(enrich_mapper_hints(root, xml_path))
    return out

