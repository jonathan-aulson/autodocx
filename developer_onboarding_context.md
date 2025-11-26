# AutodocX Developer Onboarding Context

This document equips you to extend, debug, and operate the AutodocX documentation pipeline without ambiguity. Read it once, keep it handy, and you should be able to fix, extend, diagram, or explain any part of the system.

---

## 1. Mission and High-Level Flow

AutodocX transforms heterogeneous source repositories into evidence-backed documentation:

1. **Discover** relevant files via extractor plugins.  
2. **Extract** structured signals (SIRs) with provenance.  
3. **Assemble** a knowledge graph and compute quality metrics.  
4. **Map** signals into universal artifact documents.  
5. **Render** Markdown (and optionally MkDocs sites) with diagrams.  
6. **Roll up** component summaries with LLM assistance (optional).  
7. **Publish** everything under the configured `out` directory.

The CLI entry point (`autodocx_cli/__main__.py`) orchestrates the entire workflow.

---

## 2. Repository Tour

| Path | Purpose |
| --- | --- |
| `autodocx_cli/__main__.py` | CLI bootstrap, scan flow, orchestration. |
| `autodocx/` | Library modules (extractors, graph, renderers, LLM, etc.). |
| `autodocx/extractors/` | Plugin implementations for file types (OpenAPI, Logic Apps, TIBCO BW, etc.). |
| `autodocx/graph/builder.py` | Signal -> Node/Edge conversion and joiners. |
| `autodocx/artifacts/option1.py` | Signal -> universal artifact mapping. |
| `autodocx/render/mkdocs.py` & `autodocx/render/business_renderer.py` | Markdown generation, Graphviz visuals, MkDocs integration. |
| `autodocx/features/distance_features.py` | NetworkX-based graph metrics. |
| `autodocx/scoring/facets.py` | Overall documentation quality scoring. |
| `autodocx/llm/` | Evidence index, grouping, OpenAI provider, rollup pipeline. |
| `autodocx/utils/roles.py` & `autodocx/roles/roles.json` | Role inference from connectors. |
| `tests/` | Coverage for renderer front-matter and marker matching. |
| `out/` | Default output directory for artifacts, docs, graphs, metrics, MkDocs site. |
| `autodocx.yaml` | Required runtime configuration file. |
| `scripts/legacy/autodocx_mono.py` | Historical monolithic scanner (kept for reference only). |

---

## 3. Prerequisites and Environment

1. **Python** 3.10+.  
2. **System dependencies**: Graphviz CLI (`dot`), MkDocs (`mkdocs`), Bicep CLI or Azure CLI (for Bicep extraction), optional `az` CLI.  
3. **Virtual environment**: `python -m venv .venv && .\.venv\Scripts\activate`.  
4. **Install**: `pip install -e .` (loads package and entry points).  
5. **Config file**: `autodocx.yaml` in repo root (see Section4).  
6. **Secrets**: `.env` with `OPENAI_API_KEY=...` for LLM-enabled runs.  
7. **Optional env toggles**:  
   - `AUTODOCX_CONFIG` -> override config path.  
   - `AUTODOCX_EXTRACTORS_INCLUDE` / `AUTODOCX_EXTRACTORS_EXCLUDE` -> comma-separated class or fully-qualified names for plugin selection.  
   - `AUTODOCX_DEBUG_EXTRACTORS=1` -> verbose extractor logging.
8. **Tree-sitter language packs (optional)**: Install `tree-sitter-languages` only when your OS/architecture provides wheels (Linux/macOS). Use `pip install -e .[treesitter]` or `pip install tree-sitter-languages` manually. Windows users can skip it—the AST extractors auto-disable if the module is missing.

---

## 4. Configuration (`autodocx.yaml`)

The config is validated by `autodocx/config_loader.py` and must supply:

