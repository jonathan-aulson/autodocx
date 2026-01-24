from __future__ import annotations

from pathlib import Path
from typing import Any, Dict, List, Sequence

import os
import shutil
import subprocess

import html
import json
import re

try:
    from rich import print as rprint
except Exception:  # pragma: no cover
    def rprint(msg):
        print(msg)

try:
    from graphviz import Digraph
except Exception:
    Digraph = None

_GRAPHVIZ_WARNED = False
_DEFAULT_GRAPHVIZ_TIMEOUT_SEC = 30.0


def _warn_graphviz_missing() -> None:
    global _GRAPHVIZ_WARNED
    if _GRAPHVIZ_WARNED:
        return
    _GRAPHVIZ_WARNED = True
    try:
        rprint("[yellow]Graphviz Python bindings are not installed; workflow diagrams will be skipped. "
               "Install graphviz + graphviz-dev (e.g., via scripts/setup_wsl.sh) to restore diagrams.[/yellow]")
    except Exception:
        print("Graphviz Python bindings missing; diagrams skipped.")


def _graphviz_timeout_sec(settings: Dict[str, Any] | None = None) -> float:
    env_val = os.getenv("AUTODOCX_GRAPHVIZ_TIMEOUT_SEC")
    if env_val:
        try:
            return float(env_val)
        except ValueError:
            pass
    if isinstance(settings, dict):
        docs_cfg = settings.get("docs")
        if isinstance(docs_cfg, dict):
            visuals_cfg = docs_cfg.get("visuals")
            if isinstance(visuals_cfg, dict) and "graphviz_timeout_sec" in visuals_cfg:
                try:
                    return float(visuals_cfg["graphviz_timeout_sec"])
                except (TypeError, ValueError):
                    pass
        visuals_cfg = settings.get("visuals")
        if isinstance(visuals_cfg, dict) and "graphviz_timeout_sec" in visuals_cfg:
            try:
                return float(visuals_cfg["graphviz_timeout_sec"])
            except (TypeError, ValueError):
                pass
        df_cfg = settings.get("distance_features")
        if isinstance(df_cfg, dict):
            df_visuals = df_cfg.get("visuals")
            if isinstance(df_visuals, dict) and "graphviz_timeout_sec" in df_visuals:
                try:
                    return float(df_visuals["graphviz_timeout_sec"])
                except (TypeError, ValueError):
                    pass
    return _DEFAULT_GRAPHVIZ_TIMEOUT_SEC


def _render_svg(dot: Digraph, svg_path: Path, timeout_sec: float, label: str) -> bool:
    dot_cmd = shutil.which("dot")
    if not dot_cmd:
        _warn_graphviz_missing()
        return False
    timeout = None if timeout_sec <= 0 else timeout_sec
    try:
        proc = subprocess.run(
            [dot_cmd, "-Kdot", "-Tsvg", "-o", str(svg_path)],
            input=dot.source,
            text=True,
            capture_output=True,
            timeout=timeout,
            check=False,
        )
    except subprocess.TimeoutExpired:
        try:
            rprint(f"[yellow]Graphviz timed out after {timeout_sec:.0f}s for {label}; skipping SVG.[/yellow]")
        except Exception:
            print(f"Graphviz timed out after {timeout_sec:.0f}s for {label}; skipping SVG.")
        return False
    if proc.returncode != 0:
        err = (proc.stderr or "").strip()
        if err:
            try:
                rprint(f"[yellow]Graphviz failed for {label}: {err[:200]}[/yellow]")
            except Exception:
                print(f"Graphviz failed for {label}: {err[:200]}")
        return False
    return True


def render_flow_diagrams(
    exported_paths: Sequence[Path],
    out_dir: Path,
    settings: Dict[str, Any] | None = None,
) -> None:
    """
    Render SVG diagrams for each exported workflow graph JSON.
    Outputs are stored in out/diagrams/deterministic_svg/<component>/<workflow>.svg.
    """
    if Digraph is None:
        _warn_graphviz_missing()
        return
    timeout_sec = _graphviz_timeout_sec(settings)
    for json_path in exported_paths:
        try:
            data = json.loads(Path(json_path).read_text(encoding="utf-8"))
        except Exception:
            continue
        component = data.get("component") or "ungrouped"
        workflow = data.get("workflow_id") or json_path.stem
        assets_dir = Path(out_dir) / "diagrams" / "deterministic_svg" / _safe_slug(component)
        assets_dir.mkdir(parents=True, exist_ok=True)
        svg_path = assets_dir / f"{_safe_slug(workflow)}.svg"
        dot = _build_graphviz_diagram(data)
        if dot is None:
            continue
        if not _render_svg(dot, svg_path, timeout_sec, f"{component}/{workflow}"):
            continue


