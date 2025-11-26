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
        "props": {
            "file": "flows/orders_create.bw",
            "user_story": "Orders submission journey",
            "roles": ["interface.receive"],
            "screenshots": ["assets/screenshots/orders/main.png"],
        },
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
    assert "Personas & Journeys" in body_text
    assert "What Users See" in body_text


def test_component_page_includes_relationship_sections(tmp_path: Path):
    out_dir = tmp_path / "out_rel"
    comp_json = {
        "title": "Workflow With Relationships",
        "llm_subscore": 0.91,
        "component": {
            "name": "Workflow With Relationships",
            "what_it_does": [{"claim": "Calls API", "evidence_ids": ["e1"]}],
        },
    }
    relationships = [
        {
            "id": "rel_http",
            "source": {"type": "action", "name": "CallAPI"},
            "target": {"kind": "http", "ref": "https://example.com/api", "display": "https://example.com/api"},
            "operation": {"type": "calls", "verb": "POST", "crud": "execute", "protocol": "https"},
            "connector": "http",
            "direction": "outbound",
            "context": {"url_or_resource": "https://example.com/api"},
            "roles": ["interface.calls"],
            "evidence": ["demo:CallAPI"],
            "confidence": 0.9,
        },
        {
            "id": "rel_dataverse",
            "source": {"type": "action", "name": "UpdateDataverse"},
            "target": {"kind": "dataverse", "ref": "accounts", "display": "accounts"},
            "operation": {"type": "writes", "verb": "PATCH", "crud": "update", "protocol": "dataverse"},
            "connector": "shared_commondataservice",
            "direction": "outbound",
            "context": {"table": "accounts"},
            "roles": ["data.mutates"],
            "evidence": ["demo:UpdateDataverse"],
            "confidence": 0.9,
        },
    ]
    sir = {
        "id": "workflow:demo",
        "name": "Demo Flow",
        "component_or_service": "Demo",
        "props": {
            "file": "demo.json",
            "wf_kind": "power_automate",
            "relationships": relationships,
            "roles": ["interface.receive"],
            "user_story": "Demo flow handles API calls",
            "screenshots": ["assets/screenshots/demo/flow.png"],
        },
        "relationships": relationships,
        "relationship_matrix": {"http": {"calls": 1}, "dataverse": {"writes": 1}},
    }
    ui_sir = {
        "kind": "ui_component",
        "props": {"name": "Dashboard", "framework": "react", "routes": ["/dashboard"]},
    }
    integration_sir = {
        "kind": "integration",
        "props": {"library": "axios", "integration_kind": "http_client", "language": "ts"},
    }
    code_sir = {
        "kind": "code_entity",
        "props": {"name": "BillingService", "language": "python", "docstring": "Handles billing."},
    }
    diagram_sir = {"kind": "process_diagram", "props": {"name": "Billing BPMN"}}
    entity_sir = {"kind": "business_entity", "props": {"name": "Finance"}}

    business_renderer.render_business_component_page(
        out_docs_dir=out_dir,
        group_id="DemoGroup",
        component_key="DemoWorkflow",
        c_json=comp_json,
        sirs=[sir, ui_sir, integration_sir, code_sir, diagram_sir, entity_sir],
        evidence_md_filename=None,
        facets={"score": 0.9},
        settings={},
    )

    md_path = out_dir / "components" / "DemoGroup" / "DemoWorkflow.md"
    text = md_path.read_text(encoding="utf-8")
    assert "## 🧩 Relationship Highlights" in text
    assert "## 📊 Dependency Matrix" in text
    assert "## 👥 Personas & Journeys" in text
    assert "## 🖼️ What Users See" in text
    assert "## 🎛 UI Entry Points" in text
    assert "## 🌐 Integration Catalog" in text
    assert "## 🧱 Key Code Modules" in text
    assert "## 📈 Process Diagrams" in text
    assert "## 📚 Glossary & Roles" in text


def test_group_page_process_and_integration_sections(tmp_path: Path):
    out_dir = tmp_path / "out_group"
    response = {
        "title": "Billing Platform",
        "llm_subscore": 0.77,
        "summary": "Composite services powering billing workflows.",
        "components": [{"name": "BillingFunctions"}],
    }
    extras = {
        "process_flows": [
            {"source": "TimerTrigger", "target": "InvoiceAPI", "operation": "calls"},
            {"source": "InvoiceAPI", "target": "SQL", "operation": "writes"},
        ],
        "integration_summary": [{"integration_kind": "http_client", "library": "axios", "count": 2}],
        "integrations": [{"library": "axios", "integration_kind": "http_client", "language": "ts"}],
        "ui_components": [],
        "process_diagrams": [],
        "business_entities": [{"name": "Billing Administrator", "source": "component_name"}],
    }

    docs_dir = out_dir / "docs"
    business_renderer.render_business_group_page(
        out_docs_dir=docs_dir,
        group_id="Billing",
        resp_json=response,
        extras=extras,
    )

    md_path = docs_dir / "Billing.md"
    text = md_path.read_text(encoding="utf-8")
    assert "## 🔁 Process Flows" in text
    assert "TimerTrigger -> InvoiceAPI (calls)" in text
    assert "## 🔗 Integration Summary" in text
    assert "flowchart LR" in text  # Mermaid diagram included
    assert "Billing Administrator (component_name)" in text