- `engine.plugins`: declarative plugin list (currently advisory; actual loading is via entry points).
- `engine.excludes` / `engine.max_file_mb`: scanning hints passed to extractors.
- `docs.profile`: `business`, `technical`, or `full`. `business` activates business-specific renderers.
- `docs.visuals.enable_flow_diagrams`: toggles Graphviz rendering.
- `docs.sections.*`: display toggles for confidence/interdependency/unknown sections.
- `docs.mkdocs_build`: default MkDocs build toggle (still overridden by CLI flag).
- `llm.*`: provider, model, max tokens, optional temperature, structured output schema settings, telemetry pricing for cost estimation.
- `out_dir`: root directory for outputs. CLI resolves it to an absolute path.
- `rollup.publish_threshold` / `rollup.hypothesis_threshold` / `rollup.allow_unknown_evidence`.

Missing keys raise `ConfigError`, so keep this file complete. The CLI combines `args.out` with `out_dir` for final output location.

---

## 5. End-to-End Scan Workflow

The `autodocx scan <repo>` command runs through the following stages (implementation references from `autodocx_cli/__main__.py`):

1. **Bootstrap**  
   - Load `.env` (project root, then repo-specific).  
   - Remove stray `__pycache__`/`.pyc` under the project while skipping virtualenvs.  
   - Clean the target `out` directory with `clean_out_dir_preserve_site_and_mkdocs`: keeps `mkdocs.yml`, `site/`, and only trims `metrics` down to `llm_usage.csv`.

2. **Configuration & CLI args**  
   - Default command is `scan` if omitted.  
   - CLI flags: `--out`, `--debug`, `--mkdocs-build`, `--llm-rollup`.

3. **Extractor Loading** (`autodocx.registry`)  
   - Import built-in modules under `autodocx.extractors`.  
   - Load `autodocx.extractors` entry points (see `pyproject.toml`).  
   - Deduplicate by class, apply include/exclude env filters.

4. **Extraction Stage**  
   - For each extractor: `detect(repo)` -> if `True`, run `discover(repo)` and `extract(path)`.  
   - Collect `Signal` objects. Failures are caught and logged without aborting the scan.  
   - With `--debug`, detection decisions, discovery hits, and extraction counts are printed.

5. **Graph Build** (`autodocx/graph/builder.py`)  
   - Convert `Signal` records into `Node` and `Edge` instances (`autodocx/types.py`).  
   - Node IDs follow the `Type:Name` convention (e.g., `API:Orders`).  
   - Joiners (HTTP workflows -> OpenAPI operations) are applied post-build.

6. **Distance Features** (`autodocx/features/distance_features.py`)  
   - Optional: requires `networkx`.  
   - Settings come from `distance_features` in config or defaults in the module.  
   - Computes nearest-marker distances, percentiles, anchor coverage, degree summaries, and risk hints.  
   - Results keyed by graph node ID (matching `_graph_node_id_for_signal_kind_and_props`).

7. **SIR Persistence** (`sir/*.json`)  
   - For each `Signal`, produce a sanitized filename and `sir` JSON document with fields: id, kind, name, file, component/service hints, props, roles, evidence, subscores, graph_features, generated timestamp.  
   - Roles inferred via `map_connectors_to_roles_with_evidence`, which cross-references connectors/triggers against `roles/roles.json`.

8. **Graph & Facets**  
   - Write `graph.json` with raw nodes/edges (simple `__dict__` dumps).  
   - Compute facets via `autodocx.scoring.facets.rollup_facets`. If the scorer fails or `networkx` is missing, fallback metrics ensure the run continues.

9. **Artifact Mapping** (`autodocx/artifacts/option1.py`)  
   - Signals mapped into a universal `artifacts.json` + line-delimited `artifacts.jsonl`.  
   - Captures capabilities, interfaces, workflows, data, infrastructure, operations, risks, assumptions, evidence, and computed confidence.  
   - Workflow signals add connectors, triggers, SQL snippets, and cross-flow HTTP calls.  
   - Additional derived signals (database, routes, docs) supply complementary artifacts.

10. **Rendering & MkDocs** (`autodocx/render/mkdocs.py`)  
    - `render_docs(...)` is called through `call_render_docs`, which tries multiple call signatures for compatibility.  
    - Writes `docs/index.md`, `docs/components/<group>/<component>.md`, normalizes YAML front-matter with facets and distance features, and embeds Graphviz images when present.  
    - `render/business_renderer.py` crafts business-focused content, Graph Insights tables, and evidence cross-links.  
    - Assets under `out/assets/` (e.g., Graphviz output) are mirrored into `docs/assets/`.  
    - If `--mkdocs-build` (or `docs.mkdocs_build`) is set, the CLI attempts `mkdocs build -d out/site` (best-effort).

