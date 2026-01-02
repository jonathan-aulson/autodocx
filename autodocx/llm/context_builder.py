from __future__ import annotations

import json
from pathlib import Path
from typing import Any, Dict, Iterable, List, Tuple


def _shorten(text: Any, limit: int = 800) -> str:
    value = str(text or "")
    if len(value) <= limit:
        return value
    return value[: limit - 3] + "..."


def _as_list(value: Any) -> List[Any]:
    if isinstance(value, list):
        return value
    if value is None or value == "":
        return []
    return [value]


def _safe_anchor(ev: Any) -> Dict[str, str]:
    if isinstance(ev, dict):
        return {
            "path": str(ev.get("path", "")),
            "lines": str(ev.get("lines", "")),
            "snippet": _shorten(ev.get("snippet", ""), 800),
        }
    return {"path": "", "lines": "", "snippet": _shorten(ev, 800)}


def sanitize_evidence_index(raw_index: Dict[str, Any]) -> Dict[str, Dict[str, str]]:
    sanitized: Dict[str, Dict[str, str]] = {}
    for key, value in (raw_index or {}).items():
        sanitized[str(key)] = _safe_anchor(value)
    return sanitized


def _artifact_identifier(item: Dict[str, Any]) -> str:
    name = item.get("name")
    if name:
        return str(name)
    repo_path = item.get("repo_path")
    if repo_path:
        return Path(str(repo_path)).stem or "artifact"
    return "artifact"


def sanitize_artifacts(items: Iterable[Any]) -> List[Dict[str, Any]]:
    sanitized: List[Dict[str, Any]] = []
    for item in items or []:
        if not isinstance(item, dict):
            sanitized.append({"name": str(item), "artifact_type": "unknown", "component_or_service": ""})
            continue
        name = str(item.get("name") or item.get("artifact_type") or "artifact")
        repo_path = str(item.get("repo_path") or "")
        component = str(item.get("component_or_service") or "")
        steps_summary = ""
        workflows = item.get("workflows")
        if isinstance(workflows, list) and workflows:
            first = workflows[0]
            if isinstance(first, dict):
                steps_summary = str(first.get("steps_summary") or first.get("name") or "")
            else:
                steps_summary = str(first)
        elif isinstance(workflows, dict):
            steps_summary = str(workflows.get("steps_summary") or workflows.get("name") or "")
        elif isinstance(workflows, str):
            steps_summary = workflows

        evidence = [_safe_anchor(ev) for ev in _as_list(item.get("evidence", []))]
        artifact_id = _artifact_identifier(item)
        evidence_ids = [f"artifact:{artifact_id}#e{idx}" for idx in range(len(evidence))]
        relationships = item.get("relationships") or []
        rel_matrix = item.get("relationship_matrix") or {}
        if relationships and len(relationships) > 50:
            relationships = relationships[:50]
        code_entities = item.get("code_entities") or []
        ui_components = item.get("ui_components") or []
        integrations = item.get("integrations") or []
        process_diagrams = item.get("process_diagrams") or []
        business_entities = item.get("business_entities") or []

        sanitized.append(
            {
                "name": name,
                "artifact_type": str(item.get("artifact_type") or ""),
                "repo_path": repo_path,
                "component_or_service": component,
                "confidence": item.get("confidence"),
                "steps_summary": _shorten(steps_summary, 1000),
                "evidence": evidence,
                "evidence_ids": evidence_ids,
                "relationships": relationships,
                "relationship_matrix": rel_matrix,
                "code_entities": code_entities,
                "ui_components": ui_components,
                "integrations": integrations,
                "process_diagrams": process_diagrams,
                "business_entities": business_entities,
                "personas": item.get("personas") or [],
                "primary_journeys": item.get("primary_journeys") or [],
                "ux_summaries": item.get("ux_summaries") or [],
                "before_after": item.get("before_after") or [],
                "screenshots": item.get("screenshots") or [],
                "data_examples": item.get("data_examples") or [],
                "experience_pack": item.get("experience_pack") or {},
                "artifact_id": artifact_id,
            }
        )
    return sanitized


