# AutodocX Upgrade Roadmap

This document breaks the upgrade program into actionable tasks. Each task is prefixed with `[ ]` so progress can be tracked by replacing the space with an `x` as work completes.

---

## 1. Evidence & Signal Deepening

- [x] **Logic Apps signals capture richer context**  
  - Expand `autodocx/extractors/logicapps.py` (currently `LogicAppsWDLExtractor` extends signals with triggers, steps, `calls_flows` — lines 8-199) to compute:
    - `props["user_story"]`, inferred from trigger types plus connector hints.
    - `props["inputs_example"]` / `props["outputs_example"]` derived from trigger/request schemas and action response schemas inside `definition["actions"]`.
    - `props["latency_hints"]` by inspecting recurrence intervals and built-in throttling settings.
    - `props["step_display_name"]` so renderer can emit user-facing names (map from `step["name"]` + connector titles).
  - Ensure the additional props are appended to each `Signal` before SIR creation in `autodocx_cli/__main__.py:438-482`.

- [x] **TIBCO BW extractor emits UI/UX hints**  
  - `autodocx/extractors/tibco_bw.py` currently records triggers, SQL, connectors, but not view data. Capture:
    - `props["ui_view"]` when a process references HTML templates or UI-related connectors.
    - `props["data_samples"]` by truncating SQL snippets with `autodocx/utils/redaction.py`.
    - `props["screenshots"]` placeholders to be filled by the automation step (see Section 9); extractors should allow linking captured images via filename convention.

- [x] **UI Components extractor captures hierarchy + screenshots**  
  - Extend `autodocx/extractors/ui_components.py:20-118` to compute:
    - `props["route_hierarchy"]` (parent/child routes derived from React Router / Angular module structure).
    - `props["entry_points"]` unity of component + route.
    - Accept metadata such as `props["screenshots"] = ["assets/screenshots/<component>/main.png"]` for renderer embedding.

- [x] **SQL extractor provides payload schemas**  
  - Enhance `autodocx/extractors/sql_migrations.py:8-57` to emit column definitions and inferred sample rows as `props["data_samples"]`, plus `props["foreign_keys"]` for artifact storytelling.

- [x] **Signal consumers support new props**  
  - Update `autodocx/types.py:5-28` documentation/comments to highlight the extended `props`.
  - Ensure `_graph_node_id_for_signal_kind_and_props` in `autodocx_cli/__main__.py:175-200` keeps working when new props arrive.
  - Normalize additional props when writing SIRs (`autodocx_cli/__main__.py:438-482`) so each SIR stores `user_story`, `inputs_example`, `outputs_example`, `ui_snapshot` (if provided).

---

## 2. Artifact Schema Upgrade

- [x] **Option1 mapper produces narrative-ready artifacts**  
  - Extend `autodocx/artifacts/option1.py:34-339` to add:
    - `personas` array (`[{name, goals, pain_points, evidence}]`).
    - `primary_journeys` array summarizing user stories (tie back to `user_story` props).
    - `ux_summaries`, `before_after`, `screenshots`, `data_examples`.
    - `experience_pack_id` linking workflows + routes + UI signals.

- [x] **Schema definition + validation**  
  - Create `autodocx/artifacts/schema/narrative_option1.json` describing the new structure.
  - Add a validation step (e.g., in `autodocx_cli/__main__.py` after artifact creation) using `jsonschema` to enforce required narrative fields.

- [x] **Experience pack helper**  
  - Add a module (e.g., `autodocx/artifacts/experience_packs.py`) that groups related signals (workflow + route + UI + DB) and produces aggregated props consumed by `to_option1_artifact`.
  - Ensure helper runs before rendering; consider caching packs on disk (`out/experience_packs.json`) for debugging.

---

## 3. Renderer & Template Overhaul

- [x] **MkDocs renderer layout refresh**  
  - Redesign `autodocx/render/mkdocs.py:1-200` to:
    - Inject new sections (“How users interact”, “Screens and APIs they see”, “Data they produce/consume”, “Before vs After”, “Evidence highlights”).
    - Pull screenshots from assets (see Section 9) and embed them with captions derived from `props["screenshots"]`.
    - Display narrative tables sourced from artifacts’ personas / journeys.

- [x] **Business renderer enhancements**  
  - Update `autodocx/render/business_renderer.py:200-520` to:
    - Render persona cards, UX narratives, and evidence snippets inline.
    - Insert Mermaid sequence diagrams / swimlanes when `props["relationships"]` include ordered steps (tie into `_relationship_highlights`).
    - Provide fallback text when narrative data is missing (to avoid blank sections).

- [x] **Visual integration**  
  - Extend `autodocx/visuals/graphviz_flows.py` (and potentially a new Mermaid helper) to output sequence diagrams for experience packs, storing them under `out/assets/diagrams/`.


## 4. LLM Prompt & Schema Redesign

- [x] **Schema updates**  
  - Modify `autodocx/llm/schema_store.py:1-160` so both group and component schemas require:
    - Long-form `what_it_does` blocks with explicit evidence arrays.
    - `user_experience` sections (narrative paragraphs, referenced screenshots).
    - `risk_stories`, `operational_behaviors`, `data_flows`.

