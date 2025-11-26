from __future__ import annotations

from typing import Any, Dict

from jsonschema import validate


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
                    "what_it_does",
                    "interfaces",
                    "data_highlights",
                    "risks_gaps",
                    "user_experience",
                    "risk_stories",
                    "operational_behaviors",
                    "data_flows",
                    "journey_blueprints",
                    "relationships_summary",
                    "dependency_matrix",
                ],
                "additionalProperties": False,
                "properties": {
                    "id": {"type": "string"},
                    "name": {"type": "string"},
                    "what_it_does": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "required": ["claim", "detail", "evidence_ids"],
                            "additionalProperties": False,
                            "properties": {
                                "claim": {"type": "string"},
                                "detail": {"type": "string"},
                                "evidence_ids": {"type": "array", "items": {"type": "string"}},
                            },
                        },
                    },
                    "interfaces": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "additionalProperties": False,
                            "properties": {
                                "kind": {"type": "string"},
                                "description": {"type": "string"},
                                "evidence_ids": {"type": "array", "items": {"type": "string"}},
                            },
                            "required": ["kind", "description", "evidence_ids"],
                        },
                    },
                    "data_highlights": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "additionalProperties": False,
                            "properties": {
                                "note": {"type": "string"},
                                "evidence_ids": {"type": "array", "items": {"type": "string"}},
                            },
                            "required": ["note", "evidence_ids"],
                        },
                    },
                    "risks_gaps": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "additionalProperties": False,
                            "properties": {
                                "issue": {"type": "string"},
                                "evidence_ids": {"type": "array", "items": {"type": "string"}},
                            },
                            "required": ["issue", "evidence_ids"],
                        },
                    },
                    "user_experience": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "additionalProperties": False,
                            "properties": {
                                "narrative": {"type": "string"},
                                "screenshots": {"type": "array", "items": {"type": "string"}},
                                "evidence_ids": {"type": "array", "items": {"type": "string"}},
                            },
                            "required": ["narrative", "screenshots", "evidence_ids"],
                        },
                    },
                    "risk_stories": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "additionalProperties": False,
                            "properties": {
                                "story": {"type": "string"},
                                "evidence_ids": {"type": "array", "items": {"type": "string"}},
                            },
                            "required": ["story", "evidence_ids"],
                        },
                    },
                    "operational_behaviors": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "additionalProperties": False,
                            "properties": {
                                "behavior": {"type": "string"},
                                "evidence_ids": {"type": "array", "items": {"type": "string"}},
                            },
                            "required": ["behavior", "evidence_ids"],
                        },
                    },
                    "data_flows": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "additionalProperties": False,
                            "properties": {
                                "description": {"type": "string"},
                                "evidence_ids": {"type": "array", "items": {"type": "string"}},
                            },
                            "required": ["description", "evidence_ids"],
                        },
                    },
                    "journey_blueprints": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "additionalProperties": False,
                            "properties": {
                                "title": {"type": "string"},
                                "steps": {
                                    "type": "array",
                                    "items": {"type": "string"},
                                },
                                "evidence_ids": {"type": "array", "items": {"type": "string"}},
                            },
                            "required": ["title", "steps", "evidence_ids"],
                        },
                    },
                    "relationships_summary": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "additionalProperties": False,
                            "properties": {
                                "flow": {"type": "string"},
                                "evidence_ids": {"type": "array", "items": {"type": "string"}},
                            },
                            "required": ["flow", "evidence_ids"],
                        },
                    },
                    "dependency_matrix": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "additionalProperties": False,
                            "properties": {
                                "target_kind": {"type": "string"},
                                "operation": {"type": "string"},
                                "count": {"type": "number"},
                                "evidence_ids": {"type": "array", "items": {"type": "string"}},
                            },
                            "required": ["target_kind", "operation", "count", "evidence_ids"],
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
                "what_it_does",
                "interfaces",
                "data_highlights",
                "risks_gaps",
                "user_experience",
                "risk_stories",
                "operational_behaviors",
                "data_flows",
                "journey_blueprints",
                "relationships_summary",
                "dependency_matrix",
            ],
            "additionalProperties": False,
            "properties": {
                "name": {"type": "string"},
                "what_it_does": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "required": ["claim", "detail", "evidence_ids"],
                        "additionalProperties": False,
                        "properties": {
                            "claim": {"type": "string"},
                            "detail": {"type": "string"},
                            "evidence_ids": {"type": "array", "items": {"type": "string"}},
                        },
                    },
                },
                "interfaces": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "additionalProperties": False,
                        "properties": {
                            "kind": {"type": "string"},
                            "description": {"type": "string"},
                            "evidence_ids": {"type": "array", "items": {"type": "string"}},
                        },
                        "required": ["kind", "description", "evidence_ids"],
                    },
                },
                "data_highlights": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "additionalProperties": False,
                        "properties": {
                            "note": {"type": "string"},
                            "evidence_ids": {"type": "array", "items": {"type": "string"}},
                        },
                        "required": ["note", "evidence_ids"],
                    },
                },
                "risks_gaps": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "additionalProperties": False,
                        "properties": {
                            "issue": {"type": "string"},
                            "evidence_ids": {"type": "array", "items": {"type": "string"}},
                        },
                        "required": ["issue", "evidence_ids"],
                    },
                },
                "user_experience": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "additionalProperties": False,
                        "properties": {
                            "narrative": {"type": "string"},
                            "screenshots": {"type": "array", "items": {"type": "string"}},
                            "evidence_ids": {"type": "array", "items": {"type": "string"}},
                        },
                        "required": ["narrative", "screenshots", "evidence_ids"],
                    },
                },
                "risk_stories": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "additionalProperties": False,
                        "properties": {
                            "story": {"type": "string"},
                            "evidence_ids": {"type": "array", "items": {"type": "string"}},
                        },
                        "required": ["story", "evidence_ids"],
                    },
                },
                "operational_behaviors": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "additionalProperties": False,
                        "properties": {
                            "behavior": {"type": "string"},
                            "evidence_ids": {"type": "array", "items": {"type": "string"}},
                        },
                        "required": ["behavior", "evidence_ids"],
                    },
                },
                "data_flows": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "additionalProperties": False,
                        "properties": {
                            "description": {"type": "string"},
                            "evidence_ids": {"type": "array", "items": {"type": "string"}},
                        },
                        "required": ["description", "evidence_ids"],
                    },
                },
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
                "relationships_summary": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "additionalProperties": False,
                        "properties": {
                            "flow": {"type": "string"},
                            "evidence_ids": {"type": "array", "items": {"type": "string"}},
                        },
                        "required": ["flow", "evidence_ids"],
                    },
                },
                "dependency_matrix": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "additionalProperties": False,
                        "properties": {
                            "target_kind": {"type": "string"},
                            "operation": {"type": "string"},
                            "count": {"type": "number"},
                            "evidence_ids": {"type": "array", "items": {"type": "string"}},
                        },
                        "required": ["target_kind", "operation", "count", "evidence_ids"],
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