def sanitize_sirs(items: Iterable[Any]) -> List[Dict[str, Any]]:
    sanitized: List[Dict[str, Any]] = []
    for item in items or []:
        if not isinstance(item, dict):
            sanitized.append({"id": str(item)})
            continue
        sid = str(item.get("id") or item.get("name") or "")
        props = item.get("props") or {}
        scaffold = item.get("business_scaffold") or props.get("business_scaffold") or {}
        triggers = props.get("triggers") or []
        steps = props.get("steps") or []
        relationships = props.get("relationships") or item.get("relationships") or []
        rel_matrix = props.get("relationship_matrix") or item.get("relationship_matrix") or {}
        screenshots = _as_list(props.get("screenshots") or [])
        formatted_screenshots = []
        for shot in screenshots:
            if isinstance(shot, dict):
                formatted_screenshots.append({"path": shot.get("path"), "caption": shot.get("caption")})
            else:
                formatted_screenshots.append({"path": str(shot), "caption": item.get("name") or sid})
        display_names = props.get("step_display_names")
        if not display_names:
            single_name = props.get("step_display_name")
            if single_name:
                display_names = [single_name]
        sir_evidence = [_safe_anchor(ev) for ev in _as_list(item.get("evidence", []))]
        evidence_ids = [f"{sid}#e{idx}" for idx in range(len(sir_evidence))]
        interfaces = scaffold.get("interfaces") or []
        invocations = scaffold.get("invocations") or []
        logging = scaffold.get("logging") or props.get("logging") or []
        errors = scaffold.get("errors") or []
        sanitized.append(
            {
                "id": sid,
                "name": str(item.get("name") or sid),
                "file": str(item.get("file") or props.get("file") or ""),
                "component_or_service": str(item.get("component_or_service") or props.get("component") or ""),
                "kind": str(item.get("kind") or props.get("kind") or ""),
                "roles": _as_list(item.get("roles")),
                "subscores": item.get("subscores") or {},
                "evidence": sir_evidence,
                "evidence_ids": evidence_ids,
                "roles_evidence": {
                    str(role): [_safe_anchor(ev) for ev in _as_list(evs)]
                    for role, evs in (item.get("roles_evidence") or {}).items()
                },
                "triggers": triggers,
                "steps": steps,
                "relationships": relationships,
                "relationship_matrix": rel_matrix,
                "user_story": props.get("user_story"),
                "inputs_example": props.get("inputs_example"),
                "outputs_example": props.get("outputs_example"),
                "latency_hints": props.get("latency_hints"),
                "journey_touchpoints": props.get("journey_touchpoints"),
                "step_display_names": display_names or [],
                "ui_snapshot": props.get("ui_snapshot"),
                "screenshots": formatted_screenshots,
                "data_samples": props.get("data_samples") or [],
                "interfaces": interfaces,
                "invocations": invocations,
                "logging": logging,
                "errors": errors,
                "family": item.get("family") or props.get("family"),
                "module": item.get("module_name") or props.get("module_name"),
                "module_root": item.get("module_root") or props.get("module_root"),
                "constellation": item.get("constellation_id") or props.get("constellation_id"),
                "deterministic_explanation": item.get("deterministic_explanation") or {},
                "extrapolations": item.get("extrapolations") or [],
                "interdependencies": item.get("interdependencies_slice") or {},
            }
        )
    return sanitized


def build_group_context(group_id: str, group_obj: Dict[str, Any], evidence_index: Dict[str, Dict[str, str]]) -> Dict[str, Any]:
    artifacts = sanitize_artifacts((group_obj or {}).get("artifacts") or [])
    sirs = sanitize_sirs((group_obj or {}).get("sirs") or [])
    process_flows = _synthesize_process_flows(sirs)
    integration_summary = _summarize_integrations(artifacts)
    experience_packs = _collect_experience_packs(artifacts, sirs)
    ui_snapshots = _collect_ui_snapshots(artifacts, sirs)
    payload_examples = _collect_payload_examples(artifacts, sirs)
    journey_blueprints_input = _build_journey_blueprints_input(sirs)
    recent_changes = group_obj.get("recent_changes") or {}
    traceability_inputs = _build_traceability_inputs(artifacts, sirs)
    interdependency_inputs = _collect_interdependency_inputs(sirs)
    return {
        "group_id": group_id,
        "artifacts": artifacts,
        "sirs": sirs,
        "evidence_index": evidence_index,
        "process_flows": process_flows,
        "integration_summary": integration_summary,
        "experience_packs": experience_packs,
        "ui_snapshots": ui_snapshots,
        "payload_examples": payload_examples,
        "stitched_timelines": journey_blueprints_input,
        "journey_blueprints_input": journey_blueprints_input,
        "recent_changes": recent_changes,
        "raw": group_obj or {},
        "traceability_inputs": traceability_inputs,
        "interdependency_inputs": interdependency_inputs,
    }


