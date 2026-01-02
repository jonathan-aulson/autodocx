from __future__ import annotations

import json
import logging
import re
import textwrap
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Callable, Dict, List, Optional, Sequence, Tuple

import yaml

from autodocx.llm.provider import call_openai_meta

PLAN_FILENAME = "dox_draft_plan.md"
CURATED_DIR = "curated"
PROCESS_SUBDIR = "processes"
FAMILY_SUBDIR = "families"
COMPONENT_SUBDIR = "components"
CONSTELLATION_SUBDIR = "constellations"
QUALITY_SUBDIR = "quality"
ROLLUP_FILENAME = "repo_comprehensive.md"
MAX_SOURCE_CHARS = 6000
SOURCE_CHAR_BUDGET = 60000
MAX_SECTION_ITEMS = 8
PROCESS_PRIORITY_BUCKETS: Sequence[Tuple[int, int, str]] = (
    (15, 1, "high"),
    (8, 2, "medium"),
    (0, 4, "low"),
)
CURATION_PROMPT_TEMPLATE = textwrap.dedent(
    """
    You reorganize technical documentation into business-friendly deliverables.
    - Each section must contain at least {min_words} words.
    - Preserve every concrete fact from the sources; do not invent new systems, data, or personas.
    - Keep evidence wording but smooth the narration so it reads naturally for business stakeholders.
    - Required sections (Markdown, in this order):
      1. ## Executive summary (what/why in 2–4 bullets)
      2. ## Workflow narrative (inputs → activities → outputs, referencing the exact system names)
      3. ## Interfaces & dependencies (tables/bullets describing external calls, child workflows, or shared data)
      4. ## Key data handled (identifiers, payload fields, evidence of schemas)
      5. ## Risks & follow-ups (errors, logging, monitoring needs)
      6. ## Related documents (link to other provided docs by filename; omit if unknown)
    - Cite the source filename with optional line ranges `(source: sr/orders_flow.md#L10-L42)`.
    - Never remove identifiers, endpoints, or error states that appear in the sources.
    - Do not add new sections beyond those listed above.
    Respond with Markdown only.
    """
).strip()

CONSTELLATION_PROMPT_TEMPLATE = textwrap.dedent(
    """
    You produce constellation briefs that stitch together multiple components, workflows, and shared data paths.
    - Each section must contain at least {min_words} words.
    - Required sections:
      1. ## Executive summary – describe the business outcome, participating components, and why this constellation matters.
      2. ## End-to-end workflow – narrate the step-by-step flow (inputs → activities → outputs) citing evidence snippets.
      3. ## Entry points & interfaces – table or bullets covering APIs, triggers, queues, and shared datastores.
      4. ## Evidence highlights – summarize the referenced code/config snippets with `file:line` anchors from the sources.
      5. ## Quality & risks – list anti-pattern findings for this constellation (severity, remediation guidance).
      6. ## Linked artifacts – reference SIR files, diagrams, or other docs that describe this constellation.
    - Every substantive statement must cite a source path `(source: evidence/constellations/<slug>.json)` or diagram alias.
    - Explicitly mention anti-pattern identifiers when describing risks.
    Respond with Markdown only.
    """
).strip()

ANTI_PATTERN_PROMPT_TEMPLATE = textwrap.dedent(
    """
    You compile an evidence-backed anti-pattern register for the repository.
    - Each section must contain at least {min_words} words.
    - Required sections:
      1. ## Overview – summarize scanning coverage, tools (Semgrep/heuristics), and key risk themes.
      2. ## Findings by severity – separate subsections for High/Medium/Low; enumerate rule id, file:line, impacted component, and remediation guidance.
      3. ## Constellation impact – highlight which constellations carry the densest findings and why.
      4. ## Remediation plan – prioritized steps, owners, and suggested tooling to close gaps.
      5. ## Methodology & references – cite the anti-pattern manifest and any supporting evidence packets.
    - Cite `quality/anti_patterns.json` entries or constellation packets for every finding.
    - Do not invent findings; if data is missing, state the limitation explicitly.
    Respond with Markdown only.
    """
).strip()

LLMCallable = Callable[[str, Dict[str, Any]], Dict[str, Any]]


