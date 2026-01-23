# AutodocX Salvage Workstream Plan

This document tracks the remediation of the issues identified in the AutodocX pipeline.
Progress is recorded with GitHub-style checkboxes. When you begin work on an item, change the
checkbox from `[ ]` to `[~]` to indicate “in progress”. When the item is fully validated, change it
to `[x]`. Each workstream lists explicit hypotheses to validate before implementing changes, the
implementation steps, and the post-change validation gates. Update the **Status Log** at the end of
the document whenever a checkbox state changes so that progress is easy to audit.

## How to update this plan

1. Before making a change, read the workstream entry and explicitly note the hypothesis you are
   validating.
2. Record evidence for the **Pre-Change Validation** step (console output, file snippets, etc.).
3. Execute the implementation steps.
4. Run every item in **Post-Change Validation**. Capture outputs that prove success.
5. Update the checkbox and append a brief note to the **Status Log**. Include links/paths to
   validation artifacts.

---

## Towne Park Extractor Enhancement Plan

1. **Establish Coverage Baseline**
   - Run `python -m autodocx_cli stats --out out` to record current component, SIR, and connector counts for the Towne Park repositories.
   - Inspect representative SIRs (`out/sir/*.json`) and artifacts to document missing metadata (for example, Logic Apps steps without connectors). Capture the findings in `analysis/baseline_extractor_fidelity.md`.

2. **Deepen Logic Apps / Power Automate Extraction**
   - Use the Workflow Definition Language schema reference (Microsoft Learn, updated 2025-10-06) to map triggers, actions, `connectionReferences`, loops, and nested structures.
   - Enhancements:
     - Resolve connectors via `connectionReferences` so each action carries the logical API name.
     - Preserve step metadata (HTTP method or URI, expressions, loop scope, nested conditionals).
     - Extract trigger schedules and request schemas with explicit frequency and interval values.
     - Capture downstream dependencies (Dataverse tables, HTTP endpoints) as structured evidence anchors.
     - Record evidence for each step to support Markdown rendering.
   - Validation: regenerate the sample scan, confirm workflow SIRs/artifacts include enriched fields, and extend unit tests to cover nested actions and connector resolution.

3. **Implement Azure Functions (.NET) Extractor**
   - Reference the Azure Functions C# class library guidance (Microsoft Learn) for `[FunctionName]`, `[HttpTrigger]`, `[TimerTrigger]`, and other binding attributes.
   - Extractor goals:
     - Identify functions and triggers, recording HTTP methods, routes, authorization levels, cron expressions, queue/topic names, and binding directions.
     - Associate functions with project structure to populate component identifiers.
     - Choose a parsing approach (regex vs. Roslyn) and document trade-offs.
   - Validation: add curated C# fixtures to `tests/`, ensure SIRs capture trigger metadata, and confirm artifacts summarize routes and bindings.

4. **Enhance Pipeline and Deployment Extractors**
   - Review Towne Park Azure Pipelines YAML to surface stages, environments, service connections, and artifacts.
   - Emit stage and environment names plus dependencies; expand tests to cover multi-stage pipelines.

5. **Extend SQL/Data Extractors**
   - Parse schema details from migrations (tables, constraints, stored procedures) and normalize schema names.
   - Include column metadata, data types, and evidence snippets; add tests covering diverse SQL statements.

6. **Mapper & Renderer Integration**
   - Ensure new metadata is consumed in `autodocx/artifacts/option1.py` (workflow connectors, Azure Function routes, schedules).
   - Update the Markdown renderer to display connectors, trigger details, and binding information.

7. **Operational Validation**
   - After each enhancement run `python -m pytest`, regenerate scan outputs, and execute `python -m autodocx_cli stats --out out` (with and without `--json`) to verify improvements.
   - Log each milestone in `analysis/postchange_*` and update the Status Log.

---

## Workstreams

### 1. Stabilize Data Model & Component Grouping
- [x] **Outcome:** Signals, SIRs, and artifacts consistently include `component_id` so that grouping
  produces meaningful component pages.
- **Hypothesis to validate:** Missing/empty component identifiers are the root cause of all SIRs
  landing in the `ungrouped` bucket.