11. **Evidence Index & Grouping** (`autodocx/llm/evidence_index.py`, `autodocx/llm/grouping.py`)  
    - Assemble `evidence_index.json` by walking SIR evidence and artifact evidence (including role-specific anchors).  
    - Group artifacts/SIRs by `component_or_service` with fallback heuristics (file path matching, else `ungrouped`).

12. **LLM Rollup** (`autodocx/llm/rollup.py`)  
    - Only if `--llm-rollup` and `OPENAI_API_KEY` is set.  
    - Normalize group payloads into dictionaries of `artifacts` and `sirs`.  
    - Compose prompts and call `autodocx.llm.provider.call_openai_meta` for group- and component-level summaries.  
    - Enforce structured outputs via JSON Schema (configured in YAML).  
    - Verify cited evidence IDs exist in `evidence_index`.  
    - Score responses (`llm_subscore`) and approve based on `publish_threshold`.  
    - Append telemetry to `metrics/llm_usage.csv` (usage tokens, cost estimates, latency).  
    - Emit business-profile Markdown pages with inline confidence blocks and evidence anchors.

13. **Finalization**  
    - Log completion with count of artifacts, nodes, edges, groups, etc.  
    - Output directory now contains SIR files, artifacts, docs, graph, metrics, optional site.

---

## 6. Core Data Contracts

- **Signal (`autodocx/types.py`)**  
  - `kind`: taxonomy (`api`, `op`, `workflow`, `event`, `db`, `infra`, `job`, `doc`, etc.).  
  - `props`: extractor-specific payload with at least `file` and a stable `name`.  
  - `evidence`: list of string or dict anchors (file, line ranges, snippets).  
  - `subscores`: partial confidence metrics (parsed, schema evidence, endpoint coverage, etc.).

- **Node / Edge (`autodocx/types.py`)**  
  - Node: `id`, `type`, `name`, `props`, `evidence`, `subscores`.  
  - Edge: `source`, `target`, `type`, `props`, `evidence`, `subscores`.

- **SIR JSON** (`out/sir/*.json`)  
  - Mirror Signal data with normalized IDs, sorted roles, `graph_features`, `generated_at`.  
  - `roles_evidence` preserves evidence per inferred role.

- **Artifacts** (`out/artifacts.json`, `.jsonl`)  
  - Fields for interfaces, workflows, data, infra, build/deploy, security, observability, dependencies, operations, risk, assumptions, confidence, evidence.

- **Graph Features** (`graph_features` within SIRs & aggregated in docs)  
  - `nearest_marker_id`, `nearest_marker_distance`, `avg_distance_to_markers`, `distance_percentiles`, `anchor_coverage`, `type_degrees`, `risk_flags`, plus metadata.

- **Evidence Index** (`out/evidence_index.json`)  
  - `id -> {path, lines, snippet}` map for cross-referencing.

- **LLM Outputs**  
  - Stored as Markdown pages (business profile) and recorded via structured JSON persisted under `out/docs` (component pages) or `out/business` (if technical profile is used).

---

## 7. Extractor Catalog

All extractors live under `autodocx/extractors/` and are registered via `pyproject.toml`. Each implements `detect`, `discover`, and `extract` methods (Protocol in `autodocx/extractors/base.py`).

