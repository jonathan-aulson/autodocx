from __future__ import annotations

from collections import defaultdict
import re
from typing import Any, Dict, Iterable, List, Sequence, Set

from autodocx.types import Signal


ROLE_PREFIX_MAP = {
    "http": ["interface.receive", "interface.reply"],
    "rest": ["interface.receive", "interface.reply"],
    "soap": ["interface.receive", "interface.reply"],
    "api": ["interface.receive", "interface.reply"],
    "trigger": ["interface.receive"],
    "process": ["invoke.process"],
    "bw": ["invoke.process"],
    "logicapps": ["invoke.process"],
    "azure": ["invoke.process"],
    "pb": ["invoke.process"],
    "jdbc": ["data.jdbc"],
    "sql": ["data.jdbc"],
    "jms": ["messaging.jms"],
    "sb": ["messaging.jms"],
    "mapper": ["transform.mapper"],
    "timer": ["schedule.timer"],
    "schedule": ["schedule.timer"],
    "log": ["ops.log"],
    "trace": ["ops.log"],
    "error": ["error.throw"],
    "exception": ["error.throw"],
}

ROLE_CONNECTOR_MAP = {
    "pb:ui_event": ["interface.receive"],
    "pb:method_entry": ["invoke.process"],
    "pb:method_call": ["invoke.process"],
    "pb:db_exec": ["data.jdbc"],
    "pb:datawindow_op": ["data.jdbc"],
    "pb:datawindow_sql": ["data.jdbc"],
    "pb:http_request": ["interface.receive", "interface.reply"],
    "pb:soap_call": ["interface.receive", "interface.reply"],
    "pb:ribbon_event": ["interface.receive"],
    "pb:external_function": ["invoke.process"],
    "pb:workflow": ["invoke.process"],
}

IDENTIFIER_KEYWORDS = ("id", "key", "number", "code", "guid", "token")


def build_scaffold(signal: Signal) -> Dict[str, Any]:
    props: Dict[str, Any] = signal.props if isinstance(signal.props, dict) else {}
    steps = _ensure_list(props.get("steps"))
    triggers = _ensure_list(props.get("triggers"))
    enrichment = props.get("enrichment") or {}
    control_edges = _ensure_list(props.get("control_edges"))
    identifier_hints = _gather_identifier_hints(props, enrichment)
    dependency_hints = _gather_dependency_hints(props, enrichment)
    interfaces = _collect_interfaces(triggers, steps, enrichment)
    invocations = _collect_invocations(steps, enrichment)
    dependencies = _collect_dependencies(invocations, enrichment, props, dependency_hints)
    io_summary = _collect_io_summary(props, enrichment, identifier_hints)
    errors = _collect_errors(steps, enrichment)
    logging = _collect_logging(steps)
    traceability = _build_traceability(interfaces, steps, invocations, dependencies)
    start_nodes = _find_start_nodes(triggers, steps, control_edges)
    return {
        "interfaces": interfaces,
        "invocations": invocations,
        "dependencies": dependencies,
        "io_summary": io_summary,
        "errors": errors,
        "logging": logging,
        "traceability": traceability,
        "start_nodes": start_nodes,
    }


def _collect_interfaces(
    triggers: Sequence[Dict[str, Any]], steps: Sequence[Dict[str, Any]], enrichment: Dict[str, Any]
) -> List[Dict[str, Any]]:
    interfaces: List[Dict[str, Any]] = []
    for trig in triggers:
        interfaces.append(
            {
                "kind": trig.get("type") or "trigger",
                "endpoint": trig.get("path") or (trig.get("properties") or {}).get("path"),
                "method": trig.get("method") or (trig.get("properties") or {}).get("method"),
                "evidence": trig.get("evidence") or trig.get("name"),
            }
        )
    for step in steps:
        hints = _role_hints(step)
        if hints.get("interface.receive") or hints.get("interface.reply"):
            interfaces.append(
                {
                    "kind": step.get("connector") or step.get("type") or "interface",
                    "endpoint": step.get("path") or (step.get("properties") or {}).get("path"),
                    "method": step.get("method") or (step.get("properties") or {}).get("method"),
                    "evidence": step.get("name"),
                }
            )
    for rest in enrichment.get("rest_endpoints") or []:
        interfaces.append(
            {
                "kind": rest.get("kind") or "REST",
                "endpoint": rest.get("path"),
                "method": rest.get("method"),
                "operation": rest.get("operationId"),
                "evidence": rest.get("evidence") or rest.get("operationId"),
            }
        )
    for svc in enrichment.get("bw_services") or []:
        interfaces.append(
            {
                "kind": svc.get("kind") or svc.get("connector") or "interface",
                "endpoint": svc.get("endpoint"),
                "method": svc.get("method"),
                "operation": svc.get("operation"),
                "evidence": svc.get("evidence"),
            }
        )
    return _dedupe_dicts(interfaces, ("kind", "endpoint", "method"))[:12]