def _process_priority_from_score(score: int) -> Tuple[int, str]:
    if score is None:
        score = 0
    for min_score, priority, label in PROCESS_PRIORITY_BUCKETS:
        if score >= min_score:
            return priority, label
    return 4, "low"


def _now_iso() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _safe_slug(value: str) -> str:
    slug = "".join(ch if ch.isalnum() or ch in "-_" else "_" for ch in (value or ""))
    slug = slug.strip("_")
    return slug or "doc"


def _ensure_logger(out_dir: Path) -> logging.Logger:
    logger = logging.getLogger("autodocx.docplan")
    if logger.handlers:
        return logger
    log_dir = out_dir / "logs"
    log_dir.mkdir(parents=True, exist_ok=True)
    handler = logging.FileHandler(log_dir / "doc_plan_runner.log")
    fmt = logging.Formatter("%(asctime)s %(levelname)s %(message)s")
    handler.setFormatter(fmt)
    logger.addHandler(handler)
    logger.setLevel(logging.INFO)
    logger.propagate = False
    return logger


def _extract_title(md_path: Path) -> str:
    try:
        text = md_path.read_text(encoding="utf-8")
    except Exception:
        return md_path.stem
    for line in text.splitlines():
        stripped = line.strip()
        if stripped.startswith("#"):
            return stripped.lstrip("# ").strip() or md_path.stem
    return md_path.stem


def _render_plan_text(plan: Dict[str, Any]) -> str:
    fm = yaml.safe_dump({"meta": plan["meta"], "docs": plan["docs"]}, sort_keys=False).strip()
    lines = ["---", fm, "---", "", "# Autodocx Documentation Plan", ""]
    lines.append(f"_Generated at_: {plan['meta']['generated_at']}")
    lines.append("")
    lines.append("## Checklist")
    lines.append("")
    for doc in plan["docs"]:
        status = "x" if doc.get("status") == "done" else " "
        lines.append(f"- [{status}] {doc['title']} → `{doc['filename']}`")
    lines.append("")
    lines.append("## How to use this plan")
    lines.append("")
    lines.append("1. Run `autodocx_cli scan ...` to regenerate the plan and supporting context.")
    lines.append("2. Each scan automatically fulfills all pending entries via the LLM and updates the checklist.")
    lines.append("")
    lines.append("_Sources referenced in each entry are listed under the `inputs` key in the front matter._")
    lines.append("")
    return "\n".join(lines)


def _write_plan(plan_path: Path, plan: Dict[str, Any]) -> None:
    plan_path.parent.mkdir(parents=True, exist_ok=True)
    plan_path.write_text(_render_plan_text(plan), encoding="utf-8")


def _parse_plan(plan_path: Path) -> Dict[str, Any]:
    text = plan_path.read_text(encoding="utf-8")
    if not text.startswith("---"):
        raise RuntimeError("Plan file missing YAML front matter.")
    parts = text.split("---", 2)
    if len(parts) < 3:
        raise RuntimeError("Plan file has malformed front matter.")
    front_matter = yaml.safe_load(parts[1]) or {}
    return {
        "meta": front_matter.get("meta") or {},
        "docs": front_matter.get("docs") or [],
    }


def _write_curated_doc(docs_dir: Path, spec: Dict[str, Any], body: str) -> Path:
    out_path = docs_dir / spec["filename"]
    out_path.parent.mkdir(parents=True, exist_ok=True)
    fm = {
        "title": spec["title"],
        "category": spec.get("category"),
        "sources": spec.get("inputs", []),
        "generated_at": _now_iso(),
    }
    fm_block = yaml.safe_dump(fm, sort_keys=False).strip()
    out_path.write_text(f"---\n{fm_block}\n---\n\n{body.strip()}\n", encoding="utf-8")
    return out_path


def _load_context(out_dir: Path, meta: Dict[str, Any], context_override: Optional[Dict[str, Any]]) -> Dict[str, Any]:
    if context_override is not None:
        return context_override
    ctx_file = meta.get("context_file") or "doc_context.json"
    ctx_path = Path(out_dir) / ctx_file
    if not ctx_path.exists():
        raise FileNotFoundError(f"Context file not found: {ctx_path}")
    return json.loads(ctx_path.read_text(encoding="utf-8"))