def _build_graphviz_diagram(data: Dict[str, Any]) -> Digraph | None:
    if Digraph is None:
        _warn_graphviz_missing()
        return None
    dot = Digraph(name="workflow", format="svg")
    dot.attr(
        "graph",
        rankdir="LR",
        splines="spline",
        nodesep="1.0",
        ranksep="1.0",
        pad="0.5",
        bgcolor="white",
        fontname="Helvetica",
        overlap="false",
        outputorder="edgesfirst",
        esep="1",
        forcelabels="true",
    )
    dot.attr(
        "node",
        fontname="Helvetica",
        style="rounded,filled",
        color="#4A5568",
        fontcolor="#1A202C",
        penwidth="1.2",
        shape="box",
    )
    dot.attr(
        "edge",
        fontname="Helvetica",
        color="#4A5568",
        arrowsize="0.8",
        penwidth="1.1",
        labeldistance="1.2",
        labelfontsize="10",
        labelfontname="Helvetica",
        labelfloat="false",
        decorate="true",
        minlen="1.2",
    )

    id_set = set()
    nodes_by_id: Dict[str, Dict[str, Any]] = {}
    for node in data.get("nodes", []):
        node_id = node.get("id")
        if not node_id or node_id in id_set:
            continue
        id_set.add(node_id)
        nodes_by_id[node_id] = node
        kind = node.get("kind")
        label = _prettify_label(node.get("name") or node_id)
        style_kwargs = _style_for_kind(kind)
        node_label, node_attrs = _build_node_attrs(
            label,
            node.get("ports") or [],
            style_kwargs,
            node,
        )
        dot.node(node_id, node_label, **node_attrs)

    edges_data = data.get("edges", []) or []
    direction_map = _dominant_out_directions(edges_data)
    for edge in edges_data:
        src = edge.get("source")
        tgt = edge.get("target")
        if not src or not tgt:
            continue
        edge_kind = edge.get("kind")
        style = "dashed" if edge_kind == "branch" else "solid"
        source_compass, target_compass = _edge_compass(
            edge_kind,
            nodes_by_id.get(src),
            nodes_by_id.get(tgt),
            direction_map,
            src,
            tgt,
        )
        source_ref = _node_ref_with_port(src, edge.get("source_port"), source_compass)
        target_ref = _node_ref_with_port(tgt, edge.get("target_port"), target_compass)
        edge_kwargs = {"style": style}
        label_payload = _edge_label_payload(edge_kind, edge.get("label"))
        edge_label = label_payload.pop("_label", "")
        edge_kwargs.update(label_payload)
        if edge_label:
            dot.edge(source_ref, target_ref, label=edge_label, **edge_kwargs)
        else:
            dot.edge(source_ref, target_ref, **edge_kwargs)

    _add_journey_outline(
        dot,
        data.get("journey_outline") or [],
        data.get("journey_outline_context") or "",
    )
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


def _add_journey_outline(
    dot: Digraph,
    outline: List[Dict[str, Any]],
    context: str = "",
) -> None:
    if not outline:
        return
    journey_id = "journey_outline"
    header = "Journey Outline"
    context_line = f"Context: {context.strip()}" if context.strip() else ""
    render_lines: List[str] = [header]
    if context_line:
        render_lines.append(context_line)
    for idx, entry in enumerate(outline):
        label = entry.get("label") or ""
        wrapped = _wrap_outline_text(label, width=90).replace("\n", "\\l")
        prefix = f"{idx + 1}. " if len(outline) > 1 else ""
        render_lines.append(f"{prefix}{wrapped}")
    label_text = "\\l".join([line for line in render_lines if line]) + "\\l"
    dot.node(
        journey_id,
        label_text,
        shape="note",
        fillcolor="#EDF2F7",
        style="filled",
        fontname="Helvetica",
        fontsize="12",
    )


