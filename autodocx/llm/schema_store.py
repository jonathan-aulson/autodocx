from __future__ import annotations

from typing import Any, Dict

from jsonschema import validate

CLAIM_SCHEMA: Dict[str, Any] = {
    "type": "object",
    "additionalProperties": False,
    "properties": {
        "summary": {"type": "string"},
        "detail": {"type": "string"},
        "evidence_ids": {"type": "array", "items": {"type": "string"}},
    },
    "required": ["summary", "detail", "evidence_ids"],
}

WHY_SCHEMA = {
    "type": "object",
    "additionalProperties": False,
    "properties": {
        "impact": {"type": "string"},
        "detail": {"type": "string"},
        "evidence_ids": {"type": "array", "items": {"type": "string"}},
    },
    "required": ["impact", "detail", "evidence_ids"],
}

INTERFACE_SCHEMA = {
    "type": "object",
    "additionalProperties": False,
    "properties": {
        "name": {"type": "string"},
        "kind": {"type": "string"},
        "endpoint": {"type": "string"},
        "method": {"type": "string"},
        "description": {"type": "string"},
        "evidence_ids": {"type": "array", "items": {"type": "string"}},
    },
    "required": ["name", "kind", "endpoint", "method", "description", "evidence_ids"],
}

INVOKE_SCHEMA = {
    "type": "object",
    "additionalProperties": False,
    "properties": {
        "target": {"type": "string"},
        "kind": {"type": "string"},
        "operation": {"type": "string"},
        "direction": {"type": "string"},
        "evidence_ids": {"type": "array", "items": {"type": "string"}},
    },
    "required": ["target", "kind", "operation", "direction", "evidence_ids"],
}

IO_ENTRY_SCHEMA = {
    "type": "object",
    "additionalProperties": False,
    "properties": {
        "name": {"type": "string"},
        "description": {"type": "string"},
        "evidence_ids": {"type": "array", "items": {"type": "string"}},
    },
    "required": ["name", "description", "evidence_ids"],
}

ERROR_ENTRY_SCHEMA = {
    "type": "object",
    "additionalProperties": False,
    "properties": {
        "description": {"type": "string"},
        "evidence_ids": {"type": "array", "items": {"type": "string"}},
    },
    "required": ["description", "evidence_ids"],
}

ERRORS_AND_LOGGING_SCHEMA = {
    "type": "object",
    "additionalProperties": False,
    "properties": {
        "errors": {"type": "array", "items": ERROR_ENTRY_SCHEMA},
        "logging": {"type": "array", "items": ERROR_ENTRY_SCHEMA},
    },
    "required": ["errors", "logging"],
}

INTERDEPENDENCY_ENTRY_SCHEMA = {
    "type": "object",
    "additionalProperties": False,
    "properties": {
        "partner": {"type": "string"},
        "description": {"type": "string"},
        "evidence_ids": {"type": "array", "items": {"type": "string"}},
    },
    "required": ["partner", "description", "evidence_ids"],
}

INTERDEPENDENCIES_SCHEMA = {
    "type": "object",
    "additionalProperties": False,
    "properties": {
        "calls": {"type": "array", "items": INTERDEPENDENCY_ENTRY_SCHEMA},
        "called_by": {"type": "array", "items": INTERDEPENDENCY_ENTRY_SCHEMA},
        "shared_data": {"type": "array", "items": INTERDEPENDENCY_ENTRY_SCHEMA},
    },
    "required": ["calls", "called_by", "shared_data"],
}

EXTRAPOLATION_SCHEMA = {
    "type": "object",
    "additionalProperties": False,
    "properties": {
        "hypothesis": {"type": "string"},
        "rationale": {"type": "string"},
        "hypothesis_score": {"type": "number"},
        "evidence_ids": {"type": "array", "items": {"type": "string"}},
    },
    "required": ["hypothesis", "rationale", "hypothesis_score", "evidence_ids"],
}