def _read_text_slice(path: Path) -> str:
    try:
        return path.read_text(encoding="utf-8")[:MAX_SOURCE_CHARS]
    except Exception:
        return ""


def _make_source_entry(rel_path: str, content: str, exists: bool = True) -> Dict[str, Any]:
    return {
        "path": rel_path,
        "exists": exists,
        "content": content[:MAX_SOURCE_CHARS],
    }


def _limit_items(items: Optional[Sequence[Any]], limit: int = MAX_SECTION_ITEMS) -> List[Any]:
    seq = list(items or [])
    if len(seq) <= limit:
        return seq
    truncated = seq[:limit]
    truncated.append(f"... (+{len(seq) - limit} more)")
    return truncated


def _load_json_safe(path: Path) -> Optional[Dict[str, Any]]:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except Exception:
        return None


def _summarize_interdeps(interdeps: Optional[Dict[str, Any]]) -> Dict[str, Any]:
    interdeps = interdeps or {}
    return {
        "component_peers": _limit_items(interdeps.get("component_peers")),
        "calls": _limit_items(interdeps.get("calls")),
        "called_by": _limit_items(interdeps.get("called_by")),
        "shared_datastores_with": _limit_items(interdeps.get("shared_datastores_with")),
        "shared_identifiers_with": _limit_items(interdeps.get("shared_identifiers_with")),
    }


def _summarize_business_scaffold(scaffold: Optional[Dict[str, Any]]) -> Dict[str, Any]:
    scaffold = scaffold or {}
    deps = scaffold.get("dependencies") or {}
    return {
        "summary": scaffold.get("summary"),
        "io_summary": {
            "inputs": _limit_items((scaffold.get("io_summary") or {}).get("inputs")),
            "outputs": _limit_items((scaffold.get("io_summary") or {}).get("outputs")),
            "identifiers": _limit_items((scaffold.get("io_summary") or {}).get("identifiers")),
        },
        "interfaces": _limit_items(scaffold.get("interfaces")),
        "invocations": _limit_items(scaffold.get("invocations")),
        "journey_touchpoints": _limit_items(scaffold.get("journey_touchpoints")),
        "resources": {
            "triggers": _limit_items((scaffold.get("resources") or {}).get("triggers")),
            "steps": _limit_items((scaffold.get("resources") or {}).get("steps")),
        },
        "errors": _limit_items(scaffold.get("errors")),
        "logging": _limit_items(scaffold.get("logging")),
        "dependencies": {
            "internal_services": _limit_items(deps.get("internal_services")),
            "external_services": _limit_items(deps.get("external_services") or deps.get("services")),
            "datastores": _limit_items(deps.get("datastores")),
            "processes": _limit_items(deps.get("processes")),
            "services": _limit_items(deps.get("services")),
        },
    }


def _summarize_sir_json(path: Path) -> str:
    data = _load_json_safe(path)
    if not isinstance(data, dict):
        return _read_text_slice(path)
    props = data.get("props") or {}
    resources = data.get("resources") or {}
    scaffold = data.get("business_scaffold") or props.get("business_scaffold") or {}
    steps = resources.get("steps") or props.get("steps") or []
    triggers = resources.get("triggers") or props.get("triggers") or []
    summary = {
        "id": data.get("id"),
        "kind": data.get("kind") or data.get("signal_kind"),
        "name": data.get("name") or data.get("process_name"),
        "component": data.get("component_or_service"),
        "file": data.get("file") or (data.get("source") or {}).get("file"),
        "roles": _limit_items(data.get("roles")),
        "description": props.get("description") or scaffold.get("summary"),
        "connectors": _limit_items([step.get("connector") or step.get("type") for step in steps if step]),
        "triggers": _limit_items([trig.get("type") or trig.get("name") or trig.get("path") for trig in triggers if trig]),
        "business_scaffold": _summarize_business_scaffold(scaffold),
        "interdependencies": _summarize_interdeps(data.get("interdependencies_slice")),
        "extrapolations": _limit_items(data.get("extrapolations")),
        "deterministic_explanation": data.get("deterministic_explanation"),
        "resources": {
            "triggers": _limit_items(triggers),
            "steps": _limit_items(steps),
            "journey_touchpoints": _limit_items(resources.get("journey_touchpoints")),
        },
        "graph_features": data.get("graph_features"),
    }
    return json.dumps(summary, indent=2)