def _safe_slug(value: str | None) -> str:
    if not value:
        return "item"
    return "".join(ch.lower() if ch.isalnum() else "-" for ch in value).strip("-") or "item"


def _build_node_attrs(
    label: str,
    ports: List[Dict[str, Any]],
    style: Dict[str, str],
    node: Dict[str, Any],
) -> tuple[str, Dict[str, str]]:
    if not ports:
        return label, style
    fill = style.get("fillcolor", "#FFFFFF")
    subtitle = _node_secondary_lines(node)
    html_label = _build_port_table(label, ports, fill, subtitle)
    # HTML-like labels ignore shape/fillcolor, so switch to plaintext node
    attrs = {"shape": "plain"}
    return html_label, attrs


def _build_port_table(
    label: str,
    ports: List[Dict[str, Any]],
    fill: str,
    subtitle_lines: List[str] | None = None,
) -> str:
    in_ports = [p for p in ports if (p.get("direction") or "in").lower() == "in" and p.get("name")]
    out_ports = [p for p in ports if (p.get("direction") or "out").lower() == "out"]
    rows: List[str] = []
    if in_ports:
        hidden = "".join(
            f'<td port="{html.escape(p.get("name") or "")}" height="0" width="0"></td>' for p in in_ports
        )
        rows.append(f"<tr>{hidden}</tr>")
    colspan = max(1, len(out_ports)) if out_ports else 1
    body_label = f"<b>{html.escape(label)}</b>"
    if subtitle_lines:
        detail = "<br/>".join(html.escape(_trim_line(line)) for line in subtitle_lines if line)
        if detail:
            body_label = f"{body_label}<br/><font point-size='10'>{detail}</font>"
    rows.append(
        f'<tr><td port="body" bgcolor="{fill}" colspan="{colspan}" cellpadding="8" align="center" width="220">{body_label}</td></tr>'
    )
    hidden_out_ports = [
        p
        for p in out_ports
        if p.get("name") and _hide_port_label(p.get("label"))
    ]
    if hidden_out_ports:
        cells = "".join(
            f'<td port="{html.escape(p.get("name") or "")}" height="0" width="0"></td>'
            for p in hidden_out_ports
        )
        rows.append(f"<tr>{cells}</tr>")
    labeled_out_ports = [
        p
        for p in out_ports
        if p.get("name") and not _hide_port_label(p.get("label"))
    ]
    if labeled_out_ports:
        cells = "".join(
            f'<td port="{html.escape(p["name"])}" bgcolor="#F7FAFC" cellpadding="4">'
            f'{html.escape(_prettify_label(p.get("label") or ""))}</td>'
            for p in labeled_out_ports
        )
        rows.append(f"<tr>{cells}</tr>")
    table = (
        "<table border='0' cellborder='1' cellspacing='0' cellpadding='0' style='rounded'>"
        + "".join(rows)
        + "</table>"
    )
    return f"<{table}>"


def _hide_port_label(label: str | None) -> bool:
    text = (label or "").strip()
    if not text:
        return True
    normalized = text.lower()
    return normalized in {"next", "default", "continue"}


def _node_secondary_lines(node: Dict[str, Any]) -> List[str]:
    lines: List[str] = []
    metadata = node.get("metadata") or {}
    base_label = _prettify_label(node.get("name") or "")
    friendly = metadata.get("friendly_display") or ""
    if friendly and friendly.lower() != base_label.lower():
        lines.append(_trim_line(friendly))
    summaries = metadata.get("relationship_summaries") or []
    for summary in summaries or []:
        if isinstance(summary, str):
            lines.append(summary)
    method = (metadata.get("method") or "")[:10].upper()
    url = metadata.get("url_or_path")
    if method and url:
        lines.append(f"{method} {url}")
    elif url:
        lines.append(str(url))
    if not summaries:
        schema_props = metadata.get("schema_properties") or []
        if schema_props:
            lines.append(f"Fields: {', '.join(schema_props[:3])}")
        else:
            body_fields = metadata.get("body_fields") or metadata.get("inputs_keys") or []
            if body_fields:
                lines.append(f"Fields: {', '.join(body_fields[:3])}")
    connector = metadata.get("connection_display") or node.get("connector")
    if connector:
        label = _prettify_label(connector)
        if label:
            existing = [line.lower() for line in lines if isinstance(line, str)]
            if label.lower() not in existing:
                lines.append(label)
    return lines[:2]


