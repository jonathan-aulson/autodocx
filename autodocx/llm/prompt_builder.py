from __future__ import annotations

import json
from typing import Any, Dict


GROUP_PROMPT_TEMPLATE = """You are an evidence-first documentation assistant. You receive structured context
for a component group including artifacts, SIRs, experience_packs, ui_snapshots, payload_examples, process_flows,
integration summaries, stitched timelines, and `journey_blueprints_input`. Produce JSON that satisfies the target schema
and is ready for executive consumption. For every component:

- Populate `what_it_does` with long-form claims (use the `detail` field for narrative paragraphs) derived from workflows,
  process_flows, stitched_timelines, and payload_examples. Every entry must cite evidence_ids that exist in the evidence_index;
  if you cannot cite evidence, leave evidence_ids empty and append "[cannot_conclude]" to the text.
- Fill `user_experience` using `experience_packs`, `ui_snapshots`, screenshot metadata, and `primary_journeys`.
  Reference screenshots by filename/path inside the `screenshots` array and describe how the user experiences the flow.
- Generate `journey_blueprints`: each blueprint needs a title and 4-8 ordered `steps` (short phrases). Base them on
  `journey_blueprints_input` whenever available; cite evidence_ids for the entire blueprint (reuse IDs from referenced steps when needed).
- Provide `risk_stories`, `operational_behaviors`, and `data_flows` using relationship_matrix data, integration summaries,
  and payload samples. Treat each entry as a short narrative that still cites evidence_ids.
- Populate `relationships_summary` and `dependency_matrix` using process_flows and relationship data (max 5 entries each).
- Never invent evidence ids; if a claim lacks evidence, mark it as "[cannot_conclude]" and leave the list empty.

Return strictly valid JSON that adheres to the supplied schema.

Context:
{context_json}
"""


COMPONENT_PROMPT_TEMPLATE = """You are an evidence-first documentation assistant. Using the context for a single component
below, produce JSON describing what the component does, how users experience it, key interfaces, data flows, risks,
and operational behaviors. Requirements:

- `what_it_does` items must include a descriptive `detail` paragraph citing evidence_ids.
- Use `experience_packs`, `ui_snapshots`, `payload_examples`, and `stitched_timelines` to craft `user_experience` entries.
  Reference screenshot file names/paths in the `screenshots` field and cite evidence. Highlight user journeys and UI states.
- Fill `journey_blueprints` by transforming `journey_blueprints_input` (or process_flows) into polished narratives:
  each blueprint must have a title plus ordered `steps` (phrases). Cite evidence_ids for the overall blueprint.
- Fill `risk_stories`, `operational_behaviors`, and `data_flows` using process_flows, relationship_matrix, and integration data.
- Use `process_flows`, `code_entities`, `ui_components`, and `integrations` to keep interfaces and summaries concrete.
- If evidence is missing for a statement, append "[cannot_conclude]" and leave `evidence_ids` empty.
- Keep each array focused (<=5 entries where possible) while covering the available evidence.

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