def _collect_invocations(steps: Sequence[Dict[str, Any]], enrichment: Dict[str, Any]) -> List[Dict[str, Any]]:
    invocations: List[Dict[str, Any]] = []
    for step in steps:
        hints = _role_hints(step)
        connector = step.get("connector") or step.get("type") or ""
        name = step.get("name")
        if hints.get("invoke.process"):
            invocations.append(
                {
                    "kind": "Process",
                    "target": step.get("called_process") or step.get("target") or name,
                    "operation": step.get("operation"),
                    "connector": connector,
                    "evidence": name,
                }
            )
        if hints.get("data.jdbc"):
            invocations.append(
                {
                    "kind": "JDBC",
                    "target": step.get("datasource") or step.get("database"),
                    "operation": step.get("operation") or step.get("sql"),
                    "connector": connector,
                    "evidence": name,
                }
            )
        if hints.get("messaging.jms"):
            invocations.append(
                {
                    "kind": "JMS",
                    "target": step.get("destination") or step.get("queue") or step.get("topic"),
                    "operation": step.get("operation"),
                    "connector": connector,
                    "evidence": name,
                }
            )
    for entry in enrichment.get("jms_destinations") or []:
        invocations.append(
            {
                "kind": "JMS",
                "target": entry.get("destination"),
                "operation": entry.get("connector"),
                "connector": entry.get("connector"),
                "evidence": entry.get("activity"),
            }
        )
    for entry in enrichment.get("jdbc_sql") or []:
        invocations.append(
            {
                "kind": "JDBC",
                "target": entry.get("datasource"),
                "operation": entry.get("sql"),
                "connector": "jdbc",
                "evidence": entry.get("activity"),
            }
        )
    for entry in enrichment.get("bw_invocations") or []:
        invocations.append(
            {
                "kind": entry.get("kind") or entry.get("connector"),
                "target": entry.get("target"),
                "operation": entry.get("operation"),
                "connector": entry.get("connector"),
                "evidence": entry.get("evidence"),
            }
        )
    return _dedupe_dicts(invocations, ("kind", "target", "operation"))[:20]


def _gather_identifier_hints(props: Dict[str, Any], enrichment: Dict[str, Any]) -> List[str]:
    hints: Set[str] = set()
    for src in (
        props.get("identifier_hints"),
        props.get("io_identifiers"),
        props.get("identifiers"),
        props.get("foreign_keys"),
        props.get("primary_keys"),
    ):
        for value in _ensure_list(src):
            val = str(value).strip()
            if val:
                hints.add(val)
    for entry in enrichment.get("mapper_hints") or []:
        for token in entry.get("identifiers") or []:
            tok = str(token).strip()
            if tok:
                hints.add(tok)
        for path in entry.get("paths") or []:
            token = _identifier_from_path(path)
            if token:
                hints.add(token)
    for token in _ensure_list(enrichment.get("identifier_hints")):
        val = str(token).strip()
        if val:
            hints.add(val)
    return sorted(hints)


def _gather_dependency_hints(props: Dict[str, Any], enrichment: Dict[str, Any]) -> Dict[str, Set[str]]:
    hints: Dict[str, Set[str]] = {"datastores": set(), "processes": set(), "services": set()}
    for key in ("datasource_tables", "datastores", "datasource_refs"):
        for value in _ensure_list(props.get(key)):
            val = str(value).strip()
            if val:
                hints["datastores"].add(val)
    for key in ("process_calls", "calls_flows"):
        for value in _ensure_list(props.get(key)):
            val = str(value).strip()
            if val:
                hints["processes"].add(val)
    for value in _ensure_list(props.get("service_dependencies")):
        val = str(value).strip()
        if val:
            hints["services"].add(val)
    for entry in enrichment.get("jdbc_sql") or []:
        table = entry.get("table")
        if table:
            hints["datastores"].add(str(table))
    for entry in enrichment.get("datasource_tables") or []:
        val = str(entry).strip()
        if val:
            hints["datastores"].add(val)
    for entry in enrichment.get("process_calls") or []:
        val = str(entry).strip()
        if val:
            hints["processes"].add(val)
    for entry in enrichment.get("service_dependencies") or []:
        val = str(entry).strip()
        if val:
            hints["services"].add(val)
    return hints


