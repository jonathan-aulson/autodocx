# AutodocX Developer Onboarding Context

This document equips you to extend, debug, and operate the AutodocX documentation pipeline without ambiguity. Read it once, keep it handy, and you should be able to fix, extend, diagram, or explain any part of the system.

---

## 1. Mission and High-Level Flow

AutodocX transforms heterogeneous source repositories into evidence-backed documentation:

1. **Discover** relevant files via extractor plugins.  
2. **Extract** structured signals (SIRs) with provenance.  
3. **Assemble** a knowledge graph and compute quality metrics.  
4. **Map** signals into universal artifacts and evidence indexes.  
5. **Synthesize** LLM-authored workflow diagrams plus doc-context/doc-plan scaffolding (process → family → component → repo).  
6. **Fulfill** every doc plan entry via LLM, then optionally run additional `--llm-rollup` summaries (post-doc).  
7. **Publish** curated Markdown, regenerated MkDocs nav, assets, and metrics under the configured `out` directory.

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
   - Run `az bicep version` **and** `bicep --version` after provisioning to confirm both commands are on `PATH`; the Bicep extractor now relies on those binaries and falls back to doc-only output when neither command is available.  
3. **Virtual environment**:  
   - Windows/PowerShell: `python -m venv .venv && .\.venv\Scripts\activate`.  
   - Linux/WSL: `python3.10 -m venv .venv && source .venv/bin/activate`. Keep the repo under `/home/<user>/...` (not `/mnt/c`) for faster scans.
4. **Install**: `pip install -e .` (loads package and entry points). Use `pip install -e .[treesitter]` on Linux/WSL so tree-sitter wheels are available.
5. **Config file**: `autodocx.yaml` in repo root (see Section4).  
6. **Secrets**: `.env` with `OPENAI_API_KEY=...` for LLM-enabled runs (set `AUTODOCX_LLM_MODEL` to override the YAML model per run, e.g., `gpt-5.1`).  
7. **Optional env toggles**:  
   - `AUTODOCX_CONFIG` -> override config path.  
   - `AUTODOCX_EXTRACTORS_INCLUDE` / `AUTODOCX_EXTRACTORS_EXCLUDE` -> comma-separated class or fully-qualified names for plugin selection.  
   - `AUTODOCX_DEBUG_EXTRACTORS=1` -> verbose extractor logging.
8. **Tree-sitter language packs (optional)**: Install `tree-sitter-languages` only when your OS/architecture provides wheels (Linux/macOS/WSL). Use `pip install -e .[treesitter]` or `pip install tree-sitter-languages` manually. Windows users can skip it—the AST extractors auto-disable if the module is missing.

> **WSL bootstrap:** Run `./scripts/setup_wsl.sh` to install `python3.10-venv`, dev headers, `graphviz`, fonts, `mkdocs`, and optional Azure CLI/Bicep. The script also reminds you to export `OPENAI_API_KEY`. If you prefer manual steps, install the packages listed in the README before creating your virtual environment.

> **Doctor command:** Use `python -m autodocx_cli doctor` to verify Graphviz, MkDocs, Azure CLI/Bicep, and `OPENAI_API_KEY` availability before long scans. The command exits non-zero when required tooling is missing so you can fix issues up front.

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

10. **Doc context, plan, and MkDocs nav** (`autodocx/docplan/*`, `autodocx/llm/*`, `autodocx_cli/__main__.py`)  
    - `build_doc_context(...)` consolidates SIR/SIRv2 paths, artifacts, diagram assets, interdependency slices, and facets per component/family plus a repo rollup, then writes `out/doc_context.json`.  
    - `generate_llm_workflow_diagrams(...)` calls the LLM to merge related workflows into Graphviz DOT, renders SVGs via `dot`, and saves them under `out/assets/diagrams_llm/**` (these paths are appended back into the context).  
    - `draft_doc_plan(...)` enumerates every process, every detected family, and the repo overview into `out/docs/dox_draft_plan.md` so the workflow always has a deterministic set of documents to fulfill.  
    - `fulfill_doc_plan(...)` loads that plan + context, assembles rich LLM prompts (SIR JSON excerpts, interdependencies, SVG references, artifacts, extrapolations), trims each payload to ~60k characters, enforces the `AUTODOCX_SECTION_MIN_WORDS` floor via `_generate_with_retries`, and writes curated Markdown under `out/docs/curated/**`.  
    - `sync_docs_assets` mirrors `out/assets/**` into `out/docs/assets/**`, and `regenerate_mkdocs_config` rewrites `out/mkdocs.yml` so the nav only references fresh curated pages (components/, families/, repo_overview.md).  
    - If `--mkdocs-build` (or `docs.mkdocs_build`) is set, the CLI runs `mkdocs build -d out/site` using the regenerated config; failures are non-fatal but logged.

11. **Evidence Index & Grouping** (`autodocx/llm/evidence_index.py`, `autodocx/llm/grouping.py`)  
    - Assemble `evidence_index.json` by walking SIR evidence and artifact evidence (including role-specific anchors).  
    - Group artifacts/SIRs by `component_or_service` with fallback heuristics (file path matching, else `ungrouped`).

12. **LLM Rollup (optional additive summaries)** (`autodocx/llm/rollup.py`)  
    - Runs *only* when `--llm-rollup` is passed and `OPENAI_API_KEY` is set; the core documentation already relies on LLM fulfillment earlier in the pipeline.  
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

---

## 8. Workflow Diagram Metadata (Ports & Control Edges)

Graphviz diagrams now support **port-aware routing** so branch edges land on the correct visual exit. Extractors that emit `workflow` signals must populate the following fields so the renderer can generate meaningful ports:

