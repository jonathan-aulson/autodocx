from __future__ import annotations

import json
from pathlib import Path
from typing import Any, Dict, List
import re

try:
    from lxml import etree
except ModuleNotFoundError:  # pragma: no cover - fallback when lxml not installed
    etree = None  # type: ignore[assignment]

NS_WSDL = {
    "wsdl": "http://schemas.xmlsoap.org/wsdl/",
    "wsdl2": "http://www.w3.org/ns/wsdl",
    "xs": "http://www.w3.org/2001/XMLSchema",
}


PB_ENDPOINT_PATTERN = re.compile(r"https?://[^\s\"']{8,160}", re.IGNORECASE)


def enrich_project_artifacts(repo_root: Path) -> Dict[str, Any]:
    repo_root = repo_root.resolve()
    rest = _collect_openapi_endpoints(repo_root)
    wsdl_ops = _collect_wsdl_operations(repo_root)
    xsd_items = _collect_xsd_glossary(repo_root)
    pb_endpoints = _collect_pb_endpoints(repo_root)
    enrichment: Dict[str, Any] = {}
    if rest:
        enrichment["rest_endpoints"] = rest
    if wsdl_ops:
        enrichment["wsdl_operations"] = wsdl_ops
    if xsd_items:
        enrichment["xsd_glossary"] = xsd_items
    if pb_endpoints:
        enrichment["pb_project_endpoints"] = pb_endpoints
    return enrichment


def _collect_openapi_endpoints(repo_root: Path) -> List[Dict[str, Any]]:
    endpoints: List[Dict[str, Any]] = []
    for json_path in repo_root.rglob("*.json"):
        try:
            data = json.loads(json_path.read_text(encoding="utf-8"))
        except Exception:
            continue
        if not isinstance(data, dict):
            continue
        if not (data.get("openapi") or data.get("swagger")):
            continue
        paths = data.get("paths")
        if not isinstance(paths, dict):
            continue
        for route, methods in paths.items():
            if not isinstance(methods, dict):
                continue
            for method, details in methods.items():
                if not isinstance(details, dict):
                    continue
                endpoints.append(
                    {
                        "path": route,
                        "method": method.upper(),
                        "summary": details.get("summary"),
                        "operationId": details.get("operationId"),
                        "source": str(json_path.relative_to(repo_root)),
                    }
                )
    return endpoints


def _collect_wsdl_operations(repo_root: Path) -> List[Dict[str, Any]]:
    if etree is None:
        return []
    operations: List[Dict[str, Any]] = []
    for wsdl_path in repo_root.rglob("*.wsdl"):
        try:
            root = etree.parse(str(wsdl_path)).getroot()
        except Exception:
            continue
        for op in root.xpath("//wsdl:portType/wsdl:operation", namespaces=NS_WSDL):
            operations.append(
                {
                    "portType": op.getparent().get("name") if op.getparent() is not None else None,
                    "operation": op.get("name"),
                    "source": str(wsdl_path.relative_to(repo_root)),
                }
            )
        for op in root.xpath("//wsdl2:operation", namespaces=NS_WSDL):
            operations.append(
                {
                    "portType": op.getparent().get("name") if op.getparent() is not None else None,
                    "operation": op.get("name"),
                    "source": str(wsdl_path.relative_to(repo_root)),
                }
            )
    return operations


def _collect_xsd_glossary(repo_root: Path) -> Dict[str, List[Dict[str, Any]]]:
    if etree is None:
        return {}
    elements: List[Dict[str, Any]] = []
    complex_types: List[Dict[str, Any]] = []
    enums: List[Dict[str, Any]] = []
    docs: List[Dict[str, Any]] = []
    for xsd_path in repo_root.rglob("*.xsd"):
        try:
            root = etree.parse(str(xsd_path)).getroot()
        except Exception:
            continue
        for el in root.xpath("//xs:element", namespaces=NS_WSDL):
            if el.get("name"):
                elements.append(
                    {
                        "name": el.get("name"),
                        "type": el.get("type"),
                        "source": str(xsd_path.relative_to(repo_root)),
                    }
                )
        for ct in root.xpath("//xs:complexType", namespaces=NS_WSDL):
            if ct.get("name"):
                complex_types.append(
                    {
                        "name": ct.get("name"),
                        "source": str(xsd_path.relative_to(repo_root)),
                    }
                )
        for st in root.xpath("//xs:simpleType", namespaces=NS_WSDL):
            type_name = st.get("name")
            for enum in st.xpath(".//xs:enumeration", namespaces=NS_WSDL):
                if enum.get("value"):
                    enums.append(
                        {
                            "type": type_name,
                            "value": enum.get("value"),
                            "source": str(xsd_path.relative_to(repo_root)),
                        }
                    )
        for ann in root.xpath("//xs:annotation/xs:documentation", namespaces=NS_WSDL):
            text = (ann.text or "").strip()
            if text:
                docs.append({"documentation": text[:400], "source": str(xsd_path.relative_to(repo_root))})
    glossary: Dict[str, List[Dict[str, Any]]] = {}
    if elements:
        glossary["elements"] = elements
    if complex_types:
        glossary["complex_types"] = complex_types
    if enums:
        glossary["enums"] = enums
    if docs:
        glossary["documentation"] = docs
    return glossary


def _collect_pb_endpoints(repo_root: Path) -> List[Dict[str, Any]]:
    endpoints: List[Dict[str, Any]] = []
    seen: set[tuple[str, str]] = set()
    for pattern in ("*.ini", "*.json"):
        for candidate in repo_root.rglob(pattern):
            try:
                text = candidate.read_text(encoding="utf-8", errors="ignore")
            except Exception:
                continue
            for match in PB_ENDPOINT_PATTERN.findall(text):
                rel = str(candidate.relative_to(repo_root)) if candidate.is_relative_to(repo_root) else str(candidate)
                key = (match, rel)
                if key in seen:
                    continue
                seen.add(key)
                endpoints.append({"endpoint": match, "file": rel})
    return endpoints[:50]
