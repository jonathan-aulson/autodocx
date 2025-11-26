from __future__ import annotations

from pathlib import Path
from typing import Any, Dict, List, Sequence

import json

try:
    from graphviz import Digraph
except Exception:
    Digraph = None


def render_flow_diagrams(exported_paths: Sequence[Path], out_dir: Path) -> None:
    """
    Render SVG diagrams for each exported workflow graph JSON.
    Outputs are stored in out/assets/diagrams/<component>/<workflow>.svg.
    """
    if Digraph is None:
        return
    for json_path in exported_paths:
        try:
            data = json.loads(Path(json_path).read_text(encoding="utf-8"))
        except Exception:
            continue
        component = data.get("component") or "ungrouped"
        workflow = data.get("workflow_id") or json_path.stem
        assets_dir = Path(out_dir) / "assets" / "diagrams" / _safe_slug(component)
        assets_dir.mkdir(parents=True, exist_ok=True)
        svg_path = assets_dir / f"{_safe_slug(workflow)}.svg"
        dot = _build_graphviz_diagram(data)
        if dot is None:
            continue
        try:
            dot.render(filename=svg_path.with_suffix("").as_posix(), format="svg", cleanup=True)
        except Exception:
            continue


def _build_graphviz_diagram(data: Dict[str, Any]) -> Digraph | None:
    if Digraph is None:
        return None
    dot = Digraph(name="workflow", format="svg")
    dot.attr("graph", rankdir="LR", splines="spline", bgcolor="white", fontname="Helvetica")
    dot.attr("node", fontname="Helvetica", style="filled", color="#4A5568", fontcolor="#1A202C")
    dot.attr("edge", fontname="Helvetica", color="#4A5568", arrowsize="0.8")

    id_set = set()
    for node in data.get("nodes", []):
        node_id = node.get("id")
        if not node_id or node_id in id_set:
            continue
        id_set.add(node_id)
        kind = node.get("kind")
        label = node.get("name") or node_id
        style_kwargs = _style_for_kind(kind)
        dot.node(node_id, label, **style_kwargs)

    for edge in data.get("edges", []):
        src = edge.get("source")
        tgt = edge.get("target")
        if not src or not tgt:
            continue
        label = edge.get("label") or edge.get("kind") or ""
        style = "dashed" if edge.get("kind") == "branch" else "solid"
        dot.edge(src, tgt, label=label, style=style)

    _add_journey_outline(dot, data.get("journey_outline") or [])
    return dot


def _style_for_kind(kind: str | None) -> Dict[str, str]:
    kind = (kind or "").lower()
    if kind == "trigger":
        return {"shape": "oval", "fillcolor": "#D3F9D8"}
    if kind == "external":
        return {"shape": "diamond", "fillcolor": "#FFE8CC"}
    if kind == "control":
        return {"shape": "diamond", "fillcolor": "#C3DAFE"}
    return {"shape": "box", "fillcolor": "#E8F0FE"}


def _add_journey_outline(dot: Digraph, outline: List[Dict[str, Any]]) -> None:
    if not outline:
        return
    journey_id = "journey_outline"
    dot.node(
        journey_id,
        "\n".join(f"{idx+1}. {entry.get('label')}" for idx, entry in enumerate(outline)),
        shape="note",
        fillcolor="#EDF2F7",
        style="filled",
        fontname="Helvetica",
    )


def _safe_slug(value: str | None) -> str:
    if not value:
        return "item"
    return "".join(ch.lower() if ch.isalnum() else "-" for ch in value).strip("-") or "item"
