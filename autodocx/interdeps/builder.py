from __future__ import annotations

from collections import defaultdict
import re
from typing import Any, Dict, List


def build_interdependencies(sirs: List[Dict[str, Any]]) -> Dict[str, Any]:
    nodes: Dict[str, Dict[str, Any]] = {}
    edges: List[Dict[str, Any]] = []
    component_groups: Dict[str, List[str]] = defaultdict(list)
    family_groups: Dict[str, List[str]] = defaultdict(list)
    module_groups: Dict[str, List[str]] = defaultdict(list)
    for sir in sirs:
        name = sir.get("name") or sir.get("process_name")
        if not name:
            continue
        scaffold = sir.get("business_scaffold") or {}
        io = scaffold.get("io_summary") or {}
        deps = scaffold.get("dependencies") or {}
        identifiers = io.get("identifiers") or []
        datastores = deps.get("datastores") or []
        props = sir.get("props") or {}
        component = sir.get("component_or_service") or props.get("component") or "ungrouped"
        family = (
            sir.get("family")
            or props.get("family")
            or _infer_family(name)
            or component
            or "ungrouped"
        )
        module = sir.get("module_name") or props.get("module_name") or (props.get("packaging") or {}).get("module")
        module_root = sir.get("module_root") or props.get("module_root") or (props.get("packaging") or {}).get("module_root")
        nodes[name] = {
            "component": component,
            "family": family,
            "module": module,
            "module_root": module_root,
            "identifiers": identifiers,
            "datastores": datastores,
            "interfaces": scaffold.get("interfaces") or [],
        }
        component_groups[component].append(name)
        family_groups[family].append(name)
        if module:
            module_groups[module].append(name)
        for proc in deps.get("processes") or []:
            edges.append({"from": name, "to": proc, "kind": "calls"})
    _append_shared_edges(nodes, edges, "identifiers", "shared_identifier")
    _append_shared_edges(nodes, edges, "datastores", "shared_datastore")
    for comp, procs in component_groups.items():
        if len(procs) <= 1:
            continue
        for proc in procs:
            edges.append({"from": proc, "component": comp, "kind": "component_member"})
    for fam, procs in family_groups.items():
        if len(procs) <= 1:
            continue
        for proc in procs:
            edges.append({"from": proc, "family": fam, "kind": "family_member"})
    for module_name, procs in module_groups.items():
        if len(procs) <= 1:
            continue
        for proc in procs:
            edges.append({"from": proc, "module": module_name, "kind": "module_member"})
    return {
        "nodes": nodes,
        "edges": edges,
        "components": dict(component_groups),
        "families": dict(family_groups),
        "modules": dict(module_groups),
    }


def slice_interdependencies(interdeps: Dict[str, Any], process_name: str) -> Dict[str, List[str]]:
    related = {
        "calls": [],
        "called_by": [],
        "shared_identifiers_with": [],
        "shared_datastores_with": [],
        "component_peers": [],
        "family_peers": [],
    }
    for edge in interdeps.get("edges", []):
        if edge.get("kind") == "calls":
            if edge.get("from") == process_name:
                related["calls"].append(edge.get("to"))
            elif edge.get("to") == process_name:
                related["called_by"].append(edge.get("from"))
        if edge.get("kind") == "shared_identifier" and edge.get("from") == process_name:
            related["shared_identifiers_with"].append(edge.get("to"))
        if edge.get("kind") == "shared_datastore" and edge.get("from") == process_name:
            related["shared_datastores_with"].append(edge.get("to"))
        if edge.get("kind") == "component_member" and edge.get("component"):
            comp = edge["component"]
            if process_name in interdeps.get("components", {}).get(comp, []):
                peers = [p for p in interdeps["components"][comp] if p != process_name]
                related["component_peers"].extend(peers)
        if edge.get("kind") == "family_member" and edge.get("family"):
            fam = edge["family"]
            if process_name in interdeps.get("families", {}).get(fam, []):
                peers = [p for p in interdeps["families"][fam] if p != process_name]
                related["family_peers"].extend(peers)
    for key in related:
        related[key] = sorted({item for item in related[key] if item})
    return related


def _append_shared_edges(nodes: Dict[str, Dict[str, Any]], edges: List[Dict[str, Any]], key: str, edge_kind: str) -> None:
    lookup: Dict[str, List[str]] = defaultdict(list)
    for name, node in nodes.items():
        for value in node.get(key) or []:
            lookup[value].append(name)
    for value, procs in lookup.items():
        if len(procs) <= 1:
            continue
        for proc in procs:
            for other in procs:
                if proc == other:
                    continue
                edges.append({"from": proc, "to": other, "kind": edge_kind, "value": value})


_FAMILY_PATTERNS = [
    re.compile(r"^(?P<fam>[A-Za-z0-9]+(?:\.[A-Za-z0-9]+)+)\.(?P<leaf>[^.]+)$"),
    re.compile(r"^(?P<fam>[A-Za-z0-9]+)\.(?P<leaf>[^.]+)$"),
]


def _infer_family(process_name: str | None) -> str | None:
    if not process_name:
        return None
    for pattern in _FAMILY_PATTERNS:
        match = pattern.match(process_name)
        if match:
            return match.group("fam").lower()
    return None
