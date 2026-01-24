from __future__ import annotations

import json
import os
import subprocess
import textwrap
from pathlib import Path
from typing import Any, Dict, List, Optional, Sequence

from autodocx.llm.provider import call_openai_meta
try:
    from rich import print as rprint
except Exception:  # pragma: no cover
    def rprint(msg):
        print(msg)

DIAGRAM_PROMPT = textwrap.dedent(
    """
    You are an expert workflow illustrator. Using the provided component metadata, workflow facts,
    and deterministic graph exports, produce a single Graphviz DOT diagram that merges the related
    processes into one readable flow.

    Requirements:
    - Use `workflow_graphs` (nodes/edges) as the primary source of structure.
    - Use `workflows` (activities/transitions) to add human-readable labels.
    - When relationships/calls mention cross-workflow dependencies, draw an edge between the corresponding nodes.
    - Cluster related steps from the same workflow using subgraphs named after the workflow.
    - Use graph styling for readability: rankdir=LR, splines=ortho, concentrate=true, node [shape=box, style=rounded,filled].
    - Keep the diagram under 40 nodes; consolidate obvious sequences into single nodes if needed.
    - For decision/branch edges, color "true/yes/success" paths green and "false/no/failure" paths red.
    - Output **only** Graphviz DOT syntax. Do not wrap it in Markdown fences or commentary.
    """
).strip()
_DEFAULT_GRAPHVIZ_TIMEOUT_SEC = 30.0
_DEFAULT_MAX_PAYLOAD_BYTES = 120000
_DEFAULT_MAX_ACTIVITIES = 120
_DEFAULT_MAX_TRANSITIONS = 160
_DEFAULT_MAX_RELATIONSHIPS = 40
_DEFAULT_MAX_GRAPHS_FALLBACK = 6
_DEFAULT_MAX_DETERMINISTIC_SVGS = 6
_DEFAULT_MAX_NODES = 120
_DEFAULT_MAX_EDGES = 180
_DEFAULT_MAX_FLOWS_PER_BATCH = 3
_DEFAULT_COMPACT_ACTIVITY_NAMES = 40
_DEFAULT_COMPACT_TRANSITIONS = 60


def _graphviz_timeout_sec() -> float:
    env_val = os.getenv("AUTODOCX_GRAPHVIZ_TIMEOUT_SEC")
    if env_val:
        try:
            return float(env_val)
        except ValueError:
            pass
    return _DEFAULT_GRAPHVIZ_TIMEOUT_SEC


def _load_json(path: Path) -> Optional[Dict[str, Any]]:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except Exception:
        return None


def _int_env(name: str, default: int) -> int:
    raw = os.getenv(name)
    if raw is None:
        return default
    try:
        return int(raw)
    except ValueError:
        return default


def _diagram_limits() -> Dict[str, int]:
    return {
        "max_payload_bytes": _int_env("AUTODOCX_LLM_DIAGRAM_MAX_PAYLOAD_BYTES", _DEFAULT_MAX_PAYLOAD_BYTES),
        "max_activities": _int_env("AUTODOCX_LLM_DIAGRAM_MAX_ACTIVITIES", _DEFAULT_MAX_ACTIVITIES),
        "max_transitions": _int_env("AUTODOCX_LLM_DIAGRAM_MAX_TRANSITIONS", _DEFAULT_MAX_TRANSITIONS),
        "max_relationships": _int_env("AUTODOCX_LLM_DIAGRAM_MAX_RELATIONSHIPS", _DEFAULT_MAX_RELATIONSHIPS),
        "max_graphs_fallback": _int_env("AUTODOCX_LLM_DIAGRAM_MAX_GRAPHS_FALLBACK", _DEFAULT_MAX_GRAPHS_FALLBACK),
        "max_deterministic_svgs": _int_env("AUTODOCX_LLM_DIAGRAM_MAX_SVGS", _DEFAULT_MAX_DETERMINISTIC_SVGS),
        "max_nodes": _int_env("AUTODOCX_LLM_DIAGRAM_MAX_NODES", _DEFAULT_MAX_NODES),
        "max_edges": _int_env("AUTODOCX_LLM_DIAGRAM_MAX_EDGES", _DEFAULT_MAX_EDGES),
        "max_flows_per_batch": _int_env("AUTODOCX_LLM_DIAGRAM_MAX_FLOWS_PER_BATCH", _DEFAULT_MAX_FLOWS_PER_BATCH),
        "compact_activity_names": _int_env("AUTODOCX_LLM_DIAGRAM_COMPACT_ACTIVITIES", _DEFAULT_COMPACT_ACTIVITY_NAMES),
        "compact_transitions": _int_env("AUTODOCX_LLM_DIAGRAM_COMPACT_TRANSITIONS", _DEFAULT_COMPACT_TRANSITIONS),
    }


