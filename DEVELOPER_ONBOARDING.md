# Developer Onboarding

This guide is the single stop for understanding, running, and extending AutoDocX. It starts with architecture, then walks you through the codebase and runtime pipeline end-to-end.

## High-level architecture

AutoDocX is a pipeline that turns source repos into evidence-backed documentation, diagrams, and a browsable MkDocs site.

```
Repository
  -> Extractors (Signals)
  -> Graph + Scoring
  -> Universal Artifacts
  -> Evidence Index + Grouping
  -> Doc Context + Doc Plan
  -> LLM Fulfillment + Diagrams
  -> Markdown Docs + MkDocs Site
```

Key ideas:
- Evidence-first: every claim points back to file/line anchors.
- Signals (SIRs) are the normalized unit of extracted knowledge.
- The doc plan is deterministic; fulfillment is LLM-driven but evidence-constrained.
- Output is a unified docs tree with parent component pages and child detail pages.

## Quick start

1) Install dependencies and create a venv:

```bash
python3.10 -m venv .venv
source .venv/bin/activate
pip install -e .
```

2) Add `.env` at repo root (required):

```
OPENAI_API_KEY=...
```

3) Run a scan:

```bash
autodocx scan /path/to/repo --out out --debug --mkdocs-build --llm-rollup

# Or use the below to run and include step to index docs for RAG

autodocx scan <repo> --out out --mkdocs-build --llm-rollup --rag-docs
```

4) Browse docs:

```bash
mkdocs serve -f out/mkdocs.yml -a 127.0.0.1:8000
```

If you are on WSL, prefer placing repos under `/home/<user>/...` and use `scripts/setup_wsl.sh` to install Graphviz, MkDocs, and build dependencies.

## Configuration and runtime controls

Primary config is `autodocx.yaml`. The CLI reads it and merges CLI flags + env overrides.

Important config keys:
- `docs.profile`: `business`, `technical`, or `full`
- `docs.visuals.enable_flow_diagrams`: Graphviz workflow diagrams
- `docs.sections.*`: toggles for confidence/interdependency/unknowns sections
- `llm.*`: provider/model/token limits and structured output controls
- `rollup.*`: publish thresholds for LLM rollups
- `out_dir`: default output root

Common environment overrides:
- `AUTODOCX_CONFIG`: override config file path
- `AUTODOCX_LLM_MODEL`: override LLM model per run
- `AUTODOCX_SECTION_MIN_WORDS`: per-section word floor in LLM fulfillment
- `AUTODOCX_EXTRACTORS_INCLUDE` / `AUTODOCX_EXTRACTORS_EXCLUDE`
- `AUTODOCX_DEBUG_EXTRACTORS=1`
- `AUTODOCX_ENABLE_CONSTELLATIONS=1`: optional cross-repo constellation briefs (disabled by default)

## Output layout (unified docs tree)

```
out/
  docs/
    <component>/<component>.md
    <component>/components/<process-or-detail>.md
    <component>/processes/<process>.md
    families/<family>.md
    constellations/<constellation>.md (only when enabled)
    evidence/index.md
    evidence/packets/*.md
    rag/*.md
    repo_comprehensive.md
    dox_draft_plan.md
    assets/graphs/**.svg
  signals/
    sir_v2/*.json
    interdeps.json
    graph.json
    doc_context.json
    rollup/**
  artifacts/artifacts.json(.jsonl)
  diagrams/
    flows_json/**.json
    deterministic_svg/**.svg
    llm_svg/**.svg
  manifests/
  reports/
  site/ (MkDocs build output, if enabled)
  mkdocs.yml
```

## Walking tour: how a scan works end-to-end

This is the actual execution path, mapped to key files so you can follow the data.

1) CLI entry and orchestration
- `autodocx_cli/__main__.py`
  - Loads `.env`, config, and CLI args.
  - Cleans `out/` while preserving `mkdocs.yml`, `site/`, and `metrics/llm_usage.csv`.
  - Orchestrates the pipeline (extract, graph, artifacts, docs, diagrams, rollups).

2) Extractors (signals/SIRs)
- `autodocx/registry.py` loads built-ins + entry points from `pyproject.toml`.
- `autodocx/extractors/*` implement `detect`, `discover`, `extract`.
- `autodocx/types.py` defines `Signal` and graph types.

Key signals include workflows, APIs, routes, infra, DB, docs. Each signal carries evidence anchors.

3) Graph build and scoring
- `autodocx/graph/builder.py` converts signals to nodes/edges and runs joiners.
- `autodocx/features/distance_features.py` computes graph metrics (markers, distances, risk hints).
- `autodocx/scoring/facets.py` aggregates score facets for the run.

