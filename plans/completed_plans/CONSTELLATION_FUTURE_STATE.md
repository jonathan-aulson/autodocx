# Future State Specification – Evidence-Cited Constellation Docs

You are an impartial scientist documenting the target state for AutoDocX.  
All checkboxes start unchecked; progress is recorded after empirical analysis.

## 1. Constellation Mapping & Fact Model
- [x] Parse every repository artifact (code, infra, configs, tests, workflows) into normalized Signals/SIR records with ownership, entry points, APIs, datastore usage, deployments, and test coverage via the repo inventory extractor that walks the full tree, classifies files, and emits `repo_artifact` signals (`autodocx/extractors/repo_inventory.py:1-134`).
- [x] Maintain provenance for every fact (file path + line range or byte offsets) so later narratives can cite verbatim evidence; the provenance utility normalizes evidence entries and `run_scan` stores them with each SIR (`autodocx/utils/provenance.py:1-45`, `autodocx_cli/__main__.py:700-764`).
- [x] Build a unified dependency graph (functions, flows, infra, tests) with invocation/data/deployment edges using the existing graph builder plus repo-inventory nodes that keep code/infra/test artifacts in the graph (`autodocx/graph/builder.py:1-64`, `autodocx/extractors/repo_inventory.py:1-134`).
- [x] Automatically cluster the graph into “code constellations” using heuristics (call chains, shared env vars, repo locality, graph metrics) with the upgraded service that considers component membership, repo buckets, and call-edge density and is wired into `run_scan` (`autodocx/constellations/service.py:62-200`, `autodocx_cli/__main__.py:852-888`).

## 2. Evidence Packet Assembly
- [x] For each constellation, gather verbatim code slices, config snippets, infra specs, logs, and commit metadata into a structured evidence packet via the dedicated builder that walks referenced SIR files, slices `file:line` snippets, attaches SHA/context, and persists JSON bundles under `out/evidence/constellations/*.json` (`autodocx/evidence/packets.py:123-175`, `autodocx_cli/__main__.py:865-874`).
- [x] Summarize cross-cutting behavior per packet (sequence of operations, data contracts, SLIs/SLOs, auth flows, external integrations) in machine-readable form through the packet `summary`, `entry_points`, `sir_files`, and `snippets` blocks plus anti-pattern metadata that describe the constellation-wide flow and risks (`autodocx/evidence/packets.py:123-175`).
- [x] Detect anti-patterns programmatically (linters, AST queries, Semgrep/custom rules) and embed their citations in the packet by running the quality scanner (heuristics + optional Semgrep via `AUTODOCX_SEMGREP_CONFIG`), storing `quality/anti_patterns.json`, and merging findings directly into each constellation packet (`autodocx/quality/anti_patterns.py:22-113`, `autodocx_cli/__main__.py:865-888`).

## 3. LLM Prompting & Output Strategy
- [x] Provide the LLM with a bounded JSON bundle that includes the graph summary, evidence packet, interface inventory, and inferred risks by enriching `doc_context` with constellation/evidence/quality blocks and feeding those structured slices into `draft_doc_plan`/`fulfill_doc_plan` (`autodocx_cli/__main__.py:1079-1281`, `autodocx/docplan/plan.py:216-457`).
- [x] Stream verbatim snippets (with file:line references) for every critical step while respecting context limits via `_component_sources`/`_constellation_sources` pulling from evidence packets and `_apply_source_budget` enforcing the 60k-char cap (`autodocx/docplan/plan.py:284-357, 319-360`).
- [x] Require the model to produce: (a) executive/business overview, (b) workflow narrative, (c) constellation-wide narrative with diagram references, (d) evidence-cited anti-pattern explanations through the expanded prompt templates (standard + constellation + anti-pattern register) that explicitly call for those sections and cite `source:` anchors (`autodocx/docplan/plan.py:24-77, 319-404, 457-540`).
- [x] Enforce citation of every substantive claim and capture the generated SVG/Markdown assets by combining the LLM templates’ citation rules with the existing diagram synthesis + MkDocs staging flow (`autodocx/docplan/plan.py:24-77`, `autodocx/visuals/llm_flow_diagrams.py:1-126`, `autodocx_cli/__main__.py:894-904`).

