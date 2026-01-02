from __future__ import annotations

from typing import Any, Dict, List, Set
import re

from autodocx.types import Signal

DATASTORE_KIND_HINTS = ("db", "sql", "table", "cosmos", "storage", "postgres", "mysql", "mongo")
SERVICE_KIND_HINTS = ("queue", "servicebus", "topic", "event", "http", "api")
IDENTIFIER_SUFFIXES = ("id", "key", "code", "number", "guid", "token")


def ensure_business_scaffold_inputs(signal: Signal) -> None:
    """Fill in triggers/steps/datastore hints when extractors omit them."""

    props: Dict[str, Any] = signal.props if isinstance(signal.props, dict) else {}
    if props is None:
        return
    relationships = _normalize_relationships(props.get("relationships") or [])
    if relationships:
        props["relationships"] = relationships

    if not props.get("steps") and relationships:
        props["steps"] = _steps_from_relationships(relationships)

    # Derive control_edges from transition relationships when extractors didn't populate them
    if not props.get("control_edges"):
        control_edges = []
        for rel in relationships:
            if rel.get("type") != "transition":
                continue
            source = (rel.get("source") or {}).get("name") or (rel.get("source") or {}).get("display")
            target = (rel.get("target") or {}).get("name") or (rel.get("target") or {}).get("display")
            if not (source and target):
                continue
            branch = (rel.get("operation") or {}).get("type") or rel.get("branch")
            control_edges.append({"parent": source, "branch": branch, "children": [target]})
        if control_edges:
            props["control_edges"] = control_edges

    if not props.get("triggers"):
        derived = _derive_triggers_from_props(props)
        if derived:
            props["triggers"] = derived

    datastores = set(_ensure_list(props.get("datastores")))
    datasource_tables = set(_ensure_list(props.get("datasource_tables")))
    services = set(_ensure_list(props.get("service_dependencies")))
    process_calls = set(_ensure_list(props.get("process_calls")))

    for rel in relationships:
        if not isinstance(rel, dict):
            continue
        target = _ensure_dict(rel.get("target"))
        if not target:
            continue
        kind = str(target.get("kind") or "").lower()
        ref = target.get("display") or target.get("ref")
        if ref and any(hint in kind for hint in DATASTORE_KIND_HINTS):
            datastores.add(ref)
            datasource_tables.add(ref)
        if ref and any(hint in kind for hint in SERVICE_KIND_HINTS):
            services.add(ref)
        if rel.get("type") in {"calls", "invokes"} and ref:
            process_calls.add(ref)

    if datastores:
        props["datastores"] = sorted(datastores)
    if datasource_tables:
        props["datasource_tables"] = sorted(datasource_tables)
    if services:
        props["service_dependencies"] = sorted(services)
    if process_calls:
        props["process_calls"] = sorted(process_calls)

    identifier_hints = set(_ensure_list(props.get("identifier_hints")))
    identifier_hints.update(_derive_identifiers_from_triggers(props.get("triggers") or []))
    identifier_hints.update(_derive_identifiers_from_route(props))
    if identifier_hints:
        props["identifier_hints"] = sorted(identifier_hints)


def _steps_from_relationships(relationships: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
    steps: List[Dict[str, Any]] = []
    for idx, rel in enumerate(relationships, start=1):
        if not isinstance(rel, dict):
            continue
        target = _ensure_dict(rel.get("target"))
        connector = rel.get("connector") or target.get("kind") or "external"
        target_name = target.get("display") or target.get("ref") or f"target_{idx}"
        step = {
            "name": f"Step_{idx}_{target_name}",
            "connector": connector,
            "target": target_name,
            "operation": (rel.get("operation") or {}).get("type"),
        }
        # carry partnerLink / module info forward if present
        for key in ("family", "module", "module_root"):
            if rel.get(key):
                step[key] = rel.get(key)
            elif target.get(key):
                step[key] = target.get(key)
        steps.append(step)
    return steps


def _derive_triggers_from_props(props: Dict[str, Any]) -> List[Dict[str, Any]]:
    triggers: List[Dict[str, Any]] = []
    method = props.get("method")
    path = props.get("path") or props.get("route")
    if method or path:
        triggers.append(
            {
                "type": "http",
                "method": (method or "GET").upper(),
                "path": path,
                "evidence": props.get("file"),
            }
        )
    for rel in props.get("relationships") or []:
        if not isinstance(rel, dict):
            continue
        if rel.get("source", {}).get("type") == "trigger":
            target = _ensure_dict(rel.get("target"))
            triggers.append(
                {
                    "type": rel.get("connector") or target.get("kind") or "trigger",
                    "method": (rel.get("operation") or {}).get("type"),
                    "path": target.get("display") or target.get("ref"),
                    "evidence": props.get("file"),
                }
            )
    return triggers


def _derive_identifiers_from_triggers(triggers: List[Dict[str, Any]]) -> Set[str]:
    hints: Set[str] = set()
    for trig in triggers:
        path = trig.get("path") or ""
        hints.update(_identifier_tokens(path))
        for key in (trig.get("name"), trig.get("type")):
            if key:
                hints.update(_identifier_tokens(key))
    return hints


def _derive_identifiers_from_route(props: Dict[str, Any]) -> Set[str]:
    hints: Set[str] = set()
    route = props.get("path") or props.get("route") or ""
    hints.update(_identifier_tokens(route))
    for sample in _ensure_list(props.get("inputs_example")) + _ensure_list(props.get("outputs_example")):
        hints.update(_identifier_tokens(sample))
    return hints


def _identifier_tokens(value: Any) -> Set[str]:
    tokens: Set[str] = set()
    if not value:
        return tokens
    for token in re.findall(r"[A-Za-z0-9_]+", str(value)):
        lower = token.lower()
        if any(lower.endswith(suffix) for suffix in IDENTIFIER_SUFFIXES):
            tokens.add(token)
    return tokens


def _ensure_list(value: Any) -> List[Any]:
    if isinstance(value, list):
        return value
    if value is None:
        return []
    return [value]


def _ensure_dict(value: Any) -> Dict[str, Any]:
    if isinstance(value, dict):
        return value
    if isinstance(value, str):
        return {"display": value}
    return {}


def _normalize_relationships(rel_list: List[Any]) -> List[Dict[str, Any]]:
    normalized: List[Dict[str, Any]] = []
    for rel in rel_list:
        if isinstance(rel, dict):
            rel_copy = dict(rel)
        elif isinstance(rel, str):
            rel_copy = {"type": "related_to", "target": {"display": rel}}
        else:
            continue

        target = rel_copy.get("target")
        if isinstance(target, str):
            rel_copy["target"] = {"display": target}
        elif not isinstance(target, dict):
            rel_copy["target"] = {}

        source = rel_copy.get("source")
        if isinstance(source, str):
            rel_copy["source"] = {"name": source}
        elif source is None:
            rel_copy["source"] = {}

        operation = rel_copy.get("operation")
        if isinstance(operation, str):
            rel_copy["operation"] = {"type": operation}

        normalized.append(rel_copy)
    return normalized
