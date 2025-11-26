# autodocx/llm/rollup.py
from __future__ import annotations
import json
import time
import hashlib
import os
import re
from pathlib import Path
from typing import Dict, Any, List, Optional

from jsonschema import validate as jsonschema_validate, ValidationError
from rapidfuzz import fuzz

from autodocx.llm.provider import call_openai_meta
from autodocx.llm.evidence_index import build_evidence_index
from autodocx.config_loader import get_all_settings

from autodocx.render.business_renderer import (
    render_business_component_page,
    render_business_group_page,
)




# Single source of truth: values come only from autodocx.yaml
SETTINGS = get_all_settings()
LLM_MODEL = SETTINGS["llm"]["model"]
LLM_PROVIDER = SETTINGS["llm"]["provider"]
LLM_MAX_OUTPUT_TOKENS = SETTINGS["llm"]["max_output_tokens"]
ROLLUP_PUBLISH_THRESHOLD = SETTINGS["rollup"]["publish_threshold"]
OUT_DIR = SETTINGS["out_dir"]

# -------------------------- Schemas --------------------------

# -------------------------- Generation Schemas (for LLM) --------------------------
# Minimal provenance the model must emit
PROVENANCE_GEN = {
    "type": "object",
    "additionalProperties": False,
    "properties": {
        "model": {"type": "string"},
        "provider": {"type": "string"},
        "prompt_hash": {"type": "string"},
        "input_hash": {"type": "string"},
        "generated_at": {"type": "number"}
    },
    # Strict-mode rule: include every key in properties
    "required": ["model", "provider", "prompt_hash", "input_hash", "generated_at"]
}

COMPONENT_RESPONSE_SCHEMA_GEN = {
    "type": "object",
    "additionalProperties": False,
    "properties": {
        "group_id": {"type": "string"},
        "component_key": {"type": "string"},
        "title": {"type": "string"},
        "summary": {"type": "string"},
        "component": {
            "type": "object",
            "additionalProperties": False,
            "properties": {
                "id": {"type": "string"},
                "name": {"type": "string"},
                "what_it_does": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "additionalProperties": False,
                        "properties": {
                            "claim": {"type": "string"},
                            "evidence_ids": {"type": "array", "items": {"type": "string"}},
                            "confidence": {"type": "number"}
                        },
                        "required": ["claim", "evidence_ids", "confidence"]
                    }
                },
                "interfaces": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "additionalProperties": False,
                        "properties": {
                            "kind": {"type": "string"},
                            "description": {"type": "string"},
                            "evidence_ids": {"type": "array", "items": {"type": "string"}}
                        },
                        "required": ["kind", "description", "evidence_ids"]
                    }
                },
                "data_highlights": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "additionalProperties": False,
                        "properties": {
                            "note": {"type": "string"},
                            "evidence_ids": {"type": "array", "items": {"type": "string"}}
                        },
                        "required": ["note", "evidence_ids"]
                    }
                },
                "risks_gaps": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "additionalProperties": False,
                        "properties": {
                            "issue": {"type": "string"},
                            "evidence_ids": {"type": "array", "items": {"type": "string"}}
                        },
                        "required": ["issue", "evidence_ids"]
                    }
                }
            },
            "required": ["id", "name", "what_it_does", "interfaces", "data_highlights", "risks_gaps"]
        },
        "evidence_used": {"type": "array", "items": {"type": "string"}},
        "llm_subscore": {"type": "number"},
        "approved": {"type": "boolean"},
        "provenance": PROVENANCE_GEN
    },
    # Strict-mode: required must include every key in properties
    "required": ["group_id", "component_key", "title", "summary", "component", "evidence_used", "llm_subscore", "approved", "provenance"]
}

LLM_RESPONSE_SCHEMA_GEN = {
    "type": "object",
    "additionalProperties": False,
    "properties": {
        "group_id": {"type": "string"},
        "title": {"type": "string"},
        "summary": {"type": "string"},
        "components": {
            "type": "array",
            "items": {
                "type": "object",
                "additionalProperties": False,
                "properties": {
                    "id": {"type": "string"},
                    "name": {"type": "string"},
                    "what_it_does": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "additionalProperties": False,
                            "properties": {
                                "claim": {"type": "string"},
                                "evidence_ids": {"type": "array", "items": {"type": "string"}},
                                "confidence": {"type": "number"}
                            },
                            "required": ["claim", "evidence_ids", "confidence"]
                        }
                    },
                    "interfaces": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "additionalProperties": False,
                            "properties": {
                                "kind": {"type": "string"},
                                "description": {"type": "string"},
                                "evidence_ids": {"type": "array", "items": {"type": "string"}}
                            },
                            "required": ["kind", "description", "evidence_ids"]
                        }
                    },
                    "data_highlights": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "additionalProperties": False,
                            "properties": {
                                "note": {"type": "string"},
                                "evidence_ids": {"type": "array", "items": {"type": "string"}}
                            },
                            "required": ["note", "evidence_ids"]
                        }
                    },
                    "risks_gaps": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "additionalProperties": False,
                            "properties": {
                                "issue": {"type": "string"},
                                "evidence_ids": {"type": "array", "items": {"type": "string"}}
                            },
                            "required": ["issue", "evidence_ids"]
                        }
                    }
                },
                "required": ["id", "name", "what_it_does", "interfaces", "data_highlights", "risks_gaps"]
            }
        },
        "evidence_used": {"type": "array", "items": {"type": "string"}},
        "llm_subscore": {"type": "number"},
        "approved": {"type": "boolean"},
        "provenance": PROVENANCE_GEN
    },
    # Strict-mode: required must include every key in properties
    "required": ["group_id", "title", "summary", "components", "evidence_used", "llm_subscore", "approved", "provenance"]
}


