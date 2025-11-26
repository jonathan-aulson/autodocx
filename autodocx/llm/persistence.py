from __future__ import annotations

import json
from pathlib import Path
from typing import Any, Dict, Iterable, Optional, List, Tuple

import time

from autodocx.render.business_renderer import render_business_component_page, render_business_group_page


def _safe_slug(value: str) -> str:
    slug = "".join(ch if ch.isalnum() or ch in "-_" else "_" for ch in (value or ""))
    slug = slug.strip("_")
    return slug or "group"


def persist_group_outputs(
    out_dir: Path,
    group_id: str,
    response: Dict[str, Any],
    group_sirs: Optional[Iterable[Dict[str, Any]]] = None,
) -> Path:
    out_dir = Path(out_dir)
    rollup_dir = out_dir / "rollup" / "groups"
    rollup_dir.mkdir(parents=True, exist_ok=True)
    json_path = rollup_dir / f"{_safe_slug(group_id)}.json"
    json_path.write_text(json.dumps(response, indent=2), encoding="utf-8")

    sir_list = list(group_sirs or [])
    extras = _collect_group_extras(sir_list)
    render_business_group_page(
        out_docs_dir=out_dir / "docs",
        group_id=group_id,
        resp_json=response,
        evidence_md_filename=None,
        extras=extras,
    )
    return json_path


def persist_component_outputs(
    out_dir: Path,
    group_id: str,
    component_id: str,
    component_payload: Dict[str, Any],
    sirs: Iterable[Dict[str, Any]],
) -> Path:
    out_dir = Path(out_dir)
    rollup_dir = out_dir / "rollup" / "components"
    rollup_dir.mkdir(parents=True, exist_ok=True)
    json_path = rollup_dir / f"{_safe_slug(group_id)}__{_safe_slug(component_id)}.json"
    json_path.write_text(json.dumps(component_payload, indent=2), encoding="utf-8")

    render_business_component_page(
        out_docs_dir=out_dir / "docs",
        group_id=group_id,
        component_key=component_id,
        c_json=component_payload,
        sirs=list(sirs),
        evidence_md_filename=None,
        facets=None,
        settings=None,
    )
    return json_path


def append_usage_row(
    out_dir: Path,
    *,
    scope: str,
    scope_id: str,
    usage: Optional[Dict[str, Any]],
    cost: Optional[float],
    latency_ms: Optional[int],
) -> None:
    out_dir = Path(out_dir)
    metrics_dir = out_dir / "metrics"
    metrics_dir.mkdir(parents=True, exist_ok=True)
    csv_path = metrics_dir / "llm_usage.csv"
    header = "timestamp,scope,scope_id,input_tokens,output_tokens,total_tokens,cost_usd,latency_ms\n"
    line = "{ts},{scope},{scope_id},{input_tokens},{output_tokens},{total_tokens},{cost},{latency}\n".format(
        ts=int(time.time()),
        scope=scope,
        scope_id=scope_id,
        input_tokens=(usage or {}).get("input_tokens", ""),
        output_tokens=(usage or {}).get("output_tokens", ""),
        total_tokens=(usage or {}).get("total_tokens", ""),
        cost="" if cost is None else f"{cost:.6f}",
        latency="" if latency_ms is None else latency_ms,
    )
    if not csv_path.exists():
        csv_path.write_text(header + line, encoding="utf-8")
    else:
        with csv_path.open("a", encoding="utf-8") as fh:
            fh.write(line)


def _collect_group_extras(group_sirs: Iterable[Dict[str, Any]]) -> Dict[str, List[Dict[str, Any]]]:
    extras: Dict[str, List[Dict[str, Any]]] = {
        "ui_components": [],
        "integrations": [],
        "process_diagrams": [],
        "business_entities": [],
        "process_flows": [],
        "integration_summary": [],
    }
    seen_ui = set()
    seen_integ = set()
    seen_diagram = set()
    seen_entity = set()
    flow_edges: List[Dict[str, Any]] = []
    flow_seen = set()
    integration_counts: Dict[Tuple[str, str], int] = {}
    FLOW_LIMIT = 12

    for sir in group_sirs or []:
        props = sir.get("props") or sir
        kind = (sir.get("kind") or props.get("kind") or "").lower()
        relationships = sir.get("relationships") or props.get("relationships") or []
        for rel in relationships:
            source = (rel.get("source") or {}).get("name") or (rel.get("source") or {}).get("type")
            target = (rel.get("target") or {}).get("display") or (rel.get("target") or {}).get("ref")
            operation = (rel.get("operation") or {}).get("type") or "touches"
            key = (source, target, operation)
            if not source or not target or key in flow_seen:
                continue
            flow_seen.add(key)
            if len(flow_edges) < FLOW_LIMIT:
                flow_edges.append(
                    {
                        "source": source,
                        "target": target,
                        "operation": operation,
                        "target_kind": (rel.get("target") or {}).get("kind"),
                    }
                )

        if kind == "ui_component":
            key = (props.get("name"), props.get("framework"))
            if key in seen_ui:
                continue
            seen_ui.add(key)
            extras["ui_components"].append(
                {
                    "name": props.get("name"),
                    "framework": props.get("framework"),
                    "routes": props.get("routes") or [],
                }
            )
        elif kind == "integration":
            key = (props.get("library"), props.get("integration_kind"), props.get("language"))
            if key in seen_integ:
                continue
            seen_integ.add(key)
            extras["integrations"].append(
                {
                    "library": props.get("library"),
                    "integration_kind": props.get("integration_kind"),
                    "language": props.get("language"),
                }
            )
            integ_key = (
                str(props.get("integration_kind") or "unknown"),
                str(props.get("library") or "unknown"),
            )
            integration_counts[integ_key] = integration_counts.get(integ_key, 0) + 1
        elif kind == "process_diagram":
            name = props.get("name") or sir.get("name")
            if name and name not in seen_diagram:
                seen_diagram.add(name)
                extras["process_diagrams"].append({"name": name})
        elif kind == "business_entity":
            name = props.get("name") or sir.get("name")
            if name and name not in seen_entity:
                seen_entity.add(name)
                extras["business_entities"].append(
                    {
                        "name": name,
                        "source": props.get("source") or "",
                    }
                )
    extras["process_flows"] = flow_edges
    summary = [
        {"integration_kind": kind, "library": library, "count": count}
        for (kind, library), count in integration_counts.items()
    ]
    summary.sort(key=lambda entry: (-entry["count"], entry["integration_kind"], entry["library"]))
    extras["integration_summary"] = summary
    return extras
