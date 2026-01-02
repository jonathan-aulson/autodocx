from __future__ import annotations

from pathlib import Path
from typing import Any, Dict, List, Set
import json
import re

from autodocx.types import Signal

IDENTIFIER_SUFFIXES = ("id", "key", "code", "number", "guid", "token")


def enrich_signal_metadata(signal: Signal, repo_root: Path) -> Dict[str, Any]:
    props: Dict[str, Any] = signal.props if isinstance(signal.props, dict) else {}
    # Normalize relationships to dicts (strings -> target.display) to avoid attr errors
    normalized_rels: List[Dict[str, Any]] = []
    for rel in props.get("relationships") or []:
        if isinstance(rel, dict):
            normalized_rels.append(rel)
        elif isinstance(rel, str):
            normalized_rels.append({"target": {"display": rel}})
    if normalized_rels:
        props["relationships"] = normalized_rels
    enrichment: Dict[str, Any] = {}
    jdbc = _collect_jdbc_sql(props, repo_root)
    if jdbc:
        enrichment["jdbc_sql"] = jdbc
        tables = sorted({entry.get("table") for entry in jdbc if entry.get("table")})
        if tables:
            enrichment["datasource_tables"] = tables
    jms = _collect_jms_destinations(props, repo_root)
    if jms:
        enrichment["jms_destinations"] = jms
        destinations = sorted({entry.get("destination") for entry in jms if entry.get("destination")})
        if destinations:
            enrichment["service_dependencies"] = destinations
    timers = _collect_timers(props, repo_root)
    if timers:
        enrichment["timers"] = timers
    transitions = _collect_transition_conditions(props, repo_root)
    if transitions:
        enrichment["transition_conditions"] = transitions
    mapper = _collect_mapper_hints(props, repo_root)
    if mapper:
        enrichment["mapper_hints"] = mapper
    identifier_hints = _collect_identifier_hints(props, mapper, repo_root)
    if identifier_hints:
        enrichment["identifier_hints"] = identifier_hints
    process_calls = _collect_process_call_hints(props)
    if process_calls:
        enrichment["process_calls"] = process_calls
    return enrichment


def _collect_jdbc_sql(props: Dict[str, Any], repo_root: Path) -> List[Dict[str, Any]]:
    results: List[Dict[str, Any]] = []
    file_path = props.get("file")
    evidence = _evidence(file_path, repo_root)
    for step in props.get("steps") or []:
        connector = (step.get("connector") or step.get("type") or "").lower()
        if "jdbc" not in connector and "sql" not in connector:
            continue
        sql_text = step.get("sql") or step.get("statement") or _guess_sql_from_inputs(step)
        if sql_text:
            table = _primary_table_from_sql(sql_text)
            results.append(
                {
                    "activity": step.get("name"),
                    "sql": sql_text[:400],
                    "datasource": step.get("datasource"),
                    "table": table,
                    "evidence": evidence,
                }
            )
    for rel_raw in props.get("relationships") or []:
        rel = rel_raw
        if isinstance(rel_raw, str):
            rel = {"target": {"display": rel_raw}}
        if not isinstance(rel, dict):
            continue
        target = rel.get("target") or {}
        if isinstance(target, str):
            target = {"display": target}
        kind = str(target.get("kind") or rel.get("type") or "").lower()
        if not any(tok in kind for tok in ("sql", "jdbc", "db", "database")):
            continue
        ref = target.get("display") or target.get("ref") or target.get("name")
        op = (rel.get("operation") or {}).get("type") or rel.get("detail") or rel.get("branch")
        results.append(
            {
                "activity": (rel.get("source") or {}).get("name")
                if isinstance(rel.get("source"), dict)
                else rel.get("source"),
                "sql": str(op or "")[:400],
                "datasource": ref,
                "table": ref,
                "evidence": evidence,
            }
        )
    return results


def _collect_jms_destinations(props: Dict[str, Any], repo_root: Path) -> List[Dict[str, Any]]:
    results: List[Dict[str, Any]] = []
    evidence = _evidence(props.get("file"), repo_root)
    for step in props.get("steps") or []:
        connector = (step.get("connector") or step.get("type") or "").lower()
        if not any(keyword in connector for keyword in ("jms", "queue", "topic", "servicebus")):
            continue
        destination = step.get("destination") or step.get("queue") or step.get("topic")
        if not destination:
            continue
        results.append(
            {
                "activity": step.get("name"),
                "destination": destination,
                "connector": connector,
                "evidence": evidence,
            }
        )
    for rel_raw in props.get("relationships") or []:
        rel = rel_raw
        if isinstance(rel_raw, str):
            rel = {"target": {"display": rel_raw}}
        if not isinstance(rel, dict):
            continue
        target = rel.get("target") or {}
        if isinstance(target, str):
            target = {"display": target}
        kind = str(target.get("kind") or "").lower()
        if not any(keyword in kind for keyword in ("jms", "queue", "topic", "servicebus")):
            continue
        destination = target.get("display") or target.get("ref") or target.get("name")
        if not destination:
            continue
        results.append(
            {
                "activity": (rel.get("source") or {}).get("name")
                if isinstance(rel.get("source"), dict)
                else rel.get("source"),
                "destination": destination,
                "connector": kind or rel.get("type"),
                "evidence": evidence,
            }
        )
    return results


def _collect_timers(props: Dict[str, Any], repo_root: Path) -> List[Dict[str, Any]]:
    results: List[Dict[str, Any]] = []
    evidence = _evidence(props.get("file"), repo_root)
    for step in props.get("steps") or []:
        connector = (step.get("connector") or step.get("type") or "").lower()
        if "timer" in connector or "schedule" in connector:
            fields = {k: v for k, v in step.items() if k in {"cron", "rate", "interval", "timezone"} and v}
            if fields:
                results.append({"activity": step.get("name"), "fields": fields, "evidence": evidence})
    return results