COMPONENT_RESPONSE_SCHEMA = {
    "type": "object",
    "additionalProperties": False,
    "properties": {
        "group_id": {"type": "string"},
        "component_key": {"type": "string"},
        "title": {"type": "string"},
        "summary": {"type": "string"},
        "component": {
            "type": "object",
            "additionalProperties": False,
            "properties": {
                "id": {"type": "string"},
                "name": {"type": "string"},
                "what_it_does": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "additionalProperties": False,
                        "properties": {
                            "claim": {"type": "string"},
                            "evidence_ids": {
                                "type": "array",
                                "items": {"type": "string"}
                            },
                            "confidence": {"type": "number"}
                        },
                        "required": ["claim", "evidence_ids", "confidence"]
                    }
                },
                "interfaces": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "additionalProperties": False,
                        "properties": {
                            "kind": {"type": "string"},
                            "description": {"type": "string"},
                            "evidence_ids": {
                                "type": "array",
                                "items": {"type": "string"}
                            }
                        },
                        "required": ["kind", "description", "evidence_ids"]
                    }
                },
                "data_highlights": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "additionalProperties": False,
                        "properties": {
                            "note": {"type": "string"},
                            "evidence_ids": {
                                "type": "array",
                                "items": {"type": "string"}
                            }
                        },
                        "required": ["note", "evidence_ids"]
                    }
                },
                "risks_gaps": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "additionalProperties": False,
                        "properties": {
                            "issue": {"type": "string"},
                            "evidence_ids": {
                                "type": "array",
                                "items": {"type": "string"}
                            }
                        },
                        "required": ["issue", "evidence_ids"]
                    }
                }
            },
            "required": ["id", "name", "what_it_does", "interfaces", "data_highlights", "risks_gaps"]
        },
        "evidence_used": {"type": "array", "items": {"type": "string"}},
        "llm_subscore": {"type": "number"},
        "approved": {"type": "boolean"},
        "provenance": {
            "type": "object",
            "additionalProperties": False,
            "properties": {
                "model": {"type": "string"},
                "provider": {"type": "string"},
                "prompt_hash": {"type": "string"},
                "input_hash": {"type": "string"},
                "generated_at": {"type": "number"},
                "evidence_md_file": {"type": "string"},
                "usage": {
                    "type": "object",
                    "additionalProperties": False,
                    "properties": {
                        "input_tokens": {"type": ["number", "null"]},
                        "output_tokens": {"type": ["number", "null"]},
                        "total_tokens": {"type": ["number", "null"]}
                    },
                    "required": ["input_tokens", "output_tokens", "total_tokens"]
                },
                "latency_ms": {"type": ["number", "null"]},
                "cost_usd": {"type": ["number", "null"]}
            },
            "required": ["model", "provider", "prompt_hash", "input_hash", "generated_at"]
        }
    },
    "required": ["group_id", "component_key", "title", "summary", "component", "evidence_used", "llm_subscore", "approved", "provenance"]
}


LLM_RESPONSE_SCHEMA = {
    "type": "object",
    "additionalProperties": False,
    "properties": {
        "group_id": {"type": "string"},
        "title": {"type": "string"},
        "summary": {"type": "string"},
        "components": {
            "type": "array",
            "items": {
                "type": "object",
                "additionalProperties": False,
                "properties": {
                    "id": {"type": "string"},
                    "name": {"type": "string"},
                    "what_it_does": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "additionalProperties": False,
                            "properties": {
                                "claim": {"type": "string"},
                                "evidence_ids": {
                                    "type": "array",
                                    "items": {"type": "string"}
                                },
                                "confidence": {"type": "number"}
                            },
                            "required": ["claim", "evidence_ids", "confidence"]
                        }
                    },
                    "interfaces": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "additionalProperties": False,
                            "properties": {
                                "kind": {"type": "string"},
                                "description": {"type": "string"},
                                "evidence_ids": {
                                    "type": "array",
                                    "items": {"type": "string"}
                                }
                            },
                            "required": ["kind", "description", "evidence_ids"]
                        }
                    },
                    "data_highlights": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "additionalProperties": False,
                            "properties": {
                                "note": {"type": "string"},
                                "evidence_ids": {
                                    "type": "array",
                                    "items": {"type": "string"}
                                }
                            },
                            "required": ["note", "evidence_ids"]
                        }
                    },
                    "risks_gaps": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "additionalProperties": False,
                            "properties": {
                                "issue": {"type": "string"},
                                "evidence_ids": {
                                    "type": "array",
                                    "items": {"type": "string"}
                                }
                            },
                            "required": ["issue", "evidence_ids"]
                        }
                    }
                },
                "required": ["id", "name", "what_it_does", "interfaces", "data_highlights", "risks_gaps"]
            }
        },
        "evidence_used": {
            "type": "array",
            "items": {"type": "string"}
        },
        "llm_subscore": {"type": "number"},
        "approved": {"type": "boolean"},
        "provenance": {
            "type": "object",
            "additionalProperties": False,
            "properties": {
                "model": {"type": "string"},
                "provider": {"type": "string"},
                "prompt_hash": {"type": "string"},
                "input_hash": {"type": "string"},
                "generated_at": {"type": "number"},
                "evidence_md_file": {"type": "string"},
                "usage": {
                    "type": "object",
                    "additionalProperties": False,
                    "properties": {
                        "input_tokens": {"type": ["number", "null"]},
                        "output_tokens": {"type": ["number", "null"]},
                        "total_tokens": {"type": ["number", "null"]}
                    },
                    "required": ["input_tokens", "output_tokens", "total_tokens"]
                },
                "latency_ms": {"type": ["number", "null"]},
                "cost_usd": {"type": ["number", "null"]}
            },
            "required": ["model", "provider", "prompt_hash", "input_hash", "generated_at"]
        }

    },
    "required": ["group_id", "title", "summary", "components", "evidence_used", "llm_subscore", "approved", "provenance"]
}