- [x] **Prompt builder rewrite**  
  - Overhaul `autodocx/llm/prompt_builder.py:1-36` to instruct the model to:
    - Describe user journeys, UI states, and data interactions.
    - Cite `screenshots` and `inputs/outputs` when available.
    - Reject claims without evidence.

- [x] **Context builder expansion**  
  - Update `autodocx/llm/context_builder.py:1-189` to merge:
    - UI snapshots, screenshot paths, payload samples, stitched timelines (ordered steps).
    - Provide `experience_packs` section for prompts, referencing aggregated artifacts.

---

## 5. UI Screenshot Capture & Embedding

- [ ] **Define screenshot storage & metadata**  
  - Standardize on `out/assets/screenshots/<component>/<view>.png`.
  - Update extractors/renderers to set/read `props["screenshots"] = ["assets/screenshots/<component>/<view>.png"]`.

- [ ] **Playwright automation (non-prod first)**  
  - Add a script under `scripts/capture_screenshots.py` (uses Playwright):
    - Logs into non-prod (public) via MS work account, guided by `.env` credentials or interactive login.
    - Captures views specified in a config (component -> URL list).
    - Stores PNGs into the assets directory before the renderer runs.
  - Provide GitHub Actions workflow that:
    - Runs the Playwright script against non-prod.
    - Uploads screenshots as artifacts or commits them to the repo (depending on policy).

- [ ] **Prod capture via operator-run job**  
  - Document a manual/CLI entry point (`autodocx capture-ui --env prod`) that:
    - Requires VPN connectivity and interactive login.
    - Reuses the same Playwright code but skips headless automation if credentials aren’t available.

- [ ] **Renderer integration**  
  - Modify MkDocs/business renderers to look for screenshot paths matching each component and display them in a “What users see” section, with evidence callouts (file path, capture date).

---

## 6. Verification & Quality Gates

- [ ] **Renderer regression tests**  
  - Update `tests/test_renderer_frontmatter.py` to assert presence of new YAML keys (personas, UX sections, screenshot metadata).
  - Add new fixtures (e.g., `tests/fixtures/narrative_component.json`) to verify Markdown output includes narrative sections and evidence links.

- [ ] **Golden-doc fixtures**  
  - Create a sample repository under `repos/fixtures/narrative_sample/` and capture expected `docs/*.md` outputs; add tests that compare generated Markdown against golden files (allowing minor whitespace diffs).

- [ ] **Telemetry guardrails**  
  - Enhance `autodocx/llm/persistence.py:18-190` to log when LLM outputs lack required narrative sections or evidence.  
  - Fail the rollup (or mark as draft) when coverage thresholds aren’t met, preventing low-quality docs from publishing.

---

## 7. Operationalization

- [ ] **Onboarding doc update**  
  - Add a new section to `developer_onboarding_context.md` describing:
    - Narrative artifact expectations.
    - Screenshot capture workflow.
    - How to run scans in “narrative mode.”

- [ ] **CLI flags / config toggles**  
  - Extend `autodocx_cli/__main__.py:585-624` to add `--narrative-mode` (and corresponding YAML setting) that:
    - Enables screenshot embedding, narrative schema validation, enhanced renderers, and stricter LLM prompts.
    - Allows legacy runs by default for lighter output until the new pipeline is fully adopted.

- [ ] **Runbooks**  
  - Document operational steps (VPN requirements, credential rotation for screenshot automation, golden-doc review steps) either in `README-Guide_for_Technical_Doc_Gen.md` or a new `RUNBOOK.md`.

---

## 8. Narrative & Diagram Upgrade Backlog

- [ ] **LLM narrative overhaul**  
  - Enrich prompts/context so `what_it_does`, `user_experience`, and `journey_blueprints` contain executive-ready prose with cited evidence.  
  - Add guardrail tests that fail when any narrative array is empty.

- [ ] **Experience pack orchestration**  
  - Group workflows, UI, and data touchpoints into persona-focused “packs” with screenshot references, tables, and evidence-backed KPIs.

- [ ] **Journey blueprint refinement**  
  - Normalize step titles (verb + outcome), attach evidence IDs per step, and surface blueprint summaries prominently in docs.

- [ ] **Workflow diagram pipeline (branching logic)**  
  - [x] Export workflow graphs as JSON under `out/flows/<component>/<workflow>.json`.  
  - [x] Render Graphviz SVG diagrams (swim lanes, decisions, external systems).  
  - [ ] Control-aware parsing for Logic Apps / PA actions (If, Switch, Scope, Foreach, Until) so nested branches and conditions are captured with expressions + evidence.  
  - [ ] Extend renderer to visualize branch edges with labels (`if …`, `else`, `case`, `loop`) and highlight parallel/loop scopes similar to the Power Automate designer.  
  - [ ] Embed diagrams in MkDocs/business pages with captions + evidence callouts (per workflow + per component summary).

- [ ] **Data & KPI summaries**  
  - Auto-compute per-workflow stats (triggers, connectors, dependency counts) and show as infographics with risk badges.

- [ ] **Screenshot & UI context pass**  
  - Enforce screenshot references (fallback messaging) and align images with journey steps.

- [ ] **Schema automation & rollup resilience**  
  - Generate the Responses API schema from structured definitions and lint minimal payloads before every LLM call.

---

> _Next step recommendation_: review and prioritize the sections above, then convert the highest-priority tasks into GitHub issues or tickets so work can proceed incrementally.