TRACEABILITY_SCHEMA = {
    "type": "object",
    "additionalProperties": False,
    "properties": {
        "artifact": {"type": "string"},
        "signal_type": {"type": "string"},
        "description": {"type": "string"},
        "evidence_ids": {"type": "array", "items": {"type": "string"}},
    },
    "required": ["artifact", "signal_type", "description", "evidence_ids"],
}

GROUP_RESPONSE_SCHEMA: Dict[str, Any] = {
    "type": "object",
    "required": ["group_id", "title", "summary", "components", "evidence_used", "llm_subscore", "approved", "provenance"],
    "additionalProperties": False,
    "properties": {
        "group_id": {"type": "string"},
        "title": {"type": "string"},
        "summary": {"type": "string"},
        "components": {
            "type": "array",
            "items": {
                "type": "object",
                "required": [
                    "id",
                    "name",
                    "summary",
                    "what_it_does",
                    "why_it_matters",
                    "interfaces",
                    "invokes",
                    "key_inputs",
                    "key_outputs",
                    "errors_and_logging",
                    "interdependencies",
                    "extrapolations",
                    "traceability",
                    "journey_blueprints",
                ],
                "additionalProperties": False,
                "properties": {
                    "id": {"type": "string"},
                    "name": {"type": "string"},
                    "summary": {"type": "string"},
                    "what_it_does": {"type": "array", "items": CLAIM_SCHEMA},
                    "why_it_matters": {"type": "array", "items": WHY_SCHEMA},
                    "interfaces": {"type": "array", "items": INTERFACE_SCHEMA},
                    "invokes": {"type": "array", "items": INVOKE_SCHEMA},
                    "key_inputs": {"type": "array", "items": IO_ENTRY_SCHEMA},
                    "key_outputs": {"type": "array", "items": IO_ENTRY_SCHEMA},
                    "errors_and_logging": ERRORS_AND_LOGGING_SCHEMA,
                    "interdependencies": INTERDEPENDENCIES_SCHEMA,
                    "extrapolations": {"type": "array", "items": EXTRAPOLATION_SCHEMA},
                    "traceability": {"type": "array", "items": TRACEABILITY_SCHEMA},
                    "journey_blueprints": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "additionalProperties": False,
                            "properties": {
                                "title": {"type": "string"},
                                "steps": {"type": "array", "items": {"type": "string"}},
                                "evidence_ids": {"type": "array", "items": {"type": "string"}},
                            },
                            "required": ["title", "steps", "evidence_ids"],
                        },
                    },
                },
            },
        },
        "evidence_used": {"type": "array", "items": {"type": "string"}},
        "llm_subscore": {"type": "number"},
        "approved": {"type": "boolean"},
        "provenance": {
            "type": "object",
            "required": ["model", "provider", "generated_at", "prompt_hash", "input_hash", "usage", "latency_ms", "cost_usd", "evidence_md_file", "relationship_stats"],
            "additionalProperties": False,
            "properties": {
                "model": {"type": "string"},
                "provider": {"type": "string"},
                "generated_at": {"type": "number"},
                "prompt_hash": {"type": "string"},
                "input_hash": {"type": "string"},
                "usage": {
                    "type": "object",
                    "additionalProperties": False,
                    "properties": {
                        "input_tokens": {"type": ["number", "null"]},
                        "output_tokens": {"type": ["number", "null"]},
                        "total_tokens": {"type": ["number", "null"]},
                    },
                    "required": ["input_tokens", "output_tokens", "total_tokens"],
                },
                "latency_ms": {"type": ["number", "null"]},
                "cost_usd": {"type": ["number", "null"]},
                "evidence_md_file": {"type": ["string", "null"]},
                "relationship_stats": {
                    "type": "object",
                    "additionalProperties": False,
                    "properties": {
                        "artifact_relationships": {"type": ["number", "null"]},
                        "sir_relationships": {"type": ["number", "null"]},
                    },
                    "required": ["artifact_relationships", "sir_relationships"],
                },
            },
        },
    },
}


