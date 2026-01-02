# Autodocx ↔ Example Parity Checklist

## Baseline the Orchestrator & Router
- [x] Capture repo roots, archive manifests, and readiness (env vars, Graphviz availability) during bootstrap in `autodocx_cli/__main__.py`.
- [x] Extend the extraction router loop in `autodocx_cli/__main__.py` to persist a per-run manifest that maps files → modules/families for downstream graph/doc consumers.
- [x] Enhance `autodocx/orchestrator/deterministic_router.py` so assignments can incorporate module metadata instead of relying solely on pattern matches.

## Evidence-First BW/PB Extraction
- [x] Upgrade `autodocx/extractors/tibco_bw.py` into a full BPEL walker that records partner links, mapper IO, transitions, and palette role hints.
- [x] Replace directory-style relationships in `autodocx/extractors/bw_artifacts.py` with typed targets (connector, evidence pointers) that align with `signal_scaffold`.
- [x] Add archive/service descriptor/WSDL/XSD/JDBC parsing so Signals emitted from BW archives surface identifiers/datastores immediately. _(Service descriptor JSON now emits HTTP triggers.)_

## Deterministic Scaffold & Enrichment
- [x] Expand `autodocx/enrichers/process_enrichment.py` to compute mapper-driven identifiers, JDBC/JMS evidence, timers, and logging before SIR generation. _(Now mines JDBC/JMS from steps + relationships.)_
- [x] Teach `autodocx/scaffold/signal_scaffold.py` (and `ensure_business_scaffold_inputs`) to use the new enrichment data to determine start nodes, reachable invocations, and IO summaries deterministically. _(Derived steps carry packaging hints; control edges now backfilled; start nodes computed.)_

## Packaging & Interdependency Graph
- [x] Parse `TIBCO.xml`, manifests, and `.substvar` files during archive handling so each Signal/SIR carries module, family, and shared resource metadata.
- [x] Update `autodocx/interdeps/builder.py` plus `_interdeps.json` persistence to include packaging/family context, and extend `tests/test_interdeps_builder.py` with BW/PB fixtures that fail when identifiers/datastores are missing.

## Doc Planning & Prompting
- [x] Replace the quality-only ordering in `_build_plan_entries_from_context` (`autodocx/docplan/plan.py`) with a richness score (scaffold + interdeps) and cache plans between runs.
- [x] Update `autodocx/llm/context_builder.py` and `autodocx/llm/prompt_builder.py` so component/family prompts always include BW-specific interfaces, invocations, identifiers, logging, and constellation context.

## Orchestration, Diagrams, & Regression Safety
- [x] Add a CLI subcommand that runs packaging extraction → scaffold build → interdeps → doc plan sequentially (built on the stack around `autodocx_cli/__main__.py`).
- [x] Integrate deterministic Graphviz SVG export for each BW process and ensure Windows-safe temp handling.
- [x] Extend `tests/test_tibco_bw_extractor.py`, `tests/test_doc_plan.py`, and new regression fixtures to diff against `out/constellations` for the BW sample repos. _(BW extractor tests now execute with lxml installed; doc-plan/constellation fixtures still pending.)_

## Immediate Next Steps
- [x] Normalize extractor relationship payloads (especially `autodocx/extractors/bw_artifacts.py`) and stop emitting archive directory stubs so scaffold builders stop crashing.
- [x] Build the BW packaging manifest inside `autodocx_cli/__main__.py` and wire it into `plan_extractions` so every Signal is traceable to its module/family.
- [x] Capture golden BW outputs (SIR + constellation JSON) for `repos/bw-samples-master` and gate future work on matching identifiers/datastores/interdependency edges. _(Stored under `out/bw-golden`; revisit once enrichment upgrades land.)_
