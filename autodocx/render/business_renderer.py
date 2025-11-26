# autodocx/render/business_renderer.py
from __future__ import annotations
from collections import defaultdict
from pathlib import Path
from typing import Dict, Any, List, Optional

from autodocx.visuals.graphviz_flows import (
    render_bw_process_flow_svg,
    render_component_overview_svg,
    render_relationship_sequence_svg,
    render_rollup_journey_svgs,
)


def _anchor_eid(eid: str) -> str:
    return eid.replace(":", "_").replace("/", "_").replace("\\", "_").replace("#", "_")


def _safe(val: Any, default: str = "") -> str:
    return str(val) if val is not None else default


def _safe_list(x: Any) -> List[Any]:
    if x is None:
        return []
    if isinstance(x, list):
        return x
    return [x]


def _collect_relationships_from_sirs(sirs: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
    rels: List[Dict[str, Any]] = []
    for sir in sirs or []:
        rels.extend(sir.get("relationships") or (sir.get("props") or {}).get("relationships") or [])
    return rels


def _relationship_highlights(rels: List[Dict[str, Any]]) -> List[str]:
    if not rels:
        return []
    lines: List[str] = []
    kind_counts = defaultdict(int)
    for rel in rels:
        kind = ((rel.get("target") or {}).get("kind") or "unknown").lower()
        kind_counts[kind] += 1
    if kind_counts:
        if kind_counts.get("http"):
            lines.append(f"- External HTTP/API calls: {kind_counts['http']}")
        data_total = sum(kind_counts.get(k, 0) for k in ("sql", "dataverse", "sharepoint"))
        if data_total:
            lines.append(f"- Data touchpoints (SQL/Dataverse/SharePoint): {data_total}")
        if kind_counts.get("workflow"):
            lines.append(f"- Child workflows invoked: {kind_counts['workflow']}")
    if not lines:
        lines.append("- Relationships detected but no categorized summary available.")

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
        for sample in samples:
            lines.append(f"  - {sample}")
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


def _mermaid_diagram_from_relationships(rels: List[Dict[str, Any]], limit: int = 8) -> str:
    if not rels:
        return ""
    edges = []
    seen = set()
    for rel in rels[:limit]:
        source = (rel.get("source") or {}).get("name") or rel.get("source", {}).get("type") or "source"
        target = (rel.get("target") or {}).get("display") or rel.get("target", {}).get("ref") or "target"
        op = (rel.get("operation") or {}).get("type") or ""
        edge = (source, target, op)
        if edge in seen:
            continue
        seen.add(edge)
        edges.append(f'    "{source}" -->|{op}| "{target}"')
    if not edges:
        return ""
    return "```mermaid\nflowchart LR\n" + "\n".join(edges) + "\n```"


def _process_flow_summary_lines(flows: List[Dict[str, Any]]) -> List[str]:
    lines: List[str] = []
    for flow in flows or []:
        source = flow.get("source")
        target = flow.get("target")
        if not source or not target:
            continue
        op = flow.get("operation") or "flows to"
        lines.append(f"- {source} -> {target} ({op})")
    return lines


def _process_flow_mermaid(flows: List[Dict[str, Any]], limit: int = 12) -> str:
    edges: List[str] = []
    seen = set()
    for flow in flows or []:
        if len(edges) >= limit:
            break
        source = (flow.get("source") or "").replace('"', "'")
        target = (flow.get("target") or "").replace('"', "'")
        if not source or not target:
            continue
        op = (flow.get("operation") or "").replace('"', "'")
        key = (source, target, op)
        if key in seen:
            continue
        seen.add(key)
        edges.append(f'    "{source}" -->|{op}| "{target}"')
    if not edges:
        return ""
    return "```mermaid\nflowchart LR\n" + "\n".join(edges) + "\n```"


def _integration_summary_table(summary: List[Dict[str, Any]]) -> str:
    if not summary:
        return ""
    rows = ["| Integration Kind | Library | Count |", "|------------------|---------|-------|"]
    for entry in summary:
        rows.append(
            f"| {entry.get('integration_kind') or '-'} | {entry.get('library') or '-'} | {entry.get('count', 0)} |"
        )
    return "\n".join(rows)


def _integration_summary_mermaid(summary: List[Dict[str, Any]], group_label: str, limit: int = 10) -> str:
    if not summary:
        return ""
    root = (group_label or "Group").replace('"', "'")
    edges: List[str] = []
    seen = set()
    for entry in summary:
        if len(edges) >= limit:
            break
        lib = (entry.get("library") or entry.get("integration_kind") or "Integration").replace('"', "'")
        kind = (entry.get("integration_kind") or "").replace('"', "'")
        count = entry.get("count", 0)
        key = (lib, kind)
        if key in seen:
            continue
        seen.add(key)
        label = f"{kind} ({count})" if kind else str(count)
        edges.append(f'    "{root}" -->|{label}| "{lib}"')
    if not edges:
        return ""
    return "```mermaid\nflowchart LR\n" + "\n".join(edges) + "\n```"


def _collect_code_entities(sirs: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
    entries: List[Dict[str, Any]] = []
    seen = set()
    for sir in sirs or []:
        props = sir.get("props") or sir
        kind = (sir.get("kind") or props.get("kind") or "").lower()
        if kind != "code_entity":
            continue
        name = props.get("name") or sir.get("name")
        if not name or name in seen:
            continue
        seen.add(name)
        entries.append(
            {
                "name": name,
                "language": props.get("language", ""),
                "docstring": props.get("docstring", ""),
            }
        )
    return entries


def _collect_ui_components_from_sirs(sirs: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
    entries: List[Dict[str, Any]] = []
    for sir in sirs or []:
        props = sir.get("props") or sir
        kind = (sir.get("kind") or props.get("kind") or "").lower()
        if kind != "ui_component":
            continue
        entries.append(
            {
                "name": props.get("name"),
                "framework": props.get("framework"),
                "routes": props.get("routes") or [],
                "selector": props.get("selector"),
            }
        )
    return entries


def _collect_integrations_from_sirs(sirs: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
    entries: List[Dict[str, Any]] = []
    for sir in sirs or []:
        props = sir.get("props") or sir
        kind = (sir.get("kind") or props.get("kind") or "").lower()
        if kind != "integration":
            continue
        entries.append(
            {
                "library": props.get("library"),
                "integration_kind": props.get("integration_kind"),
                "language": props.get("language"),
            }
        )
    return entries


def _collect_process_diagrams_from_sirs(sirs: List[Dict[str, Any]]) -> List[str]:
    diagrams: List[str] = []
    for sir in sirs or []:
        props = sir.get("props") or sir
        kind = (sir.get("kind") or props.get("kind") or "").lower()
        if kind != "process_diagram":
            continue
        diagrams.append(props.get("name") or sir.get("name") or "Unnamed diagram")
    return diagrams


def _collect_business_entities_from_sirs(sirs: List[Dict[str, Any]]) -> List[Dict[str, str]]:
    entities: List[Dict[str, str]] = []
    seen = set()
    for sir in sirs or []:
        props = sir.get("props") or sir
        kind = (sir.get("kind") or props.get("kind") or "").lower()
        if kind != "business_entity":
            continue
        name = props.get("name") or sir.get("name")
        if name and name not in seen:
            seen.add(name)
            entities.append(
                {
                    "name": name,
                    "source": props.get("source") or "",
                }
            )
    return entities


def _collect_personas_from_sirs(sirs: List[Dict[str, Any]]) -> List[Dict[str, str]]:
    personas: Dict[str, Dict[str, str]] = {}
    for sir in sirs or []:
        props = sir.get("props") or {}
        for role in props.get("roles") or []:
            if role not in personas:
                personas[role] = {
                    "name": role,
                    "goals": props.get("user_story") or "",
                }
    return list(personas.values())


def _collect_user_stories_from_sirs(sirs: List[Dict[str, Any]]) -> List[str]:
    stories: List[str] = []
    for sir in sirs or []:
        story = (sir.get("props") or {}).get("user_story")
        if story:
            stories.append(story)
    return stories


def _collect_screenshots_from_sirs(sirs: List[Dict[str, Any]]) -> List[Dict[str, str]]:
    shots: List[Dict[str, str]] = []
    for sir in sirs or []:
        props = sir.get("props") or {}
        if props.get("screenshots"):
            for shot in props.get("screenshots") or []:
                if isinstance(shot, str):
                    shots.append({"path": shot, "caption": sir.get("name")})
                elif isinstance(shot, dict) and shot.get("path"):
                    shots.append({"path": shot["path"], "caption": shot.get("caption") or sir.get("name")})
        elif props.get("ui_snapshot"):
            shots.append({"path": props["ui_snapshot"], "caption": sir.get("name")})
    return shots


def _render_persona_lines(personas: List[Dict[str, str]]) -> List[str]:
    lines: List[str] = []
    for persona in personas or []:
        name = persona.get("name") or "Persona"
        goals = persona.get("goals") or ""
        lines.append(f"- **{name}** – {goals or 'Goals forthcoming'}")
    return lines


def _render_screenshot_markdown(screenshots: List[Dict[str, str]]) -> List[str]:
    lines: List[str] = []
    for shot in screenshots or []:
        path = shot.get("path")
        if not path:
            continue
        caption = shot.get("caption") or ""
        rel = _resolve_asset_path(path)
        lines.append(f"![{caption}]({rel})")
    return lines


def _resolve_asset_path(path: str) -> str:
    if not path:
        return ""
    path = path.replace("\\", "/")
    if path.startswith("assets/"):
        return "/" + path
    if "assets/" in path:
        idx = path.find("assets/")
        return "/" + path[idx:]
    return path


def _component_title(c_json: Dict[str, Any], component_key: str) -> str:
    title = c_json.get("title") or c_json.get("component", {}).get("name") or component_key
    return str(title)


def _confidence_line(llm_subscore: Optional[float]) -> str:
    if llm_subscore is None:
        return "> Confidence Score: N/A"
    return f"> **Confidence Score:** {llm_subscore:.2f} — *(see scoring table at bottom for details)*"


def _derive_overview_lines(c_json: Dict[str, Any]) -> List[str]:
    claims = []
    for w in _safe_list(c_json.get("component", {}).get("what_it_does")):
        if isinstance(w, dict) and w.get("claim"):
            claims.append(str(w["claim"]))
        if len(claims) >= 3:
            break
    if not claims:
        return ["Overview not available from current evidence."]
    lines = []
    lines.append("This component provides the following core capabilities:")
    for c in claims:
        lines.append(f"- {c}")
    return lines


def _e2e_flow_table(c_json: Dict[str, Any]) -> str:
    rows = []
    for w in _safe_list(c_json.get("component", {}).get("what_it_does")):
        if not isinstance(w, dict):
            continue
        action = _safe(w.get("claim"), "Action")
        if "receive" in action.lower() or "request" in action.lower():
            input_hint = "Request"
            output_hint = "Structured response"
        elif "query" in action.lower() or "retrieve" in action.lower() or "get " in action.lower():
            input_hint = "Lookup"
            output_hint = "Data"
        elif "sort" in action.lower():
            input_hint = "List or record"
            output_hint = "Ordered result"
        else:
            input_hint = "Input"
            output_hint = "Output"
        rows.append((input_hint, action, output_hint))

    if not rows:
        return ""

    out = ["| Step | Input | Action | Output |", "|------|-------|--------|--------|"]
    for i, (i1, a1, o1) in enumerate(rows, start=1):
        out.append(f"| {i} | {i1} | {a1} | {o1} |")
    return "\n".join(out)


def _interdependencies_block(sirs: List[Dict[str, Any]]) -> List[str]:
    counts = {}
    for s in sirs or []:
        for t in s.get("triggers", []) or []:
            typ = (t.get("type") or t.get("name") or "trigger").lower()
            if "http" in typ:
                counts["HTTP"] = counts.get("HTTP", 0) + 1
            elif "jms" in typ:
                counts["JMS"] = counts.get("JMS", 0) + 1
            else:
                counts["Trigger"] = counts.get("Trigger", 0) + 1
        for st in s.get("steps", []) or []:
            conn = (st.get("connector") or st.get("type") or "").lower()
            if "http" in conn:
                counts["HTTP"] = counts.get("HTTP", 0) + 1
            elif "jdbc" in conn:
                counts["JDBC"] = counts.get("JDBC", 0) + 1
            elif "jms" in conn:
                counts["JMS"] = counts.get("JMS", 0) + 1
            elif "file" in conn:
                counts["File"] = counts.get("File", 0) + 1

    lines = []
    if counts:
        lines.append("- Touchpoints:")
        for k in sorted(counts.keys()):
            lines.append(f"  - {k}: {counts[k]}")
    else:
        lines.append("- Touchpoints: Not detected in current evidence")
    return lines


def _unknowns_block(c_json: Dict[str, Any]) -> List[str]:
    out = []
    for w in _safe_list(c_json.get("component", {}).get("what_it_does")):
        if isinstance(w, dict):
            eids = _safe_list(w.get("evidence_ids"))
            if not eids:
                out.append(f"- {w.get('claim','Unknown claim')} — Evidence not cited.")
    if not out:
        out.append("- None identified.")
    return out


# -------------------------
# Distance-features helpers
# -------------------------

def _aggregate_graph_features(sirs: List[Dict[str, Any]]) -> Optional[Dict[str, Any]]:
    """
    Aggregate per-SIR graph_features into concise metrics for the component page.
    Returns None if no graph_features are present.
    """
    gfs = []
    for s in sirs or []:
        gf = s.get("graph_features") or {}
        # Accept either nearest_marker_distance or avg_distance_to_markers as indication
        if gf and isinstance(gf, dict) and ("nearest_marker_distance" in gf or "avg_distance_to_markers" in gf or "anchor_coverage" in gf):
            gfs.append(gf)

    if not gfs:
        return None

    # Coverage within radius: count SIRs with at least one anchor within R hops
    def _radius(g):
        ac = g.get("anchor_coverage") or {}
        return int(ac.get("radius", 4))

    radius_vals = [_radius(g) for g in gfs if isinstance(g, dict)]
    radius = max(radius_vals) if radius_vals else 4

    covered = 0
    nearest_distances: List[float] = []
    p50s: List[float] = []
    p90s: List[float] = []
    articulation = 0

    for g in gfs:
        ac = g.get("anchor_coverage") or {}
        anchors_within_r = int(ac.get("anchors_within_r", 0) or 0)
        if anchors_within_r >= 1:
            covered += 1

        # Support both 'nearest_marker_distance' and 'nearest_marker_distance' alternative keys
        nd = g.get("nearest_marker_distance") or g.get("nearest_distance") or g.get("avg_distance_to_markers")
        if isinstance(nd, (int, float)) and nd != float("inf"):
            nearest_distances.append(float(nd))

        dp = (g.get("distance_percentiles") or {})
        p50v = dp.get("p50", None)
        p90v = dp.get("p90", None)
        if isinstance(p50v, (int, float)) and p50v != float("inf"):
            p50s.append(float(p50v))
        if isinstance(p90v, (int, float)) and p90v != float("inf"):
            p90s.append(float(p90v))

        rf = g.get("risk_flags") or {}
        if bool(rf.get("is_articulation", False)):
            articulation += 1

    total = len(gfs)
    avg_nearest = round(sum(nearest_distances) / len(nearest_distances), 3) if nearest_distances else None
    p50 = round(sum(p50s) / len(p50s), 3) if p50s else None
    p90 = round(sum(p90s) / len(p90s), 3) if p90s else None

    # Pull marker metadata from first gf (markers tend to be shared)
    marker_info = None
    for g in gfs:
        m = g.get("markers")
        if m:
            marker_info = m
            break

    return {
        "radius": radius,
        "covered": covered,
        "total": total,
        "avg_nearest": avg_nearest,
        "p50": p50,
        "p90": p90,
        "articulation": articulation,
        "markers": marker_info or [],
    }


def _compact_graph_insights_table(agg: Dict[str, Any]) -> str:
    """
    Return a compact 2-column markdown table for graph insights.
    """
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


def _render_yaml_front_matter_for_component(title: str, facets: Optional[Dict[str, Any]], agg: Optional[Dict[str, Any]]) -> List[str]:
    """
    Build YAML front-matter lines (without final newline) for a component page.
    """
    fm: List[str] = []
    fm.append("---")
    fm.append(f'title: "{title}"')
    # facets block
    fm.append("facets:")
    fm.append(f"  score: {facets.get('score', 0.0) if facets else 0.0}")
    # distance block
    fm.append("distance:")
    fm.append(f"  avg_nearest_distance: {agg.get('avg_nearest') if agg and agg.get('avg_nearest') is not None else 'null'}")
    fm.append(f"  covered: {agg.get('covered',0) if agg else 0}")
    fm.append(f"  total: {agg.get('total',0) if agg else 0}")
    # markers: include marker ids if available
    fm.append("markers:")
    markers = (agg.get("markers") if agg else []) or []
    if isinstance(markers, list) and markers:
        for m in markers:
            if isinstance(m, dict):
                mid = m.get("id", "")
                mtype = m.get("type", "")
                fm.append(f"  - id: {mid}")
                fm.append(f"    type: {mtype}")
            else:
                fm.append(f"  - id: {m}")
    else:
        fm.append("  - []")
    fm.append("---")
    return fm


def _render_yaml_front_matter_for_sir(title: str, facets: Optional[Dict[str, Any]], gf: Optional[Dict[str, Any]]) -> List[str]:
    """
    Build YAML front-matter lines for a SIR (process) page.
    """
    fm: List[str] = []
    fm.append("---")
    fm.append(f'title: "{title}"')
    fm.append("facets:")
    fm.append(f"  score: {facets.get('score', 0.0) if facets else 0.0}")
    fm.append("distance:")
    # preferentially use per-SIR gf values, fallback to null
    avg = gf.get("avg_distance_to_markers") if gf else None
    if avg is None:
        avg = gf.get("avg_nearest") if gf else None
    fm.append(f"  avg_nearest_distance: {avg if avg is not None else 'null'}")
    acov = (gf.get("anchor_coverage") or {}) if gf else {}
    fm.append(f"  anchors_within_r: {int(acov.get('anchors_within_r', 0) if acov else 0)}")
    fm.append(f"  radius: {int(acov.get('radius', 4) if acov else 4)}")
    fm.append("markers:")
    markers = gf.get("markers") if gf else []
    if isinstance(markers, list) and markers:
        for m in markers:
            if isinstance(m, dict):
                fm.append(f"  - id: {m.get('id')}")
                fm.append(f"    type: {m.get('type')}")
            else:
                fm.append(f"  - id: {m}")
    else:
        fm.append("  - []")
    fm.append("---")
    return fm


def render_business_component_page(
    out_docs_dir: Path,
    group_id: str,
    component_key: str,
    c_json: Dict[str, Any],
    sirs: List[Dict[str, Any]],
    evidence_md_filename: Optional[str] = None,
    facets: Optional[Dict[str, Any]] = None,
    settings: Optional[Dict[str, Any]] = None,
) -> None:
    """
    Render the business-facing component page, including YAML front-matter with facets
    and distance metadata so MkDocs and LLM rollups see the same structured info.
    settings is optional and is passed to graphviz rendering functions (for marker highlighting).
    """
    gid = group_id.replace(" ", "_")
    cid = component_key.replace(" ", "_").replace("/", "_")

    page_dir = out_docs_dir / "components" / gid
    page_dir.mkdir(parents=True, exist_ok=True)
    page_path = page_dir / f"{cid}.md"

    title = _component_title(c_json, component_key)
    llm_sub = c_json.get("llm_subscore")

    # Generate per-process flows (SVG) and a component overview (SVG) - pass settings for highlighting
    svg_paths = []
    for sir in sirs or []:
        svg_rel = render_bw_process_flow_svg(sir, out_docs_dir, group_id, component_key, settings=settings)
        if svg_rel:
            svg_paths.append(svg_rel)

    overview_svg = render_component_overview_svg(sirs or [], out_docs_dir, group_id, component_key, settings=settings)

    # Aggregate graph features for front-matter and summary
    agg = _aggregate_graph_features(sirs) or {}

    # Build page lines
    md: List[str] = []

    # YAML front-matter (component-level)
    md.extend(_render_yaml_front_matter_for_component(title, facets, agg))
    md.append("")  # blank line after frontmatter

    md.append(f"# {title}")
    md.append("<!-- CONFIDENCE_INLINE -->")
    md.append(_confidence_line(llm_sub))
    md.append("")
    md.append("## 📌 Purpose")
    md.append("This guide explains the component from a business perspective. It focuses on end-to-end flow and what it delivers.")
    md.append("")
    md.append("## 👥 Audience")
    md.append("- Business owners")
    md.append("- Product managers")
    md.append("- Operations teams")
    md.append("- Compliance and audit reviewers")
    md.append("")
    md.append("## 🔑 Key Questions this answers")
    md.append("- What does this component do, end-to-end?")
    md.append("- What inputs does it require and what outputs does it produce?")
    md.append("- What other processes or services does it depend on?")
    md.append("- What value does it deliver?")
    md.append("")
    md.append("## 🛠️ Overview")
    md.extend(_derive_overview_lines(c_json))
    md.append("")
    personas = _collect_personas_from_sirs(sirs)
    stories = _collect_user_stories_from_sirs(sirs)
    if personas or stories:
        md.append("## 👥 Personas & Journeys")
        persona_lines = _render_persona_lines(personas)
        if persona_lines:
            md.extend(persona_lines)
        for story in stories[:5]:
            md.append(f"- Journey: {story}")
        if not persona_lines and not stories:
            md.append("_Persona insights pending richer evidence._")
        md.append("")
    screenshots = _collect_screenshots_from_sirs(sirs)
    if screenshots:
        md.append("## 🖼️ What Users See")
        md.extend(_render_screenshot_markdown(screenshots))
        md.append("")
    md.append("## 🔄 End-to-End Flow")
    flow_tbl = _e2e_flow_table(c_json)
    if flow_tbl:
        md.append(flow_tbl)
    else:
        md.append("_Flow will be captured as more evidence is parsed._")
    md.append("")
    md.append("## 🔗 Interdependencies & Data Touchpoints")
    md.extend(_interdependencies_block(sirs))
    md.append("")

    ui_components = _collect_ui_components_from_sirs(sirs)
    if ui_components:
        md.append("## 🎛 UI Entry Points")
        md.append("| Component | Framework | Routes |")
        md.append("|-----------|-----------|--------|")
        for comp in ui_components:
            routes = ", ".join(comp.get("routes") or []) or "-"
            md.append(f"| {comp.get('name')} | {comp.get('framework') or '-'} | {routes} |")
        md.append("")

    integrations = _collect_integrations_from_sirs(sirs)
    if integrations:
        md.append("## 🌐 Integration Catalog")
        md.append("| Library | Kind | Language |")
        md.append("|---------|------|----------|")
        for integ in integrations:
            md.append(f"| {integ.get('library')} | {integ.get('integration_kind')} | {integ.get('language') or '-'} |")
        md.append("")

    rels = _collect_relationships_from_sirs(sirs)
    if rels:
        md.append("## 🧩 Relationship Highlights")
        md.extend(_relationship_highlights(rels))
        md.append("")
        matrix_block = _relationship_matrix_table(rels)
        if matrix_block:
            md.append("## 📊 Dependency Matrix")
            md.append(matrix_block)
            md.append("")
        mermaid = _mermaid_diagram_from_relationships(rels)
        if mermaid:
            md.append("## 🗺️ Flow Diagram (Mermaid)")
            md.append(mermaid)
            md.append("")
        else:
            md.append("## 🗺️ Flow Diagram (Mermaid)")
            md.append("_Mermaid view not available; will render after more relationships are captured._")
            md.append("")

    # Graph Insights (distance features) — compact table preferred
    if agg:
        md.append("## 🧭 Graph Insights (Distance Features)")
        md.append("")
        md.append(_compact_graph_insights_table(agg))
        md.append("")
    sequence_svg = render_relationship_sequence_svg(sirs or [], out_docs_dir, group_id, component_key, settings=settings)
    if sequence_svg:
        md.append("## 🔁 Sequence Snapshot")
        md.append(f"![Sequence]({sequence_svg})")
        md.append("")

    journey_svgs = render_rollup_journey_svgs(c_json, out_docs_dir, group_id, component_key)
    if journey_svgs:
        md.append("## ✨ LLM Journey Maps")
        for journey in journey_svgs:
            md.append(f"### {journey.get('title')}")
            md.append(f"![Journey]({journey.get('path')})")
            md.append("")

    code_entities = _collect_code_entities(sirs)
    if code_entities:
        md.append("## 🧱 Key Code Modules")
        for entry in code_entities[:10]:
            doc = entry.get("docstring") or "No summary."
            md.append(f"- `{entry.get('name')}` ({entry.get('language') or '-'}) – {doc}")
        md.append("")

    diagrams = _collect_process_diagrams_from_sirs(sirs)
    if diagrams:
        md.append("## 📈 Process Diagrams")
        for name in diagrams:
            md.append(f"- {name}")
        md.append("")

    entities = _collect_business_entities_from_sirs(sirs)
    if entities:
        md.append("## 📚 Glossary & Roles")
        for entry in entities:
            source = entry.get("source")
            suffix = f" ({source})" if source else ""
            md.append(f"- {entry.get('name')}{suffix}")
        md.append("")

    md.append("## ✅ Business Value")
    md.append("- Flexibility: enables change without code modifications where applicable")
    md.append("- Reliability: consistent behavior across processes/services")
    md.append("- Traceability: evidence-backed claims and logging")
    md.append("")
    md.append("## ⚠️ Known Unknowns")
    md.extend(_unknowns_block(c_json))
    md.append("")

    if overview_svg:
        md.append("## Module Overview")
        md.append(f"![Flow]({overview_svg})")
        md.append("")

    if svg_paths:
        md.append("## Visual Flow Diagrams")
        for rel in svg_paths:
            md.append("")
            md.append(f"![Flow]({rel})")
        md.append("")

    # Details with evidence links
    md.append("## Details (Evidence-backed)")
    comp = c_json.get("component", {}) or {}
    for w in comp.get("what_it_does", []) or []:
        if isinstance(w, dict):
            claim = w.get("claim", "")
            eids = [str(e) for e in (w.get("evidence_ids") or [])]
            if evidence_md_filename and eids:
                links = [f"[{eid}]({evidence_md_filename}#{_anchor_eid(eid)})" for eid in eids]
            else:
                links = eids
            md.append(f"- {claim} (evidence: {', '.join(links)})")
    md.append("")

    # Compact confidence block
    md.append("<!-- CONFIDENCE_ROLLUP_START -->")
    md.append("## Confidence & Evidence Rollup")
    md.append("")
    md.append("!!! info \"How to read these scores\"")
    md.append("    - The confidence score reflects how closely claims match their cited evidence.")
    md.append("    - Higher scores indicate stronger alignment to the underlying sources.")
    md.append("")
    md.append("<!-- CONFIDENCE_ROLLUP_END -->")

    page_path.write_text("\n".join(md), encoding="utf-8")


def render_business_group_page(
    out_docs_dir: Path,
    group_id: str,
    resp_json: Dict[str, Any],
    evidence_md_filename: Optional[str] = None,
    facets: Optional[Dict[str, Any]] = None,
    extras: Optional[Dict[str, Any]] = None,
) -> None:
    """
    Render the group-level (component) page with YAML front-matter mirroring mkdocs output.
    """
    gid = group_id.replace(" ", "_")
    page_dir = out_docs_dir
    page_dir.mkdir(parents=True, exist_ok=True)
    page_path = page_dir / f"{gid}.md"

    title = resp_json.get("title") or f"Group: {group_id}"
    llm_sub = resp_json.get("llm_subscore")

    # Try to use aggregated graph_features if present in resp_json (some rollup flows include it)
    agg = None
    if isinstance(resp_json, dict):
        # resp_json might include 'graph_features' or 'distance' already
        agg = resp_json.get("graph_features") or resp_json.get("distance") or None

    md: List[str] = []

    # YAML front-matter for group
    md.append("---")
    md.append(f'title: "{title}"')
    md.append("facets:")
    md.append(f"  score: {facets.get('score', 0.0) if facets else 0.0}")
    md.append("distance:")
    md.append(f"  avg_nearest_distance: {agg.get('avg_nearest') if agg and agg.get('avg_nearest') is not None else 'null'}")
    md.append(f"  covered: {agg.get('covered',0) if agg else 0}")
    md.append(f"  total: {agg.get('total',0) if agg else 0}")
    md.append("markers:")
    markers = (agg.get("markers") if agg else []) or []
    if isinstance(markers, list) and markers:
        for m in markers:
            if isinstance(m, dict):
                md.append(f"  - id: {m.get('id')}")
                md.append(f"    type: {m.get('type')}")
            else:
                md.append(f"  - id: {m}")
    else:
        md.append("  - []")
    md.append("---")
    md.append("")

    md.append(f"# {title}")
    md.append("<!-- CONFIDENCE_INLINE -->")
    md.append(_confidence_line(llm_sub))
    md.append("")
    md.append("## Overview")
    md.append(_safe(resp_json.get("summary"), "See components for evidence-backed details."))
    md.append("")
    md.append("## Components")
    for comp in resp_json.get("components", []) or []:
        name = comp.get("name") if isinstance(comp, dict) else str(comp)
        md.append(f"- {name}")
    md.append("")

    extras = extras or {}
    process_flows = extras.get("process_flows") or []
    if process_flows:
        md.append("## 🔁 Process Flows")
        lines = _process_flow_summary_lines(process_flows)
        if lines:
            md.extend(lines)
        else:
            md.append("_Process flows will appear once relationships are emitted._")
        mermaid = _process_flow_mermaid(process_flows)
        if mermaid:
            md.append("")
            md.append(mermaid)
        md.append("")

    integration_summary = extras.get("integration_summary") or []
    if integration_summary:
        md.append("## 🔗 Integration Summary")
        table = _integration_summary_table(integration_summary)
        if table:
            md.append(table)
        mermaid = _integration_summary_mermaid(integration_summary, title)
        if mermaid:
            md.append("")
            md.append(mermaid)
        md.append("")

    group_integrations = extras.get("integrations") or []
    if group_integrations:
        md.append("## 🌐 Integration Catalog")
        md.append("| Library | Kind | Language |")
        md.append("|---------|------|----------|")
        for integ in group_integrations:
            md.append(f"| {integ.get('library')} | {integ.get('integration_kind')} | {integ.get('language') or '-'} |")
        md.append("")

    group_ui = extras.get("ui_components") or []
    if group_ui:
        md.append("## 🎛 UI Entry Points")
        md.append("| Component | Framework | Routes |")
        md.append("|-----------|-----------|--------|")
        for ui in group_ui:
            routes = ", ".join(ui.get("routes") or []) or "-"
            md.append(f"| {ui.get('name')} | {ui.get('framework') or '-'} | {routes} |")
        md.append("")

    group_entities = extras.get("business_entities") or []
    if group_entities:
        md.append("## 📚 Glossary & Roles")
        for entry in group_entities:
            source = entry.get("source")
            suffix = f" ({source})" if source else ""
            md.append(f"- {entry.get('name')}{suffix}")
        md.append("")

    group_diagrams = extras.get("process_diagrams") or []
    if group_diagrams:
        md.append("## 📈 Process Diagrams")
        for entry in group_diagrams:
            md.append(f"- {entry.get('name')}")
        md.append("")

    page_path.write_text("\n".join(md), encoding="utf-8")
