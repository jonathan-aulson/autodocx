# AutodocX. Prompt Library and Usage Guide

This guide consolidates the canonical reference files, modular prompt library, and execution playbook needed to reproduce an AutodocX.-style documentation pipeline. Use it alongside the files mirrored into `Example_Artifacts/` to plan, implement, test, and operate an evidence-backed documentation tool on any tech stack.

---

## 1. How to Use This Package

1. **Review canonical references** in `Example_Artifacts/` to understand concrete implementations.
2. **Read the module and data contract overview** to align your system architecture.
3. **Follow the recommended execution playbook** to run prompts in order.
4. **Issue prompts individually** to your preferred coding assistant, providing context (business goals, sample repos, target stack).
5. **Save generated outputs** in your project repo, adapting frameworks and languages as needed.
6. **Use meta-prompts** for reviews and fast iterations, and keep operational guidance handy for deployment.

> Note: When invoking prompts, include the relevant reference snippets from `Example_Artifacts/` so the assistant mirrors the intended behavior.

---

## 2. Canonical References

- `Example_Artifacts/__main__.py` – CLI bootstrap and pipeline orchestration (`autodocx_cli/__main__.py`).
- `Example_Artifacts/config_loader.py` – Configuration contract and validation (`autodocx/config_loader.py`).
- `Example_Artifacts/types.py` – `Signal`, `Node`, `Edge` data classes (`autodocx/types.py`).
- `Example_Artifacts/base.py`, `Example_Artifacts/openapi.py`, `Example_Artifacts/tibco_bw.py`, `Example_Artifacts/logicapps.py` – Extractor protocol and key implementations (`autodocx/extractors/`).
- `Example_Artifacts/builder.py` – Signal to graph conversion (`autodocx/graph/builder.py`).
- `Example_Artifacts/http_calls.py` – Joiner linking workflows to HTTP operations (`autodocx/joiners/http_calls.py`).
- `Example_Artifacts/distance_features.py` – Graph analytics (`autodocx/features/distance_features.py`).
- `Example_Artifacts/facets.py` – Documentation scoring (`autodocx/scoring/facets.py`).
- `Example_Artifacts/option1.py` – Signal to artifact mapping (`autodocx/artifacts/option1.py`).
- `Example_Artifacts/mkdocs.py`, `Example_Artifacts/business_renderer.py`, `Example_Artifacts/graphviz_flows.py` – Markdown rendering and visuals (`autodocx/render/` and `autodocx/visuals/`).
- `Example_Artifacts/evidence_index.py`, `Example_Artifacts/grouping.py`, `Example_Artifacts/rollup.py`, `Example_Artifacts/provider.py` – LLM evidence, grouping, rollup, and provider abstraction (`autodocx/llm/`).
- `Example_Artifacts/roles.py`, `Example_Artifacts/roles.json` – Role inference mapping (`autodocx/utils/roles.py`, `autodocx/roles/roles.json`).
- `Example_Artifacts/test_renderer_frontmatter.py`, `Example_Artifacts/test_visual_marker_matching.py` – Renderer and visuals tests (`tests/`).
- `Example_Artifacts/autodocx.yaml` – Sample configuration (`autodocx.yaml`).
- `Example_Artifacts/01-CORE-STANDARDS.json`, `Example_Artifacts/artifacts.json` – Example SIR and artifact outputs (`out/sir`, `out/artifacts.json`).

Use these files to supply concrete examples when tailoring prompts for new stacks.

---

## 3. Module & Data Contract Overview

- **Signals (`Example_Artifacts/types.py`)** contain `kind`, `props`, `evidence`, `subscores`.
- **Graph nodes/edges** represent relationships between signals.
- **Pipeline steps** (see `__main__.py`):
  1. Load environment/config.
  2. Run extractors.
  3. Build graph and compute distance features.
  4. Write SIR files (`out/sir`).
  5. Map artifacts to JSON/JSONL.
  6. Render docs and visuals.
  7. Build evidence index and group components.
  8. Run optional LLM rollup and telemetry.
