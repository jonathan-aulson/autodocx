from __future__ import annotations

import json
from pathlib import Path
from typing import Any, Dict

import yaml

from autodocx.docplan import draft_doc_plan, fulfill_doc_plan


def _build_context(tmp_out: Path) -> Dict[str, Any]:
    signals_dir = tmp_out / "signals"
    sir_dir = signals_dir / "sir_v2"
    sir_dir.mkdir(parents=True, exist_ok=True)
    sir_data = {"name": "OrdersFlow", "component_or_service": "Orders"}
    (sir_dir / "OrdersFlow.json").write_text(json.dumps(sir_data), encoding="utf-8")
    interdeps = {"nodes": {"OrdersFlow": {"component": "Orders", "family": "orders"}}}
    (signals_dir / "interdeps.json").write_text(json.dumps(interdeps), encoding="utf-8")
    (signals_dir / "graph.json").write_text(json.dumps({"nodes": [], "edges": []}), encoding="utf-8")
    artifacts = [{"name": "OrdersFlow", "component_or_service": "Orders"}]
    artifacts_path = tmp_out / "artifacts" / "artifacts.json"
    artifacts_path.parent.mkdir(parents=True, exist_ok=True)
    artifacts_path.write_text(json.dumps(artifacts), encoding="utf-8")
    context = {
        "components": {
            "Orders": {
                "slug": "orders",
                "sir_files": ["signals/sir_v2/OrdersFlow.json"],
                "families": ["orders"],
                "diagram_paths": [],
                "artifacts": artifacts,
                "process_slugs": ["orders-ordersflow"],
                "family_slugs": ["orders"],
            }
        },
        "families": {
            "orders": {
                "slug": "orders",
                "components": ["Orders"],
                "sir_files": ["signals/sir_v2/OrdersFlow.json"],
                "diagram_paths": [],
                "process_slugs": ["orders-ordersflow"],
                "component_slugs": ["orders"],
            }
        },
        "processes": {
            "signals/sir_v2/OrdersFlow.json": {
                "key": "signals/sir_v2/OrdersFlow.json",
                "slug": "orders-ordersflow",
                "name": "OrdersFlow",
                "component": "Orders",
                "sir_file": "signals/sir_v2/OrdersFlow.json",
                "families": ["orders"],
                "diagram_paths": [],
                "quality_score": 0,
                "quality": {"score": 0, "has_workflow_details": True, "metrics": {"identifiers": 1}},
                "has_workflow_details": True,
                "business_scaffold": {
                    "interfaces": [{"kind": "http", "endpoint": "/test", "method": "GET"}],
                    "invocations": [{"kind": "Process", "target": "ChildFlow"}],
                    "dependencies": {"processes": ["ChildFlow"], "services": [], "datastores": []},
                    "io_summary": {"inputs": ["id"], "outputs": ["result"], "identifiers": ["id"]},
                    "errors": [{"activity": "step1", "condition": "error"}],
                    "logging": [{"activity": "step1", "message_hint": "log"}],
                },
            }
        },
        "repo": {
            "slug": "repo",
            "components": ["Orders"],
            "families": ["orders"],
            "sir_files": ["signals/sir_v2/OrdersFlow.json"],
            "diagram_paths": [],
            "component_slugs": ["orders"],
            "family_slugs": ["orders"],
            "process_slugs": ["orders-ordersflow"],
        },
        "facets": {"score": 0.9},
        "interdeps_path": "signals/interdeps.json",
        "graph_path": "signals/graph.json",
        "artifacts_file": "artifacts/artifacts.json",
    }
    return context


def test_draft_doc_plan_creates_markdown(tmp_path: Path) -> None:
    out_dir = tmp_path / "out"
    context = _build_context(out_dir)
    plan_path = draft_doc_plan(out_dir, context=context)
    text = plan_path.read_text(encoding="utf-8")
    assert "Autodocx Documentation Plan" in text
    assert "OrdersFlow – Process Brief" in text
    assert "Orders – Component Brief" in text
    assert "orders – Family Brief" in text
    assert "Repository Comprehensive Narrative" in text