def _partition_by_component(artifacts: Iterable[Dict[str, Any]], sirs: Iterable[Dict[str, Any]]) -> Dict[str, Dict[str, List[Dict[str, Any]]]]:
    buckets: Dict[str, Dict[str, List[Dict[str, Any]]]] = {}
    for art in artifacts:
        comp = art.get("component_or_service") or "ungrouped"
        bucket = buckets.setdefault(comp, {"artifacts": [], "sirs": []})
        bucket["artifacts"].append(art)
    for sir in sirs:
        comp = sir.get("component_or_service") or "ungrouped"
        bucket = buckets.setdefault(comp, {"artifacts": [], "sirs": []})
        bucket["sirs"].append(sir)
    return buckets


def build_component_contexts(group_context: Dict[str, Any]) -> Dict[str, Dict[str, Any]]:
    partitions = _partition_by_component(group_context.get("artifacts", []), group_context.get("sirs", []))
    evidence_index = group_context.get("evidence_index", {})
    result: Dict[str, Dict[str, Any]] = {}
    for component, payload in partitions.items():
        process_flows = _synthesize_process_flows(payload.get("sirs", []))
        integration_summary = _summarize_integrations(payload.get("artifacts", []))
        experience_packs = _collect_experience_packs(payload.get("artifacts", []), payload.get("sirs", []))
        ui_snapshots = _collect_ui_snapshots(payload.get("artifacts", []), payload.get("sirs", []))
        payload_examples = _collect_payload_examples(payload.get("artifacts", []), payload.get("sirs", []))
        journey_blueprints_input = _build_journey_blueprints_input(payload.get("sirs", []))
        traceability_inputs = _build_traceability_inputs(payload.get("artifacts", []), payload.get("sirs", []))
        interdependency_inputs = _collect_interdependency_inputs(payload.get("sirs", []))
        result[component] = {
            "group_id": group_context.get("group_id"),
            "component_id": component,
            "artifacts": payload.get("artifacts", []),
            "sirs": payload.get("sirs", []),
            "evidence_index": evidence_index,
            "process_flows": process_flows,
            "integration_summary": integration_summary,
            "experience_packs": experience_packs,
            "ui_snapshots": ui_snapshots,
            "payload_examples": payload_examples,
            "stitched_timelines": journey_blueprints_input,
            "journey_blueprints_input": journey_blueprints_input,
            "recent_changes": group_context.get("recent_changes", {}),
            "traceability_inputs": traceability_inputs,
            "interdependency_inputs": interdependency_inputs,
        }
    return result


def summarize_context_for_prompt(context: Dict[str, Any], *, limit: int = 10) -> Dict[str, Any]:
    """Trim large context objects before serializing into prompts."""
    artifacts = context.get("artifacts", [])[:limit]
    sirs = context.get("sirs", [])[:limit]
    for sir in sirs:
        sir.setdefault("interfaces", sir.get("interfaces") or [])
        sir.setdefault("invocations", sir.get("invocations") or [])
        sir.setdefault("logging", sir.get("logging") or [])
        sir.setdefault("errors", sir.get("errors") or [])
    return {
        "group_id": context.get("group_id"),
        "component_id": context.get("component_id"),
        "artifacts": artifacts,
        "sirs": sirs,
        "evidence_index": context.get("evidence_index"),
        "process_flows": context.get("process_flows", [])[:limit],
        "integration_summary": context.get("integration_summary", [])[:limit],
        "experience_packs": context.get("experience_packs", [])[:limit],
        "ui_snapshots": context.get("ui_snapshots", [])[:limit],
        "payload_examples": context.get("payload_examples", [])[:limit],
        "stitched_timelines": context.get("stitched_timelines", [])[:limit],
        "journey_blueprints_input": context.get("journey_blueprints_input", [])[:limit],
        "recent_changes": context.get("recent_changes", {}),
        "traceability_inputs": context.get("traceability_inputs", [])[:limit],
        "interdependency_inputs": context.get("interdependency_inputs", [])[:limit],
    }


def _collect_relationships_for_sirs(sirs: Iterable[Dict[str, Any]]) -> List[Dict[str, Any]]:
    rels: List[Dict[str, Any]] = []
    for sir in sirs or []:
        rels.extend(sir.get("relationships") or (sir.get("props") or {}).get("relationships") or [])
    return rels


