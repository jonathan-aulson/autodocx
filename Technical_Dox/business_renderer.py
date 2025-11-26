# autodocx/render/business_renderer.py
from __future__ import annotations
from pathlib import Path
from typing import Dict, Any, List, Optional

from autodocx.visuals.graphviz_flows import (
    render_bw_process_flow_svg,
    render_component_overview_svg,
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

    # Graph Insights (distance features) — compact table preferred
    if agg:
        md.append("## 🧭 Graph Insights (Distance Features)")
        md.append("")
        md.append(_compact_graph_insights_table(agg))
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

    page_path.write_text("\n".join(md), encoding="utf-8")
