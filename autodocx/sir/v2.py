from __future__ import annotations

import hashlib
import re
import time
from copy import deepcopy
from pathlib import Path
from typing import Any, Dict, Iterable, List, Optional, Sequence, Tuple

from autodocx.types import Signal
from autodocx.utils.components import derive_component
from autodocx.utils.provenance import build_provenance_entries

TOOL_VERSION = "autodocx-v2-sir"
DEFAULT_SCAFFOLD = {
    "interfaces": [],
    "invocations": [],
    "dependencies": {
        "datastores": [],
        "processes": [],
        "interfaces": [],
    },
    "io_summary": {
        "inputs": [],
        "outputs": [],
        "identifiers": [],
    },
    "errors": [],
    "logging": [],
    "traceability": [],
}


def build_sir_v2(
    signal: Signal,
    repo_root: Path,
    *,
    component: Optional[str] = None,
    business_scaffold: Optional[Dict[str, Any]] = None,
    graph_features: Optional[Dict[str, Any]] = None,
    roles: Optional[Sequence[str]] = None,
    roles_evidence: Optional[Dict[str, Any]] = None,
    provenance: Optional[List[Dict[str, Any]]] = None,
    doc_slug: Optional[str] = None,
    interdependencies_slice: Optional[Dict[str, Any]] = None,
    extrapolations: Optional[Sequence[Dict[str, Any]]] = None,
    deterministic_explanation: Optional[str] = None,
) -> Optional[Dict[str, Any]]:
    props: Dict[str, Any] = signal.props if isinstance(signal.props, dict) else {}
    process_name = props.get("name") or f"{signal.kind}-process"
    component = component or props.get("component_or_service")
    if not component:
        try:
            component = derive_component(repo_root, props)
        except Exception:
            component = ""

    source_info = _build_source_info(props.get("file"), repo_root, signal)
    activities, start_activity = _build_activities(props)
    transitions = _build_transitions(props, activities, start_activity)
    external_nodes, ext_transitions = _build_external_relationships(props.get("relationships") or [])

    sir_id = _sir_identifier(process_name, source_info.get("hash_sha256"), signal.kind)
    slug = doc_slug or _safe_slug(process_name)
    provenance_entries = provenance or build_provenance_entries(
        repo_root,
        getattr(signal, "evidence", []) or [],
        props.get("file"),
    )
    scaffold_payload = deepcopy(business_scaffold or props.get("business_scaffold") or DEFAULT_SCAFFOLD)
    graph_features_payload = deepcopy(graph_features or {})
    roles_list = sorted({*(roles or [])})
    roles_evidence_payload = deepcopy(roles_evidence or {})
    sir = {
        "sir_version": "2.0",
        "id": sir_id,
        "process_name": process_name,
        "name": process_name,
        "component_or_service": component or "ungrouped",
        "kind": signal.kind,
        "signal_kind": signal.kind,
        "source": source_info,
        "file": source_info.get("file") or "",
        "metadata": {
            "generated_at": time.time(),
            "tool_version": TOOL_VERSION,
            "user_story": props.get("user_story"),
            "inputs_example": props.get("inputs_example"),
            "outputs_example": props.get("outputs_example"),
        },
        "start_activity": start_activity,
        "activities": activities + external_nodes,
        "transitions": transitions + ext_transitions,
        "relationships": deepcopy(props.get("relationships") or []),
        "resources": {
            "triggers": deepcopy(props.get("triggers") or []),
            "steps": deepcopy(props.get("steps") or []),
            "journey_touchpoints": deepcopy(props.get("journey_touchpoints") or []),
            "logging": deepcopy(props.get("logging") or []),
        },
        "graph_features": graph_features_payload,
        "roles": roles_list,
        "roles_evidence": roles_evidence_payload,
        "business_scaffold": scaffold_payload,
        "provenance": provenance_entries,
        "interdependencies_slice": deepcopy(interdependencies_slice) if interdependencies_slice else {},
        "extrapolations": deepcopy(list(extrapolations) if extrapolations else []),
        "deterministic_explanation": deterministic_explanation,
        "_doc_slug": slug,
        "doc_slug": slug,
        "evidence": deepcopy(signal.evidence),
        "subscores": deepcopy(signal.subscores),
        "enrichment": deepcopy(props.get("enrichment") or {}),
        "props": _project_props_snapshot(props),
    }
    return sir