def _collect_dependencies(
    invocations: Sequence[Dict[str, Any]],
    enrichment: Dict[str, Any],
    props: Dict[str, Any],
    extra_hints: Dict[str, Set[str]],
) -> Dict[str, List[str]]:
    deps = defaultdict(list)
    for inv in invocations:
        target = inv.get("target")
        if not target:
            continue
        if inv["kind"] == "Process":
            deps["processes"].append(target)
        elif inv["kind"] == "JMS":
            deps["services"].append(target)
        elif inv["kind"] == "JDBC":
            deps["datastores"].append(target)
    for jdbc in enrichment.get("jdbc_sql") or []:
        if jdbc.get("datasource"):
            deps["datastores"].append(jdbc["datasource"])
        if jdbc.get("table"):
            deps["datastores"].append(jdbc["table"])
    for svc in enrichment.get("bw_services") or []:
        if svc.get("endpoint"):
            deps["services"].append(svc["endpoint"])
    for ds in enrichment.get("datastores") or []:
        deps["datastores"].append(ds)
    for jdbc in enrichment.get("jdbc_sql") or []:
        if jdbc.get("datasource"):
            deps["datastores"].append(jdbc["datasource"])
        if jdbc.get("table"):
            deps["datastores"].append(jdbc["table"])
    for rel in (props.get("relationships") or []):
        target_name = (rel.get("target") or {}).get("name") if isinstance(rel.get("target"), dict) else rel.get("target")
        if rel.get("type") in {"calls", "invokes"} and target_name:
            deps["processes"].append(target_name)
        target_kind = (rel.get("target") or {}).get("kind") if isinstance(rel.get("target"), dict) else rel.get("target_kind")
        target_display = (rel.get("target") or {}).get("display") if isinstance(rel.get("target"), dict) else rel.get("display")
        if target_kind and any(token in str(target_kind).lower() for token in ("db", "sql", "table", "datastore", "cosmos", "mongo", "postgres", "mysql")):
            ref = target_name or target_display
            if ref:
                deps["datastores"].append(ref)
        if target_kind and any(token in str(target_kind).lower() for token in ("service", "api", "http", "queue", "topic")):
            ref = target_name or target_display
            if ref:
                deps["services"].append(ref)
    for data_store in props.get("datastores") or []:
        deps["datastores"].append(data_store)
    for bucket, values in extra_hints.items():
        for value in values:
            deps[bucket].append(value)
    return {k: sorted({v for v in values if v}) for k, values in deps.items()}


def _collect_io_summary(props: Dict[str, Any], enrichment: Dict[str, Any], identifier_hints: Sequence[str]) -> Dict[str, List[str]]:
    inputs = _ensure_list(props.get("inputs") or props.get("inputs_example") or enrichment.get("inputs"))
    outputs = _ensure_list(props.get("outputs") or props.get("outputs_example") or enrichment.get("outputs"))
    identifiers = set(identifier_hints or enrichment.get("identifiers") or [])
    return {
        "inputs": inputs[:10],
        "outputs": outputs[:10],
        "identifiers": sorted(identifiers)[:12],
    }


def _collect_errors(steps: Sequence[Dict[str, Any]], enrichment: Dict[str, Any]) -> List[Dict[str, Any]]:
    errors: List[Dict[str, Any]] = []
    for step in steps:
        hints = _role_hints(step)
        if hints.get("error.throw") or "error" in (step.get("name") or "").lower():
            errors.append(
                {
                    "activity": step.get("name"),
                    "condition": step.get("condition") or step.get("message"),
                    "evidence": step.get("evidence"),
                }
            )
    for transition in enrichment.get("transition_conditions") or []:
        errors.append(
            {
                "activity": transition.get("from"),
                "condition": transition.get("condition"),
                "evidence": transition.get("evidence"),
            }
        )
    if not errors:
        # Observability taxonomy defaults by connector
        for step in steps:
            connector = (step.get("connector") or "").lower()
            if connector in {"http", "rest", "soap"}:
                errors.append({"activity": step.get("name"), "condition": "http_error_handling", "evidence": step.get("evidence")})
            elif connector in {"jdbc", "sql", "db"}:
                errors.append({"activity": step.get("name"), "condition": "db_error_handling", "evidence": step.get("evidence")})
            elif connector in {"jms", "queue", "topic"}:
                errors.append({"activity": step.get("name"), "condition": "messaging_error_handling", "evidence": step.get("evidence")})
            if len(errors) >= 3:
                break
    return _dedupe_dicts(errors, ("activity", "condition"))[:10]