def _collect_ui_snapshots(
    artifacts: Iterable[Dict[str, Any]], sirs: Iterable[Dict[str, Any]], limit: int = 30
) -> List[Dict[str, Any]]:
    shots: List[Dict[str, Any]] = []
    seen = set()

    def _add(path: str, caption: str, source: str) -> None:
        if not path:
            return
        key = (path, caption or "", source or "")
        if key in seen:
            return
        seen.add(key)
        shots.append({"path": path, "caption": caption or "", "source": source or ""})

    for art in artifacts or []:
        for shot in art.get("screenshots") or []:
            if isinstance(shot, dict):
                _add(str(shot.get("path") or ""), str(shot.get("caption") or art.get("name") or ""), art.get("name") or "")
            else:
                _add(str(shot), art.get("name") or "", art.get("name") or "")

    for sir in sirs or []:
        snapshot = sir.get("ui_snapshot")
        if snapshot:
            _add(str(snapshot), sir.get("name") or "", sir.get("name") or "")
        for shot in sir.get("screenshots") or []:
            if isinstance(shot, dict):
                _add(str(shot.get("path") or ""), str(shot.get("caption") or sir.get("name") or ""), sir.get("name") or "")
            else:
                _add(str(shot), sir.get("name") or "", sir.get("name") or "")

    return shots[:limit]


def _collect_payload_examples(
    artifacts: Iterable[Dict[str, Any]], sirs: Iterable[Dict[str, Any]], limit: int = 30
) -> List[Dict[str, Any]]:
    examples: List[Dict[str, Any]] = []
    for art in artifacts or []:
        for sample in art.get("data_examples") or []:
            if not isinstance(sample, dict):
                continue
            examples.append({"source": art.get("name") or "", **sample})
    for sir in sirs or []:
        entry: Dict[str, Any] = {}
        if sir.get("inputs_example"):
            entry["inputs"] = sir["inputs_example"]
        if sir.get("outputs_example"):
            entry["outputs"] = sir["outputs_example"]
        if sir.get("data_samples"):
            entry["data_samples"] = sir["data_samples"]
        if entry:
            entry["source"] = sir.get("name") or sir.get("id")
            examples.append(entry)
    return examples[:limit]


def _collect_experience_packs(
    artifacts: Iterable[Dict[str, Any]], sirs: Iterable[Dict[str, Any]], limit: int = 20
) -> List[Dict[str, Any]]:
    packs: List[Dict[str, Any]] = []
    seen = set()
    for art in artifacts or []:
        pack = art.get("experience_pack")
        if not isinstance(pack, dict):
            continue
        pack_id = pack.get("id") or f"{art.get('name')}:{art.get('artifact_type')}"
        if pack_id in seen:
            continue
        seen.add(pack_id)
        packs.append(
            {
                "id": pack_id,
                "component": pack.get("component") or art.get("component_or_service") or "",
                "kind": pack.get("kind") or art.get("artifact_type") or "",
                "summary": pack.get("summary") or "",
                "touchpoints": pack.get("touchpoints") or [],
                "inputs_example": pack.get("inputs_example") or {},
                "outputs_example": pack.get("outputs_example") or {},
                "screenshots": pack.get("screenshots") or [],
            }
        )
    for sir in sirs or []:
        if len(packs) >= limit:
            break
        if not (sir.get("user_story") or sir.get("screenshots") or sir.get("ui_snapshot")):
            continue
        sid = sir.get("id") or sir.get("name")
        if not sid or sid in seen:
            continue
        seen.add(sid)
        screenshot_entries = sir.get("screenshots") or []
        if not screenshot_entries and sir.get("ui_snapshot"):
            screenshot_entries = [{"path": sir.get("ui_snapshot"), "caption": sir.get("name")}]
        packs.append(
            {
                "id": sid,
                "component": sir.get("component_or_service") or "",
                "kind": sir.get("kind") or "",
                "summary": sir.get("user_story") or "",
                "touchpoints": sir.get("journey_touchpoints") or sir.get("step_display_names") or [],
                "inputs_example": sir.get("inputs_example") or {},
                "outputs_example": sir.get("outputs_example") or {},
                "screenshots": screenshot_entries,
            }
        )
    return packs[:limit]


