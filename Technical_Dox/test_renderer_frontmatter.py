# tests/test_renderer_frontmatter.py
import json
from pathlib import Path

import pytest

from autodocx.render import business_renderer


def _read_front_matter_lines(md_path: Path) -> (list, list):
    """
    Return (front_matter_lines, body_lines) for the given markdown file.
    If no front-matter exists, front_matter_lines will be empty.
    """
    text = md_path.read_text(encoding="utf-8")
    lines = text.splitlines()
    if not lines:
        return [], []
    if lines[0].strip() != "---":
        return [], lines
    # find closing '---'
    try:
        end_idx = next(i for i, l in enumerate(lines[1:], start=1) if l.strip() == "---")
    except StopIteration:
        # malformed; return everything as body
        return [], lines
    fm = lines[0 : end_idx + 1]
    body = lines[end_idx + 1 :]
    return fm, body


def test_component_page_emits_yaml_front_matter(tmp_path: Path):
    out_dir = tmp_path / "out"
    # Minimal component JSON (as produced by rollup)
    comp_json = {
        "title": "Orders Component",
        "llm_subscore": 0.82,
        "component": {
            "name": "Orders",
            "what_it_does": [
                {"claim": "Accept orders via REST API", "evidence_ids": ["e1"]},
            ],
        },
    }

    # Create a sample SIR with graph_features (so markers/agg picks up something)
    sir = {
        "id": "workflow:orders-create",
        "name": "Orders Create Process",
        "props": {"file": "flows/orders_create.bw"},
        "component_or_service": "OrdersComponent",
        "graph_features": {
            "nearest_marker_id": "API:Orders",
            "nearest_marker_distance": 1,
            "avg_distance_to_markers": 1.2,
            "distance_percentiles": {"p50": 1.0, "p90": 1.5},
            "anchor_coverage": {"anchors_within_r": 2, "min_anchor_distance": 1, "avg_anchor_distance": 1.0, "radius": 4},
            "markers": [{"id": "API:Orders", "type": "API"}],
            "risk_flags": {"is_articulation": False},
        },
        "steps": [{"name": "ReceiveOrder", "type": "invoke"}, {"name": "ValidateOrder", "type": "invoke"}],
    }

    # Use a small facets dict to simulate score/facets passed in
    facets = {"score": 0.78, "ops": 3, "apis": 1}

    # Render the component page
    group_id = "OrdersComponent"
    component_key = "OrdersComponent"
    # Render - business_renderer will create dirs under out_dir/components/<group>/
    business_renderer.render_business_component_page(
        out_docs_dir=out_dir,
        group_id=group_id,
        component_key=component_key,
        c_json=comp_json,
        sirs=[sir],
        evidence_md_filename=None,
        facets=facets,
        settings={},  # keep defaults; avoid forcing Graphviz in tests
    )

    # Assert the file exists and front-matter contains expected keys/values
    gid = group_id.replace(" ", "_")
    cid = component_key.replace(" ", "_").replace("/", "_")
    md_path = out_dir / "components" / gid / f"{cid}.md"
    assert md_path.exists(), f"Component markdown not written: {md_path}"

    fm_lines, body_lines = _read_front_matter_lines(md_path)
    assert fm_lines, "Expected YAML front-matter in generated component page"

    fm_text = "\n".join(fm_lines)
    # Basic keys we expect
    assert 'title: "Orders Component"' in fm_text
    assert "facets:" in fm_text
    assert "score: 0.78" in fm_text or "score: 0.78" in fm_text.replace(" ", "")
    assert "distance:" in fm_text
    # From sample aggregation the avg_nearest should be present (1.2 propagated to agg)
    assert "avg_nearest_distance" in fm_text
    # Marker id should be present in markers block
    assert "API:Orders" in fm_text

    # The body should contain the Graph Insights header and the compact table
    body_text = "\n".join(body_lines)
    assert "Graph Insights (Distance Features)" in body_text
    assert "| Metric | Value |" in body_text