| Extractor | Key Patterns | Signals & Highlights |
| --- | --- | --- |
| `OpenAPIExtractor` | `**/*.yaml`, `**/*.yml`, `**/*.json` (with `openapi`/`swagger`) | Emits `api` and `op` signals with methods, paths, summaries, servers. |
| `LogicAppsWDLExtractor` | Workflow/definition JSON files | Parses triggers, steps, connectors, HTTP calls between flows, yields `workflow` signals. |
| `K8sManifestsExtractor` | Kubernetes YAMLs | Emits `infra` signals with resource kind/name/namespace per doc. |
| `TerraformExtractor` | `**/*.tf` (requires `python-hcl2`) | `infra` signals for resources; gracefully degrades if hcl2 missing. |
| `BicepExtractor` | `**/*.bicep` | Compiles via `az bicep build` or `bicep build`; emits `infra` and nested Logic Apps workflows. |
| `AzureFunctionsExtractor` | `function.json`, `*.cs` | Detects HTTP triggers and emits `route` signals. |
| `ExpressJSExtractor` | `*.js`, `*.ts`, `package.json` | Regex for `app.get`/`router.get`, emits `route` signals with evidence ranges. |
| `AzurePipelinesExtractor` | Azure DevOps pipeline YAML | Emits `job` signals with schedules, inferred environments, CI system. |
| `GitHubActionsExtractor` | `.github/workflows/*.yml` | Same as above but for GitHub Actions. |
| `SQLMigrationsExtractor` | `*.sql` | Extracts `CREATE TABLE` statements into `db` signals. |
| `MarkdownDocsExtractor` | `*.md`, `*.markdown` | Emits `doc` signals tagging READMEs/ADRs. |
| `TibcoBWExtractor` | `*.bwp`, `*.process`, BW-flavored XML | **Primary TIBCO support**. Parses with `lxml`, extracts process name, triggers, steps (with connectors, SQL, HTTP endpoints), cross-process calls, role hints (via connectors), SQL statements, derived `db` and `route` signals. Redacts snippets via `autodocx.utils.redaction`. |
| `TibcoProjectArtifactsExtractor` | WSDL, XSD, "Service Descriptors" JSON/XML | Emits `api` for OpenAPI-like JSON, `doc` for descriptors and schema summaries. |
| `Azure Functions`, `LogicApps`, `Bicep` | cooperate to capture hybrid Azure/TIBCO integration scenarios. |

**TIBCO specifics**:
- `_make_snippet` redacts sensitive material.  
- `_find_repo_root` climbs until `.git` or `Workflows/`.  
- Steps enriched with connector type, SQL, HTTP paths, call-process targets.  
- Connectors feed into role inference and Graphviz visuals.

Extractor extensibility: add a new module, implement the `Extractor` protocol, and register it under `[project.entry-points."autodocx.extractors"]` in `pyproject.toml`.

---

## 8. Graph Assembly, Joiners, and Features

- Graph builder (`autodocx/graph/builder.py`) ensures each `Signal` yields exactly one node with consistent IDs.  
- For operations referencing APIs, edges of type `exposes` connect operations to owning APIs.  
- Workflow HTTP calls linking to OpenAPI routes are added by `autodocx/joiners/http_calls.link_workflows_to_openapi`, which matches step URLs against known `(method, path)` combinations.  
- `autodocx/joiners/events.join_events` is currently a placeholder for future producer/consumer linking.

**Distance features** (optional but recommended):
- Controlled by `distance_features` config block.  
- Graph markers chosen based on node type and degree; markers highlight key workflows/APIs in visuals.  
- Output stored per-node in SIRs and aggregated per-component through business renderer.  
- Risk hints flag articulation nodes (potential single points of failure).

---

## 9. Role Inference

`autodocx/utils/roles.py` loads `roles/roles.json`, mapping connector prefixes (e.g., `http`, `jdbc`, `jms`) to normalized roles (`interface.receive`, `data.jdbc`, etc.).  
Roles are attached to SIRs with the evidence that justified them.  
You can update `roles.json` to support new connectors; keep the prefix keys lowercase and evidence-friendly.

---

## 10. Artifact Mapping (Option 1)

`autodocx/artifacts/option1.py` standardizes Signals into a "Option 1" schema:

- Service is inferred from first path segment relative to scan root.  
- Fields cover capabilities, interfaces, workflows, data schemas, infrastructure, CI/CD, security, observability, dependencies, operations, risk, assumptions.  
- Confidence is derived from Signal subscores (`parsed` and `schema_evidence`).  
- Workflows include triggers, step summaries (connector-labeled), cross-flow HTTP calls, and connector-based capability hints.  
- SQL and DB signals enrich data/dependency sections automatically.

