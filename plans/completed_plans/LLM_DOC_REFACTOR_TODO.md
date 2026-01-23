# AutoDocX – LLM-Only Documentation Pipeline To‑Do

This checklist tracks the work required to shift AutoDocX to an LLM-authored documentation pipeline. Tasks should be checked off as they are completed; add notes under each item if needed.

## Checklist

1. **Define desired workflow and assets**
   - [x] Confirm target deliverables (per-component, per-family, multi-level rollups, final repo overview) and required inputs (SIR/SIRv2/interdeps/graphs/assets).
   - [x] Document data payloads each LLM prompt must include. *(See plan summary in development notes.)*

2. **Disable deterministic Markdown generation**
   - [x] Remove or guard `render_docs` so it no longer emits Markdown files.
   - [x] Ensure CLI only produces structural JSON/assets prior to the LLM phase.

3. **Expand doc-plan generation**
   - [x] Generate plan entries for every component discovered.
   - [x] Generate entries for every family/domain grouping.
   - [x] Generate hierarchical rollup entries that continue until a single repo overview remains.

4. **Enhance LLM fulfillment**
   - [x] Build richer prompt template that references all available artifacts (SIR/SIRv2/interdeps/graphs/assets).
   - [x] Stream relevant evidence into the payload (Markdown snippets, JSON, SVG references) for each plan entry.
   - [x] Enforce per-section minimum word counts with validation/retry logic based on `AUTODOCX_SECTION_MIN_WORDS`.

5. **Update CLI workflow**
   - [x] Wire the new doc-plan + LLM fulfillment into `run_scan` as the sole documentation stage.
   - [x] Adjust MkDocs nav generation to reflect only LLM-authored docs.
   - [x] Ensure supporting assets (SVGs, graphs) are copied into `docs/assets` for reference links.

6. **Testing & verification**
   - [x] Update/create automated tests covering doc-plan creation, fulfillment retries, and env-controlled word floors.
   - [x] Run end-to-end scan to confirm only LLM-authored Markdown exists. *(2025-12-19: `.venv/bin/python -m autodocx_cli scan repos/PowerBuilder-RestClient-Example-master --out out` → curated docs only)*
   - [x] Manually spot-check docs for business-friendly content and rollup structure. *(Verified repo overview + component/family briefs in `out/docs/curated`)*

7. **Cleanup & documentation**
   - [x] Remove obsolete flags/docs referencing deterministic docs. *(README + developer onboarding now describe doc-plan/LLM pipeline; render_docs references removed.)*
   - [x] Update README / onboarding docs to describe the new workflow and env settings. *(Added env var guidance + LLM-only workflow documentation.)*
