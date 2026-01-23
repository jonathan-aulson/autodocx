# Pipeline Refactor – ToDo

## 1. Pipeline order & analysis
- [x] Capture the current execution order for `autodocx scan ./repos/bw-samples-master --out out --llm-rollup` (extraction → rollup) and highlight why rollup runs before curated docs. _Current `run_scan` calls `group_by_component` and immediately executes `rollup_group_and_persist` inside the `if llm_rollup` block (lines ~817-903) **before** the doc-plan/fulfillment block at lines ~903-930, so rollups never see curated docs._
- [x] Define the desired bottom-up sequencing (process docs → grouped docs → component docs → associated components → repo overview → optional rollup) and document the checkpoints. _Order: (1) generate per-process docs (one per SIR) → (2) group processes into “flows” (families) and author docs referencing process docs → (3) component briefs referencing process+family outputs → (4) associated-component/portfolio docs (existing family/repo_overview tiers) → (5) final repo comprehensive doc that ingests all curated Markdown + regenerated diagrams → (6) only after curated outputs exist, optionally invoke `--llm-rollup` for additional executive summaries._

## 2. CLI workflow changes
- [x] Update `autodocx_cli.run_scan` so doc-context/plan/fulfillment run before any optional rollup work. _Moved the rollup block to the end of `run_scan` after MkDocs regeneration; rollup now consumes curated component docs instead of raw extractor payloads._
- [x] Expand `build_doc_context`/`draft_doc_plan` to include process-level entries (individual SIR-derived docs) and enforce priority ordering for fulfillment. _Context now tracks per-process metadata + expected doc slugs; doc plan priorities: process → family → component → repo overview → repo comprehensive._
- [x] Ensure the repo-wide comprehensive doc is generated only after lower-level docs are available (feed curated doc summaries back into the LLM payload). _Added `doc_type=\"repo_final\"` which sources every curated Markdown file via `_repo_final_sources`, guaranteeing it runs last._
- [x] Rework the `--llm-rollup` flag so it emits *post*-documentation aggregate artifacts using the curated docs as context instead of raw extractor payloads. _Rollup now attaches snippets from `docs/curated/components/*.md` before invoking `rollup_group_and_persist`, and only runs after the curated docs exist._

## 3. LLM-driven diagram synthesis
- [x] Design the prompt + payload for diagram synthesis (group workflows by component/association, include key transitions, limit batch size). _Implemented `DIAGRAM_PROMPT` in `autodocx/visuals/llm_flow_diagrams.py` with batching + activity/transition payloads._
- [x] Implement a generator that calls the LLM, receives Graphviz DOT (or similar), converts it to SVG, and stores outputs under `out/assets/graphs_llm/`. _`generate_llm_workflow_diagrams` now produces DOT via LLM, renders SVG through `dot`, and writes to `out/assets/diagrams_llm/{component}/...`.*
- [x] Embed the regenerated SVGs into curated docs (and MkDocs assets) so business diagrams come from LLM output instead of scripted renderings. _CLI injects the new diagram paths back into `doc_context`, so every downstream doc source list and MkDocs asset sync references the LLM-authored SVGs._

## 4. Configurable LLM model
- [x] Add an `.env`-driven override (e.g., `AUTODOCX_LLM_MODEL`) that defaults to the current YAML model but can be set to `gpt-5.1` per run. _`get_llm_settings` now honors `AUTODOCX_LLM_MODEL`; `.env` default set to `gpt-5.1`._
- [x] Update documentation + tests to cover the new env var and default behavior. _README + onboarding guide document the variable, and `test_doc_plan` updated for the new workflow._

## 5. Validation
- [x] Run `autodocx scan ./repos/bw-samples-master --out out --llm-rollup` after refactors to verify ordering, doc richness, and diagram quality.
- [x] Spot-check curated docs + SVGs to confirm the LLM minimum word floor, new flow coverage, and final repo rollup quality.