def _summarize_artifact_entry(component: str, idx: int, artifact: Dict[str, Any]) -> str:
    summary = {
        "component": component,
        "name": artifact.get("name"),
        "artifact_type": artifact.get("artifact_type"),
        "capabilities": _limit_items(artifact.get("capabilities")),
        "http_endpoints": _limit_items(artifact.get("http_endpoints")),
        "workflows": _limit_items(artifact.get("workflows")),
        "events": _limit_items(artifact.get("events")),
        "schemas": _limit_items(artifact.get("schemas")),
        "dependencies": _limit_items(artifact.get("dependencies")),
        "risks": _limit_items(artifact.get("risks")),
    }
    return json.dumps(summary, indent=2)


def _apply_source_budget(sources: List[Dict[str, Any]], budget: int = SOURCE_CHAR_BUDGET) -> List[Dict[str, Any]]:
    if budget <= 0:
        return sources
    total = 0
    trimmed: List[Dict[str, Any]] = []
    omitted: List[str] = []
    for idx, entry in enumerate(sources):
        content = entry.get("content") or ""
        length = len(content)
        if total + length <= budget:
            trimmed.append(entry)
            total += length
            continue
        remaining = budget - total
        new_entry = dict(entry)
        if remaining > 0:
            new_entry["content"] = content[:remaining]
            new_entry["truncated"] = True
            trimmed.append(new_entry)
        omitted.extend(s["path"] for s in sources[idx + 1 :])
        break
    else:
        return trimmed

    if omitted:
        note = {
            "path": "__omitted__",
            "exists": False,
            "content": f"Omitted {len(omitted)} additional source packets due to prompt budget limits. "
                       f"Most recent paths: {', '.join(omitted[:5])}.",
        }
        trimmed.append(note)
    return trimmed


def _component_sources(out_dir: Path, context: Dict[str, Any], component: str) -> Tuple[List[Dict[str, Any]], Dict[str, Any]]:
    base = Path(out_dir)
    docs_root = base / "docs"
    data = context.get("components", {}).get(component)
    if not data:
        raise KeyError(f"Component '{component}' missing from context.")
    sources: List[Dict[str, Any]] = []
    for rel in data.get("sir_files", []):
        path = base / rel
        content = _summarize_sir_json(path)
        sources.append(_make_source_entry(rel, content, path.exists()))
    for idx, art in enumerate(data.get("artifacts", [])):
        rel = f"artifact:{component}:{idx}"
        sources.append(_make_source_entry(rel, _summarize_artifact_entry(component, idx, art), True))
    for rel in data.get("diagram_paths", []):
        path = base / rel
        sources.append(_make_source_entry(rel, f"Diagram available at {rel}", path.exists()))
    for proc_slug in data.get("process_slugs", []):
        rel = f"{CURATED_DIR}/{PROCESS_SUBDIR}/{proc_slug}.md"
        path = docs_root / rel
        if path.exists():
            sources.append(_make_source_entry(str(path.relative_to(base)), _read_text_slice(path), True))
    for fam_slug in data.get("family_slugs", []):
        rel = f"{CURATED_DIR}/{FAMILY_SUBDIR}/{fam_slug}.md"
        path = docs_root / rel
        if path.exists():
            sources.append(_make_source_entry(str(path.relative_to(base)), _read_text_slice(path), True))
    context_summary = {
        "component": component,
        "families": data.get("families", []),
        "sir_file_count": len(data.get("sir_files", [])),
        "artifact_count": len(data.get("artifacts", [])),
    }
    return _apply_source_budget(sources), context_summary