- **Config (`autodocx.yaml`)** defines engine plugins, docs profile, LLM settings, rollup thresholds, and output directory.
- **Artifacts (`option1.py`)** aggregate capabilities, interfaces, workflows, infra, operations, risk, and evidence.
- **Evidence index (`evidence_index.py`)** maps SIR/artifact evidence to stable identifiers for rollups.
- **Telemetry** (LLM usage) recorded in metrics CSV for cost tracking.

---

## 4. Prompt Library

Prompts are organized by workflow. Each entry includes persona, purpose, usage, acceptance, and key references. Paste each block into your assistant with relevant context files.

### A. Discovery & Kickoff (Architect / Tech Lead)

**DISC-01 – Architecture Brief**

Purpose: Define goals and pipeline overview.  
Inputs: Business goals, tech stack summary.  
Outputs: Markdown brief (Goals, Pipeline Overview, Risks & Mitigations, Immediate Decisions).  
Acceptance: Mentions all pipeline stages and evidence-first requirements.  
References: `Example_Artifacts/__main__.py`, `Example_Artifacts/autodocx.yaml`.

```
You are acting as the lead architect for an evidence-driven documentation pipeline.
1. Summarize business goals, tech stacks, and compliance constraints supplied in the context.
2. Outline the end-to-end pipeline stages (discovery -> extraction -> graph -> artifacts -> docs -> rollup -> ops), adapting them to the target stack.
3. Call out critical risks and mitigation ideas, especially around evidence capture and config validation.
4. Produce a Markdown brief with sections: Goals, Pipeline Overview, Risks & Mitigations, Immediate Decisions.
Ensure the design mirrors the modular approach used in the reference implementation (CLI orchestrator, plugin extractors, renderer, LLM rollup).
```

**DISC-02 – Delivery Backlog**

Purpose: Convert architecture into epics/stories.  
Inputs: Architecture brief.  
Outputs: Ordered backlog with epics and stories.  
Acceptance: At least six epics aligned to pipeline stages.  
References: `Example_Artifacts/__main__.py`, `Example_Artifacts/base.py`.

```
Convert the architecture brief into an ordered delivery backlog.
For each pipeline stage (extractors, graph/joiners, artifacts, rendering, LLM, CLI, QA, ops):
- Produce one epic with 3–5 stories.
- Each story must include a definition of done tied to evidence-first outputs.
Return Markdown with sections per epic, using checklists for stories.
```

**DISC-03 – Data Classification Policy**

Purpose: Establish evidence handling and retention.  
Inputs: Compliance notes.  
Outputs: Policy memo (Markdown).  
Acceptance: Covers PII, redaction, telemetry retention.  
References: `Example_Artifacts/tibco_bw.py`, `Example_Artifacts/roles.json`, `Example_Artifacts/roles.py`.

```
Draft a policy memo describing how extracted evidence and telemetry will be handled.
Include:
- Data categories (code snippets, configs, credentials, PII).
- Redaction strategy (refer to the redact helper in the reference project).
- Retention & deletion timelines for output directories and metrics.
- Controls for LLM telemetry (usage logs, cost data).
Format as Markdown with tables for classification vs retention.
```

### B. Extractor Design & Implementation (Extractor Engineer)

**EXT-01 – Extractor Contract**

Purpose: Define detect/discover/extract contract.  
References: `Example_Artifacts/base.py`, `Example_Artifacts/openapi.py`.  

```
You are defining the extractor plugin contract for the <TARGET STACK>.
Using the reference protocol (detect/discover/extract) as inspiration:
1. Describe expected inputs/outputs for each method and required error handling.
2. Specify evidence formatting rules (path+line ranges, snippets, subscores).
3. Document configuration hooks (filters, size limits).
4. Produce a README section titled "Extractor Contract" plus a sample interface in the target language.
```

**EXT-02 – Extractor Scaffold**

References: `Example_Artifacts/tibco_bw.py`, `Example_Artifacts/logicapps.py`.

```
Generate a code scaffold for a new extractor that targets <ARTIFACT TYPE>.
- Include detect(), discover(), extract() stubs with docstrings.
- Add TODO comments for content sniffing and evidence construction.
- Create a minimal unit test file with fixtures referencing sample inputs.
Use idioms consistent with the target language and reference extractor patterns.
Return both the extractor file and test file in Markdown code blocks.
```