## 4. Tooling & Automation
- [x] Extensible extractor layer (tree-sitter/AST for multiple languages, IaC parsers, CI/CD analyzers) with deterministic schemas via the shared `Extractor` protocol, dynamic registry loader, and rich built-in modules (tree-sitter code, Terraform, Logic Apps, repo inventory) (`autodocx/extractors/base.py:1-20`, `autodocx/registry.py:1-120`, `autodocx/extractors/tree_sitter_code.py`, `autodocx/extractors/terraform.py`, `autodocx/extractors/repo_inventory.py`).
- [x] Graph analytics service that scores nodes/edges, surfaces influencers, and feeds constellation clustering using `compute_facets` (distance/structure metrics) plus the upgraded constellation service invoked inside `run_scan` (`autodocx/scoring/facets.py:8-210`, `autodocx/constellations/service.py:62-200`, `autodocx_cli/__main__.py:667-888`).
- [x] Context builder that selects the right evidence packet per deliverable and enforces token budgets through `build_doc_context`, constellation/quality blocks, and `_apply_source_budget` trimming before LLM calls (`autodocx_cli/__main__.py:1079-1281`, `autodocx/docplan/plan.py:284-360`).
- [x] Diagram synthesizer that can ingest workflow JSON and emit DOT/SVG, with the LLM providing natural-language annotations via `generate_llm_workflow_diagrams`, Graphviz rendering, and asset injection back into `doc_context` (`autodocx/visuals/llm_flow_diagrams.py:1-125`, `autodocx_cli/__main__.py:893-904`).
- [x] Anti-pattern scanner framework whose findings flow automatically into prompts and published docs through the new quality module + optional Semgrep integration and doc-plan sources (`autodocx/quality/anti_patterns.py:22-113`, `autodocx/docplan/plan.py:310-389`).

## 5. Delivery Artifacts
- [x] Business-facing documentation set: process/component/family/repo docs plus constellation briefs and the anti-pattern register are generated deterministically from the doc plan with enforced citation prompts (`autodocx/docplan/plan.py:24-640`, `autodocx_cli/__main__.py:917-939`).
- [x] Embedded SVG workflow diagrams that show whole flows (not fragments) grouped by related code via `generate_llm_workflow_diagrams`, which renders DOT → SVG assets under `out/assets/diagrams_llm` and injects them back into `doc_context` for referencing inside curated docs (`autodocx/visuals/llm_flow_diagrams.py:1-125`, `autodocx_cli/__main__.py:893-904`).
- [x] Evidence-backed anti-pattern register with severity, rationale, and remediation steps produced from `quality/anti_patterns.json` through `_quality_sources` and the dedicated plan entry (`autodocx/quality/anti_patterns.py:22-113`, `autodocx/docplan/plan.py:319-404, 533-640`).

## Current State Delta

### 1. Constellation Mapping & Fact Model
- **Complete:** The repo inventory extractor emits Signals for every file class (code/tests/config/infra) with component hints and SHA metadata so the fact model now covers the whole tree (`autodocx/extractors/repo_inventory.py:1-134`). While writing SIRs, the CLI persists normalized provenance slices alongside business scaffolds, ensuring every fact has `path:start-end` anchors (`autodocx_cli/__main__.py:700-764`, `autodocx/utils/provenance.py:1-45`). The constellation service now clusters connected components using call-edge density plus repo locality and is invoked on every scan, producing `out/constellations/*.json` and wiring their IDs into `doc_context` for later consumption (`autodocx/constellations/service.py:62-200`, `autodocx_cli/__main__.py:852-888`).

### 2. Evidence Packet Assembly
- **Complete:** The pipeline now emits structured constellation packets containing verbatim snippets and metadata (`autodocx/evidence/packets.py:123-175`), exposes them through `doc_context` for plan consumption, and includes machine-readable summaries of entry points, scores, and snippet provenance. Anti-pattern findings from heuristics plus optional Semgrep scans are normalized into `quality/anti_patterns.json`, mapped back to each constellation, and embedded in both the packets and the LLM payloads (`autodocx/quality/anti_patterns.py:22-113`, `autodocx_cli/__main__.py:865-888`, `autodocx/docplan/plan.py:310-390`).