- **Pre-Change Validation:**  
  - [x] Run `python -m scripts.inspect_sirs` (to be created if absent) or manually inspect
    `out/sir/*.json` to confirm `component_or_service` is empty for majority of SIRs.  
  - [x] Capture summary statistics (count per component) to establish the baseline.
- **Implementation Steps:**  
  1. Introduce `component_id`/`service_id` normalization helper (e.g., in `autodocx.utils`).
  2. Update each extractor to populate the helper using repo-relative paths + domain hints.  
  3. Ensure `autodocx_cli.__main__` writes the normalized component on SIRs and artifacts.  
  4. Add aggregation layer that merges SIRs + artifacts per component before rendering.
- **Post-Change Validation:**  
  - [x] Re-run scan against sample repo; compute distribution of SIRs per component (expect multiple
    groups).  
  - [x] Confirm `out/docs/index.md` lists component names instead of a single `ungrouped` entry.  
  - [x] Run unit tests (existing + new ones for helper). Attach results to Status Log.

### 2. Repair & Extend Graph Construction
- [x] **Outcome:** Graph includes workflows, APIs, data stores, and docs with correct edges so
  distance features yield finite values.
- **Hypothesis to validate:** Joiners running inside the extraction loop and missing edge types are
  why distance metrics produce `Infinity`.
- **Pre-Change Validation:**  
  - [x] Inspect `out/sir/*.json` for `graph_features.nearest_marker_distance == Infinity`.  
  - [x] Use a small script to compute graph connectivity (number of weakly connected components).
- **Implementation Steps:**  
  1. Move joiner invocation outside the signal loop; ensure it runs once after node/edge assembly.  
  2. Add edges to connect workflows↔operations, operations↔datastores, docs↔components where
     applicable.  
  3. Guard distance-feature invocation to skip when graph is too sparse.  
  4. Add unit test covering graph builder joiner order.
- **Post-Change Validation:**  
  - [x] Re-run scan; verify graph metrics report finite distances.  
  - [x] Confirm new edges appear in `out/graph.json`.  
  - [x] Run tests for graph builder and distance features.

### 3. Rework Business Renderer Templates
- [x] **Outcome:** Component and SIR pages render evidence-driven content without static emoji or
  boilerplate.
- **Hypothesis to validate:** Static templates with hard-coded sections are overwhelming the scarce
  evidence, leading to poor documentation quality.
- **Pre-Change Validation:**  
  - [x] Review current Markdown output to catalogue empty/boilerplate sections.  
  - [x] Note specific non-ASCII characters causing encoding issues.
- **Implementation Steps:**  
  1. Replace string-built template with Jinja or structured helpers that only emit sections with
     supporting data.  
  2. Remove non-ASCII headers; ensure content is ASCII by default.  
  3. Surface key evidence lists (triggers, connectors, dependencies) from aggregated data.
- **Post-Change Validation:**  
  - [x] Regenerate docs; inspect representative component pages (include before/after diff).  
  - [x] Run renderer unit tests (update expectations as needed).  
  - [x] Confirm Markdown passes `markdownlint`/basic lint if available.

### 4. Simplify LLM Rollup Pipeline
- [x] **Outcome:** LLM rollup runs only when configured, with manageable context and verifiable
  schema enforcement.
- **Hypothesis to validate:** The current all-in-one rollup fails due to malformed grouping and
  oversized prompts.
- **Pre-Change Validation:**  
  - [x] Trigger rollup with `--llm-rollup` (without API key) to document current failure path.  
  - [x] Review existing telemetry to understand call volumes.
- **Implementation Steps:**  
  1. [x] Split `autodocx/llm/rollup.py` into context builder, prompt composer, schema validator, and
     persistence modules.  
  2. [x] Add guard that skips LLM execution if prerequisites (API key, sane context) are missing.  
  3. [x] Write unit tests for schema validation and guard rails.  
  4. [x] Document configuration expectations in repo README/plan.
- **Post-Change Validation:**  
  - [x] Run rollup with mock/stub provider to ensure pipeline completes.  
  - [x] Verify telemetry CSV records new runs accurately.  
  - [x] Run new unit tests.

### 5. Improve Extractor Fidelity
- [x] **Outcome:** Major extractors emit richer metadata (component IDs, connectors, environments,
  target URIs) that downstream stages can use.
