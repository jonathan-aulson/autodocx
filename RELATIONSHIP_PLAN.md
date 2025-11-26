# Relationship Enrichment Plan

This plan drives the work required to expose and exploit deeper relationships in Towne Park repositories so the LLM can produce UX and architecture narratives. Treat each workstream as an incremental deliverable, and update the checklist and status log as you make progress.

## Progress Marking

- Use `[ ]` (not started), `[~]` (in progress), and `[x]` (complete) for every checkbox under each workstream. A workstream should only be marked `[x]` after every checkbox inside it is marked `[x]`.
- The moment you flip a checkbox, append an entry to the **Status Log** with the date, workstream, change summary, and evidence (file path, command, PR, or artifact). Prefer permanent artifacts under `analysis/` or `out/`.
- Record measurement scripts and data samples in `analysis/<workstream>.md` files so reruns are easy to audit.
- When verification requires CLI runs, capture the exact command (e.g., `autodocx scan ...`) and a short excerpt of the output and reference it in the log entry.

---

## Workstreams

### Workstream 1 - Baseline Relationship Coverage (Complete)
**Objective:** Quantify the current absence of relationship metadata to measure lift from future work.

- [x] Run the `out/sir/*.json` scan (Python snippet recorded in `analysis/relationships_baseline.md`) to confirm `workflow` SIRs lack `relationships`.
- [x] Summarize findings, sample SIRs, and connector counts in `analysis/relationships_baseline.md`.
- [x] Update this plan and the status log with the completed baseline step, linking to the analysis file.

### Workstream 2 - Logic Apps / Power Automate Relationship Emission
**Objective:** Emit structured relationship data (triggers -> actions, HTTP calls, Dataverse/SharePoint/SQL connectors, child flows) from `LogicAppsWDLExtractor` so workflows describe their dependencies.

- [x] Capture current Logic Apps samples (3-5 SIRs under `out/sir/`) and document the missing connectors/relationships in `analysis/logicapps_relationships.md`.
- [x] Define the `relationships` schema (fields like `source_step`, `target_ref`, `relationship_type`, `connector`, `evidence`) and store the contract in the analysis doc plus inline comments in `autodocx/extractors/logicapps.py`.
- [x] Update `LogicAppsWDLExtractor._emit_from_definition` to emit relationships for triggers, HTTP actions (`url_or_path`), Dataverse/SharePoint/SQL connectors, and child workflow invocations; include role hints so `graph/builder.py` can add edges.
- [x] Add extractor unit tests/fixtures under `tests/logicapps/` verifying new relationship payloads, and regenerate representative SIRs/artifacts (commit them under `out/sir_samples/` if needed).
- [x] Re-run `autodocx scan <Towne Park repo> --out out --debug` to produce updated SIRs/artifacts, then summarize the before/after deltas in `analysis/logicapps_relationships.md` with evidence IDs and update the status log.

### Workstream 3 - Azure Functions Extraction
**Objective:** Build a robust extractor that reads both `function.json` bindings and C# attributes to emit REST/timer/queue relationships plus downstream dependencies.

- [x] Inventory current coverage by sampling `autodocx/extractors/azure_functions.py` output (both `function.json` and `.cs` paths) and document gaps in `analysis/azure_functions_relationships.md`.
- [x] Extend the extractor to parse `FunctionName`, `HttpTrigger`, `TimerTrigger`, `QueueTrigger`, `ServiceBusTrigger`, and outbound bindings, emitting `workflow` or `route` signals with `relationships` that describe inbound triggers, auth levels, queues, storage accounts, and timer cadences.
- [x] Capture DTO/schema hints by parsing adjacent C# models or `function.json` `routeParameters`, embedding them into the signal props for downstream mapping.
- [x] Add fixtures + tests under `tests/azure_functions/` that cover attribute parsing, binding normalization, and relationship emission; ensure `pytest` passes locally.
- [x] Regenerate sample outputs, compare against baseline artifacts, and document findings + commands in the analysis file before logging the workstream progress.

### Workstream 4 - Pipeline, SQL, and Data Relationships
**Objective:** Surface CI/CD dependencies and database relationships so the system map includes deployment order, environments, and CRUD linkages.

