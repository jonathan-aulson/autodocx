import os

import pytest

from autodocx.llm import rollup
from autodocx.llm.schema_store import validate_component_response, validate_group_response


def test_provider_ready_respects_enabled_flag():
    settings = {"llm": {"provider": "openai", "enabled": False}}
    assert rollup._provider_ready(settings) is False  # type: ignore[attr-defined]


def test_provider_ready_respects_environment(monkeypatch):
    monkeypatch.delenv("OPENAI_API_KEY", raising=False)
    monkeypatch.setenv("AUTODOCX_DISABLE_LLM", "1")
    settings = {"llm": {"provider": "openai", "enabled": True}}
    assert rollup._provider_ready(settings) is False  # type: ignore[attr-defined]


def test_validate_group_response_accepts_minimal_payload():
    minimal = {
        "group_id": "ExampleGroup",
        "title": "Example Group",
        "summary": "Example summary",
        "components": [
            {
                "id": "ComponentA",
                "name": "ComponentA",
                "what_it_does": [{"claim": "does something", "detail": "Long form", "evidence_ids": []}],
                "interfaces": [],
                "data_highlights": [],
                "risks_gaps": [],
                "user_experience": [],
                "risk_stories": [],
                "operational_behaviors": [],
                "data_flows": [],
                "journey_blueprints": [],
                "relationships_summary": [],
                "dependency_matrix": [],
            }
        ],
        "evidence_used": [],
        "llm_subscore": 0.0,
        "approved": False,
        "provenance": {
            "model": "test",
            "provider": "openai",
            "generated_at": 0,
            "prompt_hash": "abc",
            "input_hash": "def",
            "usage": {"input_tokens": 0, "output_tokens": 0, "total_tokens": 0},
            "latency_ms": 0,
            "cost_usd": 0.0,
            "evidence_md_file": None,
            "relationship_stats": {"artifact_relationships": 0, "sir_relationships": 0},
        },
    }
    validate_group_response(minimal)


def test_validate_component_response_rejects_missing_fields():
    incomplete = {
        "group_id": "ExampleGroup",
        "component_id": "ComponentA",
        "title": "ComponentA",
        "summary": "Summary",
        "component": {
            "name": "ComponentA",
            "what_it_does": [{"claim": "does something", "detail": "Long form", "evidence_ids": []}],
            "interfaces": [],
            "data_highlights": [],
            "risks_gaps": [],
            "user_experience": [],
            "risk_stories": [],
            "operational_behaviors": [],
            "data_flows": [],
            "journey_blueprints": [],
            "relationships_summary": [],
            "dependency_matrix": [],
        },
        "evidence_used": [],
        "llm_subscore": 0.0,
        "approved": False,
        "provenance": {
            "model": "test",
            "provider": "openai",
            "generated_at": 0,
            "prompt_hash": "abc",
            "input_hash": "def",
            "usage": {"input_tokens": 0, "output_tokens": 0, "total_tokens": 0},
            "latency_ms": 0,
            "cost_usd": 0.0,
            "evidence_md_file": None,
            "relationship_stats": {"artifact_relationships": 0, "sir_relationships": 0},
        },
    }
    validate_component_response(incomplete)  # should not raise

    bad_payload = dict(incomplete)
    bad_payload.pop("component")
    with pytest.raises(Exception):
        validate_component_response(bad_payload)