# -------------------------- Generation Schemas (dynamic, for LLM) --------------------------

def _provenance_gen_schema() -> Dict[str, Any]:
    # Minimal provenance the model must emit
    return {
        "type": "object",
        "additionalProperties": False,
        "properties": {
            "model": {"type": "string"},
            "provider": {"type": "string"},
            "prompt_hash": {"type": "string"},
            "input_hash": {"type": "string"},
            "generated_at": {"type": "number"}
        },
        # Strict mode requires all keys listed
        "required": ["model", "provider", "prompt_hash", "input_hash", "generated_at"]
    }

def _build_group_gen_schema(allowed_ids: List[str]) -> Dict[str, Any]:
    def evidence_ids_array():
        return {"type": "array", "items": {"type": "string", "enum": allowed_ids}}

    return {
        "type": "object",
        "additionalProperties": False,
        "properties": {
            "group_id": {"type": "string"},
            "title": {"type": "string"},
            "summary": {"type": "string"},
            "components": {
                "type": "array",
                "items": {
                    "type": "object",
                    "additionalProperties": False,
                    "properties": {
                        "id": {"type": "string"},
                        "name": {"type": "string"},
                        "what_it_does": {
                            "type": "array",
                            "items": {
                                "type": "object",
                                "additionalProperties": False,
                                "properties": {
                                    "claim": {"type": "string"},
                                    "evidence_ids": evidence_ids_array(),
                                    "confidence": {"type": "number"}
                                },
                                "required": ["claim", "evidence_ids", "confidence"]
                            }
                        },
                        "interfaces": {
                            "type": "array",
                            "items": {
                                "type": "object",
                                "additionalProperties": False,
                                "properties": {
                                    "kind": {"type": "string"},
                                    "description": {"type": "string"},
                                    "evidence_ids": evidence_ids_array()
                                },
                                "required": ["kind", "description", "evidence_ids"]
                            }
                        },
                        "data_highlights": {
                            "type": "array",
                            "items": {
                                "type": "object",
                                "additionalProperties": False,
                                "properties": {
                                    "note": {"type": "string"},
                                    "evidence_ids": evidence_ids_array()
                                },
                                "required": ["note", "evidence_ids"]
                            }
                        },
                        "risks_gaps": {
                            "type": "array",
                            "items": {
                                "type": "object",
                                "additionalProperties": False,
                                "properties": {
                                    "issue": {"type": "string"},
                                    "evidence_ids": evidence_ids_array()
                                },
                                "required": ["issue", "evidence_ids"]
                            }
                        }
                    },
                    "required": ["id", "name", "what_it_does", "interfaces", "data_highlights", "risks_gaps"]
                }
            },
            "evidence_used": {"type": "array", "items": {"type": "string", "enum": allowed_ids}},
            "llm_subscore": {"type": "number"},
            "approved": {"type": "boolean"},
            "provenance": _provenance_gen_schema()
        },
        "required": ["group_id", "title", "summary", "components", "evidence_used", "llm_subscore", "approved", "provenance"]
    }

def _build_component_gen_schema(allowed_ids: List[str]) -> Dict[str, Any]:
    def evidence_ids_array():
        return {"type": "array", "items": {"type": "string", "enum": allowed_ids}}

    return {
        "type": "object",
        "additionalProperties": False,
        "properties": {
            "group_id": {"type": "string"},
            "component_key": {"type": "string"},
            "title": {"type": "string"},
            "summary": {"type": "string"},
            "component": {
                "type": "object",
                "additionalProperties": False,
                "properties": {
                    "id": {"type": "string"},
                    "name": {"type": "string"},
                    "what_it_does": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "additionalProperties": False,
                            "properties": {
                                "claim": {"type": "string"},
                                "evidence_ids": evidence_ids_array(),
                                "confidence": {"type": "number"}
                            },
                            "required": ["claim", "evidence_ids", "confidence"]
                        }
                    },
                    "interfaces": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "additionalProperties": False,
                            "properties": {
                                "kind": {"type": "string"},
                                "description": {"type": "string"},
                                "evidence_ids": evidence_ids_array()
                            },
                            "required": ["kind", "description", "evidence_ids"]
                        }
                    },
                    "data_highlights": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "additionalProperties": False,
                            "properties": {
                                "note": {"type": "string"},
                                "evidence_ids": evidence_ids_array()
                            },
                            "required": ["note", "evidence_ids"]
                        }
                    },
                    "risks_gaps": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "additionalProperties": False,
                            "properties": {
                                "issue": {"type": "string"},
                                "evidence_ids": evidence_ids_array()
                            },
                            "required": ["issue", "evidence_ids"]
                        }
                    }
                },
                "required": ["id", "name", "what_it_does", "interfaces", "data_highlights", "risks_gaps"]
            },
            "evidence_used": {"type": "array", "items": {"type": "string", "enum": allowed_ids}},
            "llm_subscore": {"type": "number"},
            "approved": {"type": "boolean"},
            "provenance": _provenance_gen_schema()
        },
        "required": ["group_id", "component_key", "title", "summary", "component", "evidence_used", "llm_subscore", "approved", "provenance"]
    }


# ----------------------------- Prompts -----------------------------

