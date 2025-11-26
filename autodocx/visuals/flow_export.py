from __future__ import annotations

import json
from pathlib import Path
from typing import Any, Dict, Iterable, List, Sequence

from autodocx.types import Signal


def export_workflow_graphs(signals: Sequence[Signal], out_dir: Path) -> List[Path]:
    """
    Export workflow signals into lightweight graph JSON files that can be consumed
    by downstream diagram renderers. Files are written under
    out/flows/<component>/<workflow>.json.
    """
    base = Path(out_dir) / "flows"
    base.mkdir(parents=True, exist_ok=True)
    exported: List[Path] = []
    for sig in signals:
        if getattr(sig, "kind", "").lower() != "workflow":
            continue
        props: Dict[str, Any] = sig.props if isinstance(sig.props, dict) else {}
        component = props.get("component_or_service") or props.get("component") or "ungrouped"
        comp_dir = base / _safe_slug(component)
        comp_dir.mkdir(parents=True, exist_ok=True)

        workflow_name = props.get("name") or props.get("file") or sig.kind
        graph_obj = _build_workflow_graph(sig, props, component)
        graph_path = comp_dir / f"{_safe_slug(workflow_name)}.json"
        graph_path.write_text(json.dumps(graph_obj, indent=2), encoding="utf-8")
        exported.append(graph_path)
    return exported


def _build_workflow_graph(sig: Signal, props: Dict[str, Any], component: str) -> Dict[str, Any]:
    triggers: List[Dict[str, Any]] = list(props.get("triggers") or [])
    steps: List[Dict[str, Any]] = list(props.get("steps") or [])
    relationships: List[Dict[str, Any]] = list(props.get("relationships") or [])

    nodes: List[Dict[str, Any]] = []
    edges: List[Dict[str, Any]] = []

    trigger_ids: List[str] = []
    for trig in triggers:
        tid = _node_id("trigger", trig.get("name") or trig.get("type") or "trigger")
        trigger_ids.append(tid)
        nodes.append(
            {
                "id": tid,
                "kind": "trigger",
                "name": trig.get("name") or trig.get("type") or "Trigger",
                "type": trig.get("type"),
                "details": trig,
            }
        )

    step_ids: Dict[str, str] = {}
    for idx, step in enumerate(steps, start=1):
        label = step.get("name") or step.get("type") or f"Step {idx}"
        sid = _node_id("step", label)
        step_ids[label] = sid
        nodes.append(
            {
                "id": sid,
                "kind": "control" if step.get("control_type") else "step",
                "name": label,
                "type": step.get("type"),
                "connector": step.get("connector"),
                "metadata": {
                    "method": step.get("method"),
                    "url_or_path": step.get("url_or_path"),
                    "run_after": step.get("run_after") or [],
                    "inputs_keys": step.get("inputs_keys") or [],
                    "control_type": step.get("control_type"),
                    "control_expression": step.get("control_expression"),
                    "branch": step.get("branch"),
                    "parent_step": step.get("parent_step"),
                },
            }
        )

    for idx, step in enumerate(steps, start=1):
        label = step.get("name") or step.get("type") or f"Step {idx}"
        sid = step_ids.get(label)
        if not sid:
            continue
        predecessors = _ensure_list(step.get("run_after"))
        if predecessors:
            for prev in predecessors:
                prev_id = step_ids.get(prev)
                if prev_id:
                    edges.append({"source": prev_id, "target": sid, "kind": "sequence"})
        else:
            for trig_id in trigger_ids:
                edges.append({"source": trig_id, "target": sid, "kind": "trigger"})

    for ctrl in props.get("control_edges") or []:
        parent = ctrl.get("parent")
        branch_name = ctrl.get("branch") or "branch"
        parent_id = step_ids.get(parent)
        if not parent_id:
            continue
        for child in ctrl.get("children") or []:
            child_id = step_ids.get(child)
            if not child_id:
                continue
            edges.append({
                "source": parent_id,
                "target": child_id,
                "kind": "branch",
                "label": branch_name,
            })

    external_nodes_added: Dict[str, str] = {}
    for rel in relationships:
        target = (rel.get("target") or {}).get("display") or (rel.get("target") or {}).get("ref") or "external"
        external_id = external_nodes_added.setdefault(target, _node_id("external", target))
        if external_id not in {node["id"] for node in nodes}:
            nodes.append(
                {
                    "id": external_id,
                    "kind": "external",
                    "name": target,
                    "type": (rel.get("target") or {}).get("kind"),
                    "metadata": rel.get("target") or {},
                }
            )
        source_name = (rel.get("source") or {}).get("name") or (rel.get("source") or {}).get("type")
        source_id = step_ids.get(source_name) or _node_id("step", source_name or "step")
        if not any(node["id"] == source_id for node in nodes):
            nodes.append({"id": source_id, "kind": "step", "name": source_name or "Step", "type": "unknown"})
        edges.append(
            {
                "source": source_id,
                "target": external_id,
                "kind": (rel.get("operation") or {}).get("type") or rel.get("connector") or "external",
                "metadata": {
                    "operation": rel.get("operation"),
                    "context": rel.get("context"),
                    "evidence": rel.get("evidence"),
                },
            }
        )

    blueprint = []
    for entry in _ensure_list(props.get("journey_touchpoints")):
        if isinstance(entry, str):
            blueprint.append({"label": entry})
    if not blueprint:
        for entry in _ensure_list(props.get("step_display_names")):
            blueprint.append({"label": entry})

    return {
        "workflow_id": props.get("name"),
        "component": component,
        "engine": props.get("engine"),
        "user_story": props.get("user_story"),
        "inputs_example": props.get("inputs_example"),
        "outputs_example": props.get("outputs_example"),
        "calls_flows": props.get("calls_flows") or [],
        "nodes": nodes,
        "edges": edges,
        "journey_outline": blueprint,
        "control_edges": props.get("control_edges") or [],
        "evidence": sig.evidence if isinstance(sig.evidence, list) else [],
    }


def _ensure_list(value: Any) -> List[Any]:
    if not value:
        return []
    if isinstance(value, list):
        return value
    return [value]


def _safe_slug(value: str) -> str:
    return "".join(ch.lower() if ch.isalnum() else "-" for ch in value or "item").strip("-") or "item"


def _node_id(prefix: str, value: str) -> str:
    return f"{prefix}:{_safe_slug(value or prefix)}"
