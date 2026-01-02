from __future__ import annotations

import json
from typing import Any, Dict


GROUP_PROMPT_TEMPLATE = """You are an evidence-first documentation assistant. You receive normalized context for a component group
including artifacts, SIRs (with interdependency slices + deterministic explanations), experience packs, payload examples,
process_flows, integration summaries, journey_blueprints_input, and traceability inputs. Produce JSON that satisfies the schema
and mirrors the reference structure. For every component:

- `what_it_does`: 4–7 evidence-backed claims describing the workflow. Use long-form `detail` sentences. Cite evidence_ids from the evidence_index; if unsure, append "[cannot_conclude]" and leave the list empty.
- `why_it_matters`: explain the customer/business impact of those behaviors (2–4 entries). Always tie back to evidence or explicitly mark "[cannot_conclude]".
- `interfaces`: list exposed entry points (REST/SOAP/queue/timer/etc.) based on triggers, routes, process_flows, palette roles, and relationship data. Include endpoint/method information and evidence_ids; for BW, surface partnerLinks and mapper IO.
- `invokes`: downstream calls and dependencies (child workflows, APIs, datastores) using process_flows + interdependency slices; for BW, include mapper-derived identifiers and JDBC/JMS targets.
- `key_inputs` / `key_outputs`: summarize schemas, parameters, and payload examples discovered in `payload_examples`, SIR input/output hints, or experience_packs.
- `errors_and_logging`: describe notable error handling or logging behaviors. Use mapper hints, scaffold data, or SIR steps; cite evidence when available, including BW log/error palette steps.
- `interdependencies`: capture “calls”, “called_by”, and “shared_data” relationships from `interdependencies_slice` plus repository-level data.
- `extrapolations`: only include hypotheses clearly marked "[hypothesis]" with rationale + `hypothesis_score` between 0-1. Cite evidence backing the rationale or leave the list empty.
- `traceability`: map statements back to artifacts/SIRs using `traceability_inputs`. Each entry should reference the source artifact/signal type and evidence_ids.
- `journey_blueprints`: craft 4–8 step narratives that describe the workflow in plain language (use `journey_blueprints_input` or deterministic explanations as seed data).

General rules:
- Never invent evidence ids. When evidence is missing, append "[cannot_conclude]" to the sentence and supply an empty list.
- Keep each array concise (≤5 entries) unless the evidence demands more coverage.
- Always return strictly valid JSON that matches the schema.

Context:
{context_json}
"""


COMPONENT_PROMPT_TEMPLATE = """You are an evidence-first documentation assistant. Using the context for a single component,
produce JSON with the exact sections required by the schema. Follow these rules:

- `what_it_does` and `why_it_matters`: derive from workflows, deterministic explanations, process_flows, and payload_examples. Always include evidence_ids.
- `interfaces` / `invokes`: use triggers, routes, relationship data, integration summaries, and palette roles to enumerate inbound/outbound touchpoints. For BW, include partnerLinks, mapper IO, and JDBC/JMS/timer bindings.
- `key_inputs` / `key_outputs`: rely on payload_examples, inputs/outputs examples, schemas, and experience packs.
- `errors_and_logging`: capture observable error handling, retries, logging, monitoring (including BW log/error activities). State “[cannot_conclude]” when the repo does not expose enough evidence.
- `interdependencies`: transform `interdependencies_slice` (calls/called_by/shared_data) into narrative entries; cite evidence_ids (or leave empty if uncertain).
- `extrapolations`: only include well-reasoned hypotheses with `hypothesis_score` between 0 and 1 plus rationale.
- `traceability`: map back to SIRs/artifacts. Use the provided `traceability_inputs` to reference evidence ids that prove the linkage.
- `journey_blueprints`: translate `journey_blueprints_input` or process_flows into user-friendly step lists.
- Keep arrays focused (≤5 entries when possible) but cover all critical evidence.
- Never fabricate evidence IDs. If none exist, set `evidence_ids: []` and append "[cannot_conclude]" in the narrative.

Return strictly valid JSON adhering to the provided schema.

Context:
{context_json}
"""


def build_group_prompt(context: Dict[str, Any]) -> str:
    payload = json.dumps(context, indent=2, ensure_ascii=False)
    return GROUP_PROMPT_TEMPLATE.format(context_json=payload)


def build_component_prompt(context: Dict[str, Any]) -> str:
    payload = json.dumps(context, indent=2, ensure_ascii=False)
    return COMPONENT_PROMPT_TEMPLATE.format(context_json=payload)
