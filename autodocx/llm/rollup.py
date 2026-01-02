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
        "why_it_matters",
        "interfaces",
        "invokes",
        "key_inputs",
        "key_outputs",
        "extrapolations",
        "traceability",
    ]

    def _prune_list(items: Iterable[Dict[str, Any]]) -> None:
        for entry in items or []:
            if isinstance(entry, dict) and "evidence_ids" in entry:
                entry["evidence_ids"] = [eid for eid in entry.get("evidence_ids", []) if eid in allowed]

    def _prune_errors_logging(section: Dict[str, Any]) -> None:
        if not isinstance(section, dict):
            return
        _prune_list(section.get("errors") or [])
        _prune_list(section.get("logging") or [])

    def _prune_interdependencies(section: Dict[str, Any]) -> None:
        if not isinstance(section, dict):
            return
        _prune_list(section.get("calls") or [])
        _prune_list(section.get("called_by") or [])
        _prune_list(section.get("shared_data") or [])

    for component in doc.get("components", []) or []:
        for key in section_keys:
            _prune_list(component.get(key) or [])
        _prune_errors_logging(component.get("errors_and_logging"))
        _prune_interdependencies(component.get("interdependencies"))


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
                "summary": component.get("summary") or "",
                "what_it_does": component.get("what_it_does") or [],
                "why_it_matters": component.get("why_it_matters") or [],
                "interfaces": component.get("interfaces") or [],
                "invokes": component.get("invokes") or [],
                "key_inputs": component.get("key_inputs") or [],
                "key_outputs": component.get("key_outputs") or [],
                "errors_and_logging": component.get("errors_and_logging")
                or {"errors": [], "logging": []},
                "interdependencies": component.get("interdependencies")
                or {"calls": [], "called_by": [], "shared_data": []},
                "extrapolations": component.get("extrapolations") or [],
                "traceability": component.get("traceability") or [],
                "journey_blueprints": component.get("journey_blueprints") or [],
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
    if not component.get("what_it_does"):
        component["what_it_does"] = _fallback_claims_from_sirs(context, limit=4)
    if not component.get("why_it_matters"):
        component["why_it_matters"] = _fallback_why_from_claims(component.get("what_it_does") or [], limit=3)
    if not component.get("interfaces"):
        component["interfaces"] = _fallback_interfaces_from_sirs(context.get("sirs", []))
    if not component.get("invokes"):
        component["invokes"] = _fallback_invokes_from_flows(context.get("process_flows", []))
    if not component.get("key_inputs"):
        component["key_inputs"] = _fallback_io_entries(context, example_key="inputs_example")
    if not component.get("key_outputs"):
        component["key_outputs"] = _fallback_io_entries(context, example_key="outputs_example")
    errs = component.get("errors_and_logging") or {}
    errs.setdefault("errors", [])
    errs.setdefault("logging", [])
    if not errs["errors"] and not errs["logging"]:
        errs = _fallback_errors_logging(context)
    component["errors_and_logging"] = errs
    interdeps = component.get("interdependencies") or {}
    if not any(interdeps.get(key) for key in ("calls", "called_by", "shared_data")):
        interdeps = _fallback_interdependencies(context)
    component["interdependencies"] = interdeps
    if not component.get("traceability"):
        component["traceability"] = _fallback_traceability(context)
    if not component.get("journey_blueprints"):
        component["journey_blueprints"] = _fallback_journey_blueprints(context, limit=3)
    component.setdefault("extrapolations", [])


def _fallback_claims_from_sirs(context: Dict[str, Any], limit: int = 4) -> List[Dict[str, Any]]:
    claims: List[Dict[str, Any]] = []
    for sir in context.get("sirs", []):
        summary = sir.get("user_story") or f"{sir.get('name')} workflow"
        if not summary:
            continue
        claims.append(
            {
                "summary": summary,
                "detail": summary,
                "evidence_ids": sir.get("evidence_ids") or [],
            }
        )
        if len(claims) >= limit:
            break
    return claims


def _fallback_why_from_claims(claims: Sequence[Dict[str, Any]], limit: int = 3) -> List[Dict[str, Any]]:
    reasons: List[Dict[str, Any]] = []
    for claim in claims[:limit]:
        reasons.append(
            {
                "impact": claim.get("summary") or claim.get("detail") or "Business impact",
                "detail": claim.get("detail") or "",
                "evidence_ids": claim.get("evidence_ids") or [],
            }
        )
    if not reasons:
        reasons.append(
            {
                "impact": "Business impact unclear [cannot_conclude]",
                "detail": "Evidence not available to explain why this workflow matters.",
                "evidence_ids": [],
            }
        )
    return reasons


def _fallback_interfaces_from_sirs(sirs: Iterable[Dict[str, Any]], limit: int = 5) -> List[Dict[str, Any]]:
    entries: List[Dict[str, Any]] = []
    seen = set()
    for sir in sirs or []:
        for trig in sir.get("triggers") or []:
            key = (sir.get("name"), trig.get("type"), trig.get("path"))
            if key in seen:
                continue
            seen.add(key)
            entries.append(
                {
                    "name": sir.get("name") or trig.get("name") or "Interface",
                    "kind": trig.get("type") or "interface",
                    "endpoint": trig.get("path") or trig.get("name") or "",
                    "method": trig.get("method") or "",
                    "description": sir.get("user_story") or trig.get("type") or "",
                    "evidence_ids": sir.get("evidence_ids") or [],
                }
            )
            if len(entries) >= limit:
                return entries
    return entries


