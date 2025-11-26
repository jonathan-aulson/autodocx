# autodocx/render/mkdocs.py
from __future__ import annotations
from collections import defaultdict
from pathlib import Path
from typing import Any, Dict, List, Sequence, Iterable, Tuple, Set
import json
import shutil
import subprocess
import sys

from autodocx.render import business_renderer
from autodocx.render.business_renderer import _aggregate_graph_features
from autodocx.visuals.graphviz_flows import ensure_assets_dir

# Helpers
def _safe_slug(s: str) -> str:
    import re
    if not s:
        return "unnamed"
    return re.sub(r"[^A-Za-z0-9._-]+", "_", s).strip("_")[:120]


def _copy_assets_into_docs(out_base: Path, docs_dir: Path) -> None:
    """
    Copy the repo-level assets (out_base/assets) into docs/ so MkDocs will include them.
    Overwrites destination if present.
    """
    src = out_base / "assets"
    dst = docs_dir / "assets"
    if not src.exists():
        return
    # Remove existing dst and copy tree
    if dst.exists():
        try:
            shutil.rmtree(dst)
        except Exception:
            pass
    try:
        shutil.copytree(src, dst)
    except Exception:
        # Fallback: copy file-by-file
        for p in src.rglob("*"):
            rel = p.relative_to(src)
            target = dst / rel
            target.parent.mkdir(parents=True, exist_ok=True)
            if p.is_dir():
                continue
            try:
                shutil.copy2(p, target)
            except Exception:
                pass


def _read_sirs(out_base: Path) -> List[Dict[str, Any]]:
    sir_dir = out_base / "sir"
    if not sir_dir.exists():
        return []
    out = []
    for f in sorted(sir_dir.glob("*.json")):
        try:
            j = json.loads(f.read_text(encoding="utf-8"))
            out.append(j)
        except Exception:
            continue
    return out


def _group_sirs_by_component(sirs: Sequence[Dict[str, Any]]) -> Dict[str, List[Dict[str, Any]]]:
    groups: Dict[str, List[Dict[str, Any]]] = {}
    for s in sirs:
        gid = s.get("component_or_service") or s.get("props", {}).get("component") or "ungrouped"
        groups.setdefault(gid or "ungrouped", []).append(s)
    return groups


def _collect_flow_diagrams(doc_base: Path, component_slug: str) -> List[str]:
    diagram_dir = doc_base / "assets" / "diagrams" / component_slug
    if not diagram_dir.exists():
        return []
    diagrams = []
    for svg in sorted(diagram_dir.glob("*.svg")):
        try:
            rel = svg.relative_to(doc_base).as_posix()
            diagrams.append(rel)
        except Exception:
            continue
    return diagrams