4) Signal persistence (SIRs)
- `autodocx/scaffold/signal_scaffold.py` enriches signals with business scaffolds.
- The CLI writes SIR JSON under `out/signals/sir_v2/`.

5) Artifact mapping
- `autodocx/artifacts/option1.py` maps signals into universal artifacts:
  - Interfaces, workflows, data, infra, dependencies, risk, and confidence.
- Output: `out/artifacts/artifacts.json` + `artifacts.jsonl`.

6) Evidence index and grouping
- `autodocx/llm/evidence_index.py` builds `out/evidence/evidence_index.json`.
- `autodocx/llm/grouping.py` groups signals/artifacts by component.

7) Doc context and doc plan
- `autodocx/llm/context_builder.py` builds component/family/process context.
- `autodocx/docplan/plan.py` writes `out/docs/dox_draft_plan.md`.

8) Diagram generation
- `autodocx/visuals/flow_export.py` builds flow JSON with ports/control edges.
- `autodocx/visuals/flow_renderer.py` renders richer flow diagrams.
- `autodocx/visuals/graphviz_flows.py` renders component/process SVGs.
- LLM workflow diagrams are generated under `out/diagrams/llm_svg/**`.

9) LLM fulfillment (core docs)
- `autodocx/docplan/plan.py` fulfills each plan entry with LLM prompts.
- Outputs land in the unified docs tree under `out/docs/**`.

10) MkDocs nav and optional site build
- `autodocx_cli/__main__.py` regenerates `out/mkdocs.yml`.
- If enabled, `mkdocs build -d out/site` runs.

11) Optional rollup summaries
- `autodocx/llm/rollup.py` produces additive summaries when `--llm-rollup` is set.
- Usage metrics are logged to `out/metrics/llm_usage.csv`.

## Codebase tour (what to read first)

Start with these files to understand the system quickly:
- `autodocx_cli/__main__.py`: the orchestrator.
- `autodocx/registry.py`: extractor loading and filtering.
- `autodocx/extractors/`: built-in extractor implementations.
- `autodocx/graph/builder.py`: signal -> graph conversion and joiners.
- `autodocx/artifacts/option1.py`: signal -> artifact mapping.
- `autodocx/docplan/plan.py`: deterministic plan generation + LLM fulfillment.
- `autodocx/render/business_renderer.py`: business-focused Markdown pages.
- `autodocx/render/mkdocs.py`: MkDocs-ready docs tree renderer.
- `autodocx/visuals/graphviz_flows.py`: Graphviz SVG diagrams.
- `autodocx/llm/rollup.py`: optional post-doc summaries.
- `autodocx/rag/*`: embeddings and RAG docs pipeline.

## Adding or extending an extractor

1) Add `autodocx/extractors/<name>.py` with `detect/discover/extract`.
2) Register in `pyproject.toml` under `[project.entry-points."autodocx.extractors"]`.
3) `pip install -e .` to refresh entry points.
4) Add tests under `tests/` and run `pytest`.

Extractor signals should populate:
- `triggers`, `steps`, `relationships`, and identifier hints
- `control_edges` for branching/port-aware diagrams

## Common commands

- Run scan (basic): `autodocx scan <repo> --out out --debug`
- Run scan (full pipeline): `autodocx scan <repo> --out out --debug --mkdocs-build --llm-rollup --rag-docs`
- Serve docs locally: `mkdocs serve -f out/mkdocs.yml -a 127.0.0.1:8000`
- Build docs site: `mkdocs build -d out/site -f out/mkdocs.yml`
- Verify tools: `python -m autodocx_cli doctor`
- Run tests: `pytest`
- Secrets scan (repo history): `gitleaks detect --config .gitleaks.toml`
- Secrets scan (working tree): `gitleaks detect --no-git --config .gitleaks.toml`

## Troubleshooting

If extraction is sparse:
- Run with `--debug` and verify `detect()` returned true.
- Check `AUTODOCX_EXTRACTORS_INCLUDE/EXCLUDE`.

If diagrams fail:
- Verify Graphviz `dot` is in PATH.
- Ensure workflow steps include `run_after` and `control_edges`.

If LLM steps fail:
- Confirm `OPENAI_API_KEY` is set.
- Check `AUTODOCX_LLM_MODEL` and token limits in `autodocx.yaml`.

## How to contribute safely

- Prefer adding tests when you modify extractors or rendering.
- Avoid breaking evidence links or doc layout paths.
- Keep outputs deterministic; avoid introducing non-deterministic ordering.

---

If you want a guided walkthrough of a specific extractor or repo type (TIBCO BW, Logic Apps, PowerBuilder, etc.), call that out and we can add a focused appendix. 