PROMPT_SYSTEM = (
    "You are an evidence-first documentation synthesizer. Use the evidence items "
    "supplied in the 'evidence_index' object of the context. Do not invent facts, but do pay attention to "
    "patterns and draw fact-based conclusions. For each claim you make, include explicit evidence_ids that "
    "exactly match keys from evidence_index. If evidence is insufficient for a claim, produce the claim with "
    "evidence_ids = [] and mark the text as 'cannot_conclude'. Output must be valid JSON conforming to the provided "
    "schema. Return only JSON-embedded explanations without extra text."
)

PROMPT_USER_INSTRUCTIONS = (
    "Task: Given the provided context JSON (artifacts, SIRs, and evidence_index), produce a JSON "
    "object that: (1) summarizes the group, (2) emits concise factual claims per component, each "
    "with evidence_ids, and (3) lists evidence_used (union). Schema: group_id, title, summary, "
    "components[], evidence_used[], llm_subscore, approved, provenance.\n\n"
    "Rules:\n"
    "- Only cite evidence IDs present in evidence_index.\n"
    "- For each factual sentence, include evidence_ids (one or more).\n"
    "- If you cannot support a factual assertion, set its evidence_ids to [] and include 'cannot_conclude' "
    "in the claim text.\n"
    "- Keep claims concise and factual. Avoid interpretation beyond the evidence.\n"
    "- Return strictly valid JSON matching the schema."
)

PROMPT_USER_INSTRUCTIONS_COMPONENT = (
    "Task: Given the provided context JSON (this is ONE component's artifacts, SIRs, and evidence_index), "
    "produce a JSON object that: (1) summarizes the component, (2) emits concise factual claims under 'component.what_it_does', "
    "each with 'evidence_ids' from 'evidence_index', (3) lists 'interfaces', 'data_highlights', 'risks_gaps', "
    "and (4) lists 'evidence_used' (union). Schema: group_id, component_key, title, summary, component{...}, evidence_used, llm_subscore, approved, provenance.\n\n"
    "Rules:\n"
    "- Only cite evidence IDs present in evidence_index.\n"
    "- For each factual sentence, include evidence_ids (one or more). If you cannot support a claim, set evidence_ids=[] and add 'cannot_conclude' in the text.\n"
    "- Keep claims concise and factual. Avoid interpretation beyond the evidence.\n"
    "- Return strictly valid JSON matching the schema."
)

def _compose_prompt(context: Dict[str, Any]) -> str:
    return PROMPT_SYSTEM + "\n\n" + PROMPT_USER_INSTRUCTIONS + "\n\nContext JSON:\n" + json.dumps(context, indent=2)

def _compose_component_prompt(context: Dict[str, Any]) -> str:
    return PROMPT_SYSTEM + "\n\n" + PROMPT_USER_INSTRUCTIONS_COMPONENT + "\n\nContext JSON:\n" + json.dumps(context, indent=2)

# ------------------------- Helpers and Sanitizers -------------------------

def _norm_path(p: str) -> str:
    return os.path.normpath(p).replace("\\", "/") if p else ""

def _component_key_from_path(path: str, repo_name: str) -> str:
    """
    Given an absolute 'repo_path' or 'file' path and the group_id (repo name),
    return the first dir under the repo as the component key.
    """
    if not path or not repo_name:
        return "root"
    p = _norm_path(path)
    parts = p.split("/")
    try:
        idx = parts.index(repo_name)
        if idx + 1 < len(parts):
            return parts[idx + 1]
    except ValueError:
        pass
    return "root"

def _component_buckets_from_context(context: Dict[str, Any], group_id: str) -> Dict[str, Dict[str, Any]]:
    """
    Build buckets per component_key with filtered artifacts and sirs.
    """
    buckets: Dict[str, Dict[str, Any]] = {}
    for a in context.get("artifacts", []):
        key = _component_key_from_path(a.get("repo_path", ""), group_id)
        b = buckets.setdefault(key, {"artifacts": [], "sirs": []})
        b["artifacts"].append(a)
    for s in context.get("sirs", []):
        key = _component_key_from_path(s.get("file", ""), group_id)
        b = buckets.setdefault(key, {"artifacts": [], "sirs": []})
        b["sirs"].append(s)
    return buckets

def _evidence_ids_in_component(artifacts: List[Dict[str, Any]], sirs: List[Dict[str, Any]]) -> List[str]:
    """
    Placeholder for extracting exact evidence ids per component if available.
    Keeping full evidence_index for now.
    """
    return []

def _shorten(s: Any, n: int = 1000) -> str:
    if s is None:
        return ""
    s = str(s)
    return s if len(s) <= n else s[: n - 1] + "…"

def _hash_obj(obj: Any) -> str:
    try:
        return hashlib.sha256(json.dumps(obj, sort_keys=True).encode("utf-8")).hexdigest()
    except Exception:
        return hashlib.sha256(str(obj).encode("utf-8")).hexdigest()

def _as_list(x: Any) -> List[Any]:
    if x is None:
        return []
    if isinstance(x, list):
        return x
    return [x]

def _safe_evidence_anchor(ev: Any) -> Dict[str, str]:
    """
    Normalize any evidence into a dict with path/lines/snippet fields.
    """
    if isinstance(ev, dict):
        return {
            "path": str(ev.get("path", "")),
            "lines": str(ev.get("lines", "")),
            "snippet": _shorten(ev.get("snippet", ""), 800),
        }
    return {"path": "", "lines": "", "snippet": _shorten(str(ev), 800)}

