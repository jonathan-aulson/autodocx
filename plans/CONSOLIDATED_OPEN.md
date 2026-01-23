# Consolidated Open Work

Open items pulled from prior plans (COMBINED_IMPROVEMENT_PLAN, HIERARCHY_NORMALIZATION_PLAN, UNFINISHED_CONSOLIDATED, WORKSTREAM_GAP_PLAN, GAP_CLOSURE_PLAN). None of these have verifiable completions in code/tests yet.

## P0 / Foundations
- [x] Increase scan timeout configurability and document defaults for long BW scans.
- [x] Disable or gate `repo_inventory` in classifier prompts; add routing unit tests to confirm BW extractors are chosen.
- [ ] Golden fixtures: add SIR/doc/diagram baselines for `bw-samples-master` and `Towne-Park-Billing-Source-Code`; snapshot tests for scaffold/interdeps/enrichment and rendered Markdown/diagrams.
- [ ] Define acceptance criteria and verification workflow (analysis/business_doc_upgrade.md); add automated checks for business readability, evidence citations, integration completeness.
- [ ] Refresh scaffold gap thresholds/failure modes with new evidence.

## BW Extraction & Packaging
- [x] Implement BW extractors: module manifests (`*.jsv/*.msv/*.bwm`), resource bindings (`*.httpConnResource/*.httpClientResource/*.jdbcResource/*.jmsResource`), substitution vars (`*.substvar`), test suites (`*.bwt` + `.ml`), diagrams (`*.bwd`), archive-first expansion (`.ear/.jar/.zip/.par`), Java/OSGi adapters, narrowed Service Descriptor globs. (All extractor classes in place; integration into scaffold/interdeps still pending.)
- [ ] Wire new extractor outputs into scaffold/enrichment (identifiers, datastores, interfaces, relationships) and interdeps edges; validate constellation/family membership.
- [ ] Parse packaging (META-INF/TIBCO.xml/default.substvar/docker.substvar) to map module↔bundle↔image, validate manifests (Bundle-SymbolicName/Activator/ClassPath), flag missing libs; publish packaging QA report.

## Docs, Planner, Renderer
- [ ] Rebuild renderer with reference-aligned sections (What/Why/Interfaces/Invokes/Interdependency map/Inputs-Outputs/Errors & Logging/Extrapolations/Technical appendix/Related docs) and YAML front matter.
- [ ] Doc planning: add family-level docs and repo overviews; auto-organize Markdown; prompts/context must require populated IO, calls/called_by, shared_datastores, errors/logging.
- [ ] Observability taxonomy: extract fault handlers/logging activities; define default error categories per connector and backfill with evidence tagging.
- [ ] Migrate outputs/navigation to the new hierarchy (move artifact pages into owners’ packaging/artifacts sections; regenerate docs/nav); run full scan to validate gates and raise coverage if healthy.

## Diagrams & Graphs
- [ ] Workflow graph export: synthesize transitions from sequences/flows, resolve anonymous IDs, render Graphviz L→R with start-node highlights and human-readable labels; embed deterministic + LLM diagrams in docs; reconcile diagram edges vs interdeps and log gaps.
- [ ] Constellation/diagram synthesis: constellation-level SVGs from aggregated graphs; include in docs/MkDocs nav.

## Evidence, RAG, Anti-patterns
- [ ] Evidence index enrichment: include XSD/WSDL/JDBC/mapper evidence, repo-wide constellations with evidence packets; anti-pattern catalog (type/severity/file:line/remediation) and staged prompt builder consuming constellation evidence + anti-patterns + graph context with enforced citations.
- [ ] RAG/doc reconciliation: compare diagram edges vs interdeps; archive llm-rollup outputs/telemetry diffs before/after relationship ingestion; capture doc feedback and iterate prompts.

## Regression Safety
- [ ] Add/extend tests for new extractors, scaffold presence, interdeps edges for resources/tests, doc-section coverage using new data; add failure thresholds for IO/calls/logging/errors adjusted to new evidence.
- [ ] Review out_reorg/logs to ensure all producers write directly into layout (artifacts/, diagrams/, docs/, evidence/, fixtures/, logs/, manifests/, reports/, signals/, tmp/).

## UI/Screens & DX (P2)
- [ ] Screenshot/UI capture program (storage/metadata, Playwright automation, renderer integration, regression tests, golden docs, telemetry guards, CLI toggles, runbooks).
- [ ] DX/support pages (onboarding, FAQs, change management) and experience-pack narrative/blueprint refinements; data/KPI summaries; schema automation and rollup resilience.
