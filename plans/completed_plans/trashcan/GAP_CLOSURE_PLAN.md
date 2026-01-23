# Gap Closure Plan

This plan lists all remaining coverage gaps (BW extractors, routing, fixtures) and the work needed to close them. Tasks are grouped for sequencing; leave boxes unchecked until implemented and verified in code/tests.

## Foundations & Routing
- [ ] Increase scan timeout configuration (CLI flag/env) and document defaults so long BW scans complete.
- [ ] Disable `repo_inventory` in classifier prompts or gate it behind a flag so real extractors are preferred.
- [ ] Add/refresh unit tests covering routing choices to ensure new BW extractors are selected for their file patterns.

## BW Artifact Extractors (implement + tests)
- [ ] Add `BwModuleManifestExtractor` for `*.jsv`, `*.msv`, `*.bwm` to emit module metadata (process list, shared resources, bindings).
- [ ] Add `BwResourceBindingExtractor` for `*.httpConnResource`, `*.httpClientResource`, `*.jdbcResource`, `*.jmsResource`; emit endpoints, credentials placeholders, datastore/queue names into scaffold.
- [ ] Add `BwSubstitutionVarExtractor` for `*.substvar` to surface env-specific variables and map them to dependent resources/processes.
- [ ] Add `BwTestSuiteExtractor` for `*.bwt` and companion `.ml` datasets; emit invoked processes, inputs, and expected datastore interactions as signals.
- [ ] Add `BwDiagramExtractor` for `*.bwd` binaries to produce activity/transition graphs when `.bwp` parsing is incomplete.
- [ ] Add archive-first expansion stage (or `ArchiveExpansionExtractor`) to unpack `.ear/.jar/.zip/.par` with provenance so inner BW flows/resources are scanned.
- [ ] Add `JavaOsgiComponentExtractor` for BW plug-in code (`*.java`, `*.class`, `MANIFEST.MF`, `*.properties`) to emit exported services/adapter metadata.
- [ ] Add narrowed `BwServiceDescriptorExtractor` glob for `*.Process-*.json` and similar Service Descriptor files; parse REST/SOAP contracts without broad JSON noise.

## Scaffold & Interdeps Integration
- [ ] Wire new extractor outputs into enrichment/scaffold so identifiers, datastores, interfaces, and relationships are populated (no string-only targets).
- [ ] Ensure mapper/XPath/SQL/JMS evidence from new extractors flow into `business_scaffold` and `props["enrichment"]`.
- [ ] Extend interdependency graph builder to include edges from new resource/test/manifest signals and validate constellation/family membership.

## Regression Safety & Fixtures
- [ ] Refresh golden fixtures under `out/fixtures/bw-golden` after new extractors; include sir_v2 JSON, interdeps, and diagrams.
- [ ] Add/extend tests: extractor pattern coverage, scaffold presence, interdeps edges for resources/tests, and doc-section coverage using new data.
- [ ] Add failure thresholds for empty IO/calls/logging/errors that account for newly captured evidence; make env-configurable and documented.

## Docs & Hierarchy Output
- [ ] Update doc planner/renderers to include new interfaces/resources/tests in the repo→constellation→family/module→component→process hierarchy (no artifact-only components).
- [ ] Regenerate curated docs/navigation after extractor + planner updates; remove stale artifact-only pages.
- [ ] Verify MkDocs build/rollups/diagrams succeed after layout changes; keep `mkdocs.yml` as the only allowed file at `out/` root.

## Validation Run
- [ ] Perform full scan on BW sample repos with extended timeout; confirm zero enrichment crashes and that new artifacts are classified correctly.
- [ ] Review out_reorg/logs to ensure producers write directly into the new layout (artifacts/, diagrams/, docs/, evidence/, fixtures/, logs/, manifests/, reports/, signals/, tmp/).
- [ ] Summarize residual gaps (if any) and convert into follow-up tasks.