**EXT-03 – Extractor Implementation**

References: `Example_Artifacts/tibco_bw.py`, `Example_Artifacts/roles.py`.

```
Complete the extractor implementation for <ARTIFACT TYPE>.
Requirements:
- Parse key metadata (names, triggers, connectors, nested calls).
- Construct Signal objects with kind, props, evidence, subscores.
- Map connectors to roles when available.
- Deduplicate evidence anchors and redact sensitive text.
Return the final code, plus a JSON example of emitted Signals.
```

**EXT-04 – Plugin Registration**

References: `Example_Artifacts/base.py`, `Example_Artifacts/__main__.py`, `Example_Artifacts/autodocx.yaml`.

```
Show how to register the new extractor module <MODULE NAME> with the <PACKAGE MANAGER>.
Include:
- Dependency declaration.
- Entry point or plugin registration statement.
- Notes on semantic versioning and activation via environment variables.
Provide the snippet and a short README note for installation steps.
```

### C. Graph & Joiners (Graph Engineer)

**GRAPH-01 – Schema Design**

References: `Example_Artifacts/types.py`, `Example_Artifacts/builder.py`.

```
Design the graph representation for the extracted Signals.
- Specify Node and Edge fields (ID scheme, type taxonomy, props, evidence).
- Provide pseudo-code mapping Signals to Nodes/Edges, including operation->API links.
- Call out how to handle duplicates and missing relationships.
Deliver Markdown with schema tables plus the mapping algorithm.
```

**GRAPH-02 – Graph Builder**

References: `Example_Artifacts/builder.py`, `Example_Artifacts/http_calls.py`.

```
Implement `build_graph(signals)` for the target stack.
- Normalize IDs using consistent prefixes.
- Attach edges for relationships (e.g., operation exposed by API).
- Leave a hook to call joiners after initial graph is built.
Return the function and a sample unit test verifying node counts and edge labels.
```

**GRAPH-03 – Workflow/API Joiner**

References: `Example_Artifacts/http_calls.py`, `Example_Artifacts/tibco_bw.py`.

```
Write a joiner that inspects workflow steps and links them to API nodes when URLs match.
- Accept nodes/edges, return updated lists.
- Use parsed HTTP method/path information to match operations.
- Preserve evidence arrays for traceability.
Include test cases covering: exact match, no match, malformed URLs.
```

**GRAPH-04 – Distance Features**

References: `Example_Artifacts/distance_features.py`, `Example_Artifacts/facets.py`.

```
Design a distance-feature module for the graph.
- Outline configuration (enabled flag, edge weights, marker strategy).
- Produce pseudo-code using a graph library to compute nearest markers, percentiles, risk flags.
- Document how results attach back to SIRs.
Return Markdown plus sample config YAML fragment.
```

### D. Artifact Mapping (Knowledge Engineer)

**ART-01 – Artifact Schema**

References: `Example_Artifacts/option1.py`, `Example_Artifacts/artifacts.json`.

```
Design the artifact.schema.json for this documentation pipeline.
- Enumerate required and optional fields mirroring the universal schema (capabilities, interfaces, workflows, data, infra, dependencies, risk, evidence, confidence).
- Include types, descriptions, and example values.
- Highlight how evidence anchors link back to source files.
Provide the JSON schema and a short commentary.
```

**ART-02 – Mapping Function**

References: `Example_Artifacts/option1.py`.

```
Implement the `to_universal_artifact(signal, repo_root)` function.
Requirements:
- Derive component/service names from file paths.
- Populate interfaces/workflows based on signal kind.
- Aggregate evidence and compute a confidence score.
Return the function, plus tests asserting mapping for API, workflow, and doc signals.
```

**ART-03 – Contributor Guide**

References: `Example_Artifacts/option1.py`, `Example_Artifacts/roles.py`.

```
Write contributor documentation describing how signals are transformed into artifacts.
Include sections on:
- Service inference from repo path segments.
- Connector-to-role mapping and capabilities.
- Evidence aggregation and confidence calculation.
- How to override heuristics for new stacks.
```

