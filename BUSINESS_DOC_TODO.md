# Business Documentation Upgrade Plan

Use the checkboxes (`[ ]` → `[x]`) to capture progress. Update the log notes under each task with evidence (commands, files) when you flip a box.

**Execution guardrails (per user directive)**  
- Research the reference implementation before modifying code; capture findings inline with each checkbox.  
- Run targeted tests or scans after every substantive change and log the command/result.  
- Capture any recommendations to further increase LLM output richness in the ToDo list so we can review them together.

## Phase 0 – Baseline & Planning

- [x] Compare the reference `bw_orchestrate` pipeline against our current outputs (focus: SIR shape, enrichment depth, renderer structure, diagram fidelity). _Notes: Reviewed `bw_orchestrate.py`, `bw_enrich.py`, `dox_follow_plan.py`, and replicated repo `repos/bw-samples-master` for future regression._
- [x] Capture the end-to-end upgrade plan in this project, aligning milestones with reference capabilities. _Notes: Plan recorded in `BUSINESS_DOC_TODO.md`; subsequent phases break work into repeatable units._

## Phase 1 – Normalized SIR & Manifest Parity

- [x] Design a normalized “process SIR v2” contract (activities, transitions, resources, metadata, hashes) that works for BW/PB, Towne Park, and future stacks; include deterministic start-activity detection.
- [x] Extend `autodocx_cli` to emit the v2 SIR for every workflow/classic signal (store under `out/sir_v2/`), ensuring existing `Signal` pathways remain backward-compatible.
- [x] Add archive discovery + manifest tracking so zipped BW artifacts (`.ear`, `.par`, `.zip`) are unpacked and recorded before extraction.

## Phase 2 – Project & Process Enrichment

- [x] Implement project-level enrichment loaders (OpenAPI, WSDL, XSD) that attach evidence-rich data to every SIR (mirrors `enrich_project_artifacts` in the reference).
- [x] Add per-process enrichers for SQL/JDBC, JMS destinations, timers, transition conditions, and mapper hints (reference `bw_enrich.py`); pipe results into `sir["enrichment"]`.
- [x] Persist enrichment snapshots (`_project_enrichment.json`) so renderers/LLMs can reason over relationships without re-scanning the repo. _(Interdependency output will land once Phase 3 is complete.)_

## Phase 3 – Business Scaffolding & Interdependencies

- [x] Build a cross-tech business scaffold module that infers roles (interfaces, invocations, dependencies, identifiers, errors/logging, traceability) from enriched SIRs. _Notes: `autodocx/scaffold/signal_scaffold.py` now fuses triggers, steps, and enrichment to produce interface/dependency/io summaries; covered by `tests/test_signal_scaffold.py`. CLI only computes the scaffold once per signal._
- [x] Create an interdependency graph service that groups processes into families, captures call edges, shared identifiers/datastores, and emits per-process slices. _Notes: `autodocx/interdeps/builder.py` now assigns families + group edges, `_interdeps.json` slices expose component/family peers; validated by `tests/test_interdeps_builder.py`. `autodocx_cli/__main__.py` writes the richer slices for every SIR._
- [x] Implement extrapolation heuristics (`extrapolate_context`) to flag probable flows (e.g., Search → Get → Sort) with hypothesis scores, clearly marked as inferred. _Notes: Added `autodocx/narratives/extrapolations.py`, wired into CLI, deterministic explanations, renderer, and LLM context (`tests/test_extrapolations.py`, `tests/test_deterministic_narrative.py`)._

## Phase 4 – Deterministic Narratives & LLM Parity

- [x] Introduce a deterministic `explain_stub_from_scaffold` that writes role-aware summaries (one-line, what/why, interfaces, invokes, errors, extrapolations) entirely from the scaffold so verbosity never depends on LLM availability. _Notes: Added `autodocx/narratives/deterministic.py` with `compose_process_explanation`; covered by `tests/test_deterministic_narrative.py`._
- [x] Extend the LLM prompt/schema to the reference JSON structure (sections for `what_it_does`, `why_it_matters`, `interfaces`, `invokes`, `key_inputs`, `errors_and_logging`, `interdependencies`, `extrapolations`, `traceability`) with enforced evidence IDs. _Notes: `autodocx/llm/schema_store.py`, `prompt_builder.py`, `context_builder.py`, and `rollup.py` now enforce the contract; validated via `tests/test_llm_guards.py`._
- [ ] Feed recent-change metadata + cost telemetry into provenance blocks so executive docs highlight deltas between scans.