def test_fulfill_doc_plan_uses_llm_callback(tmp_path: Path) -> None:
    out_dir = tmp_path / "out"
    context = _build_context(out_dir)
    draft_doc_plan(out_dir, context=context)

    def fake_llm(prompt: str, payload: dict) -> dict:
        assert payload["sources"], "payload should include evidence sources"
        return {
            "text": """## Executive summary
This sentence ensures the executive summary exceeds the minimum word limit for the test case.

## Workflow narrative
Multiple detailed sentences describe inputs, activities, and outputs to satisfy the threshold.

## Interfaces & dependencies
Plenty of descriptive words are provided here so the section remains above the configured minimum.

## Key data handled
Again we discuss identifiers, payloads, and evidence to ensure a sufficient word count for the test.

## Risks & follow-ups
Additional prose enumerates risks, mitigations, and monitoring needs beyond the minimum.

## Related documents
Words continue so the related documents section also exceeds the target.""",
            "usage": {},
        }

    processed = fulfill_doc_plan(out_dir, context=context, min_words_per_section=10, llm_callable=fake_llm)
    assert processed >= 3
    docs_dir = out_dir / "docs"
    matches = list(docs_dir.rglob("*.md"))
    assert matches, "Expected a doc to be written"
    updated_plan = (out_dir / "docs" / "dox_draft_plan.md").read_text(encoding="utf-8")
    assert "- [x]" in updated_plan


def test_plan_includes_constellation_and_quality_entries(tmp_path: Path) -> None:
    out_dir = tmp_path / "out"
    context = _build_context(out_dir)
    const_dir = out_dir / "signals" / "constellations"
    const_dir.mkdir(parents=True, exist_ok=True)
    const_payload = {"id": "constellation_1", "components": ["Orders"]}
    (const_dir / "orders-constellation.json").write_text(json.dumps(const_payload), encoding="utf-8")
    evidence_dir = out_dir / "evidence" / "constellations"
    evidence_dir.mkdir(parents=True, exist_ok=True)
    (evidence_dir / "orders-constellation.json").write_text(json.dumps({"snippets": []}), encoding="utf-8")
    quality_dir = out_dir / "reports" / "quality"
    quality_dir.mkdir(parents=True, exist_ok=True)
    (quality_dir / "anti_patterns.json").write_text(json.dumps({"findings": []}), encoding="utf-8")
    context["constellations"] = {
        "constellation_1": {
            "slug": "orders-constellation",
            "components": ["Orders"],
            "sir_files": ["signals/sir_v2/OrdersFlow.json"],
            "graph_file": "signals/constellations/orders-constellation.json",
            "entry_points": [],
            "score": 0.8,
            "evidence_packet": "evidence/constellations/orders-constellation.json",
            "anti_patterns": [],
            "anti_pattern_count": 0,
        }
    }
    context["quality"] = {
        "anti_patterns_file": "reports/quality/anti_patterns.json",
        "constellation_counts": {"constellation_1": 0},
    }
    plan_path = draft_doc_plan(out_dir, context=context)
    text = plan_path.read_text(encoding="utf-8")
    assert "Constellation Brief" in text
    assert "Anti-Pattern Register" in text


def test_doc_plan_prioritizes_high_quality_processes(tmp_path: Path) -> None:
    out_dir = tmp_path / "out"
    context = _build_context(out_dir)
    # Existing process defaults to low quality
    low_proc = context["processes"]["signals/sir_v2/OrdersFlow.json"]
    low_proc["quality_score"] = 1
    low_proc["quality"]["score"] = 1
    # Add a high quality process
    context["processes"]["signals/sir_v2/BillingFlow.json"] = {
        "key": "signals/sir_v2/BillingFlow.json",
        "slug": "orders-billingflow",
        "name": "BillingFlow",
        "component": "Orders",
        "sir_file": "signals/sir_v2/BillingFlow.json",
        "families": ["orders"],
        "diagram_paths": [],
            "quality_score": 25,
            "quality": {"score": 25, "has_workflow_details": True, "metrics": {"identifiers": 2}},
            "has_workflow_details": True,
            "business_scaffold": {
                "interfaces": [{"kind": "http", "endpoint": "/billing", "method": "POST"}],
                "invocations": [{"kind": "Process", "target": "OrdersFlow"}],
                "dependencies": {"processes": ["OrdersFlow"], "services": [], "datastores": []},
                "io_summary": {"inputs": ["acct"], "outputs": ["status"], "identifiers": ["acct"]},
                "errors": [{"activity": "bill", "condition": "fail"}],
                "logging": [{"activity": "bill", "message_hint": "log"}],
            },
        }
    context["components"]["Orders"]["process_slugs"].append("orders-billingflow")
    context["repo"]["process_slugs"].append("orders-billingflow")

    plan_path = draft_doc_plan(out_dir, context=context)
    text = plan_path.read_text(encoding="utf-8")
    fm = yaml.safe_load(text.split("---", 2)[1])
    docs = fm["docs"]
    assert docs[0]["title"].startswith("BillingFlow")
    assert docs[0]["priority"] == 1
    assert docs[0]["quality_score"] >= 25