### E. Rendering & Visualization (Docs Engineer)

**RENDER-01 – Layout Spec**

References: `Example_Artifacts/mkdocs.py`, `Example_Artifacts/business_renderer.py`.

```
Define the Markdown output structure for the documentation portal.
- Specify index page sections (metrics, component list).
- Outline component page sections (front-matter, overview, graph insights, evidence).
- Detail how assets (SVGs) will be referenced.
Return as a short design doc with diagrams if helpful.
```

**RENDER-02 – Renderer Implementation**

References: `Example_Artifacts/business_renderer.py`, `Example_Artifacts/test_renderer_frontmatter.py`.

```
Implement functions to render component and group Markdown files.
- Accept artifacts, SIRs, facets, graph features.
- Emit YAML front-matter including score and markers.
- Generate sections for overview, interdependencies, graph insights, evidence.
Return code plus unit tests verifying front-matter keys and table content.
```

**RENDER-03 – Visual Assets**

References: `Example_Artifacts/graphviz_flows.py`.

```
Create a visualization helper that renders workflow/process flows.
- Build a DAG per component, highlight marker nodes.
- Save SVG/PNG to assets directory, returning relative paths.
- Provide fallback no-op behavior when graph library is missing.
Include tests or snapshots verifying file creation.
```

### F. LLM Rollup & Evidence (LLM Specialist)

**LLM-01 – Evidence Index**

References: `Example_Artifacts/evidence_index.py`.

```
Design and implement `build_evidence_index(out_dir)` for the target system.
- Aggregate evidence from SIRs and artifacts.
- Assign stable keys for each anchor.
- Persist to a JSON file for reuse.
Provide the function and explain how unknown evidence is handled.
```

**LLM-02 – Component Grouping**

References: `Example_Artifacts/grouping.py`.

```
Write a grouping utility that clusters artifacts and SIRs by component/service.
- Primary key from component metadata.
- Fallback to path prefix matching.
- Ensure ungrouped bucket exists.
Return function and unit tests covering named and fallback cases.
```

**LLM-03 – Prompt Template**

References: `Example_Artifacts/rollup.py`, `Example_Artifacts/provider.py`.

```
Design a structured LLM prompt template for component rollups.
Include:
- Summary of provided context (artifacts, SIRs, evidence index).
- Explicit instructions to cite evidence IDs.
- JSON schema specification for the response.
- Safety instructions for unknown evidence or missing data.
Return the prompt template and accompanying schema definition.
```

**LLM-04 – Rollup Execution**

References: `Example_Artifacts/rollup.py`, `Example_Artifacts/provider.py`.

```
Implement the rollup pipeline:
- Iterate groups, normalize artifacts/SIRs.
- Call LLM provider with structured prompt and schema.
- Validate output, compute confidence score, apply publish thresholds.
- Persist Markdown/JSON outputs and append telemetry (tokens, cost, latency).
Provide code or detailed pseudocode plus unit tests for error paths (missing evidence, schema failure).
```

### G. CLI Orchestration & Config (Platform Engineer)

**CLI-01 – CLI Design Doc**

References: `Example_Artifacts/__main__.py`, `Example_Artifacts/config_loader.py`.

```
Document the CLI design for the documentation engine.
- Commands/subcommands and default behavior.
- Flag vs config precedence.
- Environment loading strategy (.env, repo-level .env).
- Out directory cleanup preservation rules.
Return as Markdown targeting developers and ops.
```

**CLI-02 – CLI Implementation**

References: `Example_Artifacts/__main__.py`.

```
Implement the CLI entrypoint with argparse (or equivalent) for the target language.
- Parse `scan` command and flags (--out, --debug, --mkdocs-build, --llm-rollup).
- Load config, determine effective out directory, clean outputs.
- Run pipeline stages sequentially with logging.
Return the code, plus a smoke test invoking the CLI against a fixture repo.
```

**CLI-03 – Config Loader**

References: `Example_Artifacts/config_loader.py`, `Example_Artifacts/autodocx.yaml`.

