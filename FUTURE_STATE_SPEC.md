# Future-State Documentation & Analysis Platform

This spec defines the target capabilities for an evidence-driven documentation pipeline that:
- Detects code constellations (end-to-end flows) automatically.
- Surfaces verbatim evidence (code, configs, infra) for every claim.
- Produces business-grade documentation with cited sources and natural-language SVG workflows.
- Identifies anti-patterns in logic, style, observability, and operational practices.

## Outcomes
- [ ] Repo-wide inventory of code constellations with provenance graphs.
- [ ] Evidence packets per constellation (code excerpts, configs, tests, infra, history).
- [ ] Automated anti-pattern catalog (per issue: type, severity, file:line, remediation hint).
- [ ] LLM-authored docs: executive summary, workflow narrative, interfaces, data, risks, related docs.
- [ ] Embedded SVG workflows describing holistic flows (spanning components).
- [ ] Final portfolio rollup plus per-constellation documents accessible via MkDocs or similar portal.

## Capabilities & Requirements

### A. Constellation Discovery
- [ ] Parse all supported artifacts (code, infra, workflow definitions) into normalized signals with evidence paths.
- [ ] Build a dependency graph (nodes = artifacts, edges = calls/data flows) with clustering to derive constellations.
- [ ] Track provenance (file, line range, commit) for every node/edge.

### B. Evidence Aggregation
- [ ] Bundle verbatim snippets + metadata for each constellation (code, configs, tests, docs, logs).
- [ ] Version evidence packets for reuse between runs and diff them to detect drift.

### C. Anti-Pattern Detection
- [ ] Run rule engines/linters to flag issues in logic, style, logging, error handling, security, and performance.
- [ ] Record findings with precise locations and include remediation suggestions.

### D. LLM Prompting & Output
- [ ] Feed structured payloads + curated evidence to LLM prompts while respecting token budgets.
- [ ] Require citations for every claim and enforce minimum word counts per section.
- [ ] Ask the LLM to produce Graphviz/Mermaid diagrams that merge related workflows into cohesive SVGs.

### E. Publishing & Governance
- [ ] Generate per-constellation docs, grouped summaries, and a repo-wide overview with embedded diagrams.
- [ ] Store outputs in a navigable site (MkDocs or equivalent) with audit logs and change diffs.
- [ ] Maintain configuration to switch LLM models/env vars per run.

## Current-State Assessment
- Architecture status: [ ] The current orchestrator parses BW/PB sources into per-process SIRs and builds a lightweight interdependency graph based on invocations, identifiers, and JDBC targets (`.roo/tools/bw/bw_orchestrate.py:1-813` and `:627-709`). Families are inferred from naming patterns (`:476-490`) rather than graph clustering, so true “constellation” detection across heterogeneous code paths does not exist yet.
- Evidence pipeline status: [ ] SIR artifacts (e.g., `out/sir/ribbon.RibbonBar.ue_closeAll.json`) list activity metadata and evidence pointers but do not capture verbatim code/config snippets. Downstream LLM runs operate on previously generated Markdown via `_gather_inputs_text` in `dox_follow_plan.py:226-247`, meaning the model never sees the raw code required by the future-state evidence packets.
- Anti-pattern detection status: [ ] Neither the developer onboarding guide nor the BW tooling reference any linter, static analysis rule set, or anti-pattern catalog; the pipeline focuses on extraction/enrichment/rendering (`developer_onboarding_context.md:9-210`). There are no modules that scan logic/logging conventions or emit remediation guidance.
- LLM doc generation status: [ ] `explain_llm_role_aware` simply dumps the SIR JSON into an OpenAI chat completion (`.roo/tools/bw/bw_orchestrate.py:931-955`), and the resulting Markdown (e.g., `out/docs/ribbon.ribbonbar/ribbon.RibbonBar.ue_closeAll.md`) lacks cited evidence or verbatim code. Higher-level planning in `dox_follow_plan.py:200-305` only rewrites existing docs, so the LLM never reasons over the actual source files.
- Diagram synthesis status: [ ] Graphviz diagrams are built deterministically per process (`graphviz_svg` in `.roo/tools/bw/bw_orchestrate.py:817-830`), and the later LLM-generated diagrams are derived from prose rather than code (`_make_diagram_prompt` usage in `dox_follow_plan.py:247-293`). There is no facility to merge multiple workflows into holistic SVGs before documentation is written.
- Publishing/governance status: [ ] MkDocs is configured (`mkdocs.yml:1-24`) and helper scripts exist (`.roo/tools/bw/mkdocs_bootstrap.py`), but there are no audit logs, diffing, or governance controls around plan execution, LLM models, or artifact promotion.

## Gap Analysis & Plan
- [x] **Constellation graph service** – Prototype implemented in `bw_orchestrate.py`: the interdependency graph now feeds `build_constellations`, which tags every process with a `constellation_id` and persists JSON bundles under `out/constellations/`. Follow-up: expand clustering beyond BW/PB inputs and expose metrics to downstream tooling.
- [x] **Evidence packet builder** – `build_constellation_evidence` captures verbatim source excerpts, interface metadata, and anti-pattern findings per constellation, making them available to deterministic docs and future LLM prompts. Next step: add diff/versioning and additional artifact types (tests, infra, logs).
- [x] **Anti-pattern analyzer** – New `anti_patterns.py` module runs lightweight heuristics (missing logging, missing error handling, insecure HTTP references, absent traceability) for every process; findings are embedded in SIRs and summarized in `out/constellations/anti_patterns.json`. Future work: plug in richer rule engines per language.
- [ ] **LLM orchestration overhaul** – Replace the current SIR-only prompt with a staged prompt builder that consumes constellation evidence, anti-patterns, and graph context, enforces citation tags, and requests multiple outputs: (a) per-constellation narrative, (b) grouped summaries, (c) repo-wide rollup. Add retry/validation to ensure citations resolve to actual evidence records.
- [ ] **Holistic diagram synthesis** – Before doc generation, feed the aggregated graph (multi-process, cross-component) into an LLM-assisted or deterministic DOT generator that can stitch entire user/business flows into a handful of SVGs stored under `out/assets/diagrams_constellations/`. Reference these diagrams in every related doc.
- [ ] **Publishing & governance upgrades** – Add audit metadata (who/when/which model) to plan executions, capture diffs between runs, expose configuration switches for model selection/env vars, and update MkDocs nav/build scripts to include constellation pages, anti-pattern reports, and change logs.

## Notes
- Use this document to record observations, deltas, and the evolving remediation plan.