- **`steps[*].run_after`**: existing array of predecessor names (string or list). Required for sequential edges. Keep names stable (case-sensitive) so we can map them back to the `steps[*].name`.
- **`control_edges`**: list of `{ "parent": "<step name>", "branch": "<label>", "children": ["<step name>", ...] }`.  
  - `parent` should be the exact `steps[*].name` of the control node (e.g., Logic Apps `Switch`, Power Automate `Condition`, custom scope).  
  - `branch` is the human-readable label (“Success”, “Failure”, “Case: Premium”) and becomes the out-port label.  
  - `children` is one or more step names entered when that branch executes.
- **Loop metadata**: for `Apply to each` / `Foreach`, continue emitting entries in `control_edges` so we can expose `loop_<slug>` ports. If a connector does not expose branch names, emit a synthetic label like `"branch": "Each item"` so the renderer can still create a unique port.

During export (`autodocx/visuals/flow_export.py`):

- Node IDs are generated via `_node_id("step", name)` and **must not contain colons**. If you add new extractors, reuse `_safe_slug` to keep IDs deterministic.
- Every node automatically receives default ports `in_main` and `out_default`. Control nodes gain additional `branch_<slug>` ports based on `control_edges`. External relationship nodes expose `in_external`/`out_external`.
- Edges now include `source_port` / `target_port` fields. Sequential/trigger edges map to the default ports, branch edges map to their branch port, and external edges land on `in_external`.

### How to supply metadata from an extractor

1. **Populate `steps[*]`:** Ensure each step name is unique within the workflow and add `control_type` (e.g., `"if"`, `"switch"`, `"foreach"`) so we know when to expect branch ports.
2. **Emit `control_edges`:** Whenever the source format defines branches (Logic Apps `runAfter`, BPEL transitions, etc.), convert them into the normalized structure above. You do **not** need to emit port names—use the branch labels and the exporter will slug them automatically.
3. **Validate with tests:** Add or update extractor tests to confirm the resulting SIR has `control_edges`. You can then run `python -m pytest tests/test_flow_export_ports.py` to verify the branch shows up with `source_port`.

Following this contract ensures new technologies (e.g., PowerBuilder or Azure Logic Apps) automatically benefit from port-aware diagrams without additional renderer changes.
| `ExpressJSExtractor` | `*.js`, `*.ts`, `package.json` | Regex for `app.get`/`router.get`, emits `route` signals with evidence ranges. |
| `AzurePipelinesExtractor` | Azure DevOps pipeline YAML | Emits `job` signals with schedules, inferred environments, CI system. |
| `GitHubActionsExtractor` | `.github/workflows/*.yml` | Same as above but for GitHub Actions. |
| `SQLMigrationsExtractor` | `*.sql` | Extracts `CREATE TABLE` statements into `db` signals. |
| `MarkdownDocsExtractor` | `*.md`, `*.markdown` | Emits `doc` signals tagging READMEs/ADRs. |
| `TibcoBWExtractor` | `*.bwp`, `*.process`, BW-flavored XML | **Primary TIBCO support**. Parses with `lxml`, extracts process name, triggers, steps (with connectors, SQL, HTTP endpoints), cross-process calls, role hints (via connectors), SQL statements, derived `db` and `route` signals. Redacts snippets via `autodocx.utils.redaction`. |
| `TibcoProjectArtifactsExtractor` | WSDL, XSD, "Service Descriptors" JSON/XML | Emits `api` for OpenAPI-like JSON, `doc` for descriptors and schema summaries. |
| `AWSLambdaExtractor` | SAM/CloudFormation templates, `serverless.yml` | Emits `workflow` signals for `AWS::Lambda::Function` resources and Serverless Framework functions, capturing runtime, handler, triggers, and event relationships (HTTP, schedule, S3, streams, etc.). |
| `TypeScriptProjectExtractor` | `tsconfig.json`, `package.json` (when `typescript` dependency detected) | Emits `doc` signals summarizing compiler options, framework hints, and TypeScript-centric scripts to help renderers/LLMs understand build behavior. |
| `Azure Functions`, `LogicApps`, `Bicep` | cooperate to capture hybrid Azure/TIBCO integration scenarios. |
| `MuleSoftExtractor` | Mule `.xml` flows | Parses HTTP listeners/requests, DB/JMS connectors, and flow references to populate identifiers, datastores, and process dependencies. |
| `BizTalkLogicAppsExtractor` | BizTalk `.odx`, Logic Apps Standard `workflow.json`, Durable orchestrations | Extracts triggers/actions/callouts from hybrid workflows, capturing service/datastore usage plus downstream orchestration calls. |

### Business-scaffold signal contract

Every extractor that emits `workflow`, `route`, or `operation` signals must provide the inputs needed by `autodocx/scaffold/signal_scaffold.py`:

- `triggers`: include the trigger type, method, path/url, and evidence pointer.
- `steps`: each step should record `name`, `connector`, `datasource` or `destination`, `inputs_keys`, and any mapper/JDBC hints so we can derive identifiers and datastores.
- `relationships`: normalized edges describing downstream processes, services, or datastores (`calls`, `invokes`, `reads`, `writes`, `publishes`, etc.).
- Identifier hints: populate at least one of `identifiers`, `identifier_hints`, `primary_keys`, or `foreign_keys`.
- Dependency hints: supply `datasource_tables`, `process_calls`, and `service_dependencies` when those facts are observable (e.g., SQL statements, `CallProcess` activities, JMS destinations).

The scan now writes `out/scaffold_coverage.json` and `out/scaffold_coverage.csv`, plus `scripts/check_scaffold_coverage.py` will summarize which extractors emitted signals missing identifiers/datastores/processes. Run the script after a scan to keep business scaffolds complete.

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