def _sanitize_evidence_index(idx: Dict[str, Any]) -> Dict[str, Dict[str, str]]:
    out: Dict[str, Dict[str, str]] = {}
    for eid, ev in idx.items():
        out[eid] = _safe_evidence_anchor(ev)
    return out

def _sanitize_artifact(a: Any) -> Dict[str, Any]:
    if not isinstance(a, dict):
        return {"name": str(a)}
    name = a.get("name")
    artifact_type = a.get("artifact_type")
    repo_path = a.get("repo_path")
    confidence = a.get("confidence")
    workflows = a.get("workflows")
    steps_summary = ""
    if isinstance(workflows, list) and workflows:
        first = workflows[0]
        if isinstance(first, dict):
            steps_summary = str(first.get("steps_summary", "")) or str(first.get("name", ""))
        else:
            steps_summary = str(first)
    elif isinstance(workflows, dict):
        steps_summary = str(workflows.get("steps_summary", "")) or str(workflows.get("name", ""))
    elif isinstance(workflows, str):
        steps_summary = workflows
    ev_list = []
    for ev in _as_list(a.get("evidence", [])):
        ev_list.append(_safe_evidence_anchor(ev))
    return {
        "name": name,
        "artifact_type": artifact_type,
        "repo_path": repo_path,
        "confidence": confidence,
        "steps_summary": _shorten(steps_summary, 1000),
        "evidence": ev_list,
    }

def _sanitize_sir(s: Any) -> Dict[str, Any]:
    """
    Robust SIR sanitizer; tolerates strings, dicts with mixed shapes.
    - Preserves props.triggers and props.steps (needed for visuals)
    - Preserves roles_evidence
    """
    if not isinstance(s, dict):
        return {"id": str(s)}

    sid = s.get("id") or s.get("name") or ""
    sname = s.get("name") or sid
    sfile = s.get("file", "")

    # Pull roles list and subscores
    roles = s.get("roles", [])
    subscores = s.get("subscores", {})

    # Evidence arrays (plain and per-role)
    sir_evidence = []
    for ev in _as_list(s.get("evidence", [])):
        sir_evidence.append(_safe_evidence_anchor(ev))

    roles_evidence_in = s.get("roles_evidence", {})
    roles_evidence_out: Dict[str, List[Dict[str, str]]] = {}
    if isinstance(roles_evidence_in, dict):
        for role, evs in roles_evidence_in.items():
            lst = []
            for ev in _as_list(evs):
                lst.append(_safe_evidence_anchor(ev))
            roles_evidence_out[str(role)] = lst

    # Preserve triggers and steps if they exist in props (TIBCO BW extractors)
    props = s.get("props", {}) or {}
    triggers = props.get("triggers") or s.get("triggers") or []
    steps = props.get("steps") or s.get("steps") or []

    return {
        "id": sid,
        "name": sname,
        "file": sfile,
        "roles": roles if isinstance(roles, list) else _as_list(roles),
        "subscores": subscores if isinstance(subscores, dict) else {},
        "evidence": sir_evidence,
        "roles_evidence": roles_evidence_out,
        "triggers": triggers if isinstance(triggers, list) else [],
        "steps": steps if isinstance(steps, list) else []
    }


# --------------------------- JSON Parsing ---------------------------

def _extract_first_json_object(s: str) -> Optional[str]:
    start = None
    depth = 0
    in_string = False
    escape = False
    for i, ch in enumerate(s):
        if start is None:
            if ch == '{':
                start = i
                depth = 1
                in_string = False
                escape = False
            continue
        if in_string:
            if escape:
                escape = False
            elif ch == '\\':
                escape = True
            elif ch == '"':
                in_string = False
        else:
            if ch == '"':
                in_string = True
            elif ch == '{':
                depth += 1
            elif ch == '}':
                depth -= 1
                if depth == 0:
                    return s[start:i+1]
    return None

def _parse_json_loose(text: str):
    try:
        return json.loads(text)
    except Exception:
        pass
    fence = re.search(r"```json\s*(.*?)```", text, flags=re.DOTALL | re.IGNORECASE)
    if fence:
        candidate = fence.group(1).strip()
        try:
            return json.loads(candidate)
        except Exception:
            pass
    obj = _extract_first_json_object(text)
    if obj is not None:
        try:
            return json.loads(obj)
        except Exception:
            pass
    return None

def _parse_llm_json_or_raise(text: str):
    obj = _parse_json_loose(text)
    if obj is None:
        snippet = text[:2000]
        raise RuntimeError("LLM returned non-JSON or unparsable response (after cleanup). Raw (first 2k chars): " + snippet)
    return obj

# --------------------------- Provenance ---------------------------

def _sha256_string(s: str) -> str:
    return hashlib.sha256(s.encode("utf-8")).hexdigest()

def _sha256_jsonable(obj) -> str:
    return hashlib.sha256(
        json.dumps(obj, sort_keys=True, ensure_ascii=False).encode("utf-8")
    ).hexdigest()

def _attach_provenance(doc: dict, prompt: str, input_context) -> None:
    doc.setdefault("provenance", {})
    doc["provenance"].update({
        "model": LLM_MODEL,
        "provider": LLM_PROVIDER,
        "prompt_hash": _sha256_string(prompt),
        "input_hash": _sha256_jsonable(input_context) if input_context is not None else _sha256_string("none"),
        "generated_at": int(time.time())
    })

# ---------------------------- Context Builders ----------------------------

