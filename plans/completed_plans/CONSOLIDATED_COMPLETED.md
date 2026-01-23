# Consolidated Completed Work

Verified tasks that now exist in code/tests. Evidence paths are noted.

- [x] Hierarchy guards and ownership metadata enforced (component/family links, artifact blacklist) — tests `tests/test_component_hierarchy_guard.py`, renderer/planner changes in `autodocx_cli/__main__.py` and `autodocx/render` ensure hierarchy fields and skip artifact-only pages.
- [x] Doc section linters added — `tests/test_curated_doc_sections.py` enforces required sections for curated component docs.
- [x] Interdeps/logging/calls regression checks — `tests/test_interdeps_calls_logging.py` plus SIR quality gating in `autodocx_cli/__main__.py` raise failures for empty calls/logging/errors.
- [x] Signals layout normalized — SIRs/interdeps/graph now write to `out/signals/*` with legacy cleanup (`autodocx_cli/__main__.py`, `autodocx/llm/evidence_index.py`, `autodocx/render/mkdocs.py`, `tests/test_constellation_service.py`).
