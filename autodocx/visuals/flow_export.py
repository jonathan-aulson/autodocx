from __future__ import annotations

import json
from pathlib import Path
from typing import Any, Dict, Iterable, List, Sequence, Set, Tuple
import re

from autodocx.types import Signal

DEFAULT_IN_PORT = "in_main"
DEFAULT_OUT_PORT = "out_default"
EXTERNAL_IN_PORT = "in_external"
EXTERNAL_OUT_PORT = "out_external"
WRAPPER_STEP_NAMES = {"extensionactivity", "bwactivity", "activityconfig"}


def _is_wrapper_step(step: Dict[str, Any]) -> bool:
    name = str(step.get("name") or "").lower()
    typ = str(step.get("type") or "").lower()
    if name in WRAPPER_STEP_NAMES or typ in WRAPPER_STEP_NAMES:
        return True
    if name.startswith("extensionactivity") or name.startswith("bwactivity") or name.startswith("activityconfig"):
        return True
    return False


def _prune_steps(steps: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
    filtered = [step for step in steps if not _is_wrapper_step(step)]
    return filtered or steps


def export_workflow_graphs(signals: Sequence[Signal], out_dir: Path) -> List[Path]:
    """
    Export workflow signals into lightweight graph JSON files that can be consumed
    by downstream diagram renderers. Files are written under
    out/diagrams/flows_json/<component>/<workflow>.json.
    """
    base = Path(out_dir) / "diagrams" / "flows_json"
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
        graph_obj["nodes"] = sorted(graph_obj.get("nodes", []), key=lambda n: n.get("id") or n.get("name") or "")
        graph_obj["edges"] = sorted(
            graph_obj.get("edges", []),
            key=lambda e: (
                e.get("source") or "",
                e.get("target") or "",
                e.get("kind") or "",
                e.get("label") or "",
            ),
        )
        graph_path = comp_dir / f"{_safe_slug(workflow_name)}.json"
        graph_path.write_text(json.dumps(graph_obj, indent=2), encoding="utf-8")
        exported.append(graph_path)
    return exported


def _build_workflow_graph(sig: Signal, props: Dict[str, Any], component: str) -> Dict[str, Any]:
    triggers: List[Dict[str, Any]] = list(props.get("triggers") or [])
    raw_steps: List[Dict[str, Any]] = list(props.get("steps") or [])
    steps: List[Dict[str, Any]] = _prune_steps(raw_steps)
    relationships: List[Dict[str, Any]] = list(props.get("relationships") or [])

    nodes: List[Dict[str, Any]] = []
    edges: List[Dict[str, Any]] = []
    edge_keys: Set[Tuple[str, str, str]] = set()
    branch_map = _collect_branch_map(props.get("control_edges") or [])
    rel_map: Dict[str, List[Dict[str, Any]]] = {}
    for rel in relationships:
        source_name = ((rel.get("source") or {}).get("name")) or ((rel.get("source") or {}).get("step_id"))
        if not source_name:
            continue
        rel_map.setdefault(source_name, []).append(rel)

    trigger_ids: List[str] = []
    trigger_name_to_id: Dict[str, str] = {}
    for trig in triggers:
        tid = _node_id("trigger", trig.get("name") or trig.get("type") or "trigger")
        trigger_ids.append(tid)
        trig_name = trig.get("name") or ""
        if trig_name:
            trigger_name_to_id.setdefault(trig_name, tid)
        trig_type = trig.get("type")
        if trig_type:
            trigger_name_to_id.setdefault(trig_type, tid)
        nodes.append(
            {
                "id": tid,
                "kind": "trigger",
                "name": trig.get("name") or trig.get("type") or "Trigger",
                "type": trig.get("type"),
                "details": trig,
                "ports": _default_ports(),
            }
        )

    step_ids: Dict[str, str] = {}
    seen_step_keys: Set[Tuple[str, str]] = set()
    used_node_ids: Set[str] = set()
    for idx, step in enumerate(steps, start=1):
        display_label = step.get("friendly_display") or step.get("name") or step.get("type") or f"Step {idx}"
        connector_hint = step.get("connector") or step.get("type") or ""
        dedupe_key = (str(display_label), str(connector_hint))
        if dedupe_key in seen_step_keys:
            continue
        seen_step_keys.add(dedupe_key)
        base_id = _node_id("step", str(display_label))
        sid = base_id
        counter = 1
        while sid in used_node_ids:
            counter += 1
            sid = f"{base_id}_{counter}"
        used_node_ids.add(sid)
        label_key = step.get("name") or step.get("type") or display_label
        step_ids.setdefault(str(label_key), sid)
        nodes.append(
            {
                "id": sid,
                "kind": "control" if step.get("control_type") else "step",
                "name": display_label,
                "type": step.get("type"),
                "connector": step.get("connector"),
                "metadata": {
                    "method": step.get("method"),
                    "url_or_path": step.get("url_or_path"),
                    "run_after": step.get("run_after") or [],
                    "inputs_keys": step.get("inputs_keys") or [],
                    "parameter_keys": step.get("parameter_keys") or [],
                    "body_fields": step.get("body_fields") or [],
                    "schema_properties": step.get("schema_properties") or [],
                    "control_type": step.get("control_type"),
                    "control_expression": step.get("control_expression"),
                    "branch": step.get("branch"),
                    "parent_step": step.get("parent_step"),
                    "friendly_display": step.get("friendly_display"),
                    "connection_display": step.get("connection_display"),
                    "operation_detail": step.get("operation_detail"),
                    "relationship_summaries": _summarize_relationships(rel_map.get(label_key) or []),
                },
                "ports": _default_ports(_branch_ports(branch_map.get(label_key) or [])),
            }
        )

    def _append_edge(edge: Dict[str, Any]) -> None:
        src = edge.get("source")
        tgt = edge.get("target")
        if not src or not tgt:
            return
        key = (src, tgt, edge.get("kind") or edge.get("label") or "edge")
        if key in edge_keys:
            return
        edge_keys.add(key)
        edges.append(edge)

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
                    _append_edge(
                        {
                            "source": prev_id,
                            "target": sid,
                            "kind": "sequence",
                            "source_port": DEFAULT_OUT_PORT,
                            "target_port": DEFAULT_IN_PORT,
                        }
                    )
        else:
            for trig_id in trigger_ids:
                _append_edge(
                    {
                        "source": trig_id,
                        "target": sid,
                        "kind": "trigger",
                        "source_port": DEFAULT_OUT_PORT,
                        "target_port": DEFAULT_IN_PORT,
                    }
                )

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
            _append_edge(
                {
                    "source": parent_id,
                    "target": child_id,
                    "kind": "branch",
                    "label": branch_name,
                    "source_port": _branch_port_name(branch_name),
                    "target_port": DEFAULT_IN_PORT,
                }
            )

    external_nodes_added: Dict[str, str] = {}
    has_non_wrapper = any(not _is_wrapper_step(step) for step in raw_steps)
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
                    "ports": _external_ports(),
                }
            )
        source_meta = rel.get("source") or {}
        source_name = source_meta.get("name") or source_meta.get("step_id") or source_meta.get("type")
        if has_non_wrapper and isinstance(source_name, str):
            if _is_wrapper_step({"name": source_name}):
                continue
        source_id = step_ids.get(source_name or "")
        if not source_id and source_meta.get("step_id"):
            source_id = step_ids.get(source_meta.get("step_id"))
        if not source_id:
            src_type = (source_meta.get("type") or "").lower()
            if src_type == "trigger":
                source_id = trigger_name_to_id.get(source_name or "") or trigger_name_to_id.get(
                    source_meta.get("step_id") or ""
                )
        if not source_id:
            source_id = _node_id("step", source_name or "step")
        if not any(node["id"] == source_id for node in nodes):
            nodes.append(
                {
                    "id": source_id,
                    "kind": "step",
                    "name": source_name or "Step",
                    "type": "unknown",
                    "ports": _default_ports(),
                }
            )
        _append_edge(
            {
                "source": source_id,
                "target": external_id,
                "kind": (rel.get("operation") or {}).get("type") or rel.get("connector") or "external",
                "metadata": {
                    "operation": rel.get("operation"),
                    "context": rel.get("context"),
                    "evidence": rel.get("evidence"),
                },
                "label": ((rel.get("operation") or {}).get("detail") or ""),
                "source_port": DEFAULT_OUT_PORT,
                "target_port": EXTERNAL_IN_PORT,
            }
        )

    transitions = props.get("transitions") or []
    for transition in transitions:
        src_name = transition.get("from")
        tgt_name = transition.get("to")
        src_id = step_ids.get(src_name or "")
        tgt_id = step_ids.get(tgt_name or "")
        if not src_id or not tgt_id:
            continue
        _append_edge(
            {
                "source": src_id,
                "target": tgt_id,
                "kind": "sequence",
                "source_port": DEFAULT_OUT_PORT,
                "target_port": DEFAULT_IN_PORT,
            }
        )

    blueprint: List[Dict[str, str]] = []
    outline_context = ""
    touchpoints = props.get("journey_touchpoints") or []
    if touchpoints:
        blueprint = [{"label": str(tp)} for tp in touchpoints if tp]
        outline_context = "Business touchpoints"

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
        "journey_outline_context": outline_context,
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