```
Implement config loading and validation.
- Resolve config path from env override or default.
- Parse YAML/JSON, validate required sections (LLM, rollup, out_dir).
- Provide helper to access full settings and LLM-specific settings.
Return code and tests covering missing file, malformed YAML, missing keys.
```

### H. Testing & QA (QA Engineer)

**QA-01 – Extractor Unit Tests**

References: `Example_Artifacts/openapi.py`, `Example_Artifacts/tibco_bw.py`, `Example_Artifacts/test_renderer_frontmatter.py`.

```
Write unit tests for the <EXTRACTOR NAME> extractor.
- Provide fixtures referencing sample repo files.
- Assert detect/discover behaviors and resulting Signal fields.
- Validate evidence paths and subscores.
Return the test module and instructions for running it.
```

**QA-02 – Integration Test**

References: `Example_Artifacts/__main__.py`, `Example_Artifacts/01-CORE-STANDARDS.json`.

```
Design an integration test that runs the CLI against a fixture repository.
- Execute scan command with temporary out directory.
- Assert SIR files exist, graph.json created, artifacts.json schema valid.
- Optionally verify docs/index.md contains component list.
Provide the test script and expected assertions.
```

**QA-03 – Renderer Validation**

References: `Example_Artifacts/test_renderer_frontmatter.py`, `Example_Artifacts/test_visual_marker_matching.py`.

```
Create tests ensuring rendered Markdown pages include required front-matter and tables.
- Parse generated component page, check YAML keys (score, distance markers).
- Verify Graph Insights table exists.
- Confirm visual assets paths resolve.
Return test code and helper utilities for parsing.
```

### I. Operations & Troubleshooting (SRE / Ops)

**OPS-01 – Runbook**

References: `Example_Artifacts/__main__.py`, `Example_Artifacts/mkdocs.py`, `Example_Artifacts/rollup.py`.

```
Produce an operations runbook covering:
- Prerequisite installs (graphviz, runtime CLIs, mkdocs).
- How to run scans with flags and environment variables.
- Output directory structure and log locations.
- Common failure scenarios and remediation steps.
Use Markdown with checklists and tables.
```

**OPS-02 – Monitoring Plan**

References: `Example_Artifacts/rollup.py`, `Example_Artifacts/roles.json`, `Example_Artifacts/artifacts.json`.

```
Develop a monitoring plan for the documentation pipeline.
- Enumerate telemetry outputs (usage CSV, logs).
- Recommend alert thresholds (LLM cost spikes, schema failures).
- Suggest dashboard views and retention policies.
Return as Markdown with table of metrics -> alert rules.
```

**OPS-03 – Troubleshooting Tree**

References: `Example_Artifacts/__main__.py`, `Example_Artifacts/rollup.py`.

```
Create a troubleshooting decision tree for pipeline failures.
Include branches for:
- Extractor exceptions.
- Graph generation missing dependencies.
- LLM rollup disabled due to missing API key.
- Schema validation failures.
Represent as nested bullet list with corrective actions.
```

### J. Evaluation & Continuous Improvement (Tech Lead / QA)

**EVAL-01 – Release Checklist**

References: `Example_Artifacts/facets.py`, `Example_Artifacts/test_renderer_frontmatter.py`.

```
Write a release acceptance checklist ensuring documentation output quality.
- List required automated tests.
- Include manual spot checks (docs, evidence links).
- Define thresholds for facet scores or confidence.
Return as Markdown checklist.
```

**EVAL-02 – Artifact Validation Script**

References: `Example_Artifacts/option1.py`, `Example_Artifacts/artifacts.json`.

```
Describe how to validate artifacts.json in CI.
- Use schema validation and structural checks (component counts).
- Ensure evidence entries have paths.
- Return a script outline plus command to integrate in CI.
```

**EVAL-03 – Telemetry Retro**

References: `Example_Artifacts/rollup.py`, `Example_Artifacts/facets.py`, `Example_Artifacts/artifacts.json`.

```
Create a retrospective report template analyzing rollout telemetry.
Sections:
- LLM cost summary (input/output tokens, USD).
- Components with low confidence or missing evidence.
- Proposed extractor/renderer improvements.
Produce Markdown template with tables.
```

