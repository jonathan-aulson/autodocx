from __future__ import annotations

import hashlib
import json
import os
import time
from pathlib import Path
from typing import Any, Dict, Iterable, Optional, Sequence, List

from jsonschema import ValidationError
from rich import print as rprint

from autodocx.config_loader import get_all_settings
from autodocx.llm.context_builder import (
    build_component_contexts,
    build_group_context,
    sanitize_evidence_index,
    summarize_context_for_prompt,
)
from autodocx.llm.evidence_index import build_evidence_index
from autodocx.llm.persistence import append_usage_row, persist_component_outputs, persist_group_outputs
from autodocx.llm.prompt_builder import build_group_prompt
from autodocx.llm.provider import call_openai_meta
from autodocx.llm.schema_store import (
    COMPONENT_RESPONSE_SCHEMA,
    GROUP_RESPONSE_SCHEMA,
    validate_component_response,
    validate_group_response,
)


def _provider_ready(settings: Dict[str, Any]) -> bool:
    llm_cfg = settings.get("llm") or {}
    if not llm_cfg.get("enabled", True):
        rprint("[yellow][llm] Skipping rollup: llm.enabled is false in configuration.[/yellow]")
        return False
    if os.getenv("AUTODOCX_DISABLE_LLM", "").lower() in {"1", "true", "yes"}:
        rprint("[yellow][llm] Skipping rollup: AUTODOCX_DISABLE_LLM environment flag detected.[/yellow]")
        return False
    provider = (settings.get("llm") or {}).get("provider")
    if provider != "openai":
        rprint("[yellow][llm] Skipping rollup: unsupported provider (expected 'openai').[/yellow]")
        return False
    if not os.getenv("OPENAI_API_KEY"):
        rprint("[yellow][llm] Skipping rollup: OPENAI_API_KEY not configured.[/yellow]")
        return False
    return True


def _hash_text(text: str) -> str:
    return hashlib.sha256(text.encode("utf-8")).hexdigest()


def _parse_response_text(raw_text: str) -> Dict[str, Any]:
    text = raw_text.strip()
    if text.startswith("```"):
        parts = text.split("```", 2)
        if len(parts) >= 3:
            text = parts[1] if parts[1].strip() else parts[2]
    try:
        return json.loads(text)
    except json.JSONDecodeError as exc:
        raise RuntimeError(f"LLM returned invalid JSON: {exc}") from exc


def _prune_evidence_ids(doc: Dict[str, Any], allowed_ids: Iterable[str]) -> None:
    allowed = set(allowed_ids)
    doc["evidence_used"] = [eid for eid in doc.get("evidence_used", []) if eid in allowed]
    section_keys = [
        "what_it_does",
        "interfaces",
        "data_highlights",
        "risks_gaps",
        "user_experience",
        "risk_stories",
        "operational_behaviors",
        "data_flows",
        "relationships_summary",
        "dependency_matrix",
    ]

    def _prune_list(items: Iterable[Dict[str, Any]]) -> None:
        for entry in items or []:
            if isinstance(entry, dict) and "evidence_ids" in entry:
                entry["evidence_ids"] = [eid for eid in entry.get("evidence_ids", []) if eid in allowed]

    for component in doc.get("components", []) or []:
        for key in section_keys:
            _prune_list(component.get(key) or [])


def _compute_llm_subscore(claims: Sequence[Dict[str, Any]]) -> float:
    total = 0
    with_evidence = 0
    for claim in claims or []:
        if not isinstance(claim, dict):
            continue
        total += 1
        if claim.get("evidence_ids"):
            with_evidence += 1
    if total == 0:
        return 0.0
    return round(with_evidence / total, 3)


def _compute_group_subscore(group_doc: Dict[str, Any]) -> float:
    totals = []
    for component in group_doc.get("components", []) or []:
        totals.extend(component.get("what_it_does", []) or [])
    return _compute_llm_subscore(totals)


def _calculate_cost(usage: Optional[Dict[str, Any]], pricing: Dict[str, Any]) -> Optional[float]:
    if not usage:
        return None
    in_tokens = usage.get("input_tokens")
    out_tokens = usage.get("output_tokens")
    if in_tokens is None or out_tokens is None:
        return None
    in_price = pricing.get("input_tokens_per_million_usd")
    out_price = pricing.get("output_tokens_per_million_usd")
    if in_price is None or out_price is None:
        return None
    cost = (in_tokens / 1_000_000.0) * float(in_price) + (out_tokens / 1_000_000.0) * float(out_price)
    return round(cost, 6)