def _family_sources(out_dir: Path, context: Dict[str, Any], family: str) -> Tuple[List[Dict[str, Any]], Dict[str, Any]]:
    base = Path(out_dir)
    docs_root = base / "docs"
    data = context.get("families", {}).get(family)
    if not data:
        raise KeyError(f"Family '{family}' missing from context.")
    sources: List[Dict[str, Any]] = []
    for rel in data.get("sir_files", []):
        path = base / rel
        sources.append(_make_source_entry(rel, _summarize_sir_json(path), path.exists()))
    for rel in data.get("diagram_paths", []):
        path = base / rel
        sources.append(_make_source_entry(rel, f"Diagram available at {rel}", path.exists()))
    for proc_slug in data.get("process_slugs", []):
        rel = f"{CURATED_DIR}/{PROCESS_SUBDIR}/{proc_slug}.md"
        path = docs_root / rel
        if path.exists():
            sources.append(_make_source_entry(str(path.relative_to(base)), _read_text_slice(path), True))
    for comp_slug in data.get("component_slugs", []):
        rel = f"{CURATED_DIR}/{COMPONENT_SUBDIR}/{comp_slug}.md"
        path = docs_root / rel
        if path.exists():
            sources.append(_make_source_entry(str(path.relative_to(base)), _read_text_slice(path), True))
    context_summary = {
        "family": family,
        "components": data.get("components", []),
        "sir_file_count": len(data.get("sir_files", [])),
    }
    return _apply_source_budget(sources), context_summary


def _constellation_sources(out_dir: Path, context: Dict[str, Any], constellation_id: str) -> Tuple[List[Dict[str, Any]], Dict[str, Any]]:
    base = Path(out_dir)
    data = (context.get("constellations") or {}).get(constellation_id)
    if not data:
        raise KeyError(f"Constellation '{constellation_id}' missing from context.")
    sources: List[Dict[str, Any]] = []
    for sir_rel in data.get("sir_files") or []:
        path = base / sir_rel
        sources.append(_make_source_entry(sir_rel, _summarize_sir_json(path), path.exists()))
    graph_rel = data.get("graph_file")
    if graph_rel:
        path = base / graph_rel
        sources.append(_make_source_entry(graph_rel, _read_text_slice(path), path.exists()))
    packet_rel = data.get("evidence_packet")
    if packet_rel:
        path = base / packet_rel
        sources.append(_make_source_entry(packet_rel, _read_text_slice(path), path.exists()))
    anti_patterns = data.get("anti_patterns") or []
    if anti_patterns:
        payload = json.dumps(anti_patterns, indent=2)
        sources.append(_make_source_entry(f"anti_patterns:{constellation_id}", payload, True))
    context_summary = {
        "constellation_id": constellation_id,
        "slug": data.get("slug"),
        "components": data.get("components", []),
        "score": data.get("score"),
        "entry_points": data.get("entry_points", []),
        "anti_pattern_count": len(anti_patterns),
    }
    return _apply_source_budget(sources), context_summary


def _quality_sources(out_dir: Path, context: Dict[str, Any]) -> Tuple[List[Dict[str, Any]], Dict[str, Any]]:
    base = Path(out_dir)
    quality = context.get("quality") or {}
    anti_path = quality.get("anti_patterns_file")
    sources: List[Dict[str, Any]] = []
    if anti_path:
        path = base / anti_path
        sources.append(_make_source_entry(anti_path, _read_text_slice(path), path.exists()))
    constellation_counts = json.dumps(quality.get("constellation_counts") or {}, indent=2)
    sources.append(_make_source_entry("quality:constellation_counts", constellation_counts, True))
    context_summary = {
        "anti_patterns_file": anti_path,
        "constellation_counts": quality.get("constellation_counts") or {},
    }
    return _apply_source_budget(sources), context_summary