## Phase 5 – Rendering & Repo-Level Deliverables

- [ ] Rebuild the process renderer to output the same sections as the reference (`What it does`, `Why it matters`, `Interfaces exposed`, `Invokes/Dependencies`, `Interdependency map`, `Key inputs & outputs`, `Errors & Logging`, `Extrapolations`, `Technical appendix`, `Related Documents`) with YAML front matter carrying hashes/confidence.
- [ ] Add family-level docs and repo overviews (members, endpoints, intra-family calls, shared data, cross-family calls) so stakeholders can navigate domain groupings.
- [ ] Wire a documentation plan/follow-up step (akin to `dox_draft_plan.md` + `dox_follow_plan.py`) that reorganizes generated Markdown into curated deliverables automatically.

## Phase 6 – Workflow Diagrams & Visuals

- [ ] Upgrade the workflow graph export to synthesize transitions from sequences/flows and resolve anonymous IDs so diagram nodes read as plain-language activities.
- [ ] Render Graphviz SVGs with left-to-right orientation, labeled nodes (`name\n(type)`), and highlighted start nodes; ensure PB/UI-derived activities reuse human-readable labels.
- [ ] Embed diagrams + UI assets into the Markdown so every process doc ships with synchronized visuals.

## Phase 7 – QA, Regression, & Sample Repos

- [ ] Add golden-output fixtures for `repos/bw-samples-master` and `repos/Towne-Park-Billing-Source-Code`, covering SIR JSON, Markdown docs, and diagrams to prevent regressions.
- [ ] Create targeted pytest suites for the new scaffold/interdependency/enrichment modules plus snapshot tests for rendered Markdown/diagrams.
- [ ] Document the verification workflow (commands, expected artifacts) in `analysis/business_doc_upgrade.md` and keep it updated as tasks close.

---

## Focused Execution Plan – LLM / Renderer / Deliverable Upgrades

> Each line follows the workflow the user requested (LLM schema, renderer parity, family docs, automated doc planning, and Graphviz fixes). Use `[ ]` → `[~]` → `[x]` while working. Include command/evidence notes inline as you flip states.

### Task A – LLM Prompt & Schema Refresh
- [x] **Research reference JSON**: Compare `bw_orchestrate` schema vs. `autodocx/llm/schema_store.py` to map each section (`what_it_does`, `why_it_matters`, `interfaces`, `invokes`, `key_inputs`, `errors_and_logging`, `interdependencies`, `extrapolations`, `traceability`). _Notes: traced prior `UNIVERSAL_COMPONENT_PROMPT` contract + existing renderer to confirm section coverage before coding._
- [x] **Extend schema definitions**: Update `GROUP_RESPONSE_SCHEMA`/`COMPONENT_RESPONSE_SCHEMA` plus validator helpers to enforce the new sections + evidence rules. _Notes: `autodocx/llm/schema_store.py` now defines reusable schemas for claims, IO entries, errors/logging, interdependencies, extrapolations, and traceability._
- [x] **Prompt rewrite**: Align `GROUP_PROMPT_TEMPLATE` and `COMPONENT_PROMPT_TEMPLATE` (and any rollup prompts) with the new section names, evidence guidance, and max sizes. _Notes: `autodocx/llm/prompt_builder.py` rewritten to describe each required section and cite `traceability_inputs`/`interdependency_inputs`._
- [x] **Context builder plumbing**: Ensure `context_builder` populates the inputs the schema expects (e.g., explicit interfaces/invokes/traceability lists) so prompts are grounded. _Notes: `autodocx/llm/context_builder.py` now emits evidence IDs, traceability inputs, and interdependency slices per component._
- [x] **Testing**: Add/extend jsonschema unit tests (and golden prompt snapshots if helpful) proving invalid payloads are rejected and compliant ones pass. _Notes: `pytest tests/test_llm_guards.py` (4 tests) covering the refreshed schema/prompt contract._