def _simplify_activity(activity: Dict[str, Any]) -> Dict[str, Any]:
    return {
        "id": activity.get("id") or activity.get("name"),
        "name": activity.get("name") or activity.get("id"),
        "type": activity.get("type"),
        "role": activity.get("role") or activity.get("role_hints"),
    }


def _simplify_transition(transition: Dict[str, Any]) -> Dict[str, Any]:
    return {
        "from": transition.get("from") or transition.get("source"),
        "to": transition.get("to") or transition.get("target"),
        "label": transition.get("label"),
    }


def _simplify_relationship(rel: Dict[str, Any]) -> Dict[str, Any]:
    src = rel.get("source") or {}
    tgt = rel.get("target") or {}
    return {
        "source": src.get("name") or src.get("type") or src.get("id"),
        "target": tgt.get("display") or tgt.get("ref") or tgt.get("name") or tgt.get("id"),
        "operation": (rel.get("operation") or {}).get("type") or rel.get("connector"),
    }


def _collect_workflows(
    out_base: Path,
    component_entry: Dict[str, Any],
    limits: Dict[str, int],
) -> List[Dict[str, Any]]:
    workflows: List[Dict[str, Any]] = []
    max_activities = limits.get("max_activities", _DEFAULT_MAX_ACTIVITIES)
    max_transitions = limits.get("max_transitions", _DEFAULT_MAX_TRANSITIONS)
    max_relationships = limits.get("max_relationships", _DEFAULT_MAX_RELATIONSHIPS)
    for rel in component_entry.get("sir_files", []):
        data = _load_json(out_base / rel)
        if not data:
            continue
        raw_activities = data.get("activities") or data.get("steps") or (data.get("props") or {}).get("steps") or []
        raw_transitions = data.get("transitions") or []
        raw_relationships = data.get("relationships") or []
        activities = [
            _simplify_activity(a) for a in raw_activities[:max_activities] if isinstance(a, dict)
        ]
        transitions = [
            _simplify_transition(t) for t in raw_transitions[:max_transitions] if isinstance(t, dict)
        ]
        relationships = [
            _simplify_relationship(r) for r in raw_relationships[:max_relationships] if isinstance(r, dict)
        ]
        workflows.append(
            {
                "name": data.get("process_name") or data.get("name") or rel,
                "activities": [a for a in activities if a.get("name") or a.get("type")],
                "transitions": [t for t in transitions if t.get("from") or t.get("to")],
                "relationships": [r for r in relationships if r.get("source") or r.get("target")],
            }
        )
    return workflows


def _collect_flow_exports(out_base: Path, component_slug: str, limits: Dict[str, int]) -> List[Dict[str, Any]]:
    flows_root = out_base / "diagrams" / "flows_json" / component_slug
    if not flows_root.exists():
        return []
    exports: List[Dict[str, Any]] = []
    max_nodes = limits.get("max_nodes", _DEFAULT_MAX_NODES)
    max_edges = limits.get("max_edges", _DEFAULT_MAX_EDGES)
    for path in sorted(flows_root.glob("*.json")):
        data = _load_json(path)
        if not data:
            continue
        nodes = [
            {"id": n.get("id"), "name": n.get("name"), "kind": n.get("kind")}
            for n in (data.get("nodes") or [])[:max_nodes]
        ]
        edges = [
            {
                "source": e.get("source"),
                "target": e.get("target"),
                "kind": e.get("kind"),
                "label": e.get("label"),
            }
            for e in (data.get("edges") or [])[:max_edges]
        ]
        exports.append(
            {
                "workflow_id": data.get("workflow_id") or path.stem,
                "nodes": nodes,
                "edges": edges,
                "journey_outline": data.get("journey_outline") or [],
            }
        )
    return exports


