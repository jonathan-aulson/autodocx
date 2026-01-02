# Business Scaffold Data Fidelity Plan

Ensure every extractor feeds the fields that `build_scaffold` aggregates so interdependency graphs and doc plans have meaningful identifiers, datastores, and process edges. This plan links pipeline internals (`run_scan`, scaffold builder, interdeps, docplan) with the sample BW and PowerBuilder repositories you provided so we can implement and validate richer Signals per stack.

## Requirements
- [x] Guarantee `business_scaffold.io_summary.identifiers` is populated for every emitted Signal, not just HTTP triggers (`autodocx/scaffold/signal_scaffold.py`).
- [x] Guarantee `business_scaffold.dependencies.datastores` and `business_scaffold.dependencies.processes` are populated so `build_interdependencies` can emit shared-id/datastore edges (`autodocx/interdeps/builder.py`).
- [x] Instrument extraction so we can trace which extractor leaves those lists empty (per-signal diagnostics within `run_scan` in `autodocx_cli/__main__.py`).
- [x] Use `repos/bw-samples-master` and the PowerBuilder repos as regression suites to prove the improved extractors output usable identifiers/datastore/process metadata.

## Scope
- In scope: extraction/enrichment/scaffold code, tests, analyzer scripts, documentation describing extractor expectations, sample repos under `repos/`.
- Out of scope: downstream LLM prompt tuning, MkDocs theming, deployment scripts (only touched indirectly if doc content changes).

## Files & Entry Points
- `autodocx_cli/__main__.py` – orchestrates extraction, enrichment, scaffolding, and interdeps writing; add coverage logging + guardrails here.
- `autodocx/scaffold/signal_scaffold.py` – controls how identifiers/datastores are derived from props; may need new hooks for extractor-provided metadata.
- `autodocx/enrichers/process_enrichment.py` – current JDBC/JMS/mapper enrichment; extend for more stacks to auto-populate dependencies.
- `autodocx/interdeps/builder.py` & `autodocx/docplan/plan.py` – consumers of scaffold data; confirm requirements for identifiers/datastore/process entries.
- `scripts/check_logicapps_relationships.py` & `scripts/render_relationship_demo.py` – existing diagnostics/renderers; extend/clone for scaffold coverage inspection.
- `autodocx/extractors/tibco_bw.py` – parses BW process XML; must emit richer steps, relationships, and datastore hints.
- Sample repos for validation: the BW and PowerBuilder README files listed in the request.
- Current output gap reference: `out/sir_v2/_interdeps.json` shows empty identifiers/datastores arrays, proving the need for this work.

## Data Model / API Changes
- [x] Document and enforce a minimal Signal contract: each extractor must set `props.triggers`, `props.steps` (with `connector`, `datasource`, `target`, `inputs_keys`, `outputs_example`), `props.relationships`, and, where possible, `props.identifiers`/`foreign_keys`. Update `autodocx/types.Signal` docstring if needed.
- [x] Extend `build_scaffold` helpers to accept new extractor-provided hints (e.g., `props.datasource_tables`, `props.process_calls`, `props.primary_keys_extracted`).
- [x] Add optional metadata to enrichment outputs (e.g., `mapper_hints[*].identifiers`, `jdbc_sql[*].table`) so scaffolding can deterministically derive identifiers/datastores even when the extractor cannot.