- [x] Azure Pipelines: enhance `autodocx/extractors/azure_pipelines.py` to read `stages`, `dependsOn`, `resources`, `environments`, and `publish` steps, emitting relationships such as `job -> job`, `job -> environment`, and `job -> artifact`.
- [x] SQL/Data: expand `autodocx/extractors/sql_migrations.py` (or add a sibling extractor) to parse table definitions, foreign keys, stored procedures, and CRUD operations, emitting `db` signals plus `relationships` describing table-to-table links and pipeline-to-database touchpoints.
- [x] Provide targeted regression tests and sample fixtures for both the pipeline and SQL extractors; confirm `pytest` coverage for new data paths.
- [x] Document the combined pipeline/SQL relationship coverage (including scripts/metrics) in `analysis/pipeline_sql_relationships.md`, attach sample outputs, and update the status log once evidence is in place.

### Workstream 5 - Mapper & Renderer Integration
**Objective:** Teach the artifact mapper and Markdown renderer how to consume and display the new relationship metadata as UX flows and dependency matrices.

- [x] Extend `autodocx/artifacts/option1.py` to capture `relationships`, `relationship_matrix`, and UX flow summaries on relevant artifact types (workflows, routes, jobs, db objects).
- [x] Update `autodocx/render/business_renderer.py` and `autodocx/render/mkdocs.py` to render: (a) UX/user-flow narratives sourced from workflow relationships, and (b) technical topology sections (tables or diagrams) summarizing dependencies.
- [x] Add renderer/front-matter tests (e.g., extend `tests/test_renderer_frontmatter.py`) ensuring new sections appear only when data exists and that evidence anchors are preserved.
- [x] Capture screenshots or Markdown excerpts under `analysis/renderer_relationships.md` to prove the UX/architecture outputs include relationship data, then log completion.

### Workstream 6 - LLM Context & Prompt Updates
**Objective:** Feed the enhanced relationship data into LLM prompts and update schemas so rollups produce UX narratives plus architecture detail.

- [x] Update `autodocx/llm/context_builder.py` to pass relationship matrices and UX summaries alongside artifacts/SIRs, trimming as needed for token budgets.
- [x] Revise `autodocx/llm/prompt_builder.py` templates and `autodocx/llm/schema_store.py` schemas so the LLM is explicitly asked to produce UX narratives, dependency stories, and relationship matrices (with evidence IDs).
- [x] Refresh `autodocx/llm/rollup.py` (or helper modules) as needed to log the new fields and ensure telemetry captures any prompt size changes.
- [ ] Run `autodocx scan ... --llm-rollup` twice (before/after relationship ingestion) and archive anonymized outputs plus telemetry diffs in `analysis/llm_relationships.md`; update the status log referencing both evidence sets.

### Workstream 7 - Tree-Sitter AST & Code Entity Extraction
**Objective:** Introduce tree-sitter-powered parsing so autodocx can reason about functions, classes, handlers, and docstrings across languages.