def _collect_deterministic_svgs(out_base: Path, component_slug: str, limits: Dict[str, int]) -> List[str]:
    root = out_base / "diagrams" / "deterministic_svg" / component_slug
    if not root.exists():
        return []
    max_svgs = limits.get("max_deterministic_svgs", _DEFAULT_MAX_DETERMINISTIC_SVGS)
    return [p.relative_to(out_base).as_posix() for p in sorted(root.glob("*.svg"))[:max_svgs]]


def _normalize_name(value: str) -> str:
    return "".join(ch.lower() if ch.isalnum() else "" for ch in (value or ""))


def _filter_graphs_for_chunk(
    graphs: List[Dict[str, Any]],
    workflows: List[Dict[str, Any]],
    limits: Dict[str, int],
) -> List[Dict[str, Any]]:
    if not graphs:
        return []
    names = {_normalize_name(w.get("name") or "") for w in workflows if w.get("name")}
    if not names:
        return graphs[: limits.get("max_graphs_fallback", _DEFAULT_MAX_GRAPHS_FALLBACK)]
    matched = []
    for graph in graphs:
        gid = _normalize_name(str(graph.get("workflow_id") or ""))
        if gid and gid in names:
            matched.append(graph)
    if matched:
        return matched
    return graphs[: limits.get("max_graphs_fallback", _DEFAULT_MAX_GRAPHS_FALLBACK)]


def _chunk(items: Sequence[Any], size: int) -> List[List[Any]]:
    return [list(items[i : i + size]) for i in range(0, len(items), size)]


def _compact_workflow(workflow: Dict[str, Any], limits: Dict[str, int]) -> Dict[str, Any]:
    max_act = limits.get("compact_activity_names", _DEFAULT_COMPACT_ACTIVITY_NAMES)
    max_trans = limits.get("compact_transitions", _DEFAULT_COMPACT_TRANSITIONS)
    activity_names: List[str] = []
    for act in workflow.get("activities") or []:
        name = act.get("name") or act.get("type") or act.get("id")
        if name:
            activity_names.append(str(name))
    transitions: List[str] = []
    for tr in workflow.get("transitions") or []:
        src = tr.get("from") or ""
        tgt = tr.get("to") or ""
        if not src and not tgt:
            continue
        label = tr.get("label")
        if label:
            transitions.append(f"{src} -> {tgt} ({label})")
        else:
            transitions.append(f"{src} -> {tgt}")
    return {
        "name": workflow.get("name") or workflow.get("id"),
        "activity_names": activity_names[:max_act],
        "transition_pairs": transitions[:max_trans],
    }


def _extract_dot(text: str) -> str:
    stripped = text.strip().strip("`")
    # If the LLM returned a full digraph, keep it; otherwise wrap as a body.
    lower = stripped.lower()
    if lower.startswith("digraph"):
        return stripped
    # Strip common fencing artifacts
    for fence in ("```dot", "```", "graphviz"):
        if lower.startswith(fence):
            stripped = stripped[len(fence) :].strip()
            break
    body = stripped or "label=\"Produced from textual summary\";"
    return f"digraph G {{\n{body}\n}}\n"


def _render_svg(dot_text: str, out_path: Path) -> None:
    out_path.parent.mkdir(parents=True, exist_ok=True)
    timeout_sec = _graphviz_timeout_sec()
    timeout = None if timeout_sec <= 0 else timeout_sec
    try:
        proc = subprocess.run(
            ["dot", "-Tsvg", "-o", str(out_path)],
            input=dot_text,
            text=True,
            capture_output=True,
            timeout=timeout,
        )
    except subprocess.TimeoutExpired:
        rprint(f"[yellow]Graphviz timed out after {timeout_sec:.0f}s for {out_path.name}; skipping SVG.[/yellow]")
        return
    if proc.returncode != 0:
        warn = proc.stderr.strip()
        # Second attempt: wrap the body as a label to avoid syntax errors
        safe_label = dot_text.replace("\"", "'").replace("\n", " ")[:500]
        fallback_dot = f"digraph G {{ label=\"LLM diagram unavailable; sanitized: {safe_label}\"; }}"
        try:
            proc2 = subprocess.run(
                ["dot", "-Tsvg", "-o", str(out_path)],
                input=fallback_dot,
                text=True,
                capture_output=True,
                timeout=timeout,
            )
        except subprocess.TimeoutExpired:
            rprint(f"[yellow]Graphviz timed out after {timeout_sec:.0f}s for {out_path.name}; skipping SVG.[/yellow]")
            return
        if proc2.returncode != 0:
            # Final minimal stub; suppress further warnings
            final_stub = "digraph G { label=\"LLM diagram unavailable\"; }"
            try:
                subprocess.run(
                    ["dot", "-Tsvg", "-o", str(out_path)],
                    input=final_stub,
                    text=True,
                    capture_output=True,
                    timeout=timeout,
                )
            except subprocess.TimeoutExpired:
                rprint(f"[yellow]Graphviz timed out after {timeout_sec:.0f}s for {out_path.name}; skipping SVG.[/yellow]")
                return


