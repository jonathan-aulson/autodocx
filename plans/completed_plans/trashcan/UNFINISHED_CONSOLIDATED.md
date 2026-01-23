# Unfinished Plan Items (Consolidated)

Uncompleted tasks gathered from plans in `./plans`. These appear not to be implemented in code or tests. Suggested priority: **P0 (immediate)**, **P1 (near-term)**, **P2 (later)**.

## P0 – Documentation Quality & Regression Guardrails
- [ ] Add golden-output fixtures for `repos/bw-samples-master` and `repos/Towne-Park-Billing-Source-Code` covering SIR JSON, docs, and diagrams to prevent regressions.  
- [ ] Create targeted pytest suites for the new scaffold/interdependency/enrichment modules plus snapshot tests for rendered Markdown/diagrams.  
- [ ] Document the verification workflow in `analysis/business_doc_upgrade.md` (commands, expected artifacts).
- [ ] Define acceptance criteria (business readability, evidence citations, integration completeness) and codify as automated checks or review checklist.
- [ ] Add regression tests/linters that fail builds if key sections are missing (relationship highlights, integration catalog).

## P1 – Doc Rendering, Planning, and Evidence Parity
- [ ] Rebuild the process renderer to output reference-aligned sections (`What it does`, `Why it matters`, `Interfaces exposed`, `Invokes/Dependencies`, `Interdependency map`, `Key inputs & outputs`, `Errors & Logging`, `Extrapolations`, `Technical appendix`, `Related Documents`) with YAML front matter carrying hashes/confidence.
- [ ] Add family-level docs and repo overviews (members, endpoints, intra-family calls, shared data, cross-family calls).
- [ ] Wire a documentation plan/follow-up step that reorganizes generated Markdown into curated deliverables automatically.
- [ ] Upgrade the workflow graph export to synthesize transitions from sequences/flows and resolve anonymous IDs so diagram nodes read as plain-language activities.
- [ ] Render Graphviz SVGs L→R with labeled nodes and highlighted start nodes; ensure PB/UI-derived activities use human-readable labels.
- [ ] Embed diagrams + UI assets into Markdown so every process doc ships with synchronized visuals.
- [ ] Feed recent-change metadata + cost telemetry into provenance blocks so executive docs highlight deltas between scans.
- [ ] Track improvement ideas and summarize vetted recommendations for user review (business_doc_todo.md items 93–94).
- [ ] Run `autodocx scan ... --llm-rollup` twice (before/after relationship ingestion) and archive outputs plus telemetry diffs; capture doc samples/feedback and iterate prompt/renderer tone.
- [ ] Add DX/support pages (onboarding, FAQs, change management) sourced from repo signals plus templates.
- [ ] Schedule pilot reviews with execs/analysts/support and capture structured feedback; plan incremental rollouts with telemetry.

## P1 – Future-State Evidence & LLM Orchestration
- [ ] Repo-wide inventory of code constellations with provenance graphs; evidence packets per constellation (code/config/tests/infra/history); version/diff evidence packets between runs.
- [ ] Automated anti-pattern catalog (type, severity, file:line, remediation); run linters/rule engines and record findings with remediation suggestions.
- [ ] LLM orchestration overhaul: staged prompt builder consuming constellation evidence, anti-patterns, graph context; enforce citation tags and validation.
- [ ] Holistic diagram synthesis: deterministic/LLM DOT over aggregated graph to produce constellation-level SVGs and reference in docs.
- [ ] Publishing/governance upgrades: audit metadata (who/when/model), config switches for models/env, MkDocs nav/build includes constellation pages, anti-pattern reports, change logs.

## P2 – Screenshot & UI Capture Program (Upgrade Roadmap)
- [ ] Define screenshot storage & metadata; Playwright automation (non-prod first); prod capture via operator job; renderer integration; renderer regression tests; golden-doc fixtures; telemetry guardrails; onboarding doc update; CLI flags/config toggles; runbooks.
- [ ] LLM narrative overhaul (experience pack orchestration, journey blueprint refinement).
- [ ] Workflow diagram pipeline enhancements for branching logic across Logic Apps/Power Automate: control-aware parsing, labeled branch edges, embed diagrams in MkDocs/business pages.
- [ ] Data & KPI summaries; screenshot & UI context pass; schema automation & rollup resilience.