COMPONENT_RESPONSE_SCHEMA: Dict[str, Any] = {
    "type": "object",
    "required": ["group_id", "component_id", "title", "summary", "component", "evidence_used", "llm_subscore", "approved", "provenance"],
    "additionalProperties": False,
    "properties": {
        "group_id": {"type": "string"},
        "component_id": {"type": "string"},
        "title": {"type": "string"},
        "summary": {"type": "string"},
            "component": {
                "type": "object",
                "required": [
                    "name",
                    "summary",
                    "what_it_does",
                    "why_it_matters",
                    "interfaces",
                    "invokes",
                    "key_inputs",
                    "key_outputs",
                    "errors_and_logging",
                    "interdependencies",
                    "extrapolations",
                    "traceability",
                    "journey_blueprints",
                ],
                "additionalProperties": False,
                "properties": {
                    "name": {"type": "string"},
                    "summary": {"type": "string"},
                    "what_it_does": {"type": "array", "items": CLAIM_SCHEMA},
                    "why_it_matters": {"type": "array", "items": WHY_SCHEMA},
                    "interfaces": {"type": "array", "items": INTERFACE_SCHEMA},
                    "invokes": {"type": "array", "items": INVOKE_SCHEMA},
                    "key_inputs": {"type": "array", "items": IO_ENTRY_SCHEMA},
                    "key_outputs": {"type": "array", "items": IO_ENTRY_SCHEMA},
                    "errors_and_logging": ERRORS_AND_LOGGING_SCHEMA,
                    "interdependencies": INTERDEPENDENCIES_SCHEMA,
                    "extrapolations": {"type": "array", "items": EXTRAPOLATION_SCHEMA},
                    "traceability": {"type": "array", "items": TRACEABILITY_SCHEMA},
                    "journey_blueprints": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "additionalProperties": False,
                            "properties": {
                                "title": {"type": "string"},
                                "steps": {"type": "array", "items": {"type": "string"}},
                                "evidence_ids": {"type": "array", "items": {"type": "string"}},
                            },
                            "required": ["title", "steps", "evidence_ids"],
                        },
                    },
                },
            },
        "evidence_used": {"type": "array", "items": {"type": "string"}},
        "llm_subscore": {"type": "number"},
        "approved": {"type": "boolean"},
        "provenance": {
            "type": "object",
            "required": ["model", "provider", "generated_at", "prompt_hash", "input_hash", "usage", "latency_ms", "cost_usd", "evidence_md_file", "relationship_stats"],
            "additionalProperties": False,
            "properties": {
                "model": {"type": "string"},
                "provider": {"type": "string"},
                "generated_at": {"type": "number"},
                "prompt_hash": {"type": "string"},
                "input_hash": {"type": "string"},
                "usage": {
                    "type": "object",
                    "additionalProperties": False,
                    "properties": {
                        "input_tokens": {"type": ["number", "null"]},
                        "output_tokens": {"type": ["number", "null"]},
                        "total_tokens": {"type": ["number", "null"]},
                    },
                    "required": ["input_tokens", "output_tokens", "total_tokens"],
                },
                "latency_ms": {"type": ["number", "null"]},
                "cost_usd": {"type": ["number", "null"]},
                "evidence_md_file": {"type": ["string", "null"]},
                "relationship_stats": {
                    "type": "object",
                    "additionalProperties": False,
                    "properties": {
                        "artifact_relationships": {"type": ["number", "null"]},
                        "sir_relationships": {"type": ["number", "null"]},
                    },
                    "required": ["artifact_relationships", "sir_relationships"],
                },
            },
        },
    },
}


def validate_group_response(data: Dict[str, Any]) -> None:
    validate(instance=data, schema=GROUP_RESPONSE_SCHEMA)


def validate_component_response(data: Dict[str, Any]) -> None:
    validate(instance=data, schema=COMPONENT_RESPONSE_SCHEMA)