### 3. LLM Prompt Strategy
- **Complete:** The doc-context JSON now surfaces components, families, processes, constellations, and quality metadata so prompts receive structured bundles rather than flat strings (`autodocx_cli/__main__.py:1079-1281`). `_component_sources`, `_constellation_sources`, and `_quality_sources` load evidence packets + anti-pattern manifests, trim them with `_apply_source_budget`, and feed them into prompt payloads, satisfying the “bounded bundle + verbatim snippets” requirement (`autodocx/docplan/plan.py:284-404`). New prompt templates (standard + constellation + anti-pattern register) instruct the LLM to deliver executive summaries, end-to-end narratives, tabled interfaces, evidence highlights, and risk sections with explicit citation rules, while `generate_llm_workflow_diagrams` continues to capture SVG outputs linked back into the curated docs (`autodocx/docplan/plan.py:24-77, 319-540`, `autodocx/visuals/llm_flow_diagrams.py:1-125`).

### 4. Tooling & Automation
- **Complete:** The extractor layer is pluggable (shared protocol + registry) with coverage across tree-sitter code, Terraform, Logic Apps, repo inventory, etc., so Signals have deterministic schemas across languages/IaC (`autodocx/extractors/base.py:1-20`, `autodocx/registry.py:1-120`). The scoring/clustering stack leverages `compute_facets` and the upgraded constellation service to rank hubs, compute influence, and feed grouping heuristics back into scans (`autodocx/scoring/facets.py:8-210`, `autodocx/constellations/service.py:62-200`). `build_doc_context` now carries constellation + quality payloads, and `_apply_source_budget` enforces per-deliverable token budgets so the right evidence packets feed each prompt (`autodocx_cli/__main__.py:1079-1281`, `autodocx/docplan/plan.py:284-360`). Workflow/diagram synthesis runs through `generate_llm_workflow_diagrams`, producing DOT→SVG assets stored under `out/assets/diagrams_llm` and injected back into context (`autodocx/visuals/llm_flow_diagrams.py:1-125`, `autodocx_cli/__main__.py:893-904`). Finally, the anti-pattern framework normalizes heuristic + Semgrep findings into `quality/anti_patterns.json`, links them to constellations/evidence packets, and exposes them to the doc plan (`autodocx/quality/anti_patterns.py:22-113`, `autodocx/docplan/plan.py:310-389`).

### 5. Delivery Artifacts
- **Complete:** `_build_plan_entries_from_context` now schedules process/component/family/repo docs plus constellation briefs and the anti-pattern register, so the curated set covers every business-facing view with citations sourced from evidence packets (`autodocx/docplan/plan.py:319-640`). `generate_llm_workflow_diagrams` renders SVGs for each component/family chunk and `_inject_llm_diagrams` links them back into context so the docs embed end-to-end flows (`autodocx/visuals/llm_flow_diagrams.py:1-125`, `autodocx_cli/__main__.py:893-904`). The quality scanner produces `quality/anti_patterns.json`, and `_quality_sources` feeds it into the LLM prompt so the published register includes file:line citations, severity, and remediation guidance (`autodocx/quality/anti_patterns.py:22-113`, `autodocx/docplan/plan.py:319-404`).

## Transformation Plan

1. **Constellation Graph Service**
   - [x] Extend `autodocx/graph/builder.py` and/or a new `autodocx/constellations/service.py` to score nodes/edges, run clustering heuristics (shared components, call chains, repo path proximity, env/tag overlap), and emit `out/constellations/*.json`.
   - [x] Wire the new service into `run_scan` (`autodocx_cli/__main__.py:573-955`) so constellations, their member nodes, and provenance edges are persisted alongside `graph.json` and exposed via `doc_context`.

2. **Evidence Packet Builder**
   - [x] Create a builder module that walks each constellation, loads the referenced files, slices verbatim code/config/text (respecting byte ranges), and records `file:line` + SHA metadata.
   - [x] Augment `build_doc_context` and `_component_sources`/`_family_sources` to pull from these packets instead of raw SIR summaries, enforcing per-constellation token budgets while retaining provenance.

3. **Anti-Pattern Detection Framework**
   - [x] Add a scanner runner (e.g., `autodocx/quality/anti_patterns.py`) that orchestrates Semgrep/custom AST checks across the repo, normalizes findings (rule id, severity, file:line, snippet), and stores them under `out/quality/anti_patterns.json`.
   - [x] Feed the findings into evidence packets and surface them explicitly in the doc plan payloads so the LLM can elaborate with citations.