def _write_markdown_file(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8")


def _make_compact_table_from_agg(agg: Dict[str, Any]) -> str:
    # Build a compact 2-column markdown table for Graph Insights
    if not agg:
        return "| Metric | Value |\n|---|---|\n| Graph insights | not available |\n"
    rows: List[str] = []
    rows.append("| Metric | Value |")
    rows.append("|---|---|")
    rows.append(f"| Coverage | {agg.get('covered', 0)}/{agg.get('total', 0)} (R={agg.get('radius', 4)}) |")
    avg = agg.get("avg_nearest")
    rows.append(f"| Average nearest-marker distance | {avg if avg is not None else 'n/a'} |")
    rows.append(f"| p50 | {agg.get('p50','n/a')} |")
    rows.append(f"| p90 | {agg.get('p90','n/a')} |")
    rows.append(f"| Potential fragility (articulation nodes) | {agg.get('articulation', 0)} |")
    return "\n".join(rows)


def _format_evidence_list(evidence: Iterable[Any]) -> List[str]:
    lines: List[str] = []
    for ev in evidence or []:
        if isinstance(ev, dict):
            path = ev.get("path", "")
            lines.append(f"- {path} {ev.get('lines', '').strip()}")
        else:
            lines.append(f"- {ev}")
    return lines


def _collect_relationships_for_sir(sir: Dict[str, Any]) -> List[Dict[str, Any]]:
    return sir.get("relationships") or (sir.get("props") or {}).get("relationships") or []


def _relationship_highlight_lines(rels: List[Dict[str, Any]]) -> List[str]:
    if not rels:
        return []
    lines: List[str] = []
    kind_counts = defaultdict(int)
    for rel in rels:
        kind = ((rel.get("target") or {}).get("kind") or "unknown").lower()
        kind_counts[kind] += 1
    if kind_counts.get("http"):
        lines.append(f"- External HTTP/API calls: {kind_counts['http']}")
    data_total = sum(kind_counts.get(k, 0) for k in ("sql", "dataverse", "sharepoint", "cosmosdb"))
    if data_total:
        lines.append(f"- Data touchpoints: {data_total}")
    if kind_counts.get("workflow"):
        lines.append(f"- Child workflows invoked: {kind_counts['workflow']}")
    samples = []
    for rel in rels:
        src = (rel.get("source") or {}).get("name") or (rel.get("source") or {}).get("type")
        tgt = (rel.get("target") or {}).get("display") or (rel.get("target") or {}).get("ref")
        kind = ((rel.get("target") or {}).get("kind") or "dependency").lower()
        op = (rel.get("operation") or {}).get("type") or "touches"
        if src and tgt:
            samples.append(f"{src} {op} {tgt} [{kind}]")
        if len(samples) >= 3:
            break
    if samples:
        lines.append("- Sample flows:")
        for s in samples:
            lines.append(f"  - {s}")
    return lines


def _relationship_matrix_table(rels: List[Dict[str, Any]]) -> str:
    if not rels:
        return ""
    matrix = defaultdict(lambda: defaultdict(int))
    for rel in rels:
        kind = ((rel.get("target") or {}).get("kind") or "unknown").lower()
        op = ((rel.get("operation") or {}).get("type") or "touches").lower()
        matrix[kind][op] += 1
    rows = ["| Target Kind | Operation | Count |", "|-------------|-----------|-------|"]
    for kind in sorted(matrix.keys()):
        for op in sorted(matrix[kind].keys()):
            rows.append(f"| {kind} | {op} | {matrix[kind][op]} |")
    return "\n".join(rows)


def _collect_workflow_summary(sir: Dict[str, Any]) -> Dict[str, Any]:
    props = sir.get("props") or {}
    triggers = props.get("triggers") or []
    steps = props.get("steps") or []

    connectors: Set[str] = set()
    for step in steps:
        conn = step.get("connector") or step.get("type")
        if conn:
            connectors.add(str(conn))

    trigger_rows: List[Tuple[str, str, str]] = []
    for trig in triggers:
        name = str(trig.get("name") or trig.get("type") or "").strip()
        ttype = str(trig.get("type") or "").strip()
        detail = ""
        schedule = trig.get("schedule") or {}
        if schedule:
            freq = schedule.get("frequency")
            interval = schedule.get("interval")
            if freq or interval:
                freq_text = str(freq).title() if isinstance(freq, str) else str(freq or "")
                interval_text = str(interval or "")
                detail = (freq_text + (" every " + interval_text if interval_text else "")).strip()
        if not detail and trig.get("schema_props"):
            detail = f"Fields: {', '.join(trig.get('schema_props'))}"
        trigger_rows.append((name or "(unnamed)", ttype or "-", detail))

    step_rows: List[Tuple[int, str, str, str]] = []
    for idx, step in enumerate(steps, start=1):
        name = str(step.get("name") or "").strip() or f"Step {idx}"
        connector = str(step.get("connector") or "").strip() or "-"
        stype = str(step.get("type") or "").strip() or "-"
        method = str(step.get("method") or "").strip()
        summary = connector
        if method:
            summary = f"{connector} [{method}]".strip()
        step_rows.append((idx, name, summary, stype))

    return {
        "engine": props.get("engine") or props.get("wf_kind") or "",
        "step_count": len(steps),
        "connectors": sorted(c for c in connectors if c),
        "trigger_rows": trigger_rows,
        "step_rows": step_rows,
    }


def _render_triggers_table(rows: List[Tuple[str, str, str]]) -> str:
    if not rows:
        return ""
    lines = ["| Name | Type | Details |", "|---|---|---|"]
    for name, ttype, detail in rows:
        lines.append(f"| {name} | {ttype or '-'} | {detail or '-'} |")
    return "\n".join(lines)


def _render_steps_table(rows: List[Tuple[int, str, str, str]]) -> str:
    if not rows:
        return ""
    lines = ["| # | Step | Connector | Kind |", "|---|---|---|---|"]
    for idx, name, connector, stype in rows:
        lines.append(f"| {idx} | {name} | {connector or '-'} | {stype or '-'} |")
    return "\n".join(lines)


def _aggregate_component_insights(sirs: Sequence[Dict[str, Any]]) -> Dict[str, Any]:
    total_flows = len(sirs)
    total_steps = 0
    connectors: Set[str] = set()
    trigger_types: Set[str] = set()
    for sir in sirs:
        props = sir.get("props") or {}
        for step in props.get("steps") or []:
            total_steps += 1
            conn = step.get("connector") or step.get("type")
            if conn:
                connectors.add(str(conn))
        for trig in props.get("triggers") or []:
            ttype = trig.get("type")
            if ttype:
                trigger_types.add(str(ttype))
    return {
        "flow_count": total_flows,
        "step_count": total_steps,
        "connectors": sorted(connectors),
        "trigger_types": sorted(trigger_types),
    }


def _group_artifacts_by_component(artifacts: Sequence[Dict[str, Any]]) -> Dict[str, List[Dict[str, Any]]]:
    grouped: Dict[str, List[Dict[str, Any]]] = {}
    for art in artifacts or []:
        comp = art.get("component_or_service") or "ungrouped"
        grouped.setdefault(comp, []).append(art)
    return grouped


def _collect_narrative_from_artifacts(artifacts: Sequence[Dict[str, Any]], sirs: Sequence[Dict[str, Any]]) -> Dict[str, Any]:
    personas: List[Dict[str, Any]] = []
    journeys: List[Dict[str, Any]] = []
    ux: List[Dict[str, Any]] = []
    screenshots: List[Dict[str, Any]] = []
    http_endpoints: List[Dict[str, Any]] = []
    data_examples: List[Dict[str, Any]] = []

    for art in artifacts or []:
        personas.extend(art.get("personas") or [])
        journeys.extend(art.get("primary_journeys") or [])
        ux.extend(art.get("ux_summaries") or [])
        screenshots.extend(art.get("screenshots") or [])
        data_examples.extend(art.get("data_examples") or [])
        for endpoint in art.get("interfaces", {}).get("http_endpoints", []):
            http_endpoints.append(endpoint)

    if not journeys:
        for sir in sirs or []:
            story = (sir.get("props") or {}).get("user_story")
            if story:
                journeys.append({"story": story, "evidence": sir.get("file")})

    if not screenshots:
        for sir in sirs or []:
            snaps = (sir.get("props") or {}).get("screenshots") or []
            if isinstance(snaps, list):
                for snap in snaps:
                    if isinstance(snap, str):
                        screenshots.append({"path": snap, "caption": sir.get("name")})
            snapshot = (sir.get("props") or {}).get("ui_snapshot")
            if snapshot:
                screenshots.append({"path": snapshot, "caption": sir.get("name")})

    return {
        "personas": personas,
        "journeys": journeys,
        "ux": ux,
        "screenshots": screenshots,
        "http_endpoints": http_endpoints,
        "data_examples": data_examples,
    }


def _render_personas(personas: Sequence[Dict[str, Any]]) -> List[str]:
    lines: List[str] = []
    for persona in personas or []:
        name = persona.get("name") or "Persona"
        goals = persona.get("goals") or ""
        lines.append(f"- **{name}** – {goals or 'Goals TBD'}")
    return lines


def _render_journeys(journeys: Sequence[Dict[str, Any]]) -> List[str]:
    lines: List[str] = []
    for journey in journeys or []:
        story = journey.get("story")
        if story:
            lines.append(f"- {story}")
    return lines


def _render_http_endpoints_table(endpoints: Sequence[Dict[str, Any]]) -> str:
    if not endpoints:
        return ""
    lines = ["| Method | Path | Summary |", "|---|---|---|"]
    for ep in endpoints:
        lines.append(f"| {ep.get('method','')} | {ep.get('path','')} | {ep.get('summary','')} |")
    return "\n".join(lines)


def _render_data_examples(examples: Sequence[Dict[str, Any]]) -> List[str]:
    out: List[str] = []
    for example in examples or []:
        if "inputs" in example:
            out.append(f"- Inputs: {example['inputs']}")
        if "outputs" in example:
            out.append(f"- Outputs: {example['outputs']}")
        if "example_row" in example:
            out.append(f"- Sample row: `{example['example_row']}`")
    return out


def _render_screenshots_section(screenshots: Sequence[Dict[str, Any]]) -> List[str]:
    lines: List[str] = []
    for shot in screenshots or []:
        path = shot.get("path")
        if not path:
            continue
        caption = shot.get("caption") or ""
        rel_path = _resolve_asset_path(path)
        lines.append(f"![{caption}]({rel_path})")
    return lines


def _resolve_asset_path(path: str) -> str:
    if not path:
        return ""
    path = path.replace("\\", "/")
    if path.startswith("assets/"):
        return f"/{path}"
    if "assets/" in path:
        idx = path.find("assets/")
        return "/" + path[idx:]
    return path


def _relationships_mermaid(rels: Sequence[Dict[str, Any]]) -> str:
    edges = []
    seen = set()
    for rel in rels or []:
        src = (rel.get("source") or {}).get("name") or (rel.get("source") or {}).get("type")
        tgt = (rel.get("target") or {}).get("display") or (rel.get("target") or {}).get("ref")
        op = (rel.get("operation") or {}).get("type") or ""
        if not src or not tgt:
            continue
        key = (src, tgt, op)
        if key in seen:
            continue
        seen.add(key)
        label = op or "flows to"
        edges.append(f'    "{src}" -->|{label}| "{tgt}"')
    if not edges:
        return ""
    return "```mermaid\nflowchart LR\n" + "\n".join(edges[:20]) + "\n```"


def render_docs(out_base: Path, nodes: Sequence[Any], edges: Sequence[Any], artifacts: Sequence[Any], facets: Dict[str, Any]) -> None:
    """
    Render a minimal MkDocs-ready docs/ tree with:
      - docs/index.md summarizing facets
      - per-component docs under docs/components/<group>/<component>.md
      - assets copied under docs/assets (so SVGs produced earlier are available)
      - YAML front-matter is emitted at the top of each component page with facets/distance metadata
    """
    out_base = Path(out_base).resolve()
    docs_dir = out_base / "docs"
    docs_dir.mkdir(parents=True, exist_ok=True)

    # Copy assets so MkDocs can serve them
    _copy_assets_into_docs(out_base, docs_dir)

    # Read SIRs to discover components and their graph_features
    sirs = _read_sirs(out_base)
    groups = _group_sirs_by_component(sirs)
    artifacts_by_component = _group_artifacts_by_component(artifacts)

    # Write index.md with global facets summary and component links
    index_lines: List[str] = []
    index_lines.append("# Project Documentation")
    index_lines.append("")
    index_lines.append("## Rollup facets")
    index_lines.append("")
    index_lines.append("| Metric | Value |")
    index_lines.append("|---|---|")
    index_lines.append(f"| Score | {facets.get('score', 'n/a')} |")
    index_lines.append(f"| Ops | {facets.get('ops', 0)} |")
    index_lines.append(f"| APIs | {facets.get('apis', 0)} |")
    index_lines.append(f"| Events | {facets.get('events', 0)} |")
    index_lines.append("")
    index_lines.append("## Components")
    index_lines.append("")
    for gid, sirs_in_group in sorted(groups.items()):
        gid_slug = _safe_slug(gid)
        index_lines.append(f"- [{gid}](/components/{gid_slug}/{gid_slug}.md) - {len(sirs_in_group)} SIR(s)")
    _write_markdown_file(docs_dir / "index.md", "\n".join(index_lines))

    # Per-group and per-SIR pages
    for gid, sirs_in_group in groups.items():
        component_artifacts = artifacts_by_component.get(gid, [])
        narrative = _collect_narrative_from_artifacts(component_artifacts, sirs_in_group)
        gid_slug = _safe_slug(gid)
        group_dir = docs_dir / "components" / gid_slug
        group_dir.mkdir(parents=True, exist_ok=True)

        # Aggregate graph_features for the whole group (component-level)
        agg = _aggregate_graph_features(sirs_in_group) or {}
        # group page
        group_md: List[str] = []
        group_md.append("---")
        group_md.append(f'title: "{gid}"')
        group_md.append("facets:")
        group_md.append(f"  score: {facets.get('score', 0.0)}")
        group_md.append("distance:")
        group_md.append(f"  avg_nearest_distance: {agg.get('avg_nearest') if agg.get('avg_nearest') is not None else 'null'}")
        group_md.append(f"  covered: {agg.get('covered',0)}")
        group_md.append(f"  total: {agg.get('total',0)}")
        group_md.append("---")
        group_md.append("")
        group_md.append(f"# {gid}")
        group_md.append("")
        group_md.append("## Summary")
        summary = _aggregate_component_insights(sirs_in_group)
        group_md.append(f"- Workflow count: {summary['flow_count']}")
        group_md.append(f"- Total steps parsed: {summary['step_count']}")
        if summary["trigger_types"]:
            group_md.append(f"- Trigger types: {', '.join(summary['trigger_types'])}")
        if summary["connectors"]:
            group_md.append(f"- Connectors observed: {', '.join(summary['connectors'])}")
        group_md.append("")
        group_md.append("## How users interact")
        persona_lines = _render_personas(narrative.get("personas"))
        if persona_lines:
            group_md.extend(persona_lines)
        journey_lines = _render_journeys(narrative.get("journeys"))
        if journey_lines:
            group_md.extend(journey_lines)
        if not persona_lines and not journey_lines:
            group_md.append("_Narrative details will appear once journeys are inferred._")
        group_md.append("")
        group_md.append("## Screens and APIs they see")
        http_table = _render_http_endpoints_table(narrative.get("http_endpoints"))
        if http_table:
            group_md.append(http_table)
            group_md.append("")
        screen_lines = _render_screenshots_section(narrative.get("screenshots"))
        if screen_lines:
            group_md.extend(screen_lines)
            group_md.append("")
        else:
            group_md.append("_No screenshots provided yet._")
            group_md.append("")
        group_md.append("## Data they produce/consume")
        data_lines = _render_data_examples(narrative.get("data_examples"))
        if data_lines:
            group_md.extend(data_lines)
        else:
            group_md.append("- Data stories pending richer signals.")
        group_md.append("")
        group_md.append("## Graph Insights (component)")
        group_md.append("")
        group_md.append(_make_compact_table_from_agg(agg))
        group_md.append("")

        flow_diagrams = _collect_flow_diagrams(docs_dir, gid_slug)
        if flow_diagrams:
            group_md.append("## Comprehensive Workflow Diagrams")
            group_md.append("")
            for diagram in flow_diagrams:
                group_md.append(f"![Workflow diagram]({diagram})")
                group_md.append("")

        # Embed component overview SVG (if any)
        # The visuals module writes assets under assets/graphs/<group_slug>/<component_slug>/
        # We will attempt to locate a module-overview svg for this group (first sir's component_key)
        # Fallback: list any SVGs under assets/graphs/<gid_slug> and embed.
        assets_root = docs_dir / "assets" / "graphs" / gid_slug
        if assets_root.exists():
            # embed any module-overview or other svgs
            for svg in sorted(assets_root.rglob("*.svg")):
                # svg is under docs/assets/... path already
                rel = svg.relative_to(docs_dir).as_posix()
                group_md.append(f"![Flow]({rel})")
                group_md.append("")

        _write_markdown_file(group_dir / f"{gid_slug}.md", "\n".join(group_md))

        # Per-SIR pages
        for s in sirs_in_group:
            # Compose front-matter using SIR's graph_features (if present) and group facets
            sir_id = s.get("id") or s.get("name") or "sir"
            sir_slug = _safe_slug(sir_id)
            gf = s.get("graph_features") or {}
            fm_lines: List[str] = []
            title = s.get("name") or sir_id
            fm_lines.append("---")
            fm_lines.append(f'title: "{title}"')
            fm_lines.append("facets:")
            fm_lines.append(f"  score: {facets.get('score', 0.0)}")
            # Distance block (prefers per-SIR gf, fallback to group agg)
            fm_lines.append("distance:")
            fm_lines.append(f"  avg_nearest_distance: {gf.get('avg_distance_to_markers', agg.get('avg_nearest')) if gf or agg else 'null'}")
            acov = gf.get("anchor_coverage") or {}
            fm_lines.append(f"  anchors_within_r: {int(acov.get('anchors_within_r', 0)) if acov else 0}")
            fm_lines.append(f"  radius: {int(acov.get('radius', 4) if acov else 4)}")
            fm_lines.append("markers:")
            markers = gf.get("markers") or []
            if isinstance(markers, list):
                for m in markers:
                    # m may be dict or string
                    if isinstance(m, dict):
                        fm_lines.append(f"  - id: {m.get('id')}")
                        fm_lines.append(f"    type: {m.get('type')}")
                    else:
                        fm_lines.append(f"  - id: {m}")
            fm_lines.append("---")
            fm_lines.append("")
            body_lines: List[str] = []
            body_lines.append(f"# {title}")
            body_lines.append("")
            body_lines.append("## Graph Insights (SIR)")
            body_lines.append("")
            # Compact table for this SIR (prefers gf values)
            if gf:
                # build small table
                srows: List[str] = []
                srows.append("| Metric | Value |")
                srows.append("|---|---|")
                nmid = gf.get("nearest_marker_id") or "n/a"
                nmd = gf.get("nearest_marker_distance")
                nmd_val = nmd if (nmd is not None and nmd != float("inf")) else "n/a"
                srows.append(f"| Nearest marker | {nmid} |")
                srows.append(f"| Nearest distance | {nmd_val} |")
                dp = gf.get("distance_percentiles") or {}
                srows.append(f"| p50 | {dp.get('p50','n/a')} |")
                srows.append(f"| p90 | {dp.get('p90','n/a')} |")
                ac = gf.get("anchor_coverage") or {}
                srows.append(f"| Anchors within R | {ac.get('anchors_within_r', 0)} |")
                body_lines.extend(srows)
            else:
                body_lines.append("_No distance features available for this SIR._")
            body_lines.append("")
            user_story = (s.get("props") or {}).get("user_story")
            inputs_example = (s.get("props") or {}).get("inputs_example")
            outputs_example = (s.get("props") or {}).get("outputs_example")
            if user_story or inputs_example or outputs_example:
                body_lines.append("## User Story & Inputs")
                if user_story:
                    body_lines.append(f"- {user_story}")
                if inputs_example:
                    body_lines.append(f"- Inputs example: {inputs_example}")
                if outputs_example:
                    body_lines.append(f"- Outputs example: {outputs_example}")
                body_lines.append("")

            summary = _collect_workflow_summary(s)
            if summary["engine"] or summary["connectors"]:
                body_lines.append("## Overview")
                overview_items: List[str] = []
                if summary["engine"]:
                    overview_items.append(f"- Engine: {summary['engine']}")
                overview_items.append(f"- Steps parsed: {summary['step_count']}")
                if summary["connectors"]:
                    overview_items.append(f"- Connectors observed: {', '.join(summary['connectors'])}")
                body_lines.extend(overview_items or ["- No additional metadata captured."])
                body_lines.append("")

            trigger_table = _render_triggers_table(summary["trigger_rows"])
            if trigger_table:
                body_lines.append("## Triggers")
                body_lines.append("")
                body_lines.append(trigger_table)
                body_lines.append("")

            steps_table = _render_steps_table(summary["step_rows"])
            if steps_table:
                body_lines.append("## Steps")
                body_lines.append("")
                body_lines.append(steps_table)
                body_lines.append("")

            rels = _collect_relationships_for_sir(s)
            if rels:
                body_lines.append("## Relationship Highlights")
                body_lines.extend(_relationship_highlight_lines(rels))
                body_lines.append("")
                matrix = _relationship_matrix_table(rels)
                if matrix:
                    body_lines.append("## Dependency Matrix")
                    body_lines.append(matrix)
                    body_lines.append("")

            evidence_lines = _format_evidence_list(s.get("evidence") or [])
            if evidence_lines:
                body_lines.append("## Evidence")
                body_lines.extend(evidence_lines)
                body_lines.append("")

            rels_mermaid = _relationships_mermaid(rels)
            if rels_mermaid:
                body_lines.append("## Visual Journey")
                body_lines.append(rels_mermaid)
                body_lines.append("")

            sir_screens = (s.get("props") or {}).get("screenshots") or []
            if isinstance(sir_screens, list) and sir_screens:
                body_lines.append("## Screenshots")
                body_lines.extend(_render_screenshots_section([{"path": p, "caption": s.get("name")} for p in sir_screens if isinstance(p, str)]))
                body_lines.append("")
            elif (s.get("props") or {}).get("ui_snapshot"):
                body_lines.append("## Screenshots")
                body_lines.extend(_render_screenshots_section([{"path": (s.get("props") or {}).get("ui_snapshot"), "caption": s.get("name")}]))
                body_lines.append("")

            # Embed any SVGs produced for this SIR
            # Known path: docs/assets/graphs/<group_slug>/<component_slug>/*.svg
            comp_slug = _safe_slug(s.get("name") or s.get("id") or sir_slug)
            candidate_dir = docs_dir / "assets" / "graphs" / gid_slug / comp_slug
            if candidate_dir.exists():
                for svg in sorted(candidate_dir.glob("*.svg")):
                    rel = svg.relative_to(docs_dir).as_posix()
                    body_lines.append(f"![Flow]({rel})")
                    body_lines.append("")

            # Combine and write
            content = "\n".join(fm_lines + body_lines)
            _write_markdown_file(group_dir / f"{sir_slug}.md", content)


def build_mkdocs_site(out_base: Path) -> None:
    """
    Attempt to build a MkDocs static site for the generated docs/ tree.
    This is best-effort: we try to run 'mkdocs build' with cwd=out_base.
    """
    out_base = Path(out_base).resolve()
    cmd = ["mkdocs", "build", "-d", str(out_base / "site")]
    # If mkdocs.yml exists at out_base, allow mkdocs to pick it up.
    try:
        subprocess.run(cmd, cwd=str(out_base), check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        print(f"MkDocs site built at {out_base / 'site'}")
    except FileNotFoundError:
        print("mkdocs CLI not found. Install mkdocs to build the site (pip install mkdocs).")
    except subprocess.CalledProcessError as e:
        # Show a short diagnostic but don't raise to avoid hard failure
        print(f"mkdocs build failed: returncode={e.returncode}; stdout/stderr suppressed.")
    except Exception as e:
        print(f"mkdocs build encountered an error: {e}")