You can adapt or fork this mapping to integrate with downstream consumers (e.g., Confluence, reporting APIs).

---

## 11. Rendering, Visuals, and MkDocs

- `autodocx/render/mkdocs.py` produces Markdown under `out/docs/`.  
- `docs/index.md` summarizes facets and lists components.  
- Group pages include aggregated Graph Insights, embedded flow diagrams, YAML front-matter for MkDocs navigation, and links to component-level pages.  
- Component pages (generated by `business_renderer.render_business_component_page`) include:  
  - Confidence badges and explanation.  
  - Overview, flow tables, interdependency summaries, Graph Insights tables.  
  - Evidence callouts linking to shared evidence markdown (if produced).  
  - Visual flow diagrams (Graphviz).  
  - Business value, known unknowns, and details per claim with evidence anchors.  
- Graphviz integration (`autodocx/visuals/graphviz_flows.py`):  
  - `render_bw_process_flow_svg` (per SIR) and `render_component_overview_svg` (per component) rely on Graphviz Digraph.  
  - Assets stored under `out/assets/graphs/<group>/<component>/*.svg` and copied into `docs/assets`.  
  - Marker highlighting uses the distance-features marker set.

**MkDocs build**: optionally run automatically (if CLI flag enabled), otherwise you can run `mkdocs build -d out/site` manually inside the project root (requires `mkdocs.yml` in `out/`).

---

## 12. LLM Rollup Deep Dive

Located in `autodocx/llm/rollup.py`, this module:

1. **Loads settings** from `autodocx.yaml` (model, provider, thresholds, telemetry).  
2. **Builds prompts** using group context (artifacts + SIRs) and evidence index references.  
3. **Calls OpenAI Responses API** via `autodocx/llm/provider.call_openai_meta`, honoring structured output requirements.  
4. **Validates JSON** against strict schemas (component-level and group-level).  
5. **Scores** results using `_compute_llm_subscore` (alignment with evidence) and applies publish thresholds.  
6. **Persists** Markdown (business profile) with YAML front-matter and evidence cross-links.  
7. **Logs telemetry** to `metrics/llm_usage.csv` (scope, tokens, costs, latency). Pricing uses values from config (`llm.telemetry.pricing`).

Important safeguards:
- Unknown evidence IDs raise exceptions unless `rollup.allow_unknown_evidence` is `true`.  
- Structured outputs require OpenAI's JSON schema feature (enabled via config).  
- Without `OPENAI_API_KEY`, rollup is skipped with a warning.

---

## 13. Output Layout

Assuming default `out_dir: out`:

```
out/
+-- artifacts.json / artifacts.jsonl
+-- docs/
|   +-- index.md
|   +-- components/<component>/<page>.md
|   +-- assets/graphs/...
+-- evidence_index.json
+-- graph.json
+-- metrics/llm_usage.csv (appended per rollup call)
+-- mkdocs.yml (preserved across runs if present)
+-- sir/*.json
+-- site/ (MkDocs static site when built)
```

Artifacts, graph, SIRs, and evidence index form the core machine-readable outputs. Docs, assets, and site enable human consumption.

---

## 14. Extending the System

### New Extractor
1. Create `autodocx/extractors/<name>.py`.  
2. Implement `name`, `patterns`, `detect`, `discover`, `extract`. Return `Signal` objects.  
3. Register under `[project.entry-points."autodocx.extractors"]` in `pyproject.toml`.  
4. Run `pip install -e .` to refresh entry points.  
5. Add tests capturing expected Signals (use fixture repos where possible).

### Custom Joiners / Graph Logic
- Add modules under `autodocx/joiners/` and call them from `autodocx/graph/builder.py`.  
- For example, linking message queue producers/consumers or aligning DB schemas to workflows.

### Artifact Mapping Adjustments
- Modify `autodocx/artifacts/option1.py`, or add new mappers and call them from the CLI after signal extraction.  
- Keep evidence and confidence data intact so downstream consumers can trust outputs.

### Renderer Enhancements
- `autodocx/render/mkdocs.py` and `business_renderer.py` are the main touchpoints.  
- Add new sections, restructure pages, or introduce new templates.  
- Ensure `tests/test_renderer_frontmatter.py` continues to pass.