def _fallback_invokes_from_flows(flows: Iterable[Dict[str, Any]], limit: int = 6) -> List[Dict[str, Any]]:
    entries: List[Dict[str, Any]] = []
    seen = set()
    for rel in flows or []:
        target = rel.get("target")
        if not target:
            continue
        key = (target, rel.get("target_kind"), rel.get("operation"))
        if key in seen:
            continue
        seen.add(key)
        entries.append(
            {
                "target": target,
                "kind": rel.get("target_kind") or rel.get("operation") or "dependency",
                "operation": rel.get("operation") or "touches",
                "direction": "outbound",
                "evidence_ids": [],
            }
        )
        if len(entries) >= limit:
            break
    return entries


def _describe_example(example: Any) -> str:
    if isinstance(example, dict):
        parts = []
        for key, value in example.items():
            if isinstance(value, list):
                parts.append(f"{key}: {', '.join(str(v) for v in value[:5])}")
            elif isinstance(value, dict):
                parts.append(f"{key}: {', '.join(f'{k}={v}' for k, v in value.items())}")
            else:
                parts.append(f"{key}: {value}")
        return "; ".join(parts)
    return str(example)


def _fallback_io_entries(context: Dict[str, Any], example_key: str, limit: int = 4) -> List[Dict[str, Any]]:
    entries: List[Dict[str, Any]] = []
    for sir in context.get("sirs", []):
        sample = sir.get(example_key)
        if not sample:
            continue
        entries.append(
            {
                "name": sir.get("name") or example_key,
                "description": _describe_example(sample),
                "evidence_ids": sir.get("evidence_ids") or [],
            }
        )
        if len(entries) >= limit:
            return entries
    for payload in context.get("payload_examples", []):
        sample = payload.get("inputs") if example_key == "inputs_example" else payload.get("outputs")
        if not sample:
            continue
        entries.append(
            {
                "name": payload.get("source") or example_key,
                "description": _describe_example(sample),
                "evidence_ids": [],
            }
        )
        if len(entries) >= limit:
            break
    return entries


def _fallback_errors_logging(context: Dict[str, Any], limit: int = 4) -> Dict[str, List[Dict[str, Any]]]:
    errors: List[Dict[str, Any]] = []
    logging: List[Dict[str, Any]] = []
    for sir in context.get("sirs", []):
        for step in sir.get("steps") or []:
            name = str(step.get("name") or step.get("type") or "")
            lowered = name.lower()
            entry = {"description": f"{sir.get('name')} – {name}", "evidence_ids": sir.get("evidence_ids") or []}
            if any(keyword in lowered for keyword in ("error", "fault", "catch", "exception")):
                if len(errors) < limit:
                    errors.append(entry.copy())
            if any(keyword in lowered for keyword in ("log", "audit", "trace")):
                if len(logging) < limit:
                    logging.append(entry.copy())
            if len(errors) >= limit and len(logging) >= limit:
                break
    return {"errors": errors, "logging": logging}


def _fallback_interdependencies(context: Dict[str, Any]) -> Dict[str, List[Dict[str, Any]]]:
    calls: List[Dict[str, Any]] = []
    called_by: List[Dict[str, Any]] = []
    shared: List[Dict[str, Any]] = []
    seen_calls = set()
    seen_called = set()
    seen_shared = set()
    for row in context.get("interdependency_inputs", []):
        process = row.get("process") or "Process"
        for partner in row.get("calls") or []:
            key = (process, partner, "calls")
            if key in seen_calls:
                continue
            seen_calls.add(key)
            calls.append(
                {"partner": partner, "description": f"{process} calls {partner}", "evidence_ids": []}
            )
        for partner in row.get("called_by") or []:
            key = (process, partner, "called_by")
            if key in seen_called:
                continue
            seen_called.add(key)
            called_by.append(
                {"partner": partner, "description": f"{partner} calls {process}", "evidence_ids": []}
            )
        shared_with = (row.get("shared_identifiers_with") or []) + (row.get("shared_datastores_with") or [])
        for partner in shared_with:
            key = (process, partner, "shared")
            if key in seen_shared:
                continue
            seen_shared.add(key)
            shared.append(
                {"partner": partner, "description": f"{process} shares data with {partner}", "evidence_ids": []}
            )
    return {"calls": calls[:5], "called_by": called_by[:5], "shared_data": shared[:5]}


def _fallback_traceability(context: Dict[str, Any], limit: int = 10) -> List[Dict[str, Any]]:
    entries: List[Dict[str, Any]] = []
    for item in context.get("traceability_inputs", []):
        entries.append(
            {
                "artifact": item.get("artifact") or "artifact",
                "signal_type": item.get("signal_type") or "artifact",
                "description": item.get("description") or "",
                "evidence_ids": item.get("evidence_ids") or [],
            }
        )
        if len(entries) >= limit:
            break
    return entries


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
