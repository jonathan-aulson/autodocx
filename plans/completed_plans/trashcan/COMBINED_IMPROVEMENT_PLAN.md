# Combined Improvement Plan
Merged from UNFINISHED_CONSOLIDATED + WORKSTREAM_GAP_PLAN, ordered by execution priority.

## P0 – Extraction, Scaffold, Regression
- [x] BW contract extraction: parse BW BPEL/BWD/BWP + mapper XML for partnerLinks, mapper IO, palette roles; harvest HTTP/JMS/JDBC resources (Service Descriptor JSON, WSDL/XSD, JDBC timers) and emit concrete inputs/outputs/identifiers with evidence at extraction time.
- [x] Deterministic scaffold/enrichment: compute start/end, reachable invocations, mapper-driven identifiers, JDBC/JMS/timer evidence before SIR generation; enrich interdependencies with calls/called_by and shared datastores/identifiers.
- [ ] Golden fixtures: add SIR/doc/diagram fixtures for repos/bw-samples-master and Towne-Park-Billing-Source-Code; snapshot tests for scaffold/interdeps/enrichment and rendered Markdown/diagrams.
- [x] Regression gates: extend scaffold/coverage reports to fail when IO fields, calls, or errors/logging are empty; add linters/tests that fail builds if key doc sections (relationships, integrations) are missing; define acceptance criteria and document verification workflow in analysis/business_doc_upgrade.md.
- [x] Evidence index enrichment: include XSD/WSDL/JDBC/mapper evidence so rollups/RAG docs cite concrete schemas.

## P1 – Docs, Prompts, Observability, Packaging, Rollup
- [ ] Renderer rebuild: output reference-aligned sections (What it does, Why it matters, Interfaces, Invokes/Dependencies, Interdependency map, Key inputs/outputs, Errors & Logging, Extrapolations, Technical appendix, Related Documents) with YAML front matter (hashes/confidence).
- [ ] Doc planning: add family-level docs and repo overviews; auto-organize generated Markdown into curated deliverables; prompt/context builders must require populated inputs/outputs/identifiers, calls/called_by, shared_datastores, errors/logging (block or down-rank when absent).
- [ ] Observability taxonomy: extract fault handlers/logging activities; define default error categories per connector and backfill with evidence tagging.
- [ ] Packaging/runtime validation: parse META-INF/TIBCO.xml/default.substvar/docker.substvar to map module↔bundle↔Docker image; validate Bundle-SymbolicName/Activator/ClassPath; flag missing deps (e.g., wlfullclient.jar) and publish packaging QA report.
- [ ] Workflow graph export: synthesize transitions from sequences/flows, resolve anonymous IDs, render Graphviz L→R with start-node highlights and human-readable labels; embed deterministic + LLM diagrams into Markdown.
- [ ] Change telemetry: feed recent-change metadata + cost telemetry into provenance blocks; DX/support pages (onboarding, FAQs, change management); run `--llm-rollup` before/after relationship ingestion and archive outputs/telemetry diffs.
- [ ] Constellation/evidence orchestration: repo-wide constellations with evidence packets, anti-pattern catalog (type/severity/file:line/remediation), staged prompt builder using constellation evidence + anti-patterns + graph context; enforce citation tags/validation.

## P2 – Diagram Reconciliation, UI/Screens, Rollup/RAG polish
- [ ] Diagram reconciliation: regenerate deterministic and LLM SVGs with call edges/datastore nodes; inject paths into doc context; reconcile diagram edges vs interdeps and log gaps.
- [ ] Holistic diagram synthesis: deterministic/LLM DOT over aggregated graph for constellation-level SVGs; include in docs and MkDocs nav.
- [ ] Screenshot/UI capture: define storage/metadata; Playwright automation (non-prod first), prod capture via operator job, renderer integration, regression tests, golden-doc fixtures, telemetry guardrails, CLI flags/config toggles, runbooks.
- [ ] Data & KPI summaries; experience-pack narrative/blueprint refinement; rollup resilience and schema automation; publishing/governance (audit metadata, model/env switches, MkDocs nav/build with constellation pages, anti-pattern reports, change logs).