def build_component_context(group_id: str,
                            component_key: str,
                            artifacts: List[Dict[str, Any]],
                            sirs: List[Dict[str, Any]],
                            full_evidence_index: Dict[str, Dict[str, str]]) -> Dict[str, Any]:
    ctx: Dict[str, Any] = {
        "group_id": group_id,
        "component_key": component_key,
        "metadata": {"model": LLM_MODEL, "token_budget": LLM_MAX_OUTPUT_TOKENS},
        "artifacts": artifacts[:200],
        "sirs": sirs[:400],
        "evidence_index": {}
    }
    for eid, ev in full_evidence_index.items():
        ctx["evidence_index"][eid] = {
            "path": ev.get("path", ""),
            "lines": ev.get("lines", ""),
            "snippet": _shorten(ev.get("snippet", ""), 800)
        }
    return ctx

def build_context_for_group(group_id: str, group_obj: Dict[str, Any], evidence_index: Dict[str, Dict[str, str]]) -> Dict[str, Any]:
    ctx: Dict[str, Any] = {
        "group_id": group_id,
        "metadata": {"model": LLM_MODEL, "token_budget": LLM_MAX_OUTPUT_TOKENS},
        "artifacts": [],
        "sirs": [],
        "evidence_index": {}
    }
    for a in (group_obj.get("artifacts") or [])[:300]:
        ctx["artifacts"].append(_sanitize_artifact(a))
    for s in (group_obj.get("sirs") or [])[:600]:
        ctx["sirs"].append(_sanitize_sir(s))
    for eid, ev in evidence_index.items():
        ctx["evidence_index"][eid] = {
            "path": ev.get("path", ""),
            "lines": ev.get("lines", ""),
            "snippet": _shorten(ev.get("snippet", ""), 800)
        }
    return ctx

# ---------------------------- Scoring/Verify -----------------------------

def _score_claim_against_evidence(claim_text: str, evidence_ids: List[str], evidence_index: Dict[str, Dict[str, Any]]) -> float:
    if not evidence_ids:
        return 0.0
    snippets: List[str] = []
    for eid in evidence_ids:
        ev = evidence_index.get(eid)
        if ev and isinstance(ev, dict) and ev.get("snippet"):
            snippets.append(str(ev["snippet"]))
    if not snippets:
        return 0.0
    joined = " ".join(snippets)
    ratio = fuzz.token_set_ratio(str(claim_text), joined)
    return max(0.0, min(1.0, ratio / 100.0))

def _compute_llm_subscore(resp_json: Dict[str, Any], evidence_index: Dict[str, Dict[str, Any]]) -> float:
    scores: List[float] = []
    for comp in resp_json.get("components", []) or []:
        for w in comp.get("what_it_does", []) or []:
            if isinstance(w, dict):
                claim = w.get("claim", "")
                eids = w.get("evidence_ids", []) or []
                scores.append(_score_claim_against_evidence(claim, eids, evidence_index))
    return round((sum(scores) / len(scores)) if scores else 0.0, 3)

# ------------------------------- Persist --------------------------------

def _evidence_anchor(eid: str) -> str:
    return eid.replace(":", "_").replace("/", "_").replace("\\", "_").replace("#", "_")

def _persist_evidence_index_md(group_id: str, evidence_index: Dict[str, Dict[str, str]], out_dir: Path) -> str:
    gid_safe = group_id.replace(" ", "_")
    docs_dir = out_dir / "docs"
    docs_dir.mkdir(parents=True, exist_ok=True)
    ev_md_path = docs_dir / f"{gid_safe}__evidence.md"

    lines = [f"# Evidence Index for {group_id}\n"]
    for eid, ev in sorted(evidence_index.items()):
        anchor = _evidence_anchor(eid)
        lines.append(f'<a id="{anchor}"></a>')
        lines.append(f"## {eid}\n")
        path = ev.get("path", "")
        loc = ev.get("lines", "")
        snippet = ev.get("snippet", "")
        if path:
            lines.append(f"- Path: `{path}`")
        if loc:
            lines.append(f"- Lines: `{loc}`")
        if snippet:
            lines.append(f"\n```\n{snippet}\n```\n")
        lines.append("")  # blank line
    ev_md = "\n".join(lines)
    ev_md_path.write_text(ev_md, encoding="utf-8")
    return ev_md_path.name  # return filename for linking

def _persist_component_output(group_id: str, component_key: str, resp_json: Dict[str, Any], out_dir: Path) -> None:
    gid_safe = group_id.replace(" ", "_")
    cid_safe = component_key.replace(" ", "_").replace("/", "_")
    llm_dir = out_dir / "llm" / "components" / gid_safe
    docs_dir = out_dir / "docs" / "components" / gid_safe
    llm_dir.mkdir(parents=True, exist_ok=True)
    docs_dir.mkdir(parents=True, exist_ok=True)

    (llm_dir / f"{cid_safe}.json").write_text(json.dumps(resp_json, indent=2), encoding="utf-8")

    ev_file = resp_json.get("provenance", {}).get("evidence_md_file", "")

    title = resp_json.get("title", f"Component: {component_key}")
    summary = resp_json.get("summary", "")
    md = f"# {title}\n\n{summary}\n\n"
    comp = resp_json.get("component", {}) or {}
    name = comp.get("name", component_key)
    md += f"## {name}\n"
    for w in comp.get("what_it_does", []) or []:
        if isinstance(w, dict):
            eids = w.get("evidence_ids", []) or []
            if ev_file and eids:
                links = [f"[{eid}](../../{ev_file}#{_evidence_anchor(eid)})" for eid in eids]
            else:
                links = eids
            md += f"- {w.get('claim','')} (evidence: {', '.join(links)})\n"
    md += "\n"
    (docs_dir / f"{cid_safe}.md").write_text(md, encoding="utf-8")