- [x] Record design decisions (supported languages, dependency strategy, target signals) in `analysis/tree_sitter_plan.md`.
- [x] Add runtime dependencies (`tree_sitter`, `tree_sitter_languages`) plus a reusable parser helper (`autodocx/tree_sitter_support.py`) with graceful fallbacks.
- [x] Implement a `TreeSitterCodeExtractor` that emits `code_entity` signals for functions/classes/methods (Python, C#, JS/TS initial focus) with evidence anchors and docstring metadata.
- [x] Wire the extractor into entry points and ensure it coexists with existing language-specific extractors (Logic Apps, Azure Functions, etc.).
- [x] Add targeted unit tests covering Python and C# samples; ensure CI/tests run without requiring manual language builds.
- [x] Document how tree-sitter signals feed downstream mapping/LLM prompts (update `analysis/tree_sitter_plan.md` with context builder/prompt wiring notes).

### Workstream 8 - UI/Integration Detection & Business Entities
**Objective:** Capture UI routes, forms, integration touchpoints, and inferred business entities so docs speak to user journeys.

- [x] Extend extractors (React/Angular/Blazor templates, Razor views, BPMN/draw.io, CSV configs) to emit `ui_component` / `business_entity` signals with evidence.
- [x] Parse SDK usage/import graphs (HttpClient, Azure SDKs, CRM SDKs) to flag integrations, auth scopes, secrets, and external system roles.
- [x] Synthesize 'integration catalog' artifacts (who calls whom, protocol, auth, evidence) for use in docs and LLM prompts.
- [x] Add heuristics/LLM pass for identifying business terms (Invoice, Contract, Customer) and attach them to signals for glossary generation.
- [x] Infer business entities from `[Authorize(Roles=...)]` attributes and UI component names so glossary sections can highlight real users/roles.
- [x] Validate with new tests + sample repos; update documentation describing how these signals map to doc sections.

### Workstream 9 - Business Narrative Rollup & Doc Experience
**Objective:** Turn raw signals into narrative, executive-ready documentation with diagrams, glossary, and risk catalog.

- [x] Overhaul MkDocs/business renderer templates to include process narratives, integration diagrams (mermaid/plantuml), role maps, and glossary sections fed by new signals.
- [x] Update LLM rollup prompts (group + component) to force plain-English explanations of business value, supported processes, integrations, UX entry points, and operational risks.
- [x] Introduce a 'process/integration synthesis' pipeline step that feeds LLMs curated context (top flows, integrations, UI nodes) before prompting.
- [ ] Capture before/after doc samples for exec review, gather feedback, and iterate on prompt + renderer tone (track in `analysis/doc_feedback.md`).
- [ ] Add DX/support pages (onboarding, FAQs, change management) sourced from repo signals plus curated templates.

### Workstream 10 - Human Feedback Loop & Quality Gates
**Objective:** Ensure docs are validated by end users and continuously improved.

- [ ] Define acceptance criteria (business readability, evidence citations, integration completeness) and codify as automated checks or review checklist.
- [ ] Schedule pilot reviews with execs/analysts/support and capture structured feedback.
- [ ] Add regression tests/linters that fail builds if key sections are missing (e.g., no relationship highlights, missing integration catalog).
- [ ] Plan incremental rollouts (per repo) with telemetry measuring section usage/confidence.

---

## Status Log

| Date | Task | Change | Evidence |
|------|------|--------|----------|
| 2025-11-14 | 1. Baseline coverage | Completed | `analysis/relationships_baseline.md` |
| 2025-11-15 | 2. Logic Apps relationships | Captured SIR samples, documented gaps + schema draft | `analysis/logicapps_relationships.md` |
| 2025-11-15 | 2. Logic Apps relationships | Implemented extractor relationships + Option1/renderer plumbing | `autodocx/extractors/logicapps.py`, `autodocx/artifacts/option1.py`, `autodocx/render/business_renderer.py`, `autodocx/llm/context_builder.py`, `pytest tests/test_renderer_frontmatter.py` |
| 2025-11-15 | 2 / 5 / 6 Relationship updates | Added extractor tests, reran scan, captured renderer evidence, and pushed relationship data through mapper/renderer/LLM context | `tests/test_logicapps_extractor_relationships.py`, `python -m autodocx_cli scan repos/Towne-Park-Billing-Source-Code --out out --debug`, `scripts/check_logicapps_relationships.py`, `analysis/logicapps_relationships.md`, `analysis/renderer_relationships.md`, `tests/test_renderer_frontmatter.py`, `scripts/render_relationship_demo.py` |
| 2025-11-15 | 3 / 4 / 6 Extractor + prompt refresh | Extended Azure Functions/Pipelines/SQL extractors, added tests, and refreshed prompts/schemas/telemetry scaffolding | `autodocx/extractors/azure_functions.py`, `autodocx/extractors/azure_pipelines.py`, `autodocx/extractors/sql_migrations.py`, `tests/test_azure_functions_relationships.py`, `tests/test_azure_pipelines_relationships.py`, `tests/test_sql_migrations_relationships.py`, `analysis/azure_functions_relationships.md`, `analysis/pipeline_sql_relationships.md`, `autodocx/llm/prompt_builder.py`, `autodocx/llm/schema_store.py`, `autodocx/llm/rollup.py`, `python -m autodocx_cli scan repos/Towne-Park-Billing-Source-Code --out out --debug --llm-rollup` |
| 2025-11-15 | 7. Tree-Sitter AST | Added dependencies, parser helper, first code-entity extractor, and regression tests | `pyproject.toml`, `requirements.txt`, `autodocx/tree_sitter_support.py`, `autodocx/extractors/tree_sitter_code.py`, `tests/test_tree_sitter_code_extractor.py` |
| 2025-11-15 | 7 / 8 AST & UI kickoff | Documented tree-sitter plan, wired code entities into mapper/context, introduced UI + integration extractors and tests | `analysis/tree_sitter_plan.md`, `autodocx/artifacts/option1.py`, `autodocx/llm/context_builder.py`, `autodocx/extractors/ui_components.py`, `autodocx/extractors/integration_imports.py`, `tests/test_ui_components_extractor.py`, `tests/test_integration_imports_extractor.py`, `pytest tests/test_ui_components_extractor.py tests/test_integration_imports_extractor.py tests/test_tree_sitter_code_extractor.py` |
| 2025-11-15 | 8 / 9 Workflow surfacing | Added Angular template parsing, BPMN swimlane entities, integration catalog sections, Mermaid diagrams, and prompt updates | `autodocx/extractors/ui_components.py`, `autodocx/extractors/process_diagrams.py`, `autodocx/extractors/business_entities.py`, `autodocx/extractors/integration_imports.py`, `autodocx/artifacts/option1.py`, `autodocx/llm/context_builder.py`, `autodocx/render/business_renderer.py`, `autodocx/llm/persistence.py`, `autodocx/llm/prompt_builder.py`, `tests/test_ui_components_extractor.py`, `tests/test_business_entities_extractor.py`, `tests/test_process_diagrams_extractor.py`, `tests/test_renderer_frontmatter.py` |
| 2025-11-15 | 8 validation / 9 synthesis | Ran full scan to validate UI/integration extractors (`analysis/ui_integration_validation.md`) and added process/integration synthesis to LLM context/prompt flow | `analysis/ui_integration_validation.md`, `autodocx/llm/context_builder.py`, `autodocx/llm/prompt_builder.py`, `python -m autodocx_cli scan repos/Towne-Park-Billing-Source-Code --out out --debug --llm-rollup` |
| 2025-11-15 | 7 / 8 / 9 UI + prompts integration | Added Angular/BPMN detection, richer integration heuristics, new renderer sections, and updated LLM prompts to surface UI/code/integration data | `autodocx/extractors/ui_components.py`, `autodocx/extractors/process_diagrams.py`, `autodocx/extractors/integration_imports.py`, `autodocx/artifacts/option1.py`, `autodocx/llm/context_builder.py`, `autodocx/render/business_renderer.py`, `autodocx/llm/prompt_builder.py`, `tests/test_ui_components_extractor.py`, `tests/test_process_diagrams_extractor.py`, `tests/test_integration_imports_extractor.py`, `tests/test_renderer_frontmatter.py` |
| 2025-11-15 | 3 / 4 / 6 Extractor + prompt refresh | Extended Azure Functions, Pipelines, and SQL extractors, added tests, updated prompts/schemas/telemetry, and captured analysis artifacts | `autodocx/extractors/azure_functions.py`, `autodocx/extractors/azure_pipelines.py`, `autodocx/extractors/sql_migrations.py`, `tests/test_azure_functions_relationships.py`, `tests/test_azure_pipelines_relationships.py`, `tests/test_sql_migrations_relationships.py`, `analysis/azure_functions_relationships.md`, `analysis/pipeline_sql_relationships.md`, `autodocx/llm/prompt_builder.py`, `autodocx/llm/schema_store.py`, `autodocx/llm/rollup.py`, `python -m autodocx_cli scan repos/Towne-Park-Billing-Source-Code --out out --debug --llm-rollup` |
| 2025-11-15 | 8 / 9 Roles & flow diagrams | Added `[Authorize]`/component-name heuristics for business entities, surfaced `process_flows` + `integration_summary` into MkDocs diagrams, refreshed plan/docs/tests | `autodocx/extractors/business_entities.py`, `tests/test_business_entities_extractor.py`, `autodocx/llm/persistence.py`, `autodocx/render/business_renderer.py`, `tests/test_renderer_frontmatter.py`, `analysis/ui_integration_validation.md`, `analysis/tree_sitter_plan.md`, `RELATIONSHIP_PLAN.md` |