def _repo_sources(out_dir: Path, context: Dict[str, Any]) -> Tuple[List[Dict[str, Any]], Dict[str, Any]]:
    base = Path(out_dir)
    docs_root = base / "docs"
    repo = context.get("repo", {})
    sources: List[Dict[str, Any]] = []
    for rel in repo.get("sir_files", []):
        path = base / rel
        sources.append(_make_source_entry(rel, _summarize_sir_json(path), path.exists()))
    for rel in repo.get("diagram_paths", []):
        path = base / rel
        sources.append(_make_source_entry(rel, f"Diagram available at {rel}", path.exists()))
    facets = context.get("facets", {})
    sources.append(_make_source_entry("facets", json.dumps(facets, indent=2), True))
    for comp_slug in repo.get("component_slugs", []):
        rel = f"{CURATED_DIR}/{COMPONENT_SUBDIR}/{comp_slug}.md"
        path = docs_root / rel
        if path.exists():
            sources.append(_make_source_entry(str(path.relative_to(base)), _read_text_slice(path), True))
    for fam_slug in repo.get("family_slugs", []):
        rel = f"{CURATED_DIR}/{FAMILY_SUBDIR}/{fam_slug}.md"
        path = docs_root / rel
        if path.exists():
            sources.append(_make_source_entry(str(path.relative_to(base)), _read_text_slice(path), True))
    for proc_slug in repo.get("process_slugs", []):
        rel = f"{CURATED_DIR}/{PROCESS_SUBDIR}/{proc_slug}.md"
        path = docs_root / rel
        if path.exists():
            sources.append(_make_source_entry(str(path.relative_to(base)), _read_text_slice(path), True))
    context_summary = {
        "components": repo.get("components", []),
        "families": repo.get("families", []),
        "facets": facets,
    }
    return _apply_source_budget(sources), context_summary


def _process_sources(out_dir: Path, context: Dict[str, Any], process_key: str) -> Tuple[List[Dict[str, Any]], Dict[str, Any]]:
    base = Path(out_dir)
    data = context.get("processes", {}).get(process_key)
    if not data:
        raise KeyError(f"Process '{process_key}' missing from context.")
    sources: List[Dict[str, Any]] = []
    sir_rel = data.get("sir_file")
    if sir_rel:
        path = base / sir_rel
        sources.append(_make_source_entry(sir_rel, _summarize_sir_json(path), path.exists()))
    for rel in data.get("diagram_paths", []):
        path = base / rel
        sources.append(_make_source_entry(rel, f"Diagram available at {rel}", path.exists()))
    context_summary = {
        "process": data.get("name"),
        "component": data.get("component"),
        "families": data.get("families", []),
    }
    return _apply_source_budget(sources), context_summary


def _repo_final_sources(out_dir: Path) -> Tuple[List[Dict[str, Any]], Dict[str, Any]]:
    base = Path(out_dir)
    curated_root = base / "docs" / CURATED_DIR
    sources: List[Dict[str, Any]] = []
    if curated_root.exists():
        for md in sorted(curated_root.rglob("*.md")):
            rel = md.relative_to(base).as_posix()
            sources.append(_make_source_entry(rel, _read_text_slice(md), True))
    diag_manifest: List[str] = []
    diag_root = base / "assets" / "diagrams_llm"
    if diag_root.exists():
        for svg in sorted(diag_root.rglob("*.svg")):
            diag_manifest.append(svg.relative_to(base).as_posix())
    if diag_manifest:
        sources.append(_make_source_entry("diagrams_llm_manifest", json.dumps(diag_manifest, indent=2), True))
    context_summary = {
        "curated_docs": len(sources),
        "diagram_count": len(diag_manifest),
        "note": "Final repo doc synthesizes all curated Markdown outputs.",
    }
    return _apply_source_budget(sources), context_summary


def _count_words(text: str) -> int:
    return len(re.findall(r"\b\w+\b", text))


def _sections_below_min(markdown: str, min_words: int) -> List[str]:
    sections: Dict[str, int] = {}
    current = None
    buffer: List[str] = []
    for line in markdown.splitlines():
        if line.startswith("## "):
            if current is not None:
                sections[current] = _count_words(" ".join(buffer))
            current = line.lstrip("#").strip()
            buffer = []
        else:
            buffer.append(line)
    if current is not None:
        sections[current] = _count_words(" ".join(buffer))
    return [name for name, count in sections.items() if count < min_words]


def _invoke_llm(prompt: str, payload: Dict[str, Any], llm_callable: Optional[LLMCallable]) -> Dict[str, Any]:
    if llm_callable:
        return llm_callable(prompt, payload)
    return call_openai_meta(prompt=prompt, input_json=payload)