def _persist_llm_output(group_id: str, resp_json: Dict[str, Any], out_dir: Path) -> None:
    gid_safe = group_id.replace(" ", "_")
    llm_dir = out_dir / "llm"
    docs_dir = out_dir / "docs"
    llm_dir.mkdir(parents=True, exist_ok=True)
    docs_dir.mkdir(parents=True, exist_ok=True)

    (llm_dir / f"{gid_safe}.json").write_text(json.dumps(resp_json, indent=2), encoding="utf-8")

    ev_file = resp_json.get("provenance", {}).get("evidence_md_file", "")

    md = f"# {resp_json.get('title', 'Group: '+group_id)}\n\n{resp_json.get('summary','')}\n\n"
    for comp in resp_json.get("components", []) or []:
        name = comp.get("name") if isinstance(comp, dict) else str(comp)
        md += f"## {name}\n"
        for w in comp.get("what_it_does", []) or []:
            if isinstance(w, dict):
                eids = w.get('evidence_ids', []) or []
                if ev_file and eids:
                    links = [f"[{eid}]({ev_file}#{_evidence_anchor(eid)})" for eid in eids]
                else:
                    links = eids
                md += f"- {w.get('claim','')} (evidence: {', '.join(links)})\n"
        md += "\n"
    (docs_dir / f"{gid_safe}.md").write_text(md, encoding="utf-8")

def _safe_sample(lst, n=50):
    try:
        return list(lst)[:n]
    except Exception:
        return []

# --------------------------- Telemetry helpers ---------------------------

def _cost_from_usage(usage: Dict[str, Any]) -> Optional[float]:
    if not usage:
        return None
    pricing = SETTINGS["llm"].get("telemetry", {}).get("pricing", {}) or {}
    in_rate = pricing.get("input_tokens_per_million_usd")
    out_rate = pricing.get("output_tokens_per_million_usd")
    if in_rate is None or out_rate is None:
        return None
    it = usage.get("input_tokens") or 0
    ot = usage.get("output_tokens") or 0
    return round((it / 1_000_000.0) * float(in_rate) + (ot / 1_000_000.0) * float(out_rate), 6)

def _append_usage_csv(out_dir: Path, scope: str, scope_id: str, usage: Dict[str, Any], cost: Optional[float], latency_ms: Optional[int]) -> None:
    if not SETTINGS["llm"].get("telemetry", {}).get("enabled", False):
        return
    metrics_dir = out_dir / "metrics"
    metrics_dir.mkdir(parents=True, exist_ok=True)
    csv_path = metrics_dir / "llm_usage.csv"
    import csv, time as _t
    headers = ["ts_epoch", "scope", "scope_id", "input_tokens", "output_tokens", "total_tokens", "cost_usd", "latency_ms"]
    row = [
        int(_t.time()),
        scope,
        scope_id,
        usage.get("input_tokens"),
        usage.get("output_tokens"),
        usage.get("total_tokens"),
        cost,
        latency_ms
    ]
    write_header = not csv_path.exists()
    with csv_path.open("a", newline="", encoding="utf-8") as fh:
        w = csv.writer(fh)
        if write_header:
            w.writerow(headers)
        w.writerow(row)

def _prune_unknown_evidence_ids(doc: Dict[str, Any], evidence_index: Dict[str, Any]) -> int:
    """
    Remove any unknown evidence ids from what_it_does claims. Returns count removed.
    """
    removed = 0
    comps = doc.get("components", []) or []
    for comp in comps:
        items = comp.get("what_it_does", []) or []
        for w in items:
            if isinstance(w, dict):
                eids = w.get("evidence_ids", []) or []
                keep = [e for e in eids if e in evidence_index]
                removed += (len(eids) - len(keep))
                w["evidence_ids"] = keep
                if not keep and "cannot_conclude" not in (w.get("claim") or "").lower():
                    w["claim"] = (w.get("claim","") + " [cannot_conclude]").strip()
    return removed



# ------------------------------ Main API --------------------------------