def _build_component_payloads(
    group_id: str,
    group_response: Dict[str, Any],
    component_contexts: Dict[str, Dict[str, Any]],
    publish_threshold: float,
) -> Dict[str, Dict[str, Any]]:
    payloads: Dict[str, Dict[str, Any]] = {}
    components = group_response.get("components") or []
    for component in components:
        comp_id = str(component.get("id") or component.get("name") or "component")
        ctx = component_contexts.get(comp_id) or {}
        comp_doc = {
            "group_id": group_id,
            "component_id": comp_id,
            "title": component.get("name") or comp_id,
            "summary": component.get("summary") or "",
            "component": {
                "name": component.get("name") or comp_id,
                "what_it_does": component.get("what_it_does") or [],
                "interfaces": component.get("interfaces") or [],
                "data_highlights": component.get("data_highlights") or [],
                "risks_gaps": component.get("risks_gaps") or [],
                "user_experience": component.get("user_experience") or [],
                "risk_stories": component.get("risk_stories") or [],
                "operational_behaviors": component.get("operational_behaviors") or [],
                "data_flows": component.get("data_flows") or [],
                "journey_blueprints": component.get("journey_blueprints") or [],
                "relationships_summary": component.get("relationships_summary") or [],
                "dependency_matrix": component.get("dependency_matrix") or [],
            },
            "evidence_used": component.get("evidence_used") or [],
            "llm_subscore": _compute_llm_subscore(component.get("what_it_does") or []),
            "approved": False,
            "provenance": dict(group_response.get("provenance") or {}),
        }
        comp_doc.setdefault("provenance", {})["relationship_stats"] = _relationship_stats_from_context(ctx)
        _enrich_component_sections(comp_doc["component"], ctx)
        comp_doc["approved"] = bool(comp_doc["llm_subscore"] >= publish_threshold)
        payloads[comp_id] = {"document": comp_doc, "sirs": ctx.get("sirs", [])}
    return payloads


def _relationship_stats_from_context(context: Dict[str, Any]) -> Dict[str, Any]:
    art_count = sum(len(a.get("relationships") or []) for a in context.get("artifacts", []))
    sir_count = sum(len(s.get("relationships") or []) for s in context.get("sirs", []))
    return {"artifact_relationships": art_count, "sir_relationships": sir_count}


def _fallback_journey_blueprints(context: Dict[str, Any], limit: int = 2) -> List[Dict[str, Any]]:
    candidates: List[Dict[str, Any]] = []
    for bp in context.get("journey_blueprints_input", []):
        steps = [str(step).strip() for step in bp.get("steps", []) if str(step).strip()]
        if len(steps) < 2:
            continue
        candidates.append(
            {
                "title": bp.get("name") or bp.get("sir_id") or "Journey",
                "steps": steps,
                "evidence_ids": [],
            }
        )
        if len(candidates) >= limit:
            break
    return candidates


def _enrich_component_sections(component: Dict[str, Any], context: Dict[str, Any]) -> None:
    """
    Ensure key narrative sections are populated, deriving content from context data
    (experience packs, journey blueprints, process flows) when the LLM omitted them.
    """
    if not component.get("user_experience"):
        entries: List[Dict[str, Any]] = []
        for pack in context.get("experience_packs", [])[:3]:
            screenshots = _flatten_screenshots(pack.get("screenshots"))
            narrative = pack.get("summary") or f"{pack.get('component')} experience"
            entries.append(
                {
                    "narrative": narrative,
                    "screenshots": screenshots,
                    "evidence_ids": [],
                }
            )
        component["user_experience"] = entries

    if not component.get("journey_blueprints"):
        blueprints = _fallback_journey_blueprints(context, limit=3)
        component["journey_blueprints"] = blueprints

    if not component.get("relationships_summary"):
        rels = []
        for rel in context.get("process_flows", [])[:5]:
            source = rel.get("source")
            target = rel.get("target")
            op = rel.get("operation") or rel.get("target_kind")
            if source and target:
                rels.append({"flow": f"{source} -> {target} ({op})", "evidence_ids": []})
        component["relationships_summary"] = rels

    if not component.get("dependency_matrix"):
        matrix = []
        counts: Dict[tuple, int] = {}
        for rel in context.get("process_flows", []):
            key = (rel.get("target_kind") or "unknown", rel.get("operation") or "touches")
            counts[key] = counts.get(key, 0) + 1
        for (kind, op), count in counts.items():
            matrix.append({"target_kind": kind, "operation": op, "count": count, "evidence_ids": []})
        component["dependency_matrix"] = matrix