### K. Meta-Prompts (Review & Codegen)

**META-01 – PR Summary**

References: `Example_Artifacts/__main__.py`, `Example_Artifacts/option1.py`.

```
Given the following diff/context, produce:
1. Concise PR summary (2-3 bullets).
2. Potential risks/regressions.
3. Follow-up TODOs.
Keep focus on pipeline correctness and evidence integrity.
```

**META-02 – Test Generation**

References: `Example_Artifacts/test_renderer_frontmatter.py`.

```
You are writing unit tests for the provided module.
- Identify public functions/classes needing tests.
- Propose test cases with inputs/expected outputs.
- Generate test code skeletons compatible with the team's framework.
Ensure tests cover edge cases seen in the reference implementation.
```

**META-03 – Artifact to README**

References: `Example_Artifacts/business_renderer.py`, `Example_Artifacts/artifacts.json`.

```
Transform the supplied artifact JSON into a human-readable README.
Include sections: Overview, Interfaces, Workflows, Data, Dependencies, Risks, Evidence.
Use bullet lists and keep evidence IDs linked.
```

---

## 5. Recommended Execution Playbook

1. **Kickoff:** DISC-01 to DISC-03.  
2. **Extractors:** EXT-01 to EXT-04 for each stack.  
3. **Graph Core:** GRAPH-01 to GRAPH-03 (optionally GRAPH-04).  
4. **Artifact Mapping:** ART-01 to ART-03.  
5. **Rendering & Visuals:** RENDER-01 to RENDER-03.  
6. **LLM Enablement:** LLM-01 to LLM-04.  
7. **CLI & Config:** CLI-01 to CLI-03.  
8. **Testing:** QA-01 to QA-03.  
9. **Operations:** OPS-01 to OPS-03.  
10. **Evaluation:** EVAL-01 to EVAL-03.  
11. **Meta Support:** Use META prompts throughout.

Checkpoints:
- After extractor & graph steps, run QA-02 integration test.
- Before LLM rollup, validate config and evidence index.
- Prior to release, execute EVAL-01 checklist and review telemetry with EVAL-03.

---

## 6. Acceptance Criteria & Fixture Guidance

- **Extractors:** Use real sample repos or create fixtures mirroring structures in `Example_Artifacts/tibco_bw.py` and `Example_Artifacts/openapi.py`.  
- **Graph:** Construct unit tests using synthetic Signals similar to `tests/write_sir_from_signals.py` (see main repo for pattern).  
- **Artifacts:** Validate outputs with schema from ART-01 and compare to `Example_Artifacts/artifacts.json`.  
- **Rendering:** Mirror assertions from `Example_Artifacts/test_renderer_frontmatter.py` and `Example_Artifacts/test_visual_marker_matching.py`.  
- **LLM Telemetry:** Stub provider calls when running tests; ensure metrics appended like the reference rollup module.  
- **Operations:** Verify CLI runs end-to-end, producing outputs consistent with the structure described above.

---

## 7. High-Level Instructions for New Teams

1. **Clone or prepare a repo** that includes code, configs, and workflows you need to document.  
2. **Establish governance** (policies, retention) before ingesting sensitive content.  
3. **Adapt extractors** using prompts EXT-01 through EXT-04, referencing the copied examples.  
4. **Confirm data contracts** through GRAPH and ART prompts so downstream tooling remains consistent.  
5. **Iterate on documentation** with RENDER prompts, ensuring evidence stays linked.  
6. **Enable LLM rollups** after core pipeline stabilizes; monitor costs via OPS-02 guidance.  
7. **Automate tests and CI** with QA prompts, and adopt release/evaluation checklists.  
8. **Operate the platform** with runbooks and monitoring to maintain reliability.  
9. **Use meta-prompts** to accelerate reviews, testing, and documentation whenever code changes.  
10. **Continuously improve** by feeding telemetry and facet scores back into extractor and renderer enhancements.

By following this guide and leveraging the prompts with the canonical reference files, a new team can build a stack-agnostic, evidence-backed documentation engine comparable to AutodocX.
