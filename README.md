# autodoc.md

**PoC → modular auto-documentation engine.**

- Clean, modular foundation
- Library separated from CLI
- Plugin system with entry points
- Evidence-first extraction into a graph
- Universal artifacts for LLMs or human docs
- Docs generation with clouds/CI support surface-ready later

Day-to-day, you only touch 3 places:

1 Extractors: autodocx/extractors/*.py

- These find and parse files (OpenAPI, LogicApps/PA, K8s, Terraform, GHA, Azure Pipelines, Express, SQL, Markdown).

2 Mapper: autodocx/artifacts/option1.py

- Translates extracted Signals into your universal artifact JSON (Option 1).

3 CLI: autodocx_cli/main.py

- The command you run (autodocx) to scan and produce docs.*_

**Mental model:**

- Extractors read files → emit Signals with evidence.
- Graph builder turns Signals into Nodes/Edges (for future diagrams/joins).
- Mapper turns Signals into universal artifacts (what your docs and LLMs use).
- LLM diagramming re-synthesizes cross-workflow SVGs from the aggregated SIR/SIRv2/interdependency data so visuals reflect whole-business flows.
- Doc context + plan + LLM fulfillment turn those artifacts/SIRs into curated Markdown (components, families, repo overview) with evidence citations and enforced minimum words per section.
- MkDocs nav is rewritten from the curated docs and (optionally) built into a static site via `mkdocs build`.

**Quickstart:**

- Create venv and install: `pip install -e .`
- Run: `autodocx scan /path/to/repo --out out`
- Eg: autodocx scan ./repos --out out --debug --mkdocs-build --llm-rollup
- Review: curated parent docs live under `out/docs/<component>/<component>.md` with detailed component docs under `out/docs/<component>/components/`. Run `mkdocs serve -f out/mkdocs.yml -a 127.0.0.1:8000` to browse the regenerated navigation.

> `--llm-rollup` is now an additive option; the base scan always invokes the LLM to fulfill the standard doc plan.

### Advanced Flags

- `--debug` — Stream extractor + evidence progress so you can diagnose gaps in coverage.
- `--include-archives` — Unpack `.zip/.ear/.par` files under the repo root before scanning (helpful for Logic Apps exports).
- `--mkdocs-build` — After regenerating `mkdocs.yml`, run `mkdocs build` so `out/site` is ready for publishing.
- `--llm-rollup` — Ask the LLM to generate additional executive rollups once the standard doc plan finishes.
- `--rag-docs` — Enable the embeddings/RAG pipeline described below; produces `docs/rag/*.md` alongside the curated constellation docs.
- `scripts/check_scaffold_coverage.py` — Reads `out/manifests/scaffold_coverage.json` after a scan and highlights which extractors still emit signals without identifiers/datastore/process hints. Treat any non-zero rows as a regression.
- `scripts/run_fixture_scans.py` — Runs deterministic scans against the bundled BW/PowerBuilder sample repos (writes to `out/fixtures/<name>`). Use this when tuning extractors so you can diff scaffold coverage between iterations.

### Running inside WSL Ubuntu

- Keep the repo under your Linux filesystem (e.g., `/home/<user>/projects/...`) instead of `/mnt/c` so file-watching and cache cleanup stay fast.
- Bootstrap dependencies with `./scripts/setup_wsl.sh` (installs Python 3.10 dev headers, Graphviz, fonts, MkDocs, build-essential, plus optional Azure CLI + Bicep).
- If you prefer manual setup, run:

  ```bash
  sudo apt update && sudo apt install -y python3.10-venv python3.10-dev build-essential pkg-config \
    graphviz graphviz-dev fonts-dejavu mkdocs libxml2-dev libxslt1-dev curl git
  ```

- Optionally add Azure CLI + Bicep: `curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash && az bicep install`.
- Create the virtualenv with `python3.10 -m venv .venv && source .venv/bin/activate`, then run `pip install -e .[treesitter]`.
- Run `python -m autodocx_cli doctor` (or `python3 -m ...`) to confirm Graphviz, MkDocs, Azure CLI/Bicep, and `OPENAI_API_KEY` are available before scanning.
- Set `AUTODOCX_SKIP_PYCLEAN=1` if the repo ever lives on `/mnt/c` to skip the aggressive `__pycache__` purge during CLI startup.

## Environment variables

AutoDocX reads `.env` from the project root (and from each scanned repo) before the CLI runs. Set at least:

- `OPENAI_API_KEY=<key>` — **required**. The workflow always routes through LLM fulfillment, so scans fail fast if the key is missing. The same key is reused when `--llm-rollup` is set.
- `AUTODOCX_LLM_MODEL=gpt-5.1` — optional override. Defaults to the model specified in `autodocx.yaml` but can be changed per run via `.env`.
- `AUTODOCX_SECTION_MIN_WORDS=100` — optional but recommended. Controls the per-section word floor enforced during `fulfill_doc_plan`; defaults to 50 if unset.
- `AUTODOCX_SKIP_PYCLEAN=1` — optional escape hatch if you keep the repo on `/mnt/c` and want to skip the repo-wide `__pycache__` cleanup step.
- `AUTODOCX_SEMGREP_CONFIG=<path-or-url>` — optional Semgrep ruleset. When set, each scan shells out to `semgrep --config ...` before doc generation, and any findings are merged into the anti-pattern register/constellation briefs (requires `semgrep` in `$PATH`).
- `AUTODOCX_QDRANT_URL=https://<host>` — optional. If present, embeddings from `--rag-docs` are stored in the specified Qdrant collection instead of only the local JSONL fallback.
- `AUTODOCX_QDRANT_API_KEY=<token>` — optional but required when the referenced Qdrant cluster is secured; paired with `AUTODOCX_QDRANT_URL`.
- `AUTODOCX_EMBED_MODEL=text-embedding-3-large` — optional override for the embeddings model used by the RAG pipeline. Defaults to `text-embedding-3-large`; switch to a smaller model to reduce cost.
- `AUTODOCX_DEBUG_SCAFFOLD=1` — Optional noisy logging that prints which extractors still emit signals missing identifiers/datastores/processes (based on the scaffold coverage report). Useful when iterating on extractor changes.

## Embeddings & RAG-backed docs

Add `--rag-docs` to your scan command when you want AI-authored wiki articles that stay grounded in the indexed repository:

1. **Chunk + embed artifacts** — The embeddings service walks every artifact emitted by the mapper, chunks source files, and embeds them with the configured `AUTODOCX_EMBED_MODEL`. Chunks are persisted to `out/rag/chunks.jsonl`, and (optionally) pushed into Qdrant via `AUTODOCX_QDRANT_URL`/`AUTODOCX_QDRANT_API_KEY`.
2. **Autogenerate an XML plan** — `doc_draft_plan.xml` is created with 3‑5 suggested knowledge base pages by prompting against the repo tree + README + current constellation/component roster.
3. **Retrieve + author Markdown** — For each plan entry, the pipeline retrieves the top matching chunks, links the relevant evidence packets, and streams Markdown outputs under `out/docs/rag/<slug>.md` with citation callouts.

Running `--rag-docs` is additive: you still get the standard curated docs, MkDocs nav (now including “RAG Docs”), and optional rollups. Pair it with `--mkdocs-build` to publish the expanded portal in one pass.

## How to add a new extractor

Create autodocx/extractors/<[name]>.py
Implement name, patterns, detect, discover, extract
Register it in pyproject.toml under [project.entry-points."autodocx.extractors"]
pip install -e . to refresh entry points
Add joiners (e.g., link HTTP clients → servers):

Create autodocx/joiners/ (later)
Start with simple string matching; add more later
Add validation:

Install jsonschema and validate artifacts.json against your schema before writing

## Extractor ToDos List (Legacy Systems)

- ServiceNow / Salesforce automation
- Oracle E-Business Suite / PeopleSoft / SAP ABAP artifacts
- SharePoint / Nintex / Power Pages workflows
- Service bus / messaging infra (RabbitMQ, Kafka, IBM MQ config)


## Troubleshooting tips

“autodocx: command not found” → Run pip install -e . again inside your activated virtualenv.
“0 workflows found” → Use --debug. Look for logicapps_wdl detect -> True and “candidate:” lines. If detect -> False, your files might not match patterns; share one path pattern to adjust.
“Unicode errors” → The extractors already open files with errors="ignore"; still failing? Show the file path and we’ll tweak._

## ToDo

- Extend extractors (GitHub Actions, Azure Pipelines)
- Add confidence scoring facets
- Introduce caching and .gitignore-aware excludes
- Introspect the target code base and draw conclusions about the architecture & publish 'Arch Standards' based on observations
- Same as above but produce prioritized list of Risks (eg: Insufficient logging, poor error handling, Anti-patterns, etc.)

---
# Autodoc.md — Automated, Evidence‑First Documentation as a Service

**Turn your code and configs into business‑ready docs, diagrams, and executive briefs.**

---

## The Elevator Pitch

**The Autodoc.md pipeline automatically transforms your existing code, configurations, and integrations into polished documentation that executives, engineers, and compliance teams can actually use.** Our universal autodoc engine extracts structured facts from your repositories and generates confidence‑scored explanations with visual flow diagrams—no manual documentation effort required. **Get portfolio visibility, faster onboarding, and audit‑ready documentation in weeks, not months.**

---

## Who Buys It / Who Benefits

**Buyers:** CTOs, Engineering Directors, Compliance Officers, Portfolio Managers
**Beneficiaries:** Development teams, new hires, auditors, business stakeholders, M&A teams

- **Engineering Leadership** — Portfolio visibility and technical debt assessment
- **Compliance Teams** — Audit‑ready documentation with traceable evidence
- **HR/Onboarding** — Accelerated developer ramp‑up with clear system maps
- **Business Stakeholders** — Executive summaries of technical capabilities and risks

---

## Where It Fits

**Supported Systems & Environments:**

- **Enterprise Platforms Already Supported:** React.js frontends, Azure Functions, AWS Lambda (SAM/Serverless), Power Automate/Logic Apps with Dataverse, TIBCO BusinessWorks, PowerBuilder
- **Repository Elements Already Supported:** OpenAPI specs, PostgreSQL migrations, AsyncAPI, Kubernetes manifests, Terraform, Bicep templates, CI pipelines, Express APIs
- **Integration Targets:** GitHub, Azure DevOps, GitLab, Bitbucket, on‑premises repositories, any other repository

---

## What We Deliver

✅ **Business‑friendly documentation** with visual flow diagrams and plain‑English explanations  
✅ **Per‑component pages** for individual services, APIs, and workflows  
✅ **Family/portfolio summaries** grouping related systems and dependencies  
✅ **Executive brief** with confidence scoring and risk assessment  
✅ **Published portal** via MkDocs (GitHub Pages/Azure Static Web Apps) or Confluence integration  
✅ **Iterative refinement** based on stakeholder feedback

---

## How It Works

Our **autodoc engine** delivers documentation in 7 streamlined steps:

1. **Discover** repositories and locate every supported file type.
2. **Extract** structured facts (SIR + SIRv2), graphs, interdependencies, and baseline SVG assets from code, configs, and workflows.
3. **Build doc context** that aggregates artifacts, diagram paths, facets, and evidence per process/component/family plus the repo overview.
4. **Synthesize LLM diagrams** that merge related workflows into comprehensive Graphviz SVGs (stored under `out/diagrams/llm_svg`).
5. **Draft a documentation plan** that deterministically lists each process/family/component/repo deliverable in bottom-up order.
6. **Fulfill the plan via LLMs**: prompts synthesize compacted SIR/SIRv2/interdeps/graphs/assets (automatically trimmed to ~60k characters per doc), enforce the `AUTODOCX_SECTION_MIN_WORDS` floor, and emit curated Markdown with traceable citations.
7. **Regenerate MkDocs navigation** (and optionally `mkdocs build`) so the published portal immediately reflects the new curated docs; `--llm-rollup` can be added for extra high-level summaries.

---

## Top Use Cases

• **M&A Due Diligence** — Rapid technical portfolio assessment with confidence metrics  
• **Regulatory Compliance** — Audit‑ready documentation with traceable evidence chains  
• **Developer Onboarding** — New hire acceleration with visual system maps  
• **Legacy System Documentation** — Reverse‑engineer undocumented applications  
• **Technical Debt Assessment** — Portfolio‑wide visibility into architectural risks  
• **Vendor Handoffs** — Knowledge transfer with zero tribal knowledge loss  
• **Executive Reporting** — Technical summaries for non‑technical stakeholders  

---

## Business Outcomes & ROI

• **Reduction** in manual documentation effort  
• **3‑5x faster** developer onboarding (weeks to days)  
• **Time savings** on compliance prep and audits  
• **Complete portfolio visibility** within 2‑4 weeks  
• **Reduced technical risk** through dependency mapping and gap identification  

---

## Why Us / Differentiators

• **Evidence‑first approach** — Every claim traceable to source code  
• **Confidence scoring** — Know which documentation is rock‑solid vs. inferred  
• **Visual flow diagrams** — SVG diagrams showing data and process flows  
• **Audience‑aware pages** — Technical, business, and executive views of the same systems  
• **Multi‑stack expertise** — From legacy PowerBuilder to modern microservices  
• **Rapid deployment** — Working documentation portal in 2‑6 weeks  
• **No vendor lock‑in** — Standard Markdown output, portable across platforms  

---

## Engagement Model & Timeline

**Phased Approach (2‑6 weeks typical):**

- **Phase 1:** Discovery & scoping (3‑5 days)
- **Phase 2:** autodoc engine deployment & fact extraction (1‑4 weeks, depending on how readily repo files are decoded)
- **Phase 3:** Documentation generation & review cycles (1‑2 weeks)
- **Phase 4:** Portal publishing & stakeholder training (2‑3 days)

---

## Prerequisites

**What you provide:**
• Repository access to decoded files (read‑only sufficient)  
• Stakeholder availability for discovery sessions  
• Publishing target preferences (MkDocs, Confluence, etc.)  
• Sample of existing documentation (if any) for style matching  

---

## Quick FAQ

**Q: How long does it take to see results?**  
A: Working documentation portal typically live within 2‑6 weeks of kickoff.

**Q: What if our code has no existing documentation?**  
A: Perfect use case. Autodoc.md reverse‑engineers documentation from code structure, configs, and schemas.

**Q: Can you handle legacy systems like PowerBuilder or TIBCO?**  
A: Yes. We support 30+ technology stacks including legacy enterprise platforms.

**Q: How accurate is the generated documentation?**  
A: Every claim includes confidence scoring. High‑confidence items are typically 95%+ accurate.

**Q: Do we need to change our development workflow?**  
A: No. We work with your existing repositories and development practices.

**Q: What about ongoing maintenance?**  
A: Optional: We can set up automated regeneration triggered by repository changes.