def _collect_transition_conditions(props: Dict[str, Any], repo_root: Path) -> List[Dict[str, Any]]:
    results: List[Dict[str, Any]] = []
    evidence = _evidence(props.get("file"), repo_root)
    for edge in props.get("control_edges") or []:
        if not isinstance(edge, dict):
            continue
        parent = edge.get("parent")
        branch = edge.get("branch")
        for child in edge.get("children") or []:
            results.append(
                {
                    "from": parent,
                    "to": child,
                    "condition": branch,
                    "evidence": evidence,
                }
            )
    return results


def _collect_mapper_hints(props: Dict[str, Any], repo_root: Path) -> List[Dict[str, Any]]:
    results: List[Dict[str, Any]] = []
    evidence = _evidence(props.get("file"), repo_root)
    for step in props.get("steps") or []:
        connector = (step.get("connector") or step.get("type") or "").lower()
        if "mapper" not in connector and "map" != connector:
            continue
        inputs = step.get("inputs_keys") or []
        consts = [c for c in step.get("constants", []) if isinstance(c, str)]
        funcs = [f for f in step.get("functions", []) if isinstance(f, str)]
        if inputs or consts or funcs:
            results.append(
                {
                    "activity": step.get("name"),
                    "paths": inputs[:10],
                    "constants": consts[:10],
                    "functions": funcs[:10],
                    "identifiers": _derive_identifier_tokens(inputs + consts),
                    "evidence": evidence,
                }
            )
    return results


def _guess_sql_from_inputs(step: Dict[str, Any]) -> str | None:
    sql_text = step.get("body") or step.get("text")
    if sql_text and any(keyword in sql_text.lower() for keyword in ("select", "update", "insert", "delete")):
        return sql_text
    return None


def _evidence(file_path: Any, repo_root: Path) -> Dict[str, str]:
    if not file_path:
        return {}
    try:
        p = Path(file_path)
        if not p.is_absolute():
            p = (repo_root / p).resolve()
        else:
            p = p.resolve()
        rel = p.relative_to(repo_root)
        return {"file": str(rel)}
    except Exception:
        return {"file": str(file_path)}


def _derive_identifier_tokens(values: List[str]) -> List[str]:
    tokens: Set[str] = set()
    for value in values:
        if not value:
            continue
        for token in re.findall(r"[A-Za-z0-9_]+", str(value)):
            if _looks_like_identifier(token):
                tokens.add(token)
    return sorted(tokens)


def _looks_like_identifier(token: str) -> bool:
    lower = token.lower()
    return any(lower.endswith(suf) for suf in IDENTIFIER_SUFFIXES)


def _collect_identifier_hints(props: Dict[str, Any], mapper: List[Dict[str, Any]], repo_root: Path) -> List[str]:
    hints: Set[str] = set()
    for step in props.get("steps") or []:
        for key in ("inputs_keys", "outputs_keys", "constants"):
            for value in step.get(key) or []:
                for token in _derive_identifier_tokens([value]):
                    hints.add(token)
        if step.get("name") and _looks_like_identifier(step["name"]):
            hints.add(step["name"])
    for entry in mapper or []:
        for token in entry.get("identifiers") or []:
            if token:
                hints.add(str(token))
    fixture_tokens = _collect_fixture_identifiers(props.get("file"), repo_root)
    hints.update(fixture_tokens)
    return sorted(hints)


def _collect_process_call_hints(props: Dict[str, Any]) -> List[str]:
    hints: Set[str] = set()
    for step in props.get("steps") or []:
        for key in ("called_process", "target", "process_name"):
            val = step.get(key)
            if val and isinstance(val, str) and 2 < len(val) < 200:
                hints.add(val)
    for rel_raw in props.get("relationships") or []:
        rel = rel_raw
        if isinstance(rel_raw, str):
            rel = {"target": {"name": rel_raw}, "type": "calls"}
        if not isinstance(rel, dict):
            continue
        if rel.get("type") in {"calls", "invokes"}:
            tgt = (rel.get("target") or {}).get("name") if isinstance(rel.get("target"), dict) else rel.get("target")
            if tgt:
                hints.add(str(tgt))
    for value in props.get("calls_flows") or []:
        if value:
            hints.add(str(value))
    return sorted(hints)


def _collect_fixture_identifiers(file_path: Any, repo_root: Path) -> Set[str]:
    path = Path(str(file_path)) if file_path else None
    if not path:
        return set()
    if not path.suffix.lower() == ".json":
        return set()
    try:
        if not path.is_absolute():
            path = (repo_root / path).resolve()
        raw = json.loads(path.read_text(encoding="utf-8"))
    except Exception:
        return set()
    hints: Set[str] = set()
    if isinstance(raw, dict):
        candidates = raw.keys()
    elif isinstance(raw, list):
        candidates = set()
        for item in raw[:25]:
            if isinstance(item, dict):
                candidates.update(item.keys())
        candidates = list(candidates)
    else:
        return set()
    for token in _derive_identifier_tokens([str(c) for c in candidates]):
        hints.add(token)
    return hints


def _primary_table_from_sql(sql_text: str | None) -> str | None:
    if not sql_text:
        return None
    text = sql_text.lower()
    match = re.search(r"\bfrom\s+([A-Za-z0-9_.\[\]\"`]+)", text)
    if not match and "insert" in text:
        match = re.search(r"\binto\s+([A-Za-z0-9_.\[\]\"`]+)", text)
    if not match and "update" in text:
        match = re.search(r"\bupdate\s+([A-Za-z0-9_.\[\]\"`]+)", text)
    if match:
        return match.group(1).strip("[]\"`")
    return None