def _generate_with_retries(
    llm_callable: Optional[LLMCallable],
    prompt_template: str,
    payload: Dict[str, Any],
    min_words: int,
    max_attempts: int = 3,
) -> str:
    prompt = prompt_template.format(min_words=min_words)
    attempt_payload = dict(payload)
    for attempt in range(max_attempts):
        result = _invoke_llm(prompt, attempt_payload, llm_callable)
        text = result.get("text", "")
        short_sections = _sections_below_min(text, min_words)
        if not short_sections:
            return text
        attempt_payload = dict(payload)
        attempt_payload["previous_draft"] = text
        attempt_payload["sections_needing_expansion"] = short_sections
        prompt = (
            prompt_template.format(min_words=min_words)
            + "\n\nRevise the previous draft supplied in `previous_draft`. "
              f"Expand every section listed in `sections_needing_expansion` so each contains at least {min_words} words."
        )
    return text


def _build_plan_entries_from_context(context: Dict[str, Any]) -> List[Dict[str, Any]]:
    entries: List[Dict[str, Any]] = []
    process_items = list((context.get("processes") or {}).items())
    def _richness(kv: Tuple[str, Dict[str, Any]]) -> int:
        data = kv[1] or {}
        scaffold = data.get("business_scaffold") or {}
        interdeps = data.get("interdependencies") or {}
        richness = int(data.get("quality_score") or 0)
        richness += len((scaffold.get("interfaces") or [])) + len((scaffold.get("invocations") or []))
        richness += len((interdeps.get("calls") or [])) + len((interdeps.get("shared_datastores_with") or []))
        return richness
    process_items.sort(
        key=lambda kv: (-_richness(kv), (kv[1].get("name") or "").lower())
    )
    for proc_key, data in process_items:
        score = _richness((proc_key, data))
        priority, band = _process_priority_from_score(score)
        entries.append(
            {
                "title": f"{data['name']} – Process Brief",
                "filename": f"{CURATED_DIR}/{PROCESS_SUBDIR}/{data['slug']}.md",
                "status": "pending",
                "category": "process",
                "doc_type": "process",
                "target": {"process": proc_key},
                "priority": priority,
                "quality_score": score,
                "quality_band": band,
            }
        )
    for comp, data in sorted((context.get("components") or {}).items(), key=lambda kv: kv[0].lower()):
        entries.append(
            {
                "title": f"{comp} – Component Brief",
                "filename": f"{CURATED_DIR}/{COMPONENT_SUBDIR}/{data['slug']}.md",
                "status": "pending",
                "category": "component",
                "doc_type": "component",
                "target": {"component": comp},
                "priority": 3,
            }
        )
    for fam, data in sorted((context.get("families") or {}).items(), key=lambda kv: kv[0].lower()):
        entries.append(
            {
                "title": f"{fam} – Family Brief",
                "filename": f"{CURATED_DIR}/{FAMILY_SUBDIR}/{data['slug']}.md",
                "status": "pending",
                "category": "family",
                "doc_type": "family",
                "target": {"family": fam},
                "priority": 2,
            }
        )
    for cid, data in sorted((context.get("constellations") or {}).items(), key=lambda kv: (kv[1].get("slug") or kv[0])):
        slug = data.get("slug") or _safe_slug(cid)
        entries.append(
            {
                "title": f"{slug.replace('-', ' ').title()} – Constellation Brief",
                "filename": f"{CURATED_DIR}/{CONSTELLATION_SUBDIR}/{slug}.md",
                "status": "pending",
                "category": "constellation",
                "doc_type": "constellation",
                "target": {"constellation": cid},
                "priority": 2,
            }
        )
    if (context.get("quality") or {}).get("anti_patterns_file"):
        entries.append(
            {
                "title": "Anti-Pattern Register",
                "filename": f"{CURATED_DIR}/{QUALITY_SUBDIR}/anti_pattern_register.md",
                "status": "pending",
                "category": "quality",
                "doc_type": "quality",
                "target": {"quality": "anti_patterns"},
                "priority": 3,
            }
        )
    if context.get("repo"):
        entries.append(
            {
                "title": "Repository Overview – Portfolio Rollup",
                "filename": f"{CURATED_DIR}/repo_overview.md",
                "status": "pending",
                "category": "repo",
                "doc_type": "repo",
                "target": {"repo": True},
                "priority": 4,
            }
        )
        entries.append(
            {
                "title": "Repository Comprehensive Narrative",
                "filename": f"{CURATED_DIR}/{ROLLUP_FILENAME}",
                "status": "pending",
                "category": "repo_final",
                "doc_type": "repo_final",
                "target": {"repo_final": True},
                "priority": 5,
            }
        )
    return entries