## Action Items
1. [x] Instrument extraction coverage – Add a per-signal audit pass in `run_scan` to log which kind/extractor emitted empty `business_scaffold.io_summary.identifiers` or dependency buckets and write a JSON/CSV report for quick diffing between runs on sample repos.
2. [x] Author scaffold contract + dev docs – Update `developer_onboarding_context.md` (or create a focused doc) to spell out the required props/enrichment keys each extractor must set, referencing how `build_scaffold` consumes them; link to the new coverage script.
3. [x] Create fixture scans for BW + PB repos – Write scripts (under `analysis/` or `scripts/`) that run `autodocx scan` against `repos/bw-samples-master/bw-samples-master`, `PowerBuilder-RestClient-Example`, `PowerBuilder-RibbonBar-Example`, `PowerServer-Console-PB-Example`, and `powerbuilder-2017-master`, producing named `out/` folders for regression comparisons.
4. [x] Enhance TIBCO BW extractor – Extend `tibco_bw.py` to: (a) push partnerLink info into `props.relationships/steps` with explicit connector kinds, (b) capture JDBC/JMS adapter configuration as `steps[*].datasource/destination`, (c) parse XPATH/mapper activities for identifier hints, using BW samples to verify real datastores/process edges.
5. [x] Add PowerBuilder extractor(s) – Build a dedicated extractor that can parse `.srw/.sru/.srd/.pbt` metadata (falling back to orcascript or regex) to emit workflows/functions with SQL statements (DataWindow SELECTs, embedded EXECs) and object calls; ensure it populates `steps`, `datastores`, `identifiers`, and process references using the provided PB repos as fixtures.
6. [x] Backfill other workflow extractors – Audit Logic Apps, Power Automate, Azure Functions, AWS Lambda, GitHub Actions, etc., ensuring each sets `inputs_example/outputs_example`, `relationships`, and `datastores`. Where metadata is missing, add parsers (e.g., inspect JSON schemas, environment references) or deterministic fallbacks.
7. [x] Expand enrichment pipeline – Teach `process_enrichment` to mine identifiers/datastores from SQL text (table names, primary keys), REST schemas, and mapper payloads; optionally leverage sample data files in the Towne Park repo to auto-detect key columns.
8. [x] Testing & automation – Add pytest cases that load captured Signals from the sample repos and assert scaffold fields are non-empty; integrate the new coverage script into CI to fail when any extractor regresses.
9. [x] Docs & demos – Update README/developer docs with instructions for running the scaffold coverage report, and refresh `analysis/demo_docs` or similar to showcase interdeps graphs that now include real shared identifiers/datastores.

## Phase 2 – Cross-Extractor Rollout
Apply the same scaffold-focused improvements beyond TIBCO BW and PowerBuilder. Every remaining extractor and downstream consumer (artifacts, doc plan, interdeps slices, renderers) must emit or honor the identifier/datastore/process metadata so coverage stays consistently high across the entire portfolio.

### Additional files & owners to touch
- `autodocx/extractors/logicapps.py`, `power_automate.py`, `process_diagrams.py` – ensure each workflow step includes datasource/service hints and mapper identifiers.
- `autodocx/extractors/azure_functions.py`, `aws_lambda.py`, `github_actions.py`, `azure_pipelines.py`, `express.py`, `k8s_manifests.py`, `terraform.py`, `sql_migrations.py`, `tree_sitter_code.py`, `integration_imports.py`, `business_entities.py`, `ui_components.py`, `repo_inventory.py` – propagate identifiers/datastores/process links wherever they can be inferred.
- `autodocx/artifacts/option1.py`, `autodocx/docplan/plan.py`, `autodocx/render/business_renderer.py`, `autodocx/interdeps/builder.py` – double-check they surface the richer scaffold metadata in artifacts, doc context, and visuals.
- `tests/test_logicapps_extractor.py`, `tests/test_power_automate_extractor.py`, `tests/test_azure_functions_relationships.py`, `tests/test_aws_lambda_extractor.py`, `tests/test_github_actions_relationships.py`, `tests/test_sql_migrations_relationships.py`, `tests/test_tree_sitter_code_extractor.py`, etc. – expand coverage to assert identifiers/datastores/process lists are non-empty.

### Additional Action Items
10. [x] Logic Apps + Power Automate + Process Diagram extractors emit identifier/datastore/process hints for every flow (include mapper-derived identifiers and Dataverse/table usage).
11. [x] Azure Functions + AWS Lambda + GitHub Actions + Azure Pipelines extractors populate `steps`, `relationships`, `datasource_tables`, and `service_dependencies` for every binding/trigger/output combination.
12. [x] ExpressJS + K8s/Terraform + SQL Migrations + Tree-sitter code extractors enrich their signals with datastore/service references (e.g., Terraform backends, SQL table names, API client targets).
13. [x] Mapper/artifact/doc-plan surfaces (`autodocx/artifacts/option1.py`, `autodocx/docplan/plan.py`, `autodocx/render/business_renderer.py`) highlight the new scaffold data (identifiers, datastores, process deps) so docs and diagrams expose the richer interdependency story.
14. [x] Add regression tests per extractor asserting `business_scaffold.io_summary.identifiers`, `dependencies.datastores`, and `dependencies.processes` are populated; wire them into CI alongside the scaffold coverage script.

Progress will be tracked by checking the boxes in this file as each requirement/action item is completed.