def _build_journey_blueprints_input(sirs: Iterable[Dict[str, Any]], limit: int = 10) -> List[Dict[str, Any]]:
    blueprints: List[Dict[str, Any]] = []
    for sir in sirs or []:
        steps = sir.get("steps") or []
        if not steps:
            continue
        labels: List[str] = []
        for step in steps:
            name = str(step.get("name") or "").strip() or "Step"
            connector = str(step.get("connector") or step.get("type") or "").strip()
            if connector:
                labels.append(f"{name} ({connector})")
            else:
                labels.append(name)
            if len(labels) >= 20:
                break
        if len(labels) < 2:
            continue

        connector_set = sorted(
            {
                str(step.get("connector") or step.get("type") or "").strip()
                for step in steps
                if (step.get("connector") or step.get("type"))
            }
        )
        trigger_set = sorted(
            {
                str(trig.get("type") or trig.get("name") or "").strip()
                for trig in sir.get("triggers") or []
                if (trig.get("type") or trig.get("name"))
            }
        )

        blueprints.append(
            {
                "sir_id": sir.get("id"),
                "name": sir.get("name"),
                "user_story": sir.get("user_story"),
                "steps": labels,
                "connectors": [c for c in connector_set if c],
                "trigger_types": [t for t in trigger_set if t],
            }
        )
        if len(blueprints) >= limit:
            break
    return blueprints


def _synthesize_process_flows(sirs: Iterable[Dict[str, Any]], limit: int = 10) -> List[Dict[str, Any]]:
    flows: List[Dict[str, Any]] = []
    seen = set()
    for rel in _collect_relationships_for_sirs(sirs):
        source = (rel.get("source") or {}).get("name") or (rel.get("source") or {}).get("type")
        target = (rel.get("target") or {}).get("display") or (rel.get("target") or {}).get("ref")
        operation = (rel.get("operation") or {}).get("type")
        key = (source, target, operation)
        if not source or not target or key in seen:
            continue
        seen.add(key)
        flows.append(
            {
                "source": source,
                "target": target,
                "operation": operation,
                "target_kind": (rel.get("target") or {}).get("kind"),
                "evidence": (rel.get("evidence") or [])[:1],
            }
        )
        if len(flows) >= limit:
            break
    return flows


def _summarize_integrations(artifacts: Iterable[Dict[str, Any]]) -> List[Dict[str, Any]]:
    counts: Dict[Tuple[str, str], int] = {}
    for art in artifacts or []:
        for integ in art.get("integrations") or []:
            key = ((integ or {}).get("integration_kind") or "unknown", (integ or {}).get("library") or "unknown")
            counts[key] = counts.get(key, 0) + 1
    summary = [
        {"integration_kind": kind, "library": lib, "count": count}
        for (kind, lib), count in counts.items()
    ]
    summary.sort(key=lambda x: (-x["count"], x["integration_kind"]))
    return summary


def _build_traceability_inputs(
    artifacts: Iterable[Dict[str, Any]], sirs: Iterable[Dict[str, Any]], limit: int = 60
) -> List[Dict[str, Any]]:
    entries: List[Dict[str, Any]] = []
    for art in artifacts or []:
        evidence_ids = art.get("evidence_ids") or []
        if not evidence_ids:
            continue
        entries.append(
            {
                "artifact": art.get("name") or art.get("artifact_type") or "artifact",
                "signal_type": art.get("artifact_type") or "artifact",
                "description": art.get("steps_summary") or "",
                "evidence_ids": evidence_ids,
            }
        )
        if len(entries) >= limit:
            return entries
    for sir in sirs or []:
        evidence_ids = sir.get("evidence_ids") or []
        if not evidence_ids:
            continue
        entries.append(
            {
                "artifact": sir.get("name") or sir.get("id"),
                "signal_type": sir.get("kind") or "workflow",
                "description": sir.get("user_story") or sir.get("file") or "",
                "evidence_ids": evidence_ids,
            }
        )
        if len(entries) >= limit:
            break
    return entries


def _collect_interdependency_inputs(
    sirs: Iterable[Dict[str, Any]], limit: int = 40
) -> List[Dict[str, Any]]:
    rows: List[Dict[str, Any]] = []
    for sir in sirs or []:
        interdeps = sir.get("interdependencies") or {}
        if not interdeps:
            continue
        rows.append(
            {
                "process": sir.get("name"),
                "calls": interdeps.get("calls") or interdeps.get("calls", []),
                "called_by": interdeps.get("called_by") or interdeps.get("called_by", []),
                "shared_identifiers_with": interdeps.get("shared_identifiers_with") or [],
                "shared_datastores_with": interdeps.get("shared_datastores_with") or [],
                "component_peers": interdeps.get("component_peers") or [],
                "family_peers": interdeps.get("family_peers") or [],
            }
        )
        if len(rows) >= limit:
            break
    return rows