def _collect_logging(steps: Sequence[Dict[str, Any]]) -> List[Dict[str, Any]]:
    logs: List[Dict[str, Any]] = []
    for step in steps:
        hints = _role_hints(step)
        if hints.get("ops.log") or "log" in (step.get("name") or "").lower():
            logs.append({"activity": step.get("name"), "message_hint": step.get("message")})
    return _dedupe_dicts(logs, ("activity", "message_hint"))[:10]


def _find_start_nodes(
    triggers: Sequence[Dict[str, Any]], steps: Sequence[Dict[str, Any]], control_edges: Sequence[Dict[str, Any]]
) -> List[str]:
    """
    Determine candidate starting nodes using explicit control_edges first,
    then triggers, then the first step name.
    """
    children: Set[str] = set()
    parents: Set[str] = set()
    for edge in control_edges or []:
        parent = edge.get("parent")
        if parent:
            parents.add(parent)
        for child in edge.get("children") or []:
            if child:
                children.add(child)
    roots = sorted(parents - children)
    if roots:
        return roots
    trig_names = [
        t.get("name") or t.get("path") or t.get("type")
        for t in triggers
        if (t.get("name") or t.get("path") or t.get("type"))
    ]
    if trig_names:
        return sorted({name for name in trig_names if name})
    if steps:
        first = steps[0].get("name")
        return [first] if first else []
    return []


def _build_traceability(
    interfaces: Sequence[Dict[str, Any]],
    steps: Sequence[Dict[str, Any]],
    invocations: Sequence[Dict[str, Any]],
    dependencies: Dict[str, List[str]],
) -> List[str]:
    trace: List[str] = []
    if interfaces:
        first = interfaces[0]
        trace.append(f"interface:{first.get('method','ANY')} {first.get('endpoint') or first.get('kind')}")
    for step in steps[:5]:
        trace.append(f"step:{step.get('name')}")
    for proc in dependencies.get("processes", [])[:3]:
        trace.append(f"invoke:{proc}")
    for datastore in dependencies.get("datastores", [])[:3]:
        trace.append(f"datastore:{datastore}")
    for service in dependencies.get("services", [])[:3]:
        trace.append(f"service:{service}")
    return trace


def _role_hints(step: Dict[str, Any]) -> Dict[str, bool]:
    hints: Dict[str, bool] = {}
    for explicit in step.get("role_hints") or []:
        role = str(explicit).strip()
        if role:
            hints[role] = True
    connectors: List[str] = []
    for field in ("connector", "type"):
        raw = step.get(field)
        if not raw:
            continue
        conn = str(raw).lower()
        connectors.append(conn)
        if conn in ROLE_CONNECTOR_MAP:
            for role in ROLE_CONNECTOR_MAP[conn]:
                hints[role] = True
    for conn in connectors:
        prefix = conn.split(":", 1)[0]
        for role in ROLE_PREFIX_MAP.get(prefix, []):
            hints[role] = True
    return hints


def _ensure_list(value: Any) -> List[Any]:
    if isinstance(value, list):
        return value
    if value is None:
        return []
    return [value]


def _dedupe_dicts(items: Iterable[Dict[str, Any]], key_fields: Sequence[str]) -> List[Dict[str, Any]]:
    seen: Set[tuple] = set()
    out: List[Dict[str, Any]] = []
    for item in items:
        key = tuple(item.get(field) for field in key_fields)
        if key in seen:
            continue
        seen.add(key)
        out.append(item)
    return out


def _identifier_from_path(path: str | None) -> str | None:
    if not path:
        return None
    tokens = re.findall(r"[A-Za-z0-9]+", path)
    for token in tokens[::-1]:
        lower = token.lower()
        if any(lower.endswith(suffix) for suffix in IDENTIFIER_KEYWORDS):
            return token
    return None