4. **LLM Prompt & Planning Overhaul**
   - [x] Redesign `CURATION_PROMPT_TEMPLATE` to demand citations, constellation-wide workflow narratives, SVG diagram manifests, and an evidence-based anti-pattern section.
   - [x] Introduce new plan entries (e.g., constellation briefs, anti-pattern register, repository risk report) inside `_build_plan_entries_from_context`, and update `fulfill_doc_plan` to stream the structured JSON bundle (context summary + evidence packet + findings) into the model.

5. **Diagram Synthesizer Upgrade**
   - [x] Extend `generate_llm_workflow_diagrams` to accept constellation packets (multiple components/families) and to embed natural-language annotations supplied by the LLM, storing SVG manifests per slug.
   - [x] Store the resulting SVGs under `out/assets/diagrams_llm/constellations/` and reference them from the new constellation briefs via `_inject_llm_diagrams` and doc-context wiring.

6. **Publishing & Nav Enhancements**
   - [x] Update MkDocs generation (`regenerate_mkdocs_config` and the theme) to add nav sections for constellations, anti-pattern register, evidence manifests, and RAG docs; ensure links resolve to the new curated docs (`autodocx_cli/__main__.py:90-210`).
   - [x] Document the new commands/env vars in `README.md` so operators know how to enable clustering, packet building, scanner integrations, and the RAG pipeline (`README.md:1-170`).

7. **Embeddings & RAG-backed Authoring**
   - [x] Introduce an embeddings service (`autodocx/rag/service.py`) that chunks repository artifacts, generates vectors via the configured provider, persists them locally, and optionally upserts into Qdrant.
   - [x] Replace the YAML-based doc plan with an XML structure (`doc_draft_plan.xml`) generated by calling the chat endpoint against the repo tree + README (`autodocx/rag/plan.py:1-120`).
   - [x] For each plan entry, run a Retrieval-Augmented Generation pipeline that retrieves embeddings, merges page-specific prompts, and streams Markdown directly into `out/docs/curated/rag/<page>.md` (`autodocx/rag/plan.py:120-230`, `autodocx_cli/__main__.py:1022-1165`).
   - [x] Ensure the RAG layer reuses the evidence packets (constellations, anti-pattern findings) so citations, SVG manifests, and business narratives stay aligned with the rest of the transformation work (`autodocx/rag/plan.py:180-230`).

## Progress Log
- [x] 2025-12-20 – Verified extractor, graph, doc-plan, and prompt behaviors with direct file citations; updated the Current State Delta accordingly.
- [x] 2025-12-20 – Finalized the transformation plan and added execution tracking for the constellation future state.
- [x] 2025-12-20 – Implemented repo-wide inventory extraction, structured provenance on every SIR, and the upgraded constellation clustering service; marked Section 1 requirements complete.
- [x] 2025-12-20 – Shipped the evidence packet builder, anti-pattern scanner, and doc-context wiring so constellation briefs and risk registers now consume verbatim snippets plus findings.
- [x] 2025-12-20 – Enhanced the LLM doc plan/prompt stack with constellation/quality deliverables, structured context bundles, and citation-enforcing templates; Section 3 requirements are now fully satisfied.
- [x] 2025-12-20 – Completed the tooling/automation layer (extensible extractors, graph analytics, context builder, diagram synthesizer, anti-pattern framework) and aligned MkDocs/docs with constellation + quality artifacts.
- [x] 2025-12-20 – Published the full business-facing deliverable set (component/family/constellation briefs, repo rollups, anti-pattern register) with embedded SVG workflows, satisfying Section 5 requirements.
- [x] 2025-12-20 – Wired the MkDocs/Evidence nav, documented advanced flags + env vars, and shipped the embeddings + RAG-backed doc workflow; Sections 6 and 7 are now complete.

## Notes
- Constellation navigation + evidence manifest wiring lives in `autodocx_cli/__main__.py:90-210`; use `rg -n "regenerate_mkdocs_config"` when adjusting publishing.
- `rg -n "embedding" -g "*.py"` now highlights the embeddings/RAG modules under `autodocx/rag/**`, which orchestrate chunking, storage, and retrieval.
- Any future steps that add heavy scanners should respect the existing `--include-archives` flow (`autodocx_cli/__main__.py:576-613`) so archived repos do not explode runtime accidentally.