def _graph_id_fragment(value: str) -> str:
    text = "".join(ch.lower() if ch.isalnum() else "_" for ch in value or "item")
    text = re.sub("_+", "_", text)
    return text.strip("_") or "item"


def _node_id(prefix: str, value: str) -> str:
    return f"{prefix}__{_graph_id_fragment(value or prefix)}"


def _collect_branch_map(control_edges: Iterable[Dict[str, Any]]) -> Dict[str, List[str]]:
    branch_map: Dict[str, List[str]] = {}
    for edge in control_edges or []:
        parent = edge.get("parent")
        branch = edge.get("branch")
        if not parent or not branch:
            continue
        branch_map.setdefault(parent, []).append(branch)
    return branch_map


def _default_ports(extra_outbound: Iterable[Dict[str, str]] | None = None) -> List[Dict[str, str]]:
    ports = [
        {"name": DEFAULT_IN_PORT, "label": "", "direction": "in"},
        {"name": DEFAULT_OUT_PORT, "label": "", "direction": "out"},
    ]
    for port in extra_outbound or []:
        ports.append(port)
    return ports


def _branch_ports(branches: Iterable[str]) -> List[Dict[str, str]]:
    out: List[Dict[str, str]] = []
    for branch in branches or []:
        out.append({"name": _branch_port_name(branch), "label": branch, "direction": "out"})
    return out


def _branch_port_name(branch: str | None) -> str:
    return f"branch_{_graph_id_fragment(branch or 'branch')}"


def _external_ports() -> List[Dict[str, str]]:
    return [
        {"name": EXTERNAL_IN_PORT, "label": "", "direction": "in"},
        {"name": EXTERNAL_OUT_PORT, "label": "", "direction": "out"},
    ]


def _summarize_relationships(rels: Iterable[Dict[str, Any]]) -> List[str]:
    summaries: List[str] = []
    for rel in rels or []:
        target = rel.get("target") or {}
        target_name = target.get("display") or target.get("ref") or target.get("kind")
        operation = rel.get("operation") or {}
        verb = operation.get("verb") or operation.get("type") or rel.get("connector")
        if not target_name:
            continue
        phrase = f"{(verb or 'Calls').title()} {target_name}"
        detail = (operation.get("detail") or "").strip()
        if detail:
            phrase = f"{phrase} — {detail}"
        summaries.append(phrase)
    # keep only unique phrases in order
    seen: Dict[str, bool] = {}
    ordered: List[str] = []
    for text in summaries:
        key = text.lower()
        if key in seen:
            continue
        seen[key] = True
        ordered.append(text)
    return ordered[:4]