def _sir_identifier(name: str, file_hash: Optional[str], kind: str) -> str:
    raw = f"{name}|{file_hash or 'nohash'}|{kind}"
    return hashlib.sha256(raw.encode("utf-8")).hexdigest()[:16]


def _build_source_info(file_path: Optional[str], repo_root: Path, signal: Signal) -> Dict[str, Any]:
    info: Dict[str, Any] = {
        "file": file_path or "",
        "kind": signal.kind,
        "hash_sha256": None,
        "repo_relative_path": None,
    }
    if not file_path:
        return info
    try:
        p = Path(file_path)
        if not p.is_absolute():
            p = (repo_root / p).resolve()
        else:
            p = p.resolve()
        if p.exists() and p.is_file():
            info["hash_sha256"] = _hash_file(p)
        try:
            info["repo_relative_path"] = str(p.relative_to(repo_root))
        except ValueError:
            info["repo_relative_path"] = str(p)
    except Exception:
        info["repo_relative_path"] = file_path
    return info


def _hash_file(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as fh:
        for chunk in iter(lambda: fh.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def _normalize_name(raw: Optional[str], fallback_prefix: str, index: int) -> str:
    if raw:
        return str(raw)
    return f"{fallback_prefix}-{index+1}"


def _build_activities(props: Dict[str, Any]) -> Tuple[List[Dict[str, Any]], Optional[str]]:
    activities: List[Dict[str, Any]] = []
    name_seen = set()
    start_activity: Optional[str] = None

    triggers = props.get("triggers") or []
    for idx, trig in enumerate(triggers):
        name = _normalize_name(trig.get("name") or trig.get("type"), "trigger", idx)
        activity = {
            "id": f"trigger-{idx}",
            "name": name,
            "type": trig.get("type") or "trigger",
            "role_hints": ["trigger"],
            "props": trig,
        }
        activities.append(activity)
        name_seen.add(name)
        if start_activity is None:
            start_activity = name

    steps = props.get("steps") or []
    for idx, step in enumerate(steps):
        name = _normalize_name(step.get("name"), "step", idx)
        if name in name_seen:
            name = f"{name}-{idx+1}"
        activity = {
            "id": f"step-{idx}",
            "name": name,
            "type": step.get("connector") or step.get("type") or "step",
            "role_hints": _role_hints_for_step(step),
            "props": step,
        }
        activities.append(activity)
        name_seen.add(name)
        if start_activity is None:
            start_activity = name

    if not activities:
        # fallback to single activity from signal name
        name = props.get("name") or "process"
        activities.append(
            {
                "id": "activity-0",
                "name": name,
                "type": props.get("engine") or "process",
                "role_hints": [],
                "props": {},
            }
        )
        start_activity = name

    return activities, start_activity


def _role_hints_for_step(step: Dict[str, Any]) -> List[str]:
    hints = []
    connector = (step.get("connector") or step.get("type") or "").lower()
    if connector.startswith("http") or connector in {"rest", "api"}:
        hints.append("interface")
    if connector.startswith("jdbc") or connector.startswith("sql"):
        hints.append("data")
    if "jms" in connector or connector in {"queue", "servicebus"}:
        hints.append("messaging")
    if step.get("control_type"):
        hints.append("control")
    return hints


def _transition_key(fr: str, to: str, label: Optional[str]) -> Tuple[str, str, Optional[str]]:
    return fr, to, label or None


def _build_transitions(props: Dict[str, Any], activities: List[Dict[str, Any]], start_activity: Optional[str]) -> List[Dict[str, Any]]:
    transitions: List[Dict[str, Any]] = []
    steps = props.get("steps") or []
    name_lookup = {act["name"]: act for act in activities}
    added = set()

    for idx, step in enumerate(steps):
        name = _normalize_name(step.get("name"), "step", idx)
        run_after = step.get("run_after")
        predecessors = _ensure_list(run_after)
        if not predecessors and idx > 0:
            predecessors = [_normalize_name(steps[idx - 1].get("name"), "step", idx - 1)]
        if not predecessors and start_activity:
            predecessors = [start_activity]
        for prev in predecessors:
            if prev not in name_lookup or name not in name_lookup:
                continue
            key = _transition_key(prev, name, step.get("branch"))
            if key in added:
                continue
            transitions.append(
                {
                    "from": prev,
                    "to": name,
                    "label": step.get("branch") or step.get("control_expression"),
                }
            )
            added.add(key)

    if not transitions and len(activities) > 1:
        for prev, nxt in zip(activities[:-1], activities[1:]):
            key = _transition_key(prev["name"], nxt["name"], None)
            if key in added:
                continue
            transitions.append({"from": prev["name"], "to": nxt["name"], "label": None})
            added.add(key)
    return transitions


def _build_external_relationships(relationships: Iterable[Dict[str, Any]]) -> Tuple[List[Dict[str, Any]], List[Dict[str, Any]]]:
    external_nodes: List[Dict[str, Any]] = []
    transitions: List[Dict[str, Any]] = []
    added_nodes = set()
    for idx, rel in enumerate(relationships or []):
        if not isinstance(rel, dict):
            continue
        target_meta = rel.get("target")
        if not isinstance(target_meta, dict):
            continue
        target = target_meta.get("display") or target_meta.get("ref") or target_meta.get("name")
        if not target:
            continue
        node_name = f"External:{target}"
        if node_name not in added_nodes:
            external_nodes.append(
                {
                    "id": f"external-{idx}",
                    "name": node_name,
                    "type": target_meta.get("kind") or "external",
                    "role_hints": ["external"],
                    "props": target_meta,
                }
            )
            added_nodes.add(node_name)
        source_meta = rel.get("source")
        if not isinstance(source_meta, dict):
            continue
        source = source_meta.get("name") or source_meta.get("type")
        if not source:
            continue
        transitions.append(
            {
                "from": source,
                "to": node_name,
                "label": (rel.get("operation") or {}).get("type") if isinstance(rel.get("operation"), dict) else rel.get("connector"),
            }
        )
    return external_nodes, transitions


def _ensure_list(value: Any) -> List[Any]:
    if not value:
        return []
    if isinstance(value, list):
        return value
    return [value]


_SLUG_PATTERN = re.compile(r"[^A-Za-z0-9_.-]+")


def _safe_slug(value: Optional[str]) -> str:
    slug = _SLUG_PATTERN.sub("_", (value or "").strip())
    slug = slug.strip("_")[:200]
    return slug or "process"


def _project_props_snapshot(props: Dict[str, Any]) -> Dict[str, Any]:
    """Expose the minimal props needed by downstream rules/diagnostics."""
    snapshot: Dict[str, Any] = {}
    list_fields = {
        "triggers",
        "steps",
        "relationships",
        "journey_touchpoints",
        "logging",
        "datasource_tables",
        "process_calls",
        "service_dependencies",
        "identifier_hints",
    }
    for field in list_fields:
        snapshot[field] = deepcopy(props.get(field) or [])
    passthrough_fields = ["name", "file", "component_or_service", "engine"]
    for field in passthrough_fields:
        if field in props and props[field] is not None:
            snapshot[field] = props[field]
    if props.get("business_scaffold"):
        snapshot["business_scaffold"] = deepcopy(props["business_scaffold"])
    if props.get("enrichment"):
        snapshot["enrichment"] = deepcopy(props["enrichment"])
    if props.get("datasource"):
        snapshot["datasource"] = props["datasource"]
    if props.get("destination"):
        snapshot["destination"] = props["destination"]
    if props.get("transitions"):
        snapshot["transitions"] = deepcopy(props["transitions"])
    return snapshot