def rollup_group_and_persist(group_id: str, group_obj: Dict[str, Any], out_dir: Optional[str | Path] = None) -> Dict[str, Any]:
    out_dir = Path(out_dir or OUT_DIR)

    # 1) Build raw evidence index and sanitize all entries
    raw_index = build_evidence_index(out_dir)
    evidence_index = _sanitize_evidence_index(raw_index)

    # 2) Build robust, trimmed context from sanitized index
    context = build_context_for_group(group_id, group_obj, evidence_index)

    # 2b) Persist evidence index page now, and pass filename via provenance
    evidence_md_filename = _persist_evidence_index_md(group_id, evidence_index, out_dir)

    # 3) Compose prompt and compute hashes
    prompt = _compose_prompt(context)
    prompt_hash = _hash_obj(prompt)
    input_hash = _hash_obj(context)

    # 4) Call LLM (group) with meta
    allowed_ids = list(evidence_index.keys())
    group_gen_schema = _build_group_gen_schema(allowed_ids)
    res = call_openai_meta(prompt=prompt, input_json=None, json_schema=group_gen_schema)

    raw_resp = res["text"]
    usage = res.get("usage", {}) or {}
    latency_ms = res.get("latency_ms", None)

    # 4b) Per-component rollups
    buckets = _component_buckets_from_context(context, group_id)
    for component_key, items in buckets.items():
        c_ctx = build_component_context(group_id, component_key, items["artifacts"], items["sirs"], evidence_index)
        c_prompt = _compose_component_prompt(c_ctx)

        # LLM call with meta for telemetry
        # You can reuse the same allowed_ids since we include full evidence_index in component contexts
        comp_gen_schema = _build_component_gen_schema(allowed_ids)
        res_c = call_openai_meta(prompt=c_prompt, input_json=None, json_schema=comp_gen_schema)

        raw_c = res_c["text"]
        usage_c = res_c.get("usage", {}) or {}
        latency_ms_c = res_c.get("latency_ms", None)

        # Parse loosely, then provenance, then ensure required keys, then validate
        c_json = _parse_llm_json_or_raise(raw_c)
        c_input_context = {"group_id": group_id, "component_key": component_key}
        _attach_provenance(c_json, prompt=c_prompt, input_context=c_input_context)
        c_json["provenance"]["evidence_md_file"] = evidence_md_filename

        # Ensure required keys exist before validation (LLM may omit)
        c_json.setdefault("llm_subscore", 0.0)
        c_json.setdefault("approved", False)

        if SETTINGS["llm"].get("telemetry", {}).get("enabled", False):
            cost_c = _cost_from_usage(usage_c)
            c_json["provenance"]["usage"] = usage_c
            c_json["provenance"]["latency_ms"] = latency_ms_c
            if cost_c is not None:
                c_json["provenance"]["cost_usd"] = cost_c

        _append_usage_csv(
            out_dir,
            scope="component",
            scope_id=f"{group_id}/{component_key}",
            usage=usage_c,
            cost=_cost_from_usage(usage_c),
            latency_ms=latency_ms_c
        )

        try:
            jsonschema_validate(c_json, COMPONENT_RESPONSE_SCHEMA)
        except ValidationError as e:
            raise RuntimeError(f"Component rollup failed schema validation for {component_key}: {e}")

        # score + approve (re-use scorer on a single component shape)
        c_json["llm_subscore"] = _compute_llm_subscore(
            {"components": [{"what_it_does": c_json.get("component", {}).get("what_it_does", [])}]},
            evidence_index
        )
        c_json["approved"] = bool(c_json["llm_subscore"] >= ROLLUP_PUBLISH_THRESHOLD)

    # Business profile toggle from YAML
    PROFILE = (SETTINGS.get("docs") or {}).get("profile", "business")
    
    if PROFILE == "business":
        render_business_component_page(
            out_docs_dir=out_dir / "docs",
            group_id=group_id,
            component_key=component_key,
            c_json=c_json,
            sirs=c_ctx.get("sirs", []) or [],
            evidence_md_filename=evidence_md_filename
        )
    else:
        _persist_component_output(group_id, component_key, c_json, out_dir)
    

    # 5) Parse loosely (handles ```json fences, extra prose, etc.) for group
    resp_json = _parse_llm_json_or_raise(raw_resp)

    if SETTINGS["rollup"].get("allow_unknown_evidence", False):
        _prune_unknown_evidence_ids(resp_json, evidence_index)
    else:
        # keep strict check you already have; will raise on unknown
        for eid in resp_json.get("evidence_used", []) or []:
            if eid not in evidence_index:
                raise RuntimeError(f"LLM referenced unknown evidence id: {eid}")
    

    # 6) Build an input_context summary for reproducibility
    artifact_paths = _safe_sample([
        a.get("repo_path") for a in context.get("artifacts", [])
        if isinstance(a, dict) and a.get("repo_path")
    ])
    sir_ids = _safe_sample([
        s.get("id") for s in context.get("sirs", [])
        if isinstance(s, dict) and s.get("id")
    ])
    input_context = {
        "group_id": group_id,
        "artifact_paths": artifact_paths,
        "sir_ids": sir_ids
    }

    # 7) Inject provenance before validation (schema requires these fields)
    _attach_provenance(resp_json, prompt=prompt, input_context=input_context)
    resp_json["provenance"]["evidence_md_file"] = evidence_md_filename

    if SETTINGS["llm"].get("telemetry", {}).get("enabled", False):
        cost = _cost_from_usage(usage)
        resp_json["provenance"]["usage"] = usage
        resp_json["provenance"]["latency_ms"] = latency_ms
        if cost is not None:
            resp_json["provenance"]["cost_usd"] = cost

    _append_usage_csv(out_dir, scope="group", scope_id=group_id, usage=usage, cost=_cost_from_usage(usage), latency_ms=latency_ms)

    # 8) Ensure required keys exist before validation (LLM may omit)
    resp_json.setdefault("llm_subscore", 0.0)
    resp_json.setdefault("approved", False)

    # 9) Validate schema
    try:
        jsonschema_validate(resp_json, LLM_RESPONSE_SCHEMA)
    except ValidationError as e:
        raise RuntimeError("LLM output failed schema validation: " + str(e))

    # 10) Verify evidence IDs exist in the (sanitized) index
    for eid in resp_json.get("evidence_used", []) or []:
        if eid not in evidence_index:
            raise RuntimeError(f"LLM referenced unknown evidence id: {eid}")

    # 11) Compute llm_subscore
    llm_sub = _compute_llm_subscore(resp_json, evidence_index)
    resp_json["llm_subscore"] = llm_sub

    # 12) Approval policy
    resp_json["approved"] = bool(llm_sub >= ROLLUP_PUBLISH_THRESHOLD)

    # 13) Persist outputs
    PROFILE = (SETTINGS.get("docs") or {}).get("profile", "business")
    if PROFILE == "business":
        render_business_group_page(
            out_docs_dir=out_dir / "docs",
            group_id=group_id,
            resp_json=resp_json,
            evidence_md_filename=evidence_md_filename
        )
    else:
        _persist_llm_output(group_id, resp_json, out_dir)
    

    return resp_json

# ------------------------------ CLI helper ------------------------------

if __name__ == "__main__":
    print("rollup module loaded. Use orchestrator to run rollup per group.")