def _trim_line(value: str, limit: int = 80) -> str:
    text = value.strip()
    if len(text) <= limit:
        return text
    return text[: limit - 1] + "…"


def _wrap_outline_text(value: str, width: int = 60) -> str:
    import textwrap

    text = value.strip()
    if not text:
        return ""
    return "\n".join(textwrap.wrap(text, width=width, break_long_words=False)) or text


def _node_ref_with_port(node_id: str, port: str | None, compass: str | None = None) -> str:
    if port:
        if compass:
            return f"{node_id}:{port}:{compass}"
        return f"{node_id}:{port}"
    if compass:
        return f"{node_id}:{compass}"
    return node_id


def _edge_compass(
    kind: str | None,
    source_node: Dict[str, Any] | None,
    target_node: Dict[str, Any] | None,
    direction_map: Dict[str, str],
    source_id: str | None,
    target_id: str | None,
) -> tuple[str | None, str | None]:
    if kind == "branch":
        return ("s", "n")
    source_pref = direction_map.get(source_id) or _preferred_out_compass(source_node)
    target_pref = direction_map.get(target_id)
    if not target_pref:
        target_pref = _preferred_out_compass(target_node)
    return (source_pref, _opposite_compass(target_pref))


def _preferred_out_compass(node: Dict[str, Any] | None) -> str:
    if not node:
        return "e"
    kind = (node.get("kind") or "").lower()
    meta = node.get("metadata") or {}
    control_type = (meta.get("control_type") or "").lower()
    if kind == "trigger":
        return "e"
    if kind == "external":
        return "e"
    if kind == "control" or control_type in {"foreach", "switch", "scope", "if"}:
        return "s"
    return "e"


def _opposite_compass(compass: str | None) -> str:
    opposite = {"n": "s", "s": "n", "e": "w", "w": "e"}
    return opposite.get((compass or "").lower(), "w")


def _edge_label_payload(kind: str | None, label: str | None) -> Dict[str, str]:
    payload: Dict[str, str] = {}
    text = _prettify_label(label)
    if not text:
        return payload
    if (kind or "").lower() == "branch":
        color = _branch_color(text)
        payload["taillabel"] = text
        payload["labeldistance"] = "0.15"
        payload["labelangle"] = "0"
        payload["labelfontsize"] = "10"
        payload["color"] = color
        payload["fontcolor"] = color
        payload["penwidth"] = "1.6"
        payload["_label"] = ""
        return payload
    payload["_label"] = text
    payload["labeldistance"] = "0.35"
    payload["labelangle"] = "0"
    return payload


def _branch_color(label: str) -> str:
    low = label.strip().lower()
    if low in {"true", "yes", "success", "ok", "pass"}:
        return "#2E7D32"
    if low in {"false", "no", "failure", "fail", "error", "else"}:
        return "#C62828"
    return "#7C8FB5"


def _dominant_out_directions(edges: Sequence[Dict[str, Any]]) -> Dict[str, str]:
    weights: Dict[str, Dict[str, int]] = {}
    for edge in edges or []:
        src = edge.get("source")
        if not src:
            continue
        kind = (edge.get("kind") or "").lower()
        source_port = (edge.get("source_port") or "").lower()
        direction = "e"
        if kind == "branch" or source_port.startswith("branch_"):
            direction = "s"
        weights.setdefault(src, {}).setdefault(direction, 0)
        weights[src][direction] += 2 if direction == "s" else 1
    return {node: max(dir_counts.items(), key=lambda item: item[1])[0] for node, dir_counts in weights.items()}


def _prettify_label(label: str | None) -> str:
    if not label:
        return ""
    text = str(label)
    text = text.replace("_", " ").replace("-", " ").replace(".", " · ")
    text = re.sub(r"(?<=[a-z0-9])([A-Z])", r" \1", text)
    text = re.sub(r"\s{2,}", " ", text)
    return text.strip().title() if text.isupper() else text.strip()