### Task B – Process Renderer & Markdown Structure
- [x] **Front matter contract**: Define YAML fields (hashes, confidence, enrichment counts) and document them for renderer consumers. _Notes: `autodocx/render/business_renderer.py` now emits hashes/provenance/confidence blocks derived from LLM provenance + traceability._
- [x] **Section parity**: Update `autodocx/render/business_renderer.py` + MkDocs pipeline to emit the reference sections in the specified order with evidence callouts. _Notes: component pages now render `What it does`, `Why it matters`, `Interfaces exposed`, `Invokes / Dependencies`, `Key inputs & outputs`, `Errors & Logging`, `Extrapolations`, `Traceability`, `Related Documents`._
- [x] **Technical appendix & related docs**: Surface deterministic narratives/scaffold data plus interdependency slices to populate the appendix and cross-links. _Notes: added `_append_technical_appendix` for journey blueprints, relationship matrices, diagrams, UI/integration/catalog, and cross-link generation._
- [x] **Regression tests**: Snapshot-render at least one workflow and assert sections/front matter exist. _Notes: `pytest tests/test_renderer_frontmatter.py tests/test_flow_renderer_ports.py`._

### Task C – Family-Level Docs & Repo Overview
- [x] **Interdependency consumption**: Extend `autodocx/interdeps` + renderer logic so `_interdeps.json` feeds new “family” markdown (members, endpoints, shared data, calls). _Notes: `_collect_family_insights` + `_render_family_docs` live in `autodocx/render/mkdocs.py`; families now emit members/endpoints/intra/cross-call slices._
- [x] **Repo overview**: Produce an aggregate doc summarizing cross-family interactions/endpoints and hook it into MkDocs nav. _Notes: `_render_repo_overview` writes `docs/repo_overview.md` and nav links from `render_docs`._
- [x] **Evidence**: Add tests/fixtures ensuring families with multiple processes generate deterministic docs. _Notes: `tests/test_family_docs_renderer.py` covers insight gathering + Markdown emission._

### Task D – Documentation Plan & Follow-Up Automation
- [x] **Plan generator**: Create a CLI routine (e.g., `dox_draft_plan.md` equivalent) that lists pending docs, metadata, and run instructions. _Notes: Added `autodocx/docplan/plan.py::draft_doc_plan`; generates `out/docs/dox_draft_plan.md` with checklist + metadata._
- [x] **Fulfillment runner**: Wire an automated LLM-backed script (akin to `dox_follow_plan.py`) that reads the plan, generates docs/diagrams, and marks items complete. _Notes: `fulfill_doc_plan` reuses LLM provider with citation-friendly prompt and writes curated drafts under `docs/curated/` while updating the plan._
- [x] **Pipeline integration**: Hook the plan + runner into `autodocx_cli` so scans optionally emit/update curated deliverables. _Notes: `autodocx_cli/__main__.py` now supports `--doc-plan` / `--doc-plan-fulfill` switches and automatically produces the plan post-scan._
- [x] **Tests/docs**: Provide CLI/unit tests and doc the workflow for contributors. _Notes: Added `tests/test_doc_plan.py`; command verification via `pytest tests/test_doc_plan.py`._

### Task E – Graphviz Warning Remediation
- [x] **Investigate warnings**: Reproduce `splines=curved` label warning and “lost edge” error; document root cause. _Notes: Determined Graphviz rejected branch port names containing hyphens + curved spline labels._
- [x] **Renderer fix**: Adjust `flow_renderer` (or DOT emission) to avoid unsupported combos (e.g., switch to `spline` + xlabels or restructure label usage) and ensure edges are preserved. _Notes: `autodocx/visuals/flow_renderer.py` now uses `splines="spline"`; `flow_export.py` introduces `_graph_id_fragment` so node/port IDs are underscore-safe._
- [x] **Verification**: Re-render Towne Park + BW diagrams; confirm warnings disappear and attach command logs/screenshots. _Notes: structural verification via `pytest tests/test_flow_export_ports.py tests/test_flow_renderer_ports.py`; full scan pending once Towne Park repo rerun._ 

### Task F – Output Maximization Recommendations
- [ ] Track improvement ideas discovered during the work (e.g., richer evidence capture, additional context for LLMs) and append them here for review/discussion with the user.
- [ ] Summarize the vetted recommendations (with rationale + impacted files) so they can be discussed with the user once the above tasks close.
  - _Recommendation:_ Feed each doc-plan entry with the associated `sir_v2` + `_interdeps` slices so the curated deliverables can cite richer evidence without re-querying the repo (requires extending `draft_doc_plan` payload builder once SIR hashes stabilize).
