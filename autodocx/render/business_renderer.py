# autodocx/render/business_renderer.py
from __future__ import annotations
from collections import defaultdict
from pathlib import Path
from typing import Dict, Any, List, Optional
import re

from autodocx.visuals.graphviz_flows import (
    render_bw_process_flow_svg,
    render_component_overview_svg,
    render_relationship_sequence_svg,
    render_rollup_journey_svgs,
)
from autodocx.render.markdown_style import decorate_headings


def _anchor_eid(eid: str) -> str:
    return eid.replace(":", "_").replace("/", "_").replace("\\", "_").replace("#", "_")


def _safe(val: Any, default: str = "") -> str:
    return str(val) if val is not None else default


def _safe_list(x: Any) -> List[Any]:
    if x is None:
        return []
    if isinstance(x, list):
        return x
    return [x]


def _dir_slug(value: str) -> str:
    slug = re.sub(r"[^a-z0-9]+", "-", (value or "").lower()).strip("-")
    return slug or "doc"


def _collect_relationships_from_sirs(sirs: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
    rels: List[Dict[str, Any]] = []
    for sir in sirs or []:
        rels.extend(sir.get("relationships") or (sir.get("props") or {}).get("relationships") or [])
    return rels


def _hydrate_component_from_sirs(comp: Dict[str, Any], sirs: List[Dict[str, Any]]) -> Dict[str, Any]:
    """
    Use SIR business_scaffold slices to backfill component sections when missing.
    """
    comp = dict(comp or {})
    scaffold = {}
    for sir in sirs or []:
        bs = sir.get("business_scaffold") or (sir.get("props") or {}).get("business_scaffold") or {}
        if bs:
            scaffold = bs
            break
    if scaffold:
        io = scaffold.get("io_summary") or {}
        deps = scaffold.get("dependencies") or {}
        interfaces = scaffold.get("interfaces") or []
        invocations = scaffold.get("invocations") or []
        if not invocations and deps:
            for proc in deps.get("processes") or []:
                invocations.append({"target": proc, "kind": "process", "operation": "calls"})
            for svc in deps.get("services") or []:
                invocations.append({"target": svc, "kind": "service", "operation": "calls"})
        if not comp.get("interfaces"):
            comp["interfaces"] = interfaces
        if not comp.get("invokes"):
            comp["invokes"] = invocations
        if not comp.get("key_inputs"):
            comp["key_inputs"] = [{"name": n} for n in io.get("inputs") or []]
        if not comp.get("key_outputs"):
            comp["key_outputs"] = [{"name": n} for n in io.get("outputs") or []]
        if not comp.get("errors_and_logging"):
            errors = scaffold.get("errors") or []
            logging = scaffold.get("logging") or []
            comp["errors_and_logging"] = {
                "errors": [{"description": e.get("condition") or e.get("activity")} for e in errors],
                "logging": [{"description": l.get("message_hint") or l.get("activity")} for l in logging],
            }
    return comp


def _collect_scaffold_hints_from_sirs(sirs: List[Dict[str, Any]]) -> Dict[str, List[str]]:
    summary = {"identifiers": set(), "datastores": set(), "services": set(), "processes": set()}
    for sir in sirs or []:
        props = sir.get("props") or {}
        scaffold = sir.get("business_scaffold") or props.get("business_scaffold") or {}
        io_summary = (scaffold.get("io_summary") or {}) if scaffold else {}
        summary["identifiers"].update(io_summary.get("identifiers") or [])
        deps = scaffold.get("dependencies") or {}
        summary["datastores"].update(deps.get("datastores") or [])
        summary["services"].update(deps.get("services") or deps.get("external_services") or [])
        summary["processes"].update(deps.get("processes") or [])
        summary["datastores"].update(props.get("datasource_tables") or [])
        summary["services"].update(props.get("service_dependencies") or [])
        summary["processes"].update(props.get("process_calls") or props.get("calls_flows") or [])
        summary["identifiers"].update(props.get("identifier_hints") or [])
    return {k: sorted(v) for k, v in summary.items() if v}


def _render_interdependency_map_section(hints: Dict[str, List[str]]) -> List[str]:
    if not hints:
        return []
    lines: List[str] = []
    lines.append("## Interdependency map")
    if hints.get("processes"):
        lines.append(f"- **Process calls:** {', '.join(hints['processes'][:10])}")
    else:
        lines.append("- **Process calls:** _Not captured yet_")
    if hints.get("services"):
        lines.append(f"- **External services:** {', '.join(hints['services'][:10])}")
    else:
        lines.append("- **External services:** _Not captured yet_")
    if hints.get("datastores"):
        lines.append(f"- **Datastores:** {', '.join(hints['datastores'][:10])}")
    else:
        lines.append("- **Datastores:** _Not captured yet_")
    return lines


def _render_scaffold_dependencies_section(hints: Dict[str, List[str]]) -> List[str]:
    if not hints:
        return []
    lines = ["## Identifiers & Dependencies"]
    if hints.get("identifiers"):
        lines.append(f"- **Identifiers:** {', '.join(hints['identifiers'][:10])}")
    else:
        lines.append("- **Identifiers:** _Not captured yet_")
    if hints.get("datastores"):
        lines.append(f"- **Datastores:** {', '.join(hints['datastores'][:10])}")
    else:
        lines.append("- **Datastores:** _Not captured yet_")
    if hints.get("services"):
        lines.append(f"- **Service touchpoints:** {', '.join(hints['services'][:10])}")
    else:
        lines.append("- **Service touchpoints:** _Not captured yet_")
    if hints.get("processes"):
        lines.append(f"- **Process dependencies:** {', '.join(hints['processes'][:10])}")
    else:
        lines.append("- **Process dependencies:** _Not captured yet_")
    return lines


def _relationship_highlights(rels: List[Dict[str, Any]]) -> List[str]:
    if not rels:
        return []
    lines: List[str] = []
    kind_counts = defaultdict(int)
    for rel in rels:
        kind = ((rel.get("target") or {}).get("kind") or "unknown").lower()
        kind_counts[kind] += 1
    if kind_counts:
        if kind_counts.get("http"):
            lines.append(f"- External HTTP/API calls: {kind_counts['http']}")
        data_total = sum(kind_counts.get(k, 0) for k in ("sql", "dataverse", "sharepoint"))
        if data_total:
            lines.append(f"- Data touchpoints (SQL/Dataverse/SharePoint): {data_total}")
        if kind_counts.get("workflow"):
            lines.append(f"- Child workflows invoked: {kind_counts['workflow']}")
    if not lines:
        lines.append("- Relationships detected but no categorized summary available.")

    samples = []
    for rel in rels:
        src = (rel.get("source") or {}).get("name") or (rel.get("source") or {}).get("type")
        tgt = (rel.get("target") or {}).get("display") or (rel.get("target") or {}).get("ref")
        kind = ((rel.get("target") or {}).get("kind") or "dependency").lower()
        op = (rel.get("operation") or {}).get("type") or "touches"
        if src and tgt:
            samples.append(f"{src} {op} {tgt} [{kind}]")
        if len(samples) >= 3:
            break
    if samples:
        lines.append("- Sample flows:")
        for sample in samples:
            lines.append(f"  - {sample}")
    return lines


def _relationship_matrix_table(rels: List[Dict[str, Any]]) -> str:
    if not rels:
        return ""
    matrix = defaultdict(lambda: defaultdict(int))
    for rel in rels:
        kind = ((rel.get("target") or {}).get("kind") or "unknown").lower()
        op = ((rel.get("operation") or {}).get("type") or "touches").lower()
        matrix[kind][op] += 1
    rows = ["| Target Kind | Operation | Count |", "|-------------|-----------|-------|"]
    for kind in sorted(matrix.keys()):
        for op in sorted(matrix[kind].keys()):
            rows.append(f"| {kind} | {op} | {matrix[kind][op]} |")
    return "\n".join(rows)


def _mermaid_diagram_from_relationships(rels: List[Dict[str, Any]], limit: int = 8) -> str:
    if not rels:
        return ""
    edges = []
    seen = set()
    for rel in rels[:limit]:
        source = (rel.get("source") or {}).get("name") or rel.get("source", {}).get("type") or "source"
        target = (rel.get("target") or {}).get("display") or rel.get("target", {}).get("ref") or "target"
        op = (rel.get("operation") or {}).get("type") or ""
        edge = (source, target, op)
        if edge in seen:
            continue
        seen.add(edge)
        edges.append(f'    "{source}" -->|{op}| "{target}"')
    if not edges:
        return ""
    return "```mermaid\nflowchart LR\n" + "\n".join(edges) + "\n```"


def _process_flow_summary_lines(flows: List[Dict[str, Any]]) -> List[str]:
    lines: List[str] = ["| Source | Operation | Target |", "|--------|-----------|--------|"]
    any_row = False
    for flow in flows or []:
        source = flow.get("source")
        target = flow.get("target")
        if not source or not target:
            continue
        any_row = True
        op = flow.get("operation") or "flows to"
        lines.append(f"| {source} | {op} | {target} |")
    if not any_row:
        lines.append("| _Pending_ |  |  |")
    return lines


def _normalize_slug_for_match(value: str) -> str:
    return re.sub(r"[^a-z0-9]+", "", (value or "").lower())


def _match_assets_dir(out_docs_dir: Path, group_id: str) -> Optional[Path]:
    assets_root = out_docs_dir / "assets" / "graphs"
    if not assets_root.exists():
        return None
    target = _normalize_slug_for_match(group_id)
    for child in sorted(assets_root.iterdir()):
        if not child.is_dir():
            continue
        if _normalize_slug_for_match(child.name) == target:
            return child
    return None


def _collect_group_diagram_svgs(out_docs_dir: Path, group_id: str, limit: int = 12) -> List[Path]:
    matched = _match_assets_dir(out_docs_dir, group_id)
    if not matched:
        return []
    svgs = []
    for svg in sorted(matched.rglob("*.svg")):
        svgs.append(svg)
        if len(svgs) >= limit:
            break
    return svgs


def _process_flow_mermaid(flows: List[Dict[str, Any]], limit: int = 12) -> str:
    edges: List[str] = []
    seen = set()
    for flow in flows or []:
        if len(edges) >= limit:
            break
        source = (flow.get("source") or "").replace('"', "'")
        target = (flow.get("target") or "").replace('"', "'")
        if not source or not target:
            continue
        op = (flow.get("operation") or "").replace('"', "'")
        key = (source, target, op)
        if key in seen:
            continue
        seen.add(key)
        edges.append(f'    "{source}" -->|{op}| "{target}"')
    if not edges:
        return ""
    return "```mermaid\nflowchart LR\n" + "\n".join(edges) + "\n```"


def _integration_summary_table(summary: List[Dict[str, Any]]) -> str:
    if not summary:
        return ""
    rows = ["| Integration Kind | Library | Count |", "|------------------|---------|-------|"]
    for entry in summary:
        rows.append(
            f"| {entry.get('integration_kind') or '-'} | {entry.get('library') or '-'} | {entry.get('count', 0)} |"
        )
    return "\n".join(rows)


def _integration_summary_mermaid(summary: List[Dict[str, Any]], group_label: str, limit: int = 10) -> str:
    if not summary:
        return ""
    root = (group_label or "Group").replace('"', "'")
    edges: List[str] = []
    seen = set()
    for entry in summary:
        if len(edges) >= limit:
            break
        lib = (entry.get("library") or entry.get("integration_kind") or "Integration").replace('"', "'")
        kind = (entry.get("integration_kind") or "").replace('"', "'")
        count = entry.get("count", 0)
        key = (lib, kind)
        if key in seen:
            continue
        seen.add(key)
        label = f"{kind} ({count})" if kind else str(count)
        edges.append(f'    "{root}" -->|{label}| "{lib}"')
    if not edges:
        return ""
    return "```mermaid\nflowchart LR\n" + "\n".join(edges) + "\n```"


def _collect_code_entities(sirs: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
    entries: List[Dict[str, Any]] = []
    seen = set()
    for sir in sirs or []:
        props = sir.get("props") or sir
        kind = (sir.get("kind") or props.get("kind") or "").lower()
        if kind != "code_entity":
            continue
        name = props.get("name") or sir.get("name")
        if not name or name in seen:
            continue
        seen.add(name)
        entries.append(
            {
                "name": name,
                "language": props.get("language", ""),
                "docstring": props.get("docstring", ""),
                "business_verbs": props.get("business_verbs") or [],
            }
        )
    return entries


def _collect_ui_components_from_sirs(sirs: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
    entries: List[Dict[str, Any]] = []
    for sir in sirs or []:
        props = sir.get("props") or sir
        kind = (sir.get("kind") or props.get("kind") or "").lower()
        if kind != "ui_component":
            continue
        entries.append(
            {
                "name": props.get("name"),
                "framework": props.get("framework"),
                "routes": props.get("routes") or [],
                "selector": props.get("selector"),
            }
        )
    return entries


def _collect_integrations_from_sirs(sirs: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
    entries: List[Dict[str, Any]] = []
    for sir in sirs or []:
        props = sir.get("props") or sir
        kind = (sir.get("kind") or props.get("kind") or "").lower()
        if kind != "integration":
            continue
        entries.append(
            {
                "library": props.get("library"),
                "integration_kind": props.get("integration_kind"),
                "language": props.get("language"),
            }
        )
    return entries


def _collect_process_diagrams_from_sirs(sirs: List[Dict[str, Any]]) -> List[str]:
    diagrams: List[str] = []
    for sir in sirs or []:
        props = sir.get("props") or sir
        kind = (sir.get("kind") or props.get("kind") or "").lower()
        if kind != "process_diagram":
            continue
        diagrams.append(props.get("name") or sir.get("name") or "Unnamed diagram")
    return diagrams


def _collect_business_entities_from_sirs(sirs: List[Dict[str, Any]]) -> List[Dict[str, str]]:
    entities: List[Dict[str, str]] = []
    seen = set()
    for sir in sirs or []:
        props = sir.get("props") or sir
        kind = (sir.get("kind") or props.get("kind") or "").lower()
        if kind != "business_entity":
            continue
        name = props.get("name") or sir.get("name")
        if name and name not in seen:
            seen.add(name)
            entities.append(
                {
                    "name": name,
                    "source": props.get("source") or "",
                }
            )
    return entities


def _collect_personas_from_sirs(sirs: List[Dict[str, Any]]) -> List[Dict[str, str]]:
    personas: Dict[str, Dict[str, str]] = {}
    for sir in sirs or []:
        props = sir.get("props") or {}
        for role in props.get("roles") or []:
            if role not in personas:
                personas[role] = {
                    "name": role,
                    "goals": props.get("user_story") or "",
                }
    return list(personas.values())


def _collect_user_stories_from_sirs(sirs: List[Dict[str, Any]]) -> List[str]:
    stories: List[str] = []
    for sir in sirs or []:
        story = (sir.get("props") or {}).get("user_story")
        if story:
            stories.append(story)
    return stories


def _collect_screenshots_from_sirs(sirs: List[Dict[str, Any]]) -> List[Dict[str, str]]:
    shots: List[Dict[str, str]] = []
    for sir in sirs or []:
        props = sir.get("props") or {}
        if props.get("screenshots"):
            for shot in props.get("screenshots") or []:
                if isinstance(shot, str):
                    shots.append({"path": shot, "caption": sir.get("name")})
                elif isinstance(shot, dict) and shot.get("path"):
                    shots.append({"path": shot["path"], "caption": shot.get("caption") or sir.get("name")})
        elif props.get("ui_snapshot"):
            shots.append({"path": props["ui_snapshot"], "caption": sir.get("name")})
    return shots


def _render_persona_lines(personas: List[Dict[str, str]]) -> List[str]:
    lines: List[str] = []
    for persona in personas or []:
        name = persona.get("name") or "Persona"
        goals = persona.get("goals") or ""
        lines.append(f"- **{name}** – {goals or 'Goals forthcoming'}")
    return lines


def _render_screenshot_markdown(
    screenshots: List[Dict[str, str]],
    *,
    doc_path: Optional[Path] = None,
    docs_root: Optional[Path] = None,
) -> List[str]:
    lines: List[str] = []
    for shot in screenshots or []:
        path = shot.get("path")
        if not path:
            continue
        caption = shot.get("caption") or ""
        rel = _resolve_asset_path(path, doc_path=doc_path, docs_root=docs_root)
        lines.append(f"![{caption}]({rel})")
    return lines


def _resolve_asset_path(
    path: str,
    *,
    doc_path: Optional[Path] = None,
    docs_root: Optional[Path] = None,
) -> str:
    if not path:
        return ""
    path = path.replace("\\", "/")
    if path.startswith("/"):
        path = path.lstrip("/")
    rel = path
    if "assets/" in path:
        idx = path.find("assets/")
        rel = path[idx:]
    if doc_path and docs_root:
        try:
            target = (docs_root / rel).resolve()
            return target.relative_to(doc_path.parent.resolve()).as_posix()
        except Exception:
            pass
    if rel.startswith("assets/"):
        return f"/{rel}"
    return rel


def _slug(value: str) -> str:
    if not value:
        return "item"
    return "".join(ch.lower() if ch.isalnum() else "-" for ch in value).strip("-") or "item"


def _component_title(c_json: Dict[str, Any], component_key: str) -> str:
    title = c_json.get("title") or c_json.get("component", {}).get("name") or component_key
    return str(title)


def _confidence_line(llm_subscore: Optional[float]) -> str:
    if llm_subscore is None:
        return "> Confidence Score: N/A"
    return f"> **Confidence Score:** {llm_subscore:.2f} — *(see scoring table at bottom for details)*"


def _evidence_suffix(evidence_ids: Iterable[str]) -> str:
    ids = [str(eid) for eid in (evidence_ids or []) if str(eid)]
    if not ids:
        return ""
    preview = ", ".join(ids[:5])
    more = "" if len(ids) <= 5 else f" (+{len(ids) - 5} more)"
    return f" _(Evidence: {preview}{more})_"


def _render_claim_section(title: str, entries: Iterable[Dict[str, Any]], summary_key: str = "summary", detail_key: str = "detail") -> List[str]:
    lines: List[str] = [title]
    matched = False
    for entry in entries or []:
        if not isinstance(entry, dict):
            continue
        summary = entry.get(summary_key) or entry.get(detail_key)
        detail = entry.get(detail_key) or entry.get(summary_key)
        if not summary and not detail:
            continue
        matched = True
        bullet = f"- **{_safe(summary, 'Statement')}**"
        if detail and detail != summary:
            bullet += f" — {detail}"
        bullet += _evidence_suffix(entry.get("evidence_ids") or [])
        lines.append(bullet)
    if not matched:
        lines.append("_No evidence captured yet._")
    return lines


def _render_interfaces_section(interfaces: Iterable[Dict[str, Any]]) -> List[str]:
    lines = ["## Interfaces exposed", ""]
    header = ["| Name | Kind | Method | Endpoint | Evidence |", "|------|------|--------|----------|----------|"]
    rows = []
    for entry in interfaces or []:
        if not isinstance(entry, dict):
            continue
        rows.append(
            f"| {_safe(entry.get('name'), '-')} | {_safe(entry.get('kind'), '-')} | {_safe(entry.get('method'), '-')} | {_safe(entry.get('endpoint'), '-')} | {_safe(', '.join(entry.get('evidence_ids') or []), '-')} |"
        )
    if not rows:
        rows.append("| _Pending_ |  |  |  |  |")
    lines.extend(header + rows)
    return lines


def _render_invokes_section(invokes: Iterable[Dict[str, Any]], interdeps: Dict[str, Any]) -> List[str]:
    lines: List[str] = ["## Invokes / Dependencies"]
    table = ["| Target | Kind | Operation | Evidence |", "|--------|------|-----------|----------|"]
    any_row = False
    for entry in invokes or []:
        if not isinstance(entry, dict):
            continue
        any_row = True
        table.append(
            f"| {_safe(entry.get('target'), '-')} | {_safe(entry.get('kind'), '-')} | {_safe(entry.get('operation'), '-')} | {_safe(', '.join(entry.get('evidence_ids') or []), '-')} |"
        )
    if not any_row:
        table.append("| _Pending_ |  |  |  |")
    lines.extend(table)
    lines.append("")
    lines.append("### Interdependency map")
    interdeps = interdeps or {}
    for label, key in [("Calls", "calls"), ("Called by", "called_by"), ("Shared data", "shared_data")]:
        partners = ", ".join({entry.get("partner", "-") for entry in interdeps.get(key) or []}) or "_None yet._"
        lines.append(f"- **{label}:** {partners}")
    return lines


def _render_key_io_section(key_inputs: Iterable[Dict[str, Any]], key_outputs: Iterable[Dict[str, Any]]) -> List[str]:
    def _table(entries: Iterable[Dict[str, Any]], heading: str) -> List[str]:
        tbl = [f"### {heading}", "| Name | Description | Evidence |", "|------|-------------|----------|"]
        any_row = False
        for entry in entries or []:
            if not isinstance(entry, dict):
                continue
            any_row = True
            tbl.append(
                f"| {_safe(entry.get('name'), '-')} | {_safe(entry.get('description'), '-')} | {_safe(', '.join(entry.get('evidence_ids') or []), '-')} |"
            )
        if not any_row:
            tbl.append("| _Pending_ |  |  |")
        return tbl

    lines = ["## Key inputs & outputs"]
    lines.extend(_table(key_inputs, "Inputs"))
    lines.append("")
    lines.extend(_table(key_outputs, "Outputs"))
    return lines


def _render_errors_logging_section(errors_and_logging: Dict[str, Any]) -> List[str]:
    section = ["## Errors & Logging"]
    block = errors_and_logging or {"errors": [], "logging": []}
    if not block.get("errors") and not block.get("logging"):
        section.append("_No error or logging behavior captured yet._")
        return section
    if block.get("errors"):
        section.append("### Error handling")
        for entry in block.get("errors", []):
            section.append(f"- {entry.get('description', 'Behavior')}{_evidence_suffix(entry.get('evidence_ids') or [])}")
    if block.get("logging"):
        section.append("")
        section.append("### Logging & telemetry")
        for entry in block.get("logging", []):
            section.append(f"- {entry.get('description', 'Behavior')}{_evidence_suffix(entry.get('evidence_ids') or [])}")
    return section


def _render_extrapolations_section(entries: Iterable[Dict[str, Any]]) -> List[str]:
    lines = ["## Extrapolations"]
    any_entry = False
    for entry in entries or []:
        if not isinstance(entry, dict):
            continue
        any_entry = True
        hypothesis = entry.get("hypothesis") or "Hypothesis"
        rationale = entry.get("rationale") or ""
        score = entry.get("hypothesis_score")
        score_txt = f"(score={score})" if isinstance(score, (int, float)) else ""
        lines.append(f"- **{hypothesis}** {score_txt} — {rationale}{_evidence_suffix(entry.get('evidence_ids') or [])}")
    if not any_entry:
        lines.append("_No extrapolations have been recorded._")
    return lines


def _render_packaging_section(packaging: Dict[str, Any], artifacts: Iterable[Dict[str, Any]]) -> List[str]:
    lines: List[str] = ["## Packaging & Artifacts"]
    pkg_rows = []
    pkg_data = packaging or {}
    for key in ("bundle", "module", "docker_image", "version", "entrypoint"):
        if pkg_data.get(key):
            pkg_rows.append(f"- **{key.replace('_', ' ').title()}:** {_safe(pkg_data.get(key), '-')}")
    if pkg_rows:
        lines.extend(pkg_rows)
    art_rows = []
    for art in artifacts or []:
        if not isinstance(art, dict):
            continue
        name = art.get("name") or art.get("file") or art.get("path")
        role = art.get("role") or art.get("artifact_type")
        evidence = ", ".join(art.get("evidence_ids") or [])
        art_rows.append(f"- {_safe(name, '-')}: {_safe(role, '-')}{_evidence_suffix(art.get('evidence_ids') or [])}")
    if art_rows:
        if pkg_rows:
            lines.append("")
        lines.append("### Artifacts")
        lines.extend(art_rows)
    if not pkg_rows and not art_rows:
        lines.append("_No packaging details captured yet._")
    return lines


def _render_traceability_section(entries: Iterable[Dict[str, Any]]) -> List[str]:
    lines = ["## Traceability", "| Artifact | Type | Description | Evidence |", "|----------|------|-------------|----------|"]
    any_row = False
    for entry in entries or []:
        if not isinstance(entry, dict):
            continue
        any_row = True
        lines.append(
            f"| {_safe(entry.get('artifact'), '-')} | {_safe(entry.get('signal_type'), '-')} | {_safe(entry.get('description'), '-')} | {_safe(', '.join(entry.get('evidence_ids') or []), '-')} |"
        )
    if not any_row:
        lines.append("| _Pending_ |  |  |  |")
    return lines


def _render_related_documents_section(group_id: str, component_key: str, sirs: List[Dict[str, Any]]) -> List[str]:
    lines = ["## Related Documents"]
    group_slug = _dir_slug(group_id)
    related = [f"- [Component brief](../{group_slug}.md)"]
    for sir in sirs or []:
        name = sir.get("name") or sir.get("id")
        if not name:
            continue
        related.append(f"- {name} (process evidence)")
    if len(related) == 1:
        related.append("- _Additional related docs will appear after the next scan._")
    lines.extend(related)
    return lines


def _append_technical_appendix(
    *,
    md: List[str],
    comp: Dict[str, Any],
    sirs: List[Dict[str, Any]],
    rels: List[Dict[str, Any]],
    agg: Optional[Dict[str, Any]],
    svg_paths: List[str],
    overview_svg: Optional[str],
    sequence_svg: Optional[str],
    journey_svgs: List[Dict[str, Any]],
    screenshots: List[Dict[str, str]],
    ui_components: List[Dict[str, Any]],
    integrations: List[Dict[str, Any]],
    code_entities: List[Dict[str, Any]],
    doc_path: Optional[Path] = None,
    docs_root: Optional[Path] = None,
) -> None:
    md.append("## Technical appendix")
    md.append("")
    blueprints = comp.get("journey_blueprints") or []
    if blueprints:
        md.append("### Journey blueprints")
        for bp in blueprints:
            md.append(f"- **{bp.get('title', 'Journey')}** — {' -> '.join(bp.get('steps', []) or [])}{_evidence_suffix(bp.get('evidence_ids') or [])}")
        md.append("")
    if screenshots:
        md.append("### UI snapshots")
        md.extend(_render_screenshot_markdown(screenshots, doc_path=doc_path, docs_root=docs_root))
        md.append("")
    if rels:
        md.append("### Relationship highlights")
        md.extend(_relationship_highlights(rels))
        md.append("")
        matrix_block = _relationship_matrix_table(rels)
        if matrix_block:
            md.append(matrix_block)
            md.append("")
        mermaid = _mermaid_diagram_from_relationships(rels)
        if mermaid:
            md.append(mermaid)
            md.append("")
    if agg:
        md.append("### Graph insights")
        md.append(_compact_graph_insights_table(agg))
        md.append("")
    if sequence_svg:
        md.append("### Sequence snapshot")
        md.append(f"![Sequence]({_resolve_asset_path(sequence_svg, doc_path=doc_path, docs_root=docs_root)})")
        md.append("")
    if journey_svgs:
        md.append("### Generated journey maps")
        for journey in journey_svgs:
            md.append(f"#### {journey.get('title')}")
            md.append(
                f"![Journey]({_resolve_asset_path(journey.get('path') or '', doc_path=doc_path, docs_root=docs_root)})"
            )
        md.append("")
    if overview_svg:
        md.append("### Component overview")
        md.append(f"![Component overview]({_resolve_asset_path(overview_svg, doc_path=doc_path, docs_root=docs_root)})")
        md.append("")
    if svg_paths:
        md.append("### Process diagrams")
        for svg in svg_paths:
            md.append(f"![Process]({_resolve_asset_path(svg, doc_path=doc_path, docs_root=docs_root)})")
        md.append("")
    if ui_components:
        md.append("### UI entry points")
        md.append("| Component | Framework | Routes |")
        md.append("|-----------|-----------|--------|")
        for comp_entry in ui_components:
            routes = ", ".join(comp_entry.get("routes") or []) or "-"
            md.append(f"| {comp_entry.get('name')} | {comp_entry.get('framework') or '-'} | {routes} |")
        md.append("")
    if integrations:
        md.append("### Integration catalog")
        md.append("| Library | Kind | Language |")
        md.append("|---------|------|----------|")
        for integ in integrations:
            md.append(f"| {integ.get('library')} | {integ.get('integration_kind')} | {integ.get('language') or '-'} |")
        md.append("")
    if code_entities:
        md.append("### Key code modules")
        for entry in code_entities[:10]:
            doc = entry.get("docstring") or "No summary."
            verbs = entry.get("business_verbs") or []
            verb_suffix = f" _verbs: {', '.join(verbs[:3])}_" if verbs else ""
            md.append(f"- `{entry.get('name')}` ({entry.get('language') or '-'}) – {doc}{verb_suffix}")
        md.append("")


def render_family_pages(out_docs_dir: Path, interdeps: Dict[str, Any]) -> None:
    families = interdeps.get("families") or {}
    if not families:
        return
    family_dir = out_docs_dir / "families"
    family_dir.mkdir(parents=True, exist_ok=True)
    for family, members in families.items():
        if not members:
            continue
        _render_family_page(family_dir, family, members, interdeps)


def render_repo_overview(out_docs_dir: Path, interdeps: Dict[str, Any]) -> None:
    nodes = interdeps.get("nodes") or {}
    families = interdeps.get("families") or {}
    edges = interdeps.get("edges") or []
    mapping = {name: data.get("family") for name, data in nodes.items()}
    overview_path = out_docs_dir / "REPO_OVERVIEW.md"
    fm = [
        "---",
        'title: "Repository Overview"',
        f"family_count: {len(families)}",
        f"process_count: {len(nodes)}",
        "---",
    ]
    lines = ["# Repository Overview", ""]
    if families:
        lines.append("## Families")
        lines.append("| Family | Members |")
        lines.append("|--------|---------|")
        for fam, members in sorted(families.items()):
            lines.append(f"| {fam} | {len(members)} |")
        lines.append("")
    cross_calls: Dict[Tuple[str, str], int] = {}
    for edge in edges:
        if edge.get("kind") != "calls":
            continue
        src = edge.get("from")
        tgt = edge.get("to")
        fam_src = mapping.get(src)
        fam_tgt = mapping.get(tgt)
        if not fam_src or not fam_tgt or fam_src == fam_tgt:
            continue
        cross_calls[(fam_src, fam_tgt)] = cross_calls.get((fam_src, fam_tgt), 0) + 1
    lines.append("## Cross-family calls")
    if cross_calls:
        lines.append("| From | To | Count |")
        lines.append("|------|----|-------|")
        for (fam_src, fam_tgt), count in sorted(cross_calls.items()):
            lines.append(f"| {fam_src} | {fam_tgt} | {count} |")
    else:
        lines.append("_No cross-family calls detected._")
    lines.append("")
    shared_pairs: Dict[Tuple[str, str], Dict[str, List[str]]] = {}
    for edge in edges:
        if edge.get("kind") not in {"shared_identifier", "shared_datastore"}:
            continue
        src = edge.get("from")
        tgt = edge.get("to")
        fam_src = mapping.get(src)
        fam_tgt = mapping.get(tgt)
        if not fam_src or not fam_tgt:
            continue
        key = tuple(sorted((fam_src, fam_tgt)))
        shared = shared_pairs.setdefault(key, {"identifiers": [], "datastores": []})
        if edge.get("kind") == "shared_identifier":
            shared["identifiers"].append(edge.get("value"))
        else:
            shared["datastores"].append(edge.get("value"))
    lines.append("## Shared identifiers / datastores")
    if shared_pairs:
        for (fam_a, fam_b), payload in shared_pairs.items():
            ident = ", ".join(sorted({v for v in payload["identifiers"] if v})) or "-"
            data = ", ".join(sorted({v for v in payload["datastores"] if v})) or "-"
            lines.append(f"- **{fam_a} ↔ {fam_b}:** identifiers [{ident}] · datastores [{data}]")
    else:
        lines.append("_No shared identifiers or datastores detected._")
    lines.append("")
    content = decorate_headings(fm + lines)
    overview_path.write_text("\n".join(content), encoding="utf-8")


def _render_family_page(out_dir: Path, family: str, members: List[str], interdeps: Dict[str, Any]) -> None:
    nodes = interdeps.get("nodes") or {}
    edges = interdeps.get("edges") or []
    slug = _slug(family)
    path = out_dir / f"{slug}.md"
    fm = ["---", f'title: "Family: {family}"', f"members: {len(members)}", "---"]
    lines = [f"# Family: {family}", ""]
    lines.append("## Members")
    for name in sorted(members):
        comp = (nodes.get(name) or {}).get("component") or "unknown"
        lines.append(f"- {name} _(component: {comp})_")
    lines.append("")
    lines.append("## Interfaces")
    lines.append("| Process | Interface |")
    lines.append("|---------|-----------|")
    for name in sorted(members):
        interfaces = (nodes.get(name) or {}).get("interfaces") or []
        if not interfaces:
            lines.append(f"| {name} | _None captured_ |")
            continue
        for iface in interfaces:
            lines.append(f"| {name} | {iface} |")
    lines.append("")
    lines.append("## Intra-family calls")
    intra = []
    for edge in edges:
        if edge.get("kind") == "calls" and edge.get("from") in members and edge.get("to") in members:
            intra.append(f"- {edge.get('from')} ➜ {edge.get('to')}")
    if intra:
        lines.extend(intra)
    else:
        lines.append("_No internal calls detected._")
    lines.append("")
    lines.append("## Shared identifiers & datastores")
    shared_items = []
    for edge in edges:
        if edge.get("from") not in members or edge.get("to") not in members:
            continue
        if edge.get("kind") == "shared_identifier":
            shared_items.append(f"- {edge.get('from')} shares identifier `{edge.get('value')}` with {edge.get('to')}")
        elif edge.get("kind") == "shared_datastore":
            shared_items.append(f"- {edge.get('from')} shares datastore `{edge.get('value')}` with {edge.get('to')}")
    if shared_items:
        lines.extend(shared_items)
    else:
        lines.append("_No shared datasets or identifiers detected._")
    lines.append("")
    lines.append("## Cross-family calls originating here")
    cross = []
    member_set = set(members)
    for edge in edges:
        if edge.get("kind") != "calls":
            continue
        src = edge.get("from")
        tgt = edge.get("to")
        if src in member_set and tgt not in member_set:
            cross.append(f"- {src} ➜ {tgt}")
    if cross:
        lines.extend(cross)
    else:
        lines.append("_No outbound cross-family calls detected._")
    lines.append("")
    content = decorate_headings(fm + lines)
    path.write_text("\n".join(content), encoding="utf-8")


def _interdependencies_block(sirs: List[Dict[str, Any]]) -> List[str]:
    counts = {}
    for s in sirs or []:
        for t in s.get("triggers", []) or []:
            typ = (t.get("type") or t.get("name") or "trigger").lower()
            if "http" in typ:
                counts["HTTP"] = counts.get("HTTP", 0) + 1
            elif "jms" in typ:
                counts["JMS"] = counts.get("JMS", 0) + 1
            else:
                counts["Trigger"] = counts.get("Trigger", 0) + 1
        for st in s.get("steps", []) or []:
            conn = (st.get("connector") or st.get("type") or "").lower()
            if "http" in conn:
                counts["HTTP"] = counts.get("HTTP", 0) + 1
            elif "jdbc" in conn:
                counts["JDBC"] = counts.get("JDBC", 0) + 1
            elif "jms" in conn:
                counts["JMS"] = counts.get("JMS", 0) + 1
            elif "file" in conn:
                counts["File"] = counts.get("File", 0) + 1

    lines = []
    if counts:
        lines.append("- Touchpoints:")
        for k in sorted(counts.keys()):
            lines.append(f"  - {k}: {counts[k]}")
    else:
        lines.append("- Touchpoints: Not detected in current evidence")
    return lines


def _unknowns_block(c_json: Dict[str, Any]) -> List[str]:
    out = []
    for w in _safe_list(c_json.get("component", {}).get("what_it_does")):
        if isinstance(w, dict):
            eids = _safe_list(w.get("evidence_ids"))
            if not eids:
                out.append(f"- {w.get('claim','Unknown claim')} — Evidence not cited.")
    if not out:
        out.append("- None identified.")
    return out


# -------------------------
# Distance-features helpers
# -------------------------

def _aggregate_graph_features(sirs: List[Dict[str, Any]]) -> Optional[Dict[str, Any]]:
    """
    Aggregate per-SIR graph_features into concise metrics for the component page.
    Returns None if no graph_features are present.
    """
    gfs = []
    for s in sirs or []:
        gf = s.get("graph_features") or {}
        # Accept either nearest_marker_distance or avg_distance_to_markers as indication
        if gf and isinstance(gf, dict) and ("nearest_marker_distance" in gf or "avg_distance_to_markers" in gf or "anchor_coverage" in gf):
            gfs.append(gf)

    if not gfs:
        return None

    # Coverage within radius: count SIRs with at least one anchor within R hops
    def _radius(g):
        ac = g.get("anchor_coverage") or {}
        return int(ac.get("radius", 4))

    radius_vals = [_radius(g) for g in gfs if isinstance(g, dict)]
    radius = max(radius_vals) if radius_vals else 4

    covered = 0
    nearest_distances: List[float] = []
    p50s: List[float] = []
    p90s: List[float] = []
    articulation = 0

    for g in gfs:
        ac = g.get("anchor_coverage") or {}
        anchors_within_r = int(ac.get("anchors_within_r", 0) or 0)
        if anchors_within_r >= 1:
            covered += 1

        # Support both 'nearest_marker_distance' and 'nearest_marker_distance' alternative keys
        nd = g.get("nearest_marker_distance") or g.get("nearest_distance") or g.get("avg_distance_to_markers")
        if isinstance(nd, (int, float)) and nd != float("inf"):
            nearest_distances.append(float(nd))

        dp = (g.get("distance_percentiles") or {})
        p50v = dp.get("p50", None)
        p90v = dp.get("p90", None)
        if isinstance(p50v, (int, float)) and p50v != float("inf"):
            p50s.append(float(p50v))
        if isinstance(p90v, (int, float)) and p90v != float("inf"):
            p90s.append(float(p90v))

        rf = g.get("risk_flags") or {}
        if bool(rf.get("is_articulation", False)):
            articulation += 1

    total = len(gfs)
    avg_nearest = round(sum(nearest_distances) / len(nearest_distances), 3) if nearest_distances else None
    p50 = round(sum(p50s) / len(p50s), 3) if p50s else None
    p90 = round(sum(p90s) / len(p90s), 3) if p90s else None

    # Pull marker metadata from first gf (markers tend to be shared)
    marker_info = None
    for g in gfs:
        m = g.get("markers")
        if m:
            marker_info = m
            break

    return {
        "radius": radius,
        "covered": covered,
        "total": total,
        "avg_nearest": avg_nearest,
        "p50": p50,
        "p90": p90,
        "articulation": articulation,
        "markers": marker_info or [],
    }


def _compact_graph_insights_table(agg: Dict[str, Any]) -> str:
    """
    Return a compact 2-column markdown table for graph insights.
    """
    if not agg:
        return "| Metric | Value |\n|---|---|\n| Graph insights | not available |\n"
    rows: List[str] = []
    rows.append("| Metric | Value |")
    rows.append("|---|---|")
    rows.append(f"| Coverage | {agg.get('covered', 0)}/{agg.get('total', 0)} (R={agg.get('radius', 4)}) |")
    avg = agg.get("avg_nearest")
    rows.append(f"| Average nearest-marker distance | {avg if avg is not None else 'n/a'} |")
    rows.append(f"| p50 | {agg.get('p50','n/a')} |")
    rows.append(f"| p90 | {agg.get('p90','n/a')} |")
    rows.append(f"| Potential fragility (articulation nodes) | {agg.get('articulation', 0)} |")
    return "\n".join(rows)


def _render_yaml_front_matter_for_component(
    title: str,
    provenance: Dict[str, Any],
    llm_subscore: Optional[float],
    facets: Optional[Dict[str, Any]],
    agg: Optional[Dict[str, Any]],
    traceability_count: int,
    relationship_count: int,
) -> List[str]:
    """
    Build YAML front-matter lines (without final newline) for a component page.
    """
    fm: List[str] = []
    fm.append("---")
    fm.append(f'title: "{title}"')
    fm.append("hashes:")
    fm.append(f'  input: "{provenance.get("input_hash") or ""}"')
    fm.append(f'  prompt: "{provenance.get("prompt_hash") or ""}"')
    fm.append("provenance:")
    fm.append(f'  model: "{provenance.get("model") or ""}"')
    fm.append(f"  generated_at: {provenance.get('generated_at', 'null')}")
    fm.append("confidence:")
    fm.append(f"  llm_subscore: {llm_subscore if llm_subscore is not None else 'null'}")
    fm.append(f"  traceability: {traceability_count}")
    fm.append(f"  relationships: {relationship_count}")
    # facets block
    fm.append("facets:")
    fm.append(f"  score: {facets.get('score', 0.0) if facets else 0.0}")
    # distance block
    fm.append("distance:")
    fm.append(f"  avg_nearest_distance: {agg.get('avg_nearest') if agg and agg.get('avg_nearest') is not None else 'null'}")
    fm.append(f"  covered: {agg.get('covered',0) if agg else 0}")
    fm.append(f"  total: {agg.get('total',0) if agg else 0}")
    # markers: include marker ids if available
    fm.append("markers:")
    markers = (agg.get("markers") if agg else []) or []
    if isinstance(markers, list) and markers:
        for m in markers:
            if isinstance(m, dict):
                mid = m.get("id", "")
                mtype = m.get("type", "")
                fm.append(f"  - id: {mid}")
                fm.append(f"    type: {mtype}")
            else:
                fm.append(f"  - id: {m}")
    else:
        fm.append("  - []")
    fm.append("---")
    return fm


def _render_yaml_front_matter_for_sir(title: str, facets: Optional[Dict[str, Any]], gf: Optional[Dict[str, Any]]) -> List[str]:
    """
    Build YAML front-matter lines for a SIR (process) page.
    """
    fm: List[str] = []
    fm.append("---")
    fm.append(f'title: "{title}"')
    fm.append("facets:")
    fm.append(f"  score: {facets.get('score', 0.0) if facets else 0.0}")
    fm.append("distance:")
    # preferentially use per-SIR gf values, fallback to null
    avg = gf.get("avg_distance_to_markers") if gf else None
    if avg is None:
        avg = gf.get("avg_nearest") if gf else None
    fm.append(f"  avg_nearest_distance: {avg if avg is not None else 'null'}")
    acov = (gf.get("anchor_coverage") or {}) if gf else {}
    fm.append(f"  anchors_within_r: {int(acov.get('anchors_within_r', 0) if acov else 0)}")
    fm.append(f"  radius: {int(acov.get('radius', 4) if acov else 4)}")
    fm.append("markers:")
    markers = gf.get("markers") if gf else []
    if isinstance(markers, list) and markers:
        for m in markers:
            if isinstance(m, dict):
                fm.append(f"  - id: {m.get('id')}")
                fm.append(f"    type: {m.get('type')}")
            else:
                fm.append(f"  - id: {m}")
    else:
        fm.append("  - []")
    fm.append("---")
    return fm



def render_business_component_page(
    out_docs_dir: Path,
    group_id: str,
    component_key: str,
    c_json: Dict[str, Any],
    sirs: List[Dict[str, Any]],
    evidence_md_filename: Optional[str] = None,
    facets: Optional[Dict[str, Any]] = None,
    settings: Optional[Dict[str, Any]] = None,
) -> None:
    """Render the business-facing component page following the reference layout."""
    gid = _dir_slug(group_id)
    cid = component_key.replace(" ", "_").replace("/", "_")

    page_dir = out_docs_dir / gid / "components"
    page_dir.mkdir(parents=True, exist_ok=True)
    page_path = page_dir / f"{cid}.md"

    title = _component_title(c_json, component_key)
    llm_sub = c_json.get("llm_subscore")

    svg_paths: List[str] = []
    for sir in sirs or []:
        svg_rel = render_bw_process_flow_svg(sir, out_docs_dir, group_id, component_key, settings=settings)
        if svg_rel:
            svg_paths.append(svg_rel)

    overview_svg = render_component_overview_svg(sirs or [], out_docs_dir, group_id, component_key, settings=settings)
    agg = _aggregate_graph_features(sirs) or {}

    comp = c_json.get("component", {}) or {}
    comp = _hydrate_component_from_sirs(comp, sirs)
    comp = _hydrate_component_from_sirs(comp, sirs)
    provenance = c_json.get("provenance") or {}
    traceability = comp.get("traceability") or []
    rels = _collect_relationships_from_sirs(sirs)

    md: List[str] = []
    md.extend(
        _render_yaml_front_matter_for_component(
            title,
            provenance,
            llm_sub,
            facets,
            agg,
            len(traceability),
            len(rels),
        )
    )
    md.append("")
    md.append(f"# {title}")
    md.append("<!-- CONFIDENCE_INLINE -->")
    md.append(_confidence_line(llm_sub))
    md.append("")
    md.extend(_render_claim_section("## What it does", comp.get("what_it_does")))
    md.append("")
    md.extend(_render_claim_section("## Why it matters", comp.get("why_it_matters"), summary_key="impact"))
    md.append("")
    md.extend(_render_interfaces_section(comp.get("interfaces")))
    md.append("")
    md.extend(_render_invokes_section(comp.get("invokes"), comp.get("interdependencies")))
    md.append("")
    md.extend(_render_key_io_section(comp.get("key_inputs"), comp.get("key_outputs")))
    md.append("")
    scaffold_hints = _collect_scaffold_hints_from_sirs(sirs)
    interdep_section = _render_interdependency_map_section(scaffold_hints)
    if interdep_section:
        md.extend(interdep_section)
        md.append("")
    if scaffold_hints:
        md.extend(_render_scaffold_dependencies_section(scaffold_hints))
        md.append("")
    md.extend(_render_errors_logging_section(comp.get("errors_and_logging")))
    md.append("")
    md.extend(_render_extrapolations_section(comp.get("extrapolations")))
    md.append("")
    md.extend(_render_packaging_section(comp.get("packaging") or {}, comp.get("artifacts") or []))
    md.append("")
    md.extend(_render_traceability_section(traceability))
    md.append("")
    md.extend(_render_related_documents_section(group_id, component_key, sirs))
    md.append("")

    _append_technical_appendix(
        md=md,
        comp=comp,
        sirs=sirs,
        rels=rels,
        agg=agg,
        svg_paths=svg_paths,
        overview_svg=overview_svg,
        sequence_svg=render_relationship_sequence_svg(sirs or [], out_docs_dir, group_id, component_key, settings=settings),
        journey_svgs=render_rollup_journey_svgs(c_json, out_docs_dir, group_id, component_key),
        screenshots=_collect_screenshots_from_sirs(sirs),
        ui_components=_collect_ui_components_from_sirs(sirs),
        integrations=_collect_integrations_from_sirs(sirs),
        code_entities=_collect_code_entities(sirs),
        doc_path=page_path,
        docs_root=out_docs_dir,
    )

    if evidence_md_filename:
        md.append("")
        md.append("## Evidence appendix")
        md.append(f"See [{evidence_md_filename}]({evidence_md_filename}) for the raw evidence snippets referenced above.")

    md = decorate_headings(md)
    page_path.write_text("\n".join(md), encoding="utf-8")
def render_business_group_page(
    out_docs_dir: Path,
    group_id: str,
    resp_json: Dict[str, Any],
    evidence_md_filename: Optional[str] = None,
    facets: Optional[Dict[str, Any]] = None,
    extras: Optional[Dict[str, Any]] = None,
) -> None:
    """
    Render the group-level (component) page with YAML front-matter mirroring mkdocs output.
    """
    gid = _dir_slug(group_id)
    page_dir = out_docs_dir / gid / "components"
    page_dir.mkdir(parents=True, exist_ok=True)
    page_path = page_dir / "overview.md"

    title = resp_json.get("title") or f"Group: {group_id}"
    llm_sub = resp_json.get("llm_subscore")

    # Try to use aggregated graph_features if present in resp_json (some rollup flows include it)
    agg = None
    if isinstance(resp_json, dict):
        # resp_json might include 'graph_features' or 'distance' already
        agg = resp_json.get("graph_features") or resp_json.get("distance") or None

    md: List[str] = []

    # YAML front-matter for group
    md.append("---")
    md.append(f'title: "{title}"')
    md.append("facets:")
    md.append(f"  score: {facets.get('score', 0.0) if facets else 0.0}")
    md.append("distance:")
    md.append(f"  avg_nearest_distance: {agg.get('avg_nearest') if agg and agg.get('avg_nearest') is not None else 'null'}")
    md.append(f"  covered: {agg.get('covered',0) if agg else 0}")
    md.append(f"  total: {agg.get('total',0) if agg else 0}")
    md.append("markers:")
    markers = (agg.get("markers") if agg else []) or []
    if isinstance(markers, list) and markers:
        for m in markers:
            if isinstance(m, dict):
                md.append(f"  - id: {m.get('id')}")
                md.append(f"    type: {m.get('type')}")
            else:
                md.append(f"  - id: {m}")
    else:
        md.append("  - []")
    md.append("---")
    md.append("")

    md.append(f"# {title}")
    md.append("<!-- CONFIDENCE_INLINE -->")
    md.append(_confidence_line(llm_sub))
    md.append("")
    md.append("## Overview")
    md.append(_safe(resp_json.get("summary"), "See components for evidence-backed details."))
    md.append("")
    md.append("## Components")
    for comp in resp_json.get("components", []) or []:
        name = comp.get("name") if isinstance(comp, dict) else str(comp)
        md.append(f"- {name}")
    md.append("")

    group_diagrams = _collect_group_diagram_svgs(out_docs_dir, group_id)
    if group_diagrams:
        md.append("## Workflow diagrams")
        for svg in group_diagrams:
            rel = svg.relative_to(out_docs_dir).as_posix()
            md.append(f"![Workflow diagram]({_resolve_asset_path(rel, doc_path=page_path, docs_root=out_docs_dir)})")
        md.append("")

    extras = extras or {}
    process_flows = extras.get("process_flows") or []
    if process_flows:
        md.append("## 🔁 Process Flows")
        lines = _process_flow_summary_lines(process_flows)
        if lines:
            md.extend(lines)
        else:
            md.append("_Process flows will appear once relationships are emitted._")
        mermaid = _process_flow_mermaid(process_flows)
        if mermaid:
            md.append("")
            md.append(mermaid)
        md.append("")

    integration_summary = extras.get("integration_summary") or []
    if integration_summary:
        md.append("## 🔗 Integration Summary")
        table = _integration_summary_table(integration_summary)
        if table:
            md.append(table)
        mermaid = _integration_summary_mermaid(integration_summary, title)
        if mermaid:
            md.append("")
            md.append(mermaid)
        md.append("")

    group_integrations = extras.get("integrations") or []
    if group_integrations:
        md.append("## 🌐 Integration Catalog")
        md.append("| Library | Kind | Language |")
        md.append("|---------|------|----------|")
        for integ in group_integrations:
            md.append(f"| {integ.get('library')} | {integ.get('integration_kind')} | {integ.get('language') or '-'} |")
        md.append("")

    group_ui = extras.get("ui_components") or []
    if group_ui:
        md.append("## 🎛 UI Entry Points")
        md.append("| Component | Framework | Routes |")
        md.append("|-----------|-----------|--------|")
        for ui in group_ui:
            routes = ", ".join(ui.get("routes") or []) or "-"
            md.append(f"| {ui.get('name')} | {ui.get('framework') or '-'} | {routes} |")
        md.append("")

    group_entities = extras.get("business_entities") or []
    if group_entities:
        md.append("## 📚 Glossary & Roles")
        for entry in group_entities:
            source = entry.get("source")
            suffix = f" ({source})" if source else ""
            md.append(f"- {entry.get('name')}{suffix}")
        md.append("")

    group_diagrams = extras.get("process_diagrams") or []
    if group_diagrams:
        md.append("## 📈 Process Diagrams")
        for entry in group_diagrams:
            md.append(f"- {entry.get('name')}")
        md.append("")

    md = decorate_headings(md)
    page_path.write_text("\n".join(md), encoding="utf-8")