def _flatten_screenshots(items: Optional[List[Any]]) -> List[str]:
    paths: List[str] = []
    for shot in items or []:
        if isinstance(shot, str):
            paths.append(shot)
        elif isinstance(shot, dict) and shot.get("path"):
            paths.append(str(shot.get("path")))
    return paths


def rollup_group_and_persist(group_id: str, group_obj: Dict[str, Any], out_dir: Optional[str | Path] = None) -> Dict[str, Any]:
    settings = get_all_settings()
    out_base = Path(out_dir or settings["out_dir"]).resolve()

    if not _provider_ready(settings):
        return {"skipped": True}

    raw_index = build_evidence_index(out_base)
    evidence_index = sanitize_evidence_index(raw_index)

    group_context = build_group_context(group_id, group_obj, evidence_index)
    if not group_context["artifacts"] and not group_context["sirs"]:
        rprint(f"[yellow][llm] Skipping rollup for {group_id}: no artifacts or SIRs.[/yellow]")
        return {"skipped": True}

    prompt_context = summarize_context_for_prompt(group_context)
    relationship_stats = _relationship_stats_from_context(prompt_context)
    prompt = build_group_prompt(prompt_context)

    llm_settings = settings.get("llm", {})
    publish_threshold = settings.get("rollup", {}).get("publish_threshold", 0.75)
    pricing = (llm_settings.get("telemetry") or {}).get("pricing") or {}

    prompt_hash = _hash_text(prompt)
    input_hash = _hash_text(json.dumps(prompt_context, sort_keys=True))

    try:
        response = call_openai_meta(prompt=prompt, json_schema=GROUP_RESPONSE_SCHEMA)
    except Exception as exc:
        rprint(f"[yellow][llm] Rollup call failed for {group_id}: {exc}[/yellow]")
        return {"error": str(exc)}

    usage = response.get("usage") or {}
    latency_ms = response.get("latency_ms")
    raw_text = response.get("text", "")

    group_json = _parse_response_text(raw_text)
    group_json.setdefault("group_id", group_id)
    group_json.setdefault("provenance", {})
    group_json["provenance"].update(
        {
            "model": llm_settings.get("model", ""),
            "provider": llm_settings.get("provider", ""),
            "generated_at": time.time(),
            "prompt_hash": prompt_hash,
            "input_hash": input_hash,
            "usage": usage,
            "latency_ms": latency_ms,
            "relationship_stats": relationship_stats,
        }
    )

    allowed_ids = set(evidence_index.keys())
    _prune_evidence_ids(group_json, allowed_ids)
    group_json["llm_subscore"] = _compute_group_subscore(group_json)
    group_json["approved"] = bool(group_json["llm_subscore"] >= publish_threshold)

    try:
        validate_group_response(group_json)
    except ValidationError as exc:
        raise RuntimeError(f"Group rollup failed schema validation: {exc}") from exc

    cost = _calculate_cost(usage, pricing)
    append_usage_row(out_base, scope="group", scope_id=group_id, usage=usage, cost=cost, latency_ms=latency_ms)

    persist_group_outputs(out_base, group_id, group_json, group_context.get("sirs", []))

    component_contexts = build_component_contexts(group_context)
    component_payloads = _build_component_payloads(group_id, group_json, component_contexts, publish_threshold)

    for comp_id, payload in component_payloads.items():
        document = payload["document"]
        try:
            validate_component_response(document)
        except ValidationError:
            # Component schema is best-effort; continue even if validation fails.
            continue
        persist_component_outputs(out_base, group_id, comp_id, document, payload.get("sirs", []))

    return group_json