def generate_llm_workflow_diagrams(
    out_base: Path,
    context: Dict[str, Any],
    *,
    max_flows_per_batch: int = 5,
    llm_callable=None,
) -> Dict[str, List[str]]:
    """
    Generate LLM-authored workflow diagrams per component.
    Returns component_name -> list of diagram relative paths.
    """
    out_base = Path(out_base)
    diagrams_root = out_base / "diagrams" / "llm_svg"
    produced: Dict[str, List[str]] = {}
    limits = _diagram_limits()
    max_flows_per_batch = max(1, min(max_flows_per_batch, limits.get("max_flows_per_batch", _DEFAULT_MAX_FLOWS_PER_BATCH)))

    for component_name, entry in (context.get("components") or {}).items():
        component_slug = entry.get("slug") or ""
        workflows = _collect_workflows(out_base, entry, limits)
        if not workflows:
            continue
        graphs = _collect_flow_exports(out_base, component_slug, limits)
        deterministic_svgs = _collect_deterministic_svgs(out_base, component_slug, limits)
        chunks = _chunk(workflows, max_flows_per_batch)
        diagram_idx = 0
        for chunk in chunks:
            pending = [chunk]
            while pending:
                subchunk = pending.pop(0)
                graph_chunk = _filter_graphs_for_chunk(graphs, subchunk, limits)
                payload = {
                    "component": component_name,
                    "component_slug": component_slug,
                    "families": entry.get("families", []),
                    "workflows": subchunk,
                    "workflow_graphs": graph_chunk,
                    "deterministic_svgs": deterministic_svgs,
                }
                payload_bytes = len(json.dumps(payload, ensure_ascii=True))
                used_compact = False
                if payload_bytes > limits.get("max_payload_bytes", _DEFAULT_MAX_PAYLOAD_BYTES):
                    if len(subchunk) > 1:
                        pending = _chunk(subchunk, max(1, len(subchunk) // 2)) + pending
                        continue
                    compact_chunk = [_compact_workflow(subchunk[0], limits)]
                    payload = {
                        "component": component_name,
                        "component_slug": component_slug,
                        "families": entry.get("families", []),
                        "workflows": compact_chunk,
                        "workflow_graphs": graph_chunk,
                        "deterministic_svgs": deterministic_svgs,
                    }
                    used_compact = True
                    if len(json.dumps(payload, ensure_ascii=True)) > limits.get("max_payload_bytes", _DEFAULT_MAX_PAYLOAD_BYTES):
                        rprint(f"[yellow]LLM diagram payload too large for {component_name}; skipping diagram.[/yellow]")
                        continue
                try:
                    result = (
                        llm_callable(DIAGRAM_PROMPT, payload)
                        if llm_callable
                        else call_openai_meta(prompt=DIAGRAM_PROMPT, input_json=payload)
                    )
                except Exception as e:
                    if "context_length_exceeded" in str(e).lower() and not used_compact:
                        compact_chunk = [_compact_workflow(subchunk[0], limits)]
                        payload = {
                            "component": component_name,
                            "component_slug": component_slug,
                            "families": entry.get("families", []),
                            "workflows": compact_chunk,
                            "workflow_graphs": graph_chunk,
                            "deterministic_svgs": deterministic_svgs,
                        }
                        result = (
                            llm_callable(DIAGRAM_PROMPT, payload)
                            if llm_callable
                            else call_openai_meta(prompt=DIAGRAM_PROMPT, input_json=payload)
                        )
                    else:
                        raise
                dot_text = _extract_dot(result.get("text", ""))
                diagram_idx += 1
                svg_path = diagrams_root / entry["slug"] / f"{entry['slug']}-flow-{diagram_idx}.svg"
                _render_svg(dot_text, svg_path)
                rel = svg_path.relative_to(out_base).as_posix()
                produced.setdefault(component_name, []).append(rel)

    return produced