def draft_doc_plan(
    out_dir: Path,
    *,
    context: Dict[str, Any],
) -> Path:
    """
    Build (or refresh) the documentation plan under out/docs.
    """
    out_dir = Path(out_dir)
    docs_dir = out_dir / "docs"
    curated_dir = docs_dir / CURATED_DIR
    curated_dir.mkdir(parents=True, exist_ok=True)

    entries = _build_plan_entries_from_context(context)
    if not entries:
        raise RuntimeError("No components or families available; skipping documentation plan generation.")

    total_docs = len(entries)
    plan = {
        "meta": {
            "generated_at": _now_iso(),
            "doc_count": total_docs,
            "context_file": "doc_context.json",
        },
        "docs": entries,
    }
    plan_path = docs_dir / PLAN_FILENAME
    _write_plan(plan_path, plan)
    return plan_path


def fulfill_doc_plan(
    out_dir: Path,
    *,
    context: Optional[Dict[str, Any]] = None,
    min_words_per_section: int = 50,
    llm_callable: Optional[LLMCallable] = None,
) -> int:
    """
    Read the plan and fulfill every pending entry in priority order.
    Returns number of documents generated.
    """
    out_dir = Path(out_dir)
    docs_dir = out_dir / "docs"
    plan_path = docs_dir / PLAN_FILENAME
    if not plan_path.exists():
        raise FileNotFoundError(f"Plan file not found: {plan_path}")
    plan = _parse_plan(plan_path)
    ctx = _load_context(out_dir, plan.get("meta", {}), context)
    docs = plan.get("docs") or []
    pending = [doc for doc in docs if doc.get("status") != "done"]
    if not pending:
        return 0
    pending.sort(key=lambda d: (int(d.get("priority", 5)), -int(d.get("quality_score", 0)), d.get("title", "")))
    to_process = pending
    logger = _ensure_logger(out_dir)
    processed = 0
    for spec in to_process:
        doc_type = spec.get("doc_type") or spec.get("category")
        target = spec.get("target") or {}
        try:
            prompt_template = CURATION_PROMPT_TEMPLATE
            if doc_type == "process":
                process_key = target.get("process")
                sources, ctx_summary = _process_sources(out_dir, ctx, process_key)
            elif doc_type == "component":
                component = target.get("component")
                sources, ctx_summary = _component_sources(out_dir, ctx, component)
            elif doc_type == "family":
                family = target.get("family")
                sources, ctx_summary = _family_sources(out_dir, ctx, family)
            elif doc_type == "constellation":
                constellation_id = target.get("constellation")
                sources, ctx_summary = _constellation_sources(out_dir, ctx, constellation_id)
                prompt_template = CONSTELLATION_PROMPT_TEMPLATE
            elif doc_type == "quality":
                sources, ctx_summary = _quality_sources(out_dir, ctx)
                prompt_template = ANTI_PATTERN_PROMPT_TEMPLATE
            elif doc_type == "repo_final":
                sources, ctx_summary = _repo_final_sources(out_dir)
            else:
                sources, ctx_summary = _repo_sources(out_dir, ctx)
            payload = {
                "request_title": spec.get("title"),
                "doc_type": doc_type,
                "audience": "business stakeholders",
                "min_words_per_section": min_words_per_section,
                "sources": sources,
                "context": ctx_summary,
            }
            text = _generate_with_retries(
                llm_callable,
                prompt_template,
                payload,
                min_words=min_words_per_section,
            )
            _write_curated_doc(docs_dir, spec, text or "# Draft\n\n_No content returned._")
            spec["status"] = "done"
            spec["fulfilled_at"] = _now_iso()
            processed += 1
            logger.info("Curated doc generated for %s", spec.get("title"))
        except Exception as exc:
            spec["status"] = "error"
            spec["error"] = str(exc)
            logger.error("Doc plan fulfillment failed for %s: %s", spec.get("title"), exc)
    plan["meta"]["last_fulfilled_at"] = _now_iso()
    _write_plan(plan_path, plan)
    return processed
