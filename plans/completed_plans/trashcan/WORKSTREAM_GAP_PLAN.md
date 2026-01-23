# Workstream: Close Documented Gaps in BW Sample Repository

## Shortcomings (from curated docs)
- Missing IO contracts: many processes list empty `inputs` / `outputs` / `identifiers` despite claiming structured responses (bw-samples-master, BW_MonitoringData_Cleanup_Script, CustomConfigManagement, GetPropertyValues, Dockerfile artifacts, ISSUE_TEMPLATE docs).
- Missing interdependencies: `calls` / `called_by` are empty across most artifacts; shared datastore/identifier arrays are empty.
- Observability gap: errors/logging arrays are empty, leaving no error taxonomy or logging hooks.
- Schema evidence sparse: HTTP endpoints noted but no payload fields or examples (e.g., EquifaxScore); XSD/WSDL/JDBC evidence not surfaced into scaffolds.
- Packaging-only outputs: OSGi/Docker artifacts emit manifests but runtime interfaces and dependencies are not reconciled; required libs (e.g., wlfullclient.jar) not verified.
- Diagram / topology reconciliation: diagram assets exist but ownership/call mappings remain incomplete.

## Plan (sequenced, all items mapped to shortcomings)
1) **Extractor upgrades for contracts (P0)**  
   - Parse BW BPEL/BWD/BWP and mapper XML for partnerLinks, mapper IO, palette roles; emit concrete `inputs`/`outputs`/`identifiers` with evidence.  
   - Harvest HTTP/JMS/JDBC resources (Service Descriptor JSON, WSDL/XSD, JDBC timers) and attach schema fields + datastore identifiers at extraction time.  
   - Normalize workflow graph export to include operation labels and endpoint paths for HTTP resources.

2) **Deterministic scaffold/enrichment (P0)**  
   - Compute start/end, reachable invocations, mapper-driven identifiers, JDBC/JMS/timer evidence before SIR generation; populate `business_scaffold.io_summary` and `props["enrichment"]`.  
   - Enrich interdependencies with `calls`/`called_by` from transitions, partnerLinks, and HTTP/JMS/JDBC calls; auto-detect shared datastores/identifiers from mapper + JDBC/XSD.

3) **Observability and error taxonomy (P1)**  
   - Add extractor hooks for fault handlers/logging activities; map to `business_scaffold.errors` and `business_scaffold.logging` with evidence.  
   - Define default error categories per connector (HTTP/JMS/JDBC/custom) and backfill when explicit handlers are absent, tagging with low-confidence markers.

4) **Packaging & runtime validation (P1)**  
   - Parse META-INF/TIBCO.xml/default.substvar/docker.substvar to map module ↔ bundle ↔ Docker image; record missing required artifacts (e.g., wlfullclient.jar) as findings.  
   - Validate manifest fields (Bundle-SymbolicName/Activator/ClassPath) and surface into signals + reports; flag discrepancies in a packaging QA report.

5) **Diagram reconciliation (P2)**  
   - Regenerate deterministic SVGs from enriched workflow graphs (diagrams/deterministic_svg) and LLM SVGs (diagrams/llm_svg) with call edges and datastore nodes.  
   - Inject diagram paths into doc context and ensure component/family docs cite diagrams alongside enriched calls/datastores.

6) **Doc generation & prompts (P1)**  
   - Update prompts/context builders to require filled `inputs`/`outputs`/`identifiers`, `calls`/`called_by`, `shared_datastores`, and `errors/logging`; block or down-rank docs missing evidence.  
   - Add explicit sections for schema fields, dependency map, and observability commitments in curated docs.

7) **Regression gating (P0)**  
   - Add fixtures/tests that fail when IO fields, calls, or errors/logging are empty for BW samples (sir_v2 fixtures + golden docs).  
   - Extend coverage/scaffold reports to track fill rates for IO/calls/logging and fail CI below thresholds.

8) **RAG/doc reconciliation (P2)**  
   - Ensure `evidence_index.json` includes enriched evidence (XSD/WSDL/JDBC/mapper) so rollups and RAG docs cite concrete schemas.  
   - Add a reconciliation pass that compares diagram edges vs. interdeps and logs gaps for remediation.

## Execution notes
- Root out outputs still targeting legacy paths (`assets/diagrams*`, `out/coverage.json`, etc.) so relocations are unnecessary.  
- Prioritize P0 items (extractor + scaffold + regression gates) before observability/packaging/doc polish.