- **Hypothesis to validate:** Current extractors lack sufficient detail, causing sparse artifacts and
  poor renderer output.
- **Pre-Change Validation:**  
  - [x] Sample existing extractor outputs (Logic Apps, TIBCO BW, pipelines, SQL) to log missing
    fields.  
  - [x] Capture at least one failing example per extractor.
- **Implementation Steps:**  
  1. [x] Update each extractor to call the new component helper and include additional contextual
     metadata (Logic Apps connectors now enriched).  
  2. [x] Add targeted fixtures in `tests/` for new extractor behaviour.  
  3. [x] Ensure artifact mapper consumes the enriched props where relevant.
- **Post-Change Validation:**  
  - [x] Re-run scans; confirm enriched fields appear in SIRs/artifacts.  
  - [x] Run extractor-specific tests.  
  - [x] Update plan Status Log with evidence paths.

### 6. Add Verification & Inspection Tooling
- [ ] **Outcome:** Developers can quickly inspect grouping, component stats, and evidence coverage
  without opening generated docs.
- **Hypothesis to validate:** Lack of quick diagnostics causes regressions to go unnoticed until
  after doc generation.
- **Pre-Change Validation:**  
  - [x] Note current manual steps required to inspect component distribution.  
  - [x] Identify data points most valuable to expose (component counts, orphan signals, evidence
    counts).
- **Implementation Steps:**  
  1. [x] Implement CLI subcommand (e.g., `autodocx stats`) that summarizes scan output.  
  2. [x] Include options to dump JSON or table reports.  
  3. [x] Write tests covering core metrics.
- **Post-Change Validation:**  
  - [x] Run the new command after a scan; capture output for Status Log.  
  - [x] Confirm tests pass.

### 7. Polish Output & Asset Handling
- [ ] **Outcome:** Generated docs embed diagrams only when available, include clear evidence links,
  and avoid stale assets.
- **Hypothesis to validate:** Diagrams currently referenced but missing cause broken images and hurt
  trust in the docs.
- **Pre-Change Validation:**  
  - [ ] Identify broken image references in current docs/site.  
  - [ ] Review `out/assets/graphs` folder for stale files.
- **Implementation Steps:**  
  1. Update renderer to check file existence before embedding diagrams.  
  2. Enhance cleanup routine to prune unused assets safely.  
  3. Add smoke test that renders a minimal component with/without diagrams.
- **Post-Change Validation:**  
  - [ ] Regenerate docs and verify that all embedded assets resolve.  
  - [ ] Run smoke test and document results.

---

## Status Log

| Date | Item | Change | Evidence |
|------|------|--------|----------|
| 2025-11-13 | Workstream 1 | `In progress` | `analysis/baseline_component_grouping.md` |
| 2025-11-13 | Workstream 1 | `Completed` | `analysis/postchange_component_grouping.md`, `analysis/tests_2025-11-13.md` |
| 2025-11-13 | Workstream 2 | `In progress` | `analysis/baseline_graph_health.md` |
| 2025-11-13 | Workstream 2 | `Completed` | `analysis/postchange_graph_health.md`, `analysis/tests_2025-11-13.md` |
| 2025-11-13 | Workstream 3 | `In progress` | `analysis/baseline_renderer_output.md` |
| 2025-11-13 | Workstream 3 | `Completed` | `analysis/postchange_renderer_output.md`, `analysis/tests_2025-11-13.md` |
| 2025-11-13 | Workstream 4 | `In progress` | `analysis/baseline_llm_rollup.md` |
| 2025-11-13 | Workstream 4 | `Completed` | `analysis/postchange_llm_rollup.md`, `analysis/tests_2025-11-13.md` |
| 2025-11-13 | Workstream 5 | `In progress` | `analysis/baseline_extractor_fidelity.md` |
| 2025-11-13 | Workstream 5 | `Completed` | `analysis/postchange_extractor_fidelity.md`, `analysis/tests_2025-11-13.md` |
| 2025-11-13 | Workstream 6 | `In progress` | `analysis/baseline_verification_tooling.md` |
| 2025-11-13 | Workstream 6 | `Completed` | `analysis/postchange_verification_tooling.md`, `analysis/tests_2025-11-13.md` |