### LLM Provider Swaps
- Extend `autodocx/llm/provider.py` to support additional providers.  
- Update `autodocx/llm/rollup.py` to branch on provider settings.

### Graph Features
- Adjust `distance_features` configuration in `autodocx.yaml` (`enabled`, `edge_weights`, `marker_strategy`, `radius`).  
- For advanced analytics, extend `_risk_flags` or `select_markers` in `distance_features.py`.

---

## 15. Testing & QA

- **Primary tests**: `pytest` (focuses on front-matter and marker matching).  
- **Manual verification**:  
  1. Run `autodocx scan <repo> --debug --mkdocs-build --llm-rollup`.  
  2. Inspect `out/docs/index.md`, component pages, embedded SVGs.  
  3. Validate `artifacts.json` against downstream schema (if applicable).  
  4. Open `out/site/index.html` if MkDocs build succeeded.  
  5. Review `metrics/llm_usage.csv` for cost auditing.  
  6. Spot-check SIRs for accurate roles and graph features.

Consider adding fixtures under `tests/` for new extractors or renderers to keep coverage aligned with additions.

---

## 16. Operational Checklist & Troubleshooting

- **Before running scans**:
  - Activate virtualenv and ensure dependencies installed.  
  - Confirm `OPENAI_API_KEY` in environment or repo `.env`.  
  - Verify Graphviz `dot` is on `PATH`.  
  - Ensure `autodocx.yaml` is present and valid.

- **Common issues**:
  - *Extractor missing hits*: enable `--debug`, inspect detection logs, adjust `patterns` or `detect`.  
  - *Graph features missing*: ensure `networkx` installed and `distance_features.enabled` is true.  
  - *Graphviz errors*: install Graphviz CLI; check that `docs.profile` demands visuals.  
  - *LLM rollup failure citing unknown evidence*: inspect `evidence_index.json`, adjust config or evidence IDs.  
  - *MkDocs build failure*: confirm `mkdocs.yml` (kept in `out/`) is valid and `mkdocs` installed.  
  - *Unicode artifacts in business renderer*: some placeholder characters in `business_renderer.py` may need cleanup if they surface in rendered docs -- verify Markdown output and plan a normalization pass.  
  - *Bicep extraction failing*: ensure `az` CLI or `bicep` CLI installed; extractor logs downgrade to doc signals when compilation unavailable.

- **Cleaning outputs**: CLI automatically prunes `out/` while preserving `site/`, `mkdocs.yml`, and `metrics/llm_usage.csv`.

---

## 17. Legacy Script

`scripts/legacy/autodocx_mono.py` represents the previous monolithic scanner. It is retained only for historical reference. New development should exclusively use the modular CLI (`autodocx_cli/__main__.py`).

---

## 18. Onboarding Action Plan

1. **Setup**: install dependencies, verify config, run `pytest`.  
2. **Dry run**: `autodocx scan repos/sample --debug --mkdocs-build` (use included `repos/...` data if available).  
3. **Understand outputs**: walk through SIRs, artifacts, docs, and graph JSON.  
4. **Trace data flow**: pick a TIBCO workflow and follow it from extractor output -> SIR -> artifact -> component page.  
5. **Explore LLM rollup**: run with `--llm-rollup`, review generated Markdown, examine telemetry.  
6. **Extend**: add or tweak an extractor/renderer/test to get comfortable with the development cycle.

After these steps, you should be fully equipped to maintain or enhance AutodocX.

---

## 19. Quick Reference Commands

- Install in editable mode: `pip install -e .`  
- Run scan: `autodocx scan <repo> --out out --debug --mkdocs-build --llm-rollup`  
- Run tests: `pytest`  
- Clean outputs manually (if needed): delete `out/` contents except `mkdocs.yml`, `site/`, `metrics/llm_usage.csv` (the CLI does this automatically).  
- View generated docs: open `out/docs/index.md` or serve `out/site/` after MkDocs build.

---

Keep this document updated as you add features or adjust workflows so future developers can onboard just as quickly.
