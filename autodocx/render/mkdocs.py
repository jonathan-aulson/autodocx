# autodocx/render/mkdocs.py
from __future__ import annotations
from collections import defaultdict
from pathlib import Path
from typing import Any, Dict, List, Sequence, Iterable, Tuple, Set
import json
import shutil
import subprocess
import sys

try:
    from rich import print as rprint
except Exception:  # pragma: no cover
    def rprint(msg):
        print(msg)

from autodocx.render import business_renderer
from autodocx.render.business_renderer import _aggregate_graph_features
from autodocx.render.markdown_style import decorate_markdown
from autodocx.visuals.graphviz_flows import ensure_assets_dir

# Helpers
def _safe_slug(s: str) -> str:
    import re
    if not s:
        return "unnamed"
    return re.sub(r"[^A-Za-z0-9._-]+", "_", s).strip("_")[:120]


def _copy_assets_into_docs(out_base: Path, docs_dir: Path) -> None:
    """
    Copy diagram assets into docs/ so MkDocs will include them.
    Overwrites destination if present.
    """
    src = out_base / "diagrams"
    dst = docs_dir / "assets" / "diagrams"
    if not src.exists():
        return
    # Remove existing dst and copy tree
    if dst.exists():
        try:
            shutil.rmtree(dst)
        except Exception:
            pass
    try:
        shutil.copytree(src, dst)
    except Exception:
        # Fallback: copy file-by-file
        for p in src.rglob("*"):
            rel = p.relative_to(src)
            target = dst / rel
            target.parent.mkdir(parents=True, exist_ok=True)
            if p.is_dir():
                continue
            try:
                shutil.copy2(p, target)
            except Exception:
                pass


def _read_optional_json(path: Path) -> Any:
    if not path.exists():
        return None
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except Exception:
        return None


def _render_coverage_page(out_base: Path, docs_dir: Path) -> bool:
    coverage = _read_optional_json(out_base / "manifests" / "coverage.json")
    if not coverage:
        return False
    entries = coverage.get("extractors") or []
    if not entries:
        return False
    lines: List[str] = []
    lines.append("# Coverage Audit")
    lines.append("")
    lines.append("| Extractor | Detected | Files | Signals | Scaffold Coverage | Notes |")
    lines.append("|---|---|---|---|---|---|")
    for entry in entries:
        detected = "Yes" if entry.get("detected") else "No"
        notes = []
        if entry.get("errors"):
            notes.append(f"Errors: {len(entry['errors'])}")
        if entry.get("samples"):
            sample = entry["samples"][0]
            notes.append(f"Sample: {sample.get('name')}")
        scaffold = entry.get("business_scaffold") or {}
        tracked = scaffold.get("tracked_signals") or 0
        if scaffold.get("missing_samples"):
            missing = scaffold["missing_samples"][0]
            missing_fields = ", ".join(missing.get("missing") or [])
            missing_name = missing.get("name") or missing.get("kind") or "unknown"
            notes.append(f"Missing {missing_fields} (e.g., {missing_name})")
        if tracked:
            def _ratio(count: int) -> str:
                return f"{count}/{tracked}"
            scaffold_str = (
                f"IDs {_ratio(scaffold.get('with_identifiers', 0))}, "
                f"Datastores {_ratio(scaffold.get('with_datastores', 0))}, "
                f"Processes {_ratio(scaffold.get('with_processes', 0))}"
            )
        else:
            scaffold_str = "-"
        lines.append(
            f"| {entry.get('name')} | {detected} | {entry.get('files_considered',0)} | {entry.get('signals_emitted',0)} | {scaffold_str} | {'; '.join(notes) or '-'} |"
        )
    _write_markdown_file(docs_dir / "coverage.md", "\n".join(lines))
    return True


def _render_changelog_page(out_base: Path, docs_dir: Path) -> bool:
    data = _read_optional_json(out_base / "reports" / "changelog.json")
    if not data:
        return False
    lines: List[str] = []
    lines.append("# Change Log")
    lines.append("")
    lines.append(f"_Generated at_: {data.get('generated_at')}")
    lines.append("")
    for bucket in ("workflows", "integrations"):
        section = data.get(bucket) or {}
        lines.append(f"## {bucket.capitalize()}")
        for label in ("added", "removed", "modified"):
            items = section.get(label) or []
            lines.append(f"### {label.title()} ({len(items)})")
            if not items:
                lines.append("- None")
                continue
            for item in items[:20]:
                detail = item.get("name") or "Unnamed"
                comp = item.get("component") or "ungrouped"
                lines.append(f"- **{comp}** — {detail}")
        lines.append("")
    _write_markdown_file(docs_dir / "changelog.md", "\n".join(lines))
    return True


def _read_sirs(out_base: Path) -> List[Dict[str, Any]]:
    sir_dirs = [
        out_base / "signals" / "sir_v2",
        out_base / "signals" / "sir_v1",
    ]
    out: List[Dict[str, Any]] = []
    for sir_dir in sir_dirs:
        if not sir_dir.exists():
            continue
        for f in sorted(sir_dir.glob("*.json")):
            try:
                j = json.loads(f.read_text(encoding="utf-8"))
                out.append(j)
            except Exception:
                continue
        if out:
            break
    return out


def _group_sirs_by_component(sirs: Sequence[Dict[str, Any]]) -> Dict[str, List[Dict[str, Any]]]:
    groups: Dict[str, List[Dict[str, Any]]] = {}
    for s in sirs:
        gid = s.get("component_or_service") or s.get("props", {}).get("component") or "ungrouped"
        groups.setdefault(gid or "ungrouped", []).append(s)
    return groups


def _read_interdeps(out_base: Path) -> Dict[str, Any]:
    interdeps_path = out_base / "signals" / "interdeps.json"
    if not interdeps_path.exists():
        return {}
    try:
        data = json.loads(interdeps_path.read_text(encoding="utf-8"))
        return data if isinstance(data, dict) else {}
    except Exception:
        return {}


def _map_sirs_by_name(sirs: Sequence[Dict[str, Any]]) -> Dict[str, Dict[str, Any]]:
    out: Dict[str, Dict[str, Any]] = {}
    for sir in sirs or []:
        key = sir.get("name") or sir.get("process_name") or sir.get("id")
        if key:
            out[str(key)] = sir
    return out


def _sir_summary_line(sir: Dict[str, Any] | None) -> str:
    if not sir:
        return ""
    deterministic = sir.get("deterministic_explanation") or {}
    summary = deterministic.get("one_line_summary")
    if summary:
        return summary
    scaffold = sir.get("business_scaffold") or {}
    interfaces = scaffold.get("interfaces") or []
    if interfaces:
        iface = interfaces[0]
        method = iface.get("method") or "ANY"
        endpoint = iface.get("endpoint") or "(endpoint pending)"
        return f"Handles {method} {endpoint} traffic."
    user_story = (sir.get("props") or {}).get("user_story")
    if user_story:
        return user_story
    return sir.get("name") or ""


def _collect_shared_values(nodes: Dict[str, Dict[str, Any]], members: Sequence[str], field: str) -> Dict[str, List[str]]:
    buckets: Dict[str, Set[str]] = defaultdict(set)
    for member in members:
        values = (nodes.get(member) or {}).get(field) or []
        for value in values:
            if value:
                buckets[str(value)].add(member)
    return {val: sorted(peers) for val, peers in buckets.items() if len(peers) > 1}


def _collect_family_calls(
    edges: Sequence[Dict[str, Any]], members: Sequence[str], nodes: Dict[str, Dict[str, Any]]
) -> Tuple[List[Dict[str, str]], List[Dict[str, str]]]:
    member_set = set(members)
    intra: List[Dict[str, str]] = []
    cross: List[Dict[str, str]] = []
    for edge in edges or []:
        if edge.get("kind") != "calls":
            continue
        src = edge.get("from")
        tgt = edge.get("to")
        if not src or not tgt or src not in member_set:
            continue
        if tgt in member_set:
            intra.append({"source": src, "target": tgt})
        else:
            target_family = (nodes.get(tgt) or {}).get("family") or "unknown"
            cross.append({"source": src, "target": tgt, "target_family": target_family})
    return intra, cross


def _collect_family_interfaces(members: Sequence[str], sirs_by_name: Dict[str, Dict[str, Any]]) -> List[Dict[str, str]]:
    entries: List[Dict[str, str]] = []
    seen: Set[Tuple[str, str, str, str]] = set()
    for member in members:
        sir = sirs_by_name.get(member)
        scaffold = (sir or {}).get("business_scaffold") or {}
        for iface in scaffold.get("interfaces") or []:
            key = (
                member,
                str(iface.get("method") or ""),
                str(iface.get("endpoint") or ""),
                str(iface.get("kind") or ""),
            )
            if key in seen:
                continue
            seen.add(key)
            entries.append(
                {
                    "process": member,
                    "kind": str(iface.get("kind") or ""),
                    "method": str(iface.get("method") or ""),
                    "endpoint": str(iface.get("endpoint") or ""),
                    "description": str(iface.get("description") or iface.get("summary") or ""),
                }
            )
    return entries


def _collect_family_insights(interdeps: Dict[str, Any], sirs: Sequence[Dict[str, Any]]) -> Dict[str, Dict[str, Any]]:
    families = (interdeps or {}).get("families") or {}
    if not families:
        return {}
    nodes = (interdeps or {}).get("nodes") or {}
    edges = (interdeps or {}).get("edges") or []
    sirs_by_name = _map_sirs_by_name(sirs)
    insights: Dict[str, Dict[str, Any]] = {}
    for fam, members in families.items():
        if not members:
            continue
        member_entries: List[Dict[str, str]] = []
        for member in sorted(set(members)):
            sir = sirs_by_name.get(member)
            member_entries.append(
                {
                    "name": member,
                    "component": (sir or {}).get("component_or_service") or (nodes.get(member) or {}).get("component"),
                    "summary": _sir_summary_line(sir),
                }
            )
        interfaces = _collect_family_interfaces(members, sirs_by_name)
        shared_identifiers = _collect_shared_values(nodes, members, "identifiers")
        shared_datastores = _collect_shared_values(nodes, members, "datastores")
        intra_calls, cross_calls = _collect_family_calls(edges, members, nodes)
        insights[fam] = {
            "members": member_entries,
            "interfaces": interfaces,
            "shared_identifiers": shared_identifiers,
            "shared_datastores": shared_datastores,
            "intra_calls": intra_calls,
            "cross_calls": cross_calls,
        }
    return insights


def _render_family_docs(docs_dir: Path, family_insights: Dict[str, Dict[str, Any]]) -> bool:
    if not family_insights:
        return False
    fam_dir = docs_dir / "families"
    fam_dir.mkdir(parents=True, exist_ok=True)
    index_lines = ["# Domain Families", ""]
    wrote = False
    for fam, info in sorted(family_insights.items()):
        slug = _safe_slug(fam)
        index_lines.append(f"- [{fam}](./{slug}.md) — {len(info.get('members', []))} processes")
        lines: List[str] = []
        lines.append(f"# Family: {fam}")
        lines.append("")
        lines.append("## Members")
        if info.get("members"):
            for member in info["members"]:
                comp = member.get("component") or "ungrouped"
                summary = member.get("summary") or "Summary pending."
                lines.append(f"- **{member['name']}** (_{comp}_) — {summary}")
        else:
            lines.append("- No processes assigned to this family yet.")
        lines.append("")
        lines.append("## Interfaces & Endpoints")
        interfaces = info.get("interfaces") or []
        if interfaces:
            lines.append("| Process | Kind | Method | Endpoint | Description |")
            lines.append("|---|---|---|---|---|")
            for iface in interfaces:
                lines.append(
                    f"| {iface['process']} | {iface['kind'] or '-'} | {iface['method'] or '-'} | {iface['endpoint'] or '-'} | {iface['description'] or '-'} |"
                )
        else:
            lines.append("_No interfaces were discovered for this family._")
        lines.append("")
        lines.append("## Intra-family calls")
        intra_calls = info.get("intra_calls") or []
        if intra_calls:
            for call in intra_calls:
                lines.append(f"- {call['source']} → {call['target']}")
        else:
            lines.append("- No intra-family calls detected.")
        lines.append("")
        lines.append("## Shared data")
        identifiers = info.get("shared_identifiers") or {}
        datastores = info.get("shared_datastores") or {}
        if identifiers:
            lines.append("**Identifiers used by multiple processes**")
            for ident, procs in sorted(identifiers.items()):
                lines.append(f"- `{ident}` → {', '.join(procs)}")
        if datastores:
            lines.append("")
            lines.append("**Datastores shared across processes**")
            for ds, procs in sorted(datastores.items()):
                lines.append(f"- `{ds}` → {', '.join(procs)}")
        if not identifiers and not datastores:
            lines.append("- No shared identifiers or datastores flagged.")
        lines.append("")
        lines.append("## Cross-family calls")
        cross_calls = info.get("cross_calls") or []
        if cross_calls:
            for call in cross_calls:
                lines.append(
                    f"- {call['source']} → {call['target']} (targets family `{call.get('target_family','unknown')}`)"
                )
        else:
            lines.append("- No cross-family calls detected.")
        lines.append("")
        _write_markdown_file(fam_dir / f"{slug}.md", "\n".join(lines))
        wrote = True
    if wrote:
        _write_markdown_file(fam_dir / "index.md", "\n".join(index_lines))
    return wrote


def _aggregate_cross_family_calls(family_insights: Dict[str, Dict[str, Any]]) -> Dict[Tuple[str, str], Dict[str, Any]]:
    summary: Dict[Tuple[str, str], Dict[str, Any]] = {}
    for fam, info in family_insights.items():
        for call in info.get("cross_calls") or []:
            target_family = call.get("target_family") or "unknown"
            key = (fam, target_family)
            entry = summary.setdefault(key, {"count": 0, "samples": []})
            entry["count"] += 1
            if len(entry["samples"]) < 3:
                entry["samples"].append(f"{call['source']} → {call['target']}")
    return summary


def _collect_cross_family_shared_data(interdeps: Dict[str, Any]) -> List[Dict[str, Any]]:
    nodes = (interdeps or {}).get("nodes") or {}
    edges = (interdeps or {}).get("edges") or []
    buckets: Dict[Tuple[str, str], Dict[str, Set[str]]] = {}
    for edge in edges:
        kind = edge.get("kind")
        if kind not in {"shared_identifier", "shared_datastore"}:
            continue
        src = edge.get("from")
        tgt = edge.get("to")
        if not src or not tgt:
            continue
        fam_a = (nodes.get(src) or {}).get("family")
        fam_b = (nodes.get(tgt) or {}).get("family")
        if not fam_a or not fam_b or fam_a == fam_b:
            continue
        val = edge.get("value")
        if not val:
            continue
        fam_pair = tuple(sorted((fam_a, fam_b)))
        entry = buckets.setdefault(fam_pair, {"shared_identifier": set(), "shared_datastore": set()})
        entry[kind].add(str(val))
    output: List[Dict[str, Any]] = []
    for fam_pair, values in sorted(buckets.items()):
        output.append(
            {
                "families": fam_pair,
                "identifiers": sorted(values.get("shared_identifier") or []),
                "datastores": sorted(values.get("shared_datastore") or []),
            }
        )
    return output


def _render_repo_overview(docs_dir: Path, family_insights: Dict[str, Dict[str, Any]], interdeps: Dict[str, Any]) -> bool:
    if not family_insights:
        return False
    lines: List[str] = []
    lines.append("# Repository Overview")
    lines.append("")
    lines.append("## Domain families")
    lines.append("| Family | Members | Interfaces | Shared identifiers | Shared datastores |")
    lines.append("|---|---|---|---|---|")
    for fam, info in sorted(family_insights.items()):
        lines.append(
            f"| {fam} | {len(info.get('members', []))} | {len(info.get('interfaces', []))} | "
            f"{len(info.get('shared_identifiers', {}))} | {len(info.get('shared_datastores', {}))} |"
        )
    lines.append("")
    cross_summary = _aggregate_cross_family_calls(family_insights)
    lines.append("## Cross-family calls")
    if cross_summary:
        lines.append("| Source | Target | Count | Samples |")
        lines.append("|---|---|---|---|")
        for (src, tgt), entry in sorted(cross_summary.items()):
            samples = "; ".join(entry.get("samples") or [])
            lines.append(f"| {src} | {tgt} | {entry['count']} | {samples or '-'} |")
    else:
        lines.append("_No cross-family calls detected._")
    lines.append("")
    shared_data = _collect_cross_family_shared_data(interdeps)
    lines.append("## Shared data across families")
    if shared_data:
        lines.append("| Families | Shared identifiers | Shared datastores |")
        lines.append("|---|---|---|")
        for entry in shared_data:
            fams = " ↔ ".join(entry["families"])
            identifiers = ", ".join(entry["identifiers"]) or "-"
            datastores = ", ".join(entry["datastores"]) or "-"
            lines.append(f"| {fams} | {identifiers} | {datastores} |")
    else:
        lines.append("_No cross-family data overlaps detected._")
    _write_markdown_file(docs_dir / "repo_comprehensive.md", "\n".join(lines))
    return True


def _load_component_changes(out_base: Path) -> Dict[str, Any]:
    data = _read_optional_json(out_base / "reports" / "component_changes.json")
    return data if isinstance(data, dict) else {}


def _analysis_notes_for_component(repo_root: Path, component_slug: str) -> List[Dict[str, str]]:
    analysis_dir = repo_root / "analysis"
    if not analysis_dir.exists():
        return []
    patterns = [
        f"{component_slug}.md",
        f"{component_slug}_*.md",
        f"{component_slug}-*.md",
    ]
    matches: List[Path] = []
    for pattern in patterns:
        matches.extend(analysis_dir.glob(pattern))
    notes: List[Dict[str, str]] = []
    for path in sorted(set(matches)):
        try:
            text = path.read_text(encoding="utf-8")
        except Exception:
            continue
        snippet = "\n".join(text.strip().splitlines()[:20])
        notes.append({"path": path.relative_to(repo_root).as_posix(), "snippet": snippet})
    return notes


def _build_playbook_sections(sirs: List[Dict[str, Any]]) -> Dict[str, List[str]]:
    deploy: List[str] = []
    rollback: List[str] = []
    dr: List[str] = []
    for sir in sirs:
        kind = (sir.get("kind") or "").lower()
        props = sir.get("props") or {}
        name = props.get("name") or sir.get("name") or kind
        if kind == "job":
            envs = ", ".join(props.get("environments") or [])
            ci = props.get("ci_system") or ""
            deploy.append(f"{name} ({ci}) deploys to {envs or 'unspecified environments'}")
        elif kind == "workflow":
            rels = props.get("relationships") or []
            targets = {((rel.get("target") or {}).get("display") or (rel.get("target") or {}).get("kind")) for rel in rels}
            if targets:
                dr.append(f"{name} touches {', '.join(sorted(t for t in targets if t))}")
        elif kind in {"db", "sql"} or props.get("tables"):
            tables = props.get("tables") or [props.get("name")]
            rollback.append(f"Review migration for {', '.join(t for t in tables if t)} ({props.get('file')})")
    return {"deploy": deploy, "rollback": rollback, "dr": dr}


def _render_playbooks(docs_dir: Path, groups: Dict[str, List[Dict[str, Any]]]) -> bool:
    pb_dir = docs_dir / "playbooks"
    pb_entries = []
    for comp, sirs in groups.items():
        sections = _build_playbook_sections(sirs)
        if not any(sections.values()):
            continue
        slug = _safe_slug(comp)
        comp_dir = pb_dir
        comp_dir.mkdir(parents=True, exist_ok=True)
        md: List[str] = []
        md.append(f"# {comp} Playbook")
        md.append("")
        md.append("## Deployment")
        md.extend([f"- {entry}" for entry in sections["deploy"]] or ["- No deployment guidance captured yet."])
        md.append("")
        md.append("## Rollback & Data Fix")
        md.extend([f"- {entry}" for entry in sections["rollback"]] or ["- No rollback notes captured yet."])
        md.append("")
        md.append("## DR / Operational Notes")
        md.extend([f"- {entry}" for entry in sections["dr"]] or ["- No DR dependencies detected."])
        _write_markdown_file(comp_dir / f"{slug}.md", "\n".join(md))
        pb_entries.append((comp, slug))
    if not pb_entries:
        return False
    index_lines = ["# Runbooks & Playbooks", ""]
    for comp, slug in sorted(pb_entries):
        index_lines.append(f"- [{comp}](./{slug}.md)")
    pb_dir.mkdir(parents=True, exist_ok=True)
    _write_markdown_file(pb_dir / "index.md", "\n".join(index_lines))
    return True


def _recent_change_lines(changes: Dict[str, Any]) -> List[str]:
    mapping = [
        ("added_workflows", "Added workflows"),
        ("removed_workflows", "Removed workflows"),
        ("modified_workflows", "Modified workflows"),
        ("added_integrations", "New integrations"),
        ("removed_integrations", "Retired integrations"),
        ("modified_integrations", "Modified integrations"),
    ]
    lines: List[str] = []
    for key, title in mapping:
        items = changes.get(key) or []
        if not items:
            continue
        names = ", ".join(sorted({item.get("name") or "Unnamed" for item in items}))
        lines.append(f"- {title}: {names}")
    return lines


def _collect_flow_diagrams(doc_base: Path, component_slug: str) -> List[str]:
    diagram_dir = doc_base / "assets" / "diagrams" / component_slug
    if not diagram_dir.exists():
        return []
    diagrams = []
    for svg in sorted(diagram_dir.glob("*.svg")):
        try:
            rel = svg.relative_to(doc_base).as_posix()
            diagrams.append(rel)
        except Exception:
            continue
    return diagrams


def _write_markdown_file(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    content = decorate_markdown(content)
    path.write_text(content, encoding="utf-8")


def _make_compact_table_from_agg(agg: Dict[str, Any]) -> str:
    # Build a compact 2-column markdown table for Graph Insights
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


def _format_evidence_list(evidence: Iterable[Any]) -> List[str]:
    lines: List[str] = []
    for ev in evidence or []:
        if isinstance(ev, dict):
            path = ev.get("path", "")
            lines.append(f"- {path} {ev.get('lines', '').strip()}")
        else:
            lines.append(f"- {ev}")
    return lines


def _collect_relationships_for_sir(sir: Dict[str, Any]) -> List[Dict[str, Any]]:
    return sir.get("relationships") or (sir.get("props") or {}).get("relationships") or []


def _relationship_highlight_lines(rels: List[Dict[str, Any]]) -> List[str]:
    if not rels:
        return []
    lines: List[str] = []
    kind_counts = defaultdict(int)
    for rel in rels:
        kind = ((rel.get("target") or {}).get("kind") or "unknown").lower()
        kind_counts[kind] += 1
    if kind_counts.get("http"):
        lines.append(f"- External HTTP/API calls: {kind_counts['http']}")
    data_total = sum(kind_counts.get(k, 0) for k in ("sql", "dataverse", "sharepoint", "cosmosdb"))
    if data_total:
        lines.append(f"- Data touchpoints: {data_total}")
    if kind_counts.get("workflow"):
        lines.append(f"- Child workflows invoked: {kind_counts['workflow']}")
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
        for s in samples:
            lines.append(f"  - {s}")
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


def _collect_workflow_summary(sir: Dict[str, Any]) -> Dict[str, Any]:
    props = sir.get("props") or {}
    triggers = props.get("triggers") or []
    steps = props.get("steps") or []

    connectors: Set[str] = set()
    for step in steps:
        conn = step.get("connector") or step.get("type")
        if conn:
            connectors.add(str(conn))

    trigger_rows: List[Tuple[str, str, str]] = []
    for trig in triggers:
        name = str(trig.get("name") or trig.get("type") or "").strip()
        ttype = str(trig.get("type") or "").strip()
        detail = ""
        schedule = trig.get("schedule") or {}
        if schedule:
            freq = schedule.get("frequency")
            interval = schedule.get("interval")
            if freq or interval:
                freq_text = str(freq).title() if isinstance(freq, str) else str(freq or "")
                interval_text = str(interval or "")
                detail = (freq_text + (" every " + interval_text if interval_text else "")).strip()
        if not detail and trig.get("schema_props"):
            detail = f"Fields: {', '.join(trig.get('schema_props'))}"
        trigger_rows.append((name or "(unnamed)", ttype or "-", detail))

    step_rows: List[Tuple[int, str, str, str]] = []
    for idx, step in enumerate(steps, start=1):
        name = str(step.get("name") or "").strip() or f"Step {idx}"
        connector = str(step.get("connector") or "").strip() or "-"
        stype = str(step.get("type") or "").strip() or "-"
        method = str(step.get("method") or "").strip()
        summary = connector
        if method:
            summary = f"{connector} [{method}]".strip()
        step_rows.append((idx, name, summary, stype))

    return {
        "engine": props.get("engine") or props.get("wf_kind") or "",
        "step_count": len(steps),
        "connectors": sorted(c for c in connectors if c),
        "trigger_rows": trigger_rows,
        "step_rows": step_rows,
    }


def _render_triggers_table(rows: List[Tuple[str, str, str]]) -> str:
    if not rows:
        return ""
    lines = ["| Name | Type | Details |", "|---|---|---|"]
    for name, ttype, detail in rows:
        lines.append(f"| {name} | {ttype or '-'} | {detail or '-'} |")
    return "\n".join(lines)


def _render_steps_table(rows: List[Tuple[int, str, str, str]]) -> str:
    if not rows:
        return ""
    lines = ["| # | Step | Connector | Kind |", "|---|---|---|---|"]
    for idx, name, connector, stype in rows:
        lines.append(f"| {idx} | {name} | {connector or '-'} | {stype or '-'} |")
    return "\n".join(lines)


def _aggregate_component_insights(sirs: Sequence[Dict[str, Any]]) -> Dict[str, Any]:
    total_flows = len(sirs)
    total_steps = 0
    connectors: Set[str] = set()
    trigger_types: Set[str] = set()
    for sir in sirs:
        props = sir.get("props") or {}
        for step in props.get("steps") or []:
            total_steps += 1
            conn = step.get("connector") or step.get("type")
            if conn:
                connectors.add(str(conn))
        for trig in props.get("triggers") or []:
            ttype = trig.get("type")
            if ttype:
                trigger_types.add(str(ttype))
    return {
        "flow_count": total_flows,
        "step_count": total_steps,
        "connectors": sorted(connectors),
        "trigger_types": sorted(trigger_types),
    }


def _group_artifacts_by_component(artifacts: Sequence[Dict[str, Any]]) -> Dict[str, List[Dict[str, Any]]]:
    grouped: Dict[str, List[Dict[str, Any]]] = {}
    for art in artifacts or []:
        comp = art.get("component_or_service") or "ungrouped"
        grouped.setdefault(comp, []).append(art)
    return grouped


def _collect_narrative_from_artifacts(artifacts: Sequence[Dict[str, Any]], sirs: Sequence[Dict[str, Any]]) -> Dict[str, Any]:
    personas: List[Dict[str, Any]] = []
    journeys: List[Dict[str, Any]] = []
    ux: List[Dict[str, Any]] = []
    screenshots: List[Dict[str, Any]] = []
    http_endpoints: List[Dict[str, Any]] = []
    data_examples: List[Dict[str, Any]] = []

    for art in artifacts or []:
        personas.extend(art.get("personas") or [])
        journeys.extend(art.get("primary_journeys") or [])
        ux.extend(art.get("ux_summaries") or [])
        screenshots.extend(art.get("screenshots") or [])
        data_examples.extend(art.get("data_examples") or [])
        for endpoint in art.get("interfaces", {}).get("http_endpoints", []):
            http_endpoints.append(endpoint)

    if not journeys:
        for sir in sirs or []:
            story = (sir.get("props") or {}).get("user_story")
            if story:
                journeys.append({"story": story, "evidence": sir.get("file")})

    if not screenshots:
        for sir in sirs or []:
            snaps = (sir.get("props") or {}).get("screenshots") or []
            if isinstance(snaps, list):
                for snap in snaps:
                    if isinstance(snap, str):
                        screenshots.append({"path": snap, "caption": sir.get("name")})
            snapshot = (sir.get("props") or {}).get("ui_snapshot")
            if snapshot:
                screenshots.append({"path": snapshot, "caption": sir.get("name")})

    return {
        "personas": personas,
        "journeys": journeys,
        "ux": ux,
        "screenshots": screenshots,
        "http_endpoints": http_endpoints,
        "data_examples": data_examples,
    }


def _render_personas(personas: Sequence[Dict[str, Any]]) -> List[str]:
    lines: List[str] = []
    for persona in personas or []:
        name = persona.get("name") or "Persona"
        goals = persona.get("goals") or ""
        lines.append(f"- **{name}** – {goals or 'Goals TBD'}")
    return lines


def _render_journeys(journeys: Sequence[Dict[str, Any]]) -> List[str]:
    lines: List[str] = []
    for journey in journeys or []:
        story = journey.get("story")
        if story:
            lines.append(f"- {story}")
    return lines


def _render_http_endpoints_table(endpoints: Sequence[Dict[str, Any]]) -> str:
    if not endpoints:
        return ""
    lines = ["| Method | Path | Summary |", "|---|---|---|"]
    for ep in endpoints:
        lines.append(f"| {ep.get('method','')} | {ep.get('path','')} | {ep.get('summary','')} |")
    return "\n".join(lines)


def _render_data_examples(examples: Sequence[Dict[str, Any]]) -> List[str]:
    out: List[str] = []
    for example in examples or []:
        if "inputs" in example:
            out.append(f"- Inputs: {example['inputs']}")
        if "outputs" in example:
            out.append(f"- Outputs: {example['outputs']}")
        if "example_row" in example:
            out.append(f"- Sample row: `{example['example_row']}`")
    return out


def _render_screenshots_section(
    screenshots: Sequence[Dict[str, Any]],
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
        rel_path = _resolve_asset_path(path, doc_path=doc_path, docs_root=docs_root)
        lines.append(f"![{caption}]({rel_path})")
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


def _relationships_mermaid(rels: Sequence[Dict[str, Any]]) -> str:
    edges = []
    seen = set()
    for rel in rels or []:
        src = (rel.get("source") or {}).get("name") or (rel.get("source") or {}).get("type")
        tgt = (rel.get("target") or {}).get("display") or (rel.get("target") or {}).get("ref")
        op = (rel.get("operation") or {}).get("type") or ""
        if not src or not tgt:
            continue
        key = (src, tgt, op)
        if key in seen:
            continue
        seen.add(key)
        label = op or "flows to"
        edges.append(f'    "{src}" -->|{label}| "{tgt}"')
    if not edges:
        return ""
    return "```mermaid\nflowchart LR\n" + "\n".join(edges[:20]) + "\n```"


def render_docs(out_base: Path, nodes: Sequence[Any], edges: Sequence[Any], artifacts: Sequence[Any], facets: Dict[str, Any]) -> None:
    """
    Render a minimal MkDocs-ready docs/ tree with:
      - docs/index.md summarizing facets
      - component summaries under docs/<group>/<group>.md
      - per-SIR details under docs/<group>/components/<sir>.md
      - assets copied under docs/assets (so SVGs produced earlier are available)
      - YAML front-matter is emitted at the top of each component page with facets/distance metadata
    """
    out_base = Path(out_base).resolve()
    docs_dir = out_base / "docs"
    docs_dir.mkdir(parents=True, exist_ok=True)
    repo_root = out_base.parent

    # Copy assets so MkDocs can serve them
    _copy_assets_into_docs(out_base, docs_dir)
    coverage_written = _render_coverage_page(out_base, docs_dir)
    changelog_written = _render_changelog_page(out_base, docs_dir)

    # Read SIRs to discover components and their graph_features
    sirs = _read_sirs(out_base)
    groups = _group_sirs_by_component(sirs)
    artifacts_by_component = _group_artifacts_by_component(artifacts)
    playbooks_written = _render_playbooks(docs_dir, groups)
    component_changes = _load_component_changes(out_base)
    interdeps = _read_interdeps(out_base)
    family_insights = _collect_family_insights(interdeps, sirs)
    families_written = _render_family_docs(docs_dir, family_insights)
    repo_overview_written = _render_repo_overview(docs_dir, family_insights, interdeps)

    # Write index.md with global facets summary and component links
    index_lines: List[str] = []
    index_lines.append("# Project Documentation")
    index_lines.append("")
    index_lines.append("## Rollup facets")
    index_lines.append("")
    index_lines.append("| Metric | Value |")
    index_lines.append("|---|---|")
    index_lines.append(f"| Score | {facets.get('score', 'n/a')} |")
    index_lines.append(f"| Ops | {facets.get('ops', 0)} |")
    index_lines.append(f"| APIs | {facets.get('apis', 0)} |")
    index_lines.append(f"| Events | {facets.get('events', 0)} |")
    index_lines.append("")
    index_lines.append("## Components")
    index_lines.append("")
    for gid, sirs_in_group in sorted(groups.items()):
        gid_slug = _safe_slug(gid)
        index_lines.append(f"- [{gid}](/{gid_slug}/{gid_slug}.md) - {len(sirs_in_group)} SIR(s)")
    if families_written:
        index_lines.append("")
        index_lines.append("- [Domain Families](/families/index.md)")
    if repo_overview_written:
        index_lines.append("- [Repository Overview](/repo_comprehensive.md)")
    if coverage_written:
        index_lines.append("")
        index_lines.append("- [Coverage Audit](/coverage.md)")
    if changelog_written:
        index_lines.append("- [Change Log](/changelog.md)")
    if playbooks_written:
        index_lines.append("- [Runbooks & Playbooks](/playbooks/index.md)")
    _write_markdown_file(docs_dir / "index.md", "\n".join(index_lines))

    # Per-group and per-SIR pages
    for gid, sirs_in_group in groups.items():
        component_artifacts = artifacts_by_component.get(gid, [])
        narrative = _collect_narrative_from_artifacts(component_artifacts, sirs_in_group)
        gid_slug = _safe_slug(gid)
        group_dir = docs_dir / gid_slug
        group_dir.mkdir(parents=True, exist_ok=True)
        group_doc_path = group_dir / f"{gid_slug}.md"

        # Aggregate graph_features for the whole group (component-level)
        agg = _aggregate_graph_features(sirs_in_group) or {}
        # group page
        group_md: List[str] = []
        group_md.append("---")
        group_md.append(f'title: "{gid}"')
        group_md.append("facets:")
        group_md.append(f"  score: {facets.get('score', 0.0)}")
        group_md.append("distance:")
        group_md.append(f"  avg_nearest_distance: {agg.get('avg_nearest') if agg.get('avg_nearest') is not None else 'null'}")
        group_md.append(f"  covered: {agg.get('covered',0)}")
        group_md.append(f"  total: {agg.get('total',0)}")
        group_md.append("---")
        group_md.append("")
        group_md.append(f"# {gid}")
        group_md.append("")
        group_md.append("## Summary")
        summary = _aggregate_component_insights(sirs_in_group)
        group_md.append(f"- Workflow count: {summary['flow_count']}")
        group_md.append(f"- Total steps parsed: {summary['step_count']}")
        if summary["trigger_types"]:
            group_md.append(f"- Trigger types: {', '.join(summary['trigger_types'])}")
        if summary["connectors"]:
            group_md.append(f"- Connectors observed: {', '.join(summary['connectors'])}")
        group_md.append("")
        group_md.append("## How users interact")
        persona_lines = _render_personas(narrative.get("personas"))
        if persona_lines:
            group_md.extend(persona_lines)
        journey_lines = _render_journeys(narrative.get("journeys"))
        if journey_lines:
            group_md.extend(journey_lines)
        if not persona_lines and not journey_lines:
            group_md.append("_Narrative details will appear once journeys are inferred._")
        group_md.append("")
        analysis_notes = _analysis_notes_for_component(repo_root, gid_slug)
        if analysis_notes:
            group_md.append("## Analyst Insights")
            for note in analysis_notes[:2]:
                group_md.append(f"**{note['path']}**")
                group_md.append("")
                group_md.append("```markdown")
                group_md.append(note.get("snippet") or "")
                group_md.append("```")
                group_md.append("")
        group_md.append("## Screens and APIs they see")
        http_table = _render_http_endpoints_table(narrative.get("http_endpoints"))
        if http_table:
            group_md.append(http_table)
            group_md.append("")
        screen_lines = _render_screenshots_section(
            narrative.get("screenshots"),
            doc_path=group_doc_path,
            docs_root=docs_dir,
        )
        if screen_lines:
            group_md.extend(screen_lines)
            group_md.append("")
        else:
            group_md.append("_No screenshots provided yet._")
            group_md.append("")
        group_md.append("## Data they produce/consume")
        data_lines = _render_data_examples(narrative.get("data_examples"))
        if data_lines:
            group_md.extend(data_lines)
        else:
            group_md.append("- Data stories pending richer signals.")
        group_md.append("")
        group_md.append("## Graph Insights (component)")
        group_md.append("")
        group_md.append(_make_compact_table_from_agg(agg))
        group_md.append("")
        recent_changes = component_changes.get(gid, {})
        change_lines = _recent_change_lines(recent_changes)
        if change_lines:
            group_md.append("## Recent Changes")
            group_md.extend(change_lines)
            group_md.append("")

        flow_diagrams = _collect_flow_diagrams(docs_dir, gid_slug)
        if flow_diagrams:
            group_md.append("## Comprehensive Workflow Diagrams")
            group_md.append("")
            for diagram in flow_diagrams:
                group_md.append(
                    f"![Workflow diagram]({_resolve_asset_path(diagram, doc_path=group_doc_path, docs_root=docs_dir)})"
                )
                group_md.append("")

        # Embed component overview SVG (if any)
        # The visuals module writes assets under assets/graphs/<group_slug>/<component_slug>/
        # We will attempt to locate a module-overview svg for this group (first sir's component_key)
        # Fallback: list any SVGs under assets/graphs/<gid_slug> and embed.
        assets_root = docs_dir / "assets" / "graphs" / gid_slug
        if assets_root.exists():
            # embed any module-overview or other svgs
            for svg in sorted(assets_root.rglob("*.svg")):
                # svg is under docs/assets/... path already
                rel = svg.relative_to(docs_dir).as_posix()
                group_md.append(f"![Flow]({_resolve_asset_path(rel, doc_path=group_doc_path, docs_root=docs_dir)})")
                group_md.append("")

        _write_markdown_file(group_doc_path, "\n".join(group_md))

        # Per-SIR pages
        details_dir = group_dir / "components"
        details_dir.mkdir(parents=True, exist_ok=True)
        for s in sirs_in_group:
            # Compose front-matter using SIR's graph_features (if present) and group facets
            sir_id = s.get("id") or s.get("name") or "sir"
            sir_slug = _safe_slug(sir_id)
            gf = s.get("graph_features") or {}
            fm_lines: List[str] = []
            title = s.get("name") or sir_id
            fm_lines.append("---")
            fm_lines.append(f'title: "{title}"')
            fm_lines.append("facets:")
            fm_lines.append(f"  score: {facets.get('score', 0.0)}")
            # Distance block (prefers per-SIR gf, fallback to group agg)
            fm_lines.append("distance:")
            fm_lines.append(f"  avg_nearest_distance: {gf.get('avg_distance_to_markers', agg.get('avg_nearest')) if gf or agg else 'null'}")
            acov = gf.get("anchor_coverage") or {}
            fm_lines.append(f"  anchors_within_r: {int(acov.get('anchors_within_r', 0)) if acov else 0}")
            fm_lines.append(f"  radius: {int(acov.get('radius', 4) if acov else 4)}")
            fm_lines.append("markers:")
            markers = gf.get("markers") or []
            if isinstance(markers, list):
                for m in markers:
                    # m may be dict or string
                    if isinstance(m, dict):
                        fm_lines.append(f"  - id: {m.get('id')}")
                        fm_lines.append(f"    type: {m.get('type')}")
                    else:
                        fm_lines.append(f"  - id: {m}")
            fm_lines.append("---")
            fm_lines.append("")
            body_lines: List[str] = []
            body_lines.append(f"# {title}")
            body_lines.append("")
            _render_explanation(body_lines, s.get("deterministic_explanation") or {})
            body_lines.append("## Graph Insights (SIR)")
            body_lines.append("")
            # Compact table for this SIR (prefers gf values)
            if gf:
                # build small table
                srows: List[str] = []
                srows.append("| Metric | Value |")
                srows.append("|---|---|")
                nmid = gf.get("nearest_marker_id") or "n/a"
                nmd = gf.get("nearest_marker_distance")
                nmd_val = nmd if (nmd is not None and nmd != float("inf")) else "n/a"
                srows.append(f"| Nearest marker | {nmid} |")
                srows.append(f"| Nearest distance | {nmd_val} |")
                dp = gf.get("distance_percentiles") or {}
                srows.append(f"| p50 | {dp.get('p50','n/a')} |")
                srows.append(f"| p90 | {dp.get('p90','n/a')} |")
                ac = gf.get("anchor_coverage") or {}
                srows.append(f"| Anchors within R | {ac.get('anchors_within_r', 0)} |")
                body_lines.extend(srows)
            else:
                body_lines.append("_No distance features available for this SIR._")
            body_lines.append("")
            user_story = (s.get("props") or {}).get("user_story")
            inputs_example = (s.get("props") or {}).get("inputs_example")
            outputs_example = (s.get("props") or {}).get("outputs_example")
            if user_story or inputs_example or outputs_example:
                body_lines.append("## User Story & Inputs")
                if user_story:
                    body_lines.append(f"- {user_story}")
                if inputs_example:
                    body_lines.append(f"- Inputs example: {inputs_example}")
                if outputs_example:
                    body_lines.append(f"- Outputs example: {outputs_example}")
                body_lines.append("")

            summary = _collect_workflow_summary(s)
            if summary["engine"] or summary["connectors"]:
                body_lines.append("## Overview")
                overview_items: List[str] = []
                if summary["engine"]:
                    overview_items.append(f"- Engine: {summary['engine']}")
                overview_items.append(f"- Steps parsed: {summary['step_count']}")
                if summary["connectors"]:
                    overview_items.append(f"- Connectors observed: {', '.join(summary['connectors'])}")
                body_lines.extend(overview_items or ["- No additional metadata captured."])
                body_lines.append("")

            trigger_table = _render_triggers_table(summary["trigger_rows"])
            if trigger_table:
                body_lines.append("## Triggers")
                body_lines.append("")
                body_lines.append(trigger_table)
                body_lines.append("")

            steps_table = _render_steps_table(summary["step_rows"])
            if steps_table:
                body_lines.append("## Steps")
                body_lines.append("")
                body_lines.append(steps_table)
                body_lines.append("")

            rels = _collect_relationships_for_sir(s)
            if rels:
                body_lines.append("## Relationship Highlights")
                body_lines.extend(_relationship_highlight_lines(rels))
                body_lines.append("")
                matrix = _relationship_matrix_table(rels)
                if matrix:
                    body_lines.append("## Dependency Matrix")
                    body_lines.append(matrix)
                    body_lines.append("")

            evidence_lines = _format_evidence_list(s.get("evidence") or [])
            if evidence_lines:
                body_lines.append("## Evidence")
                body_lines.extend(evidence_lines)
                body_lines.append("")

            rels_mermaid = _relationships_mermaid(rels)
            if rels_mermaid:
                body_lines.append("## Visual Journey")
                body_lines.append(rels_mermaid)
                body_lines.append("")

            sir_screens = (s.get("props") or {}).get("screenshots") or []
            sir_doc_path = details_dir / f"{sir_slug}.md"
            if isinstance(sir_screens, list) and sir_screens:
                body_lines.append("## Screenshots")
                body_lines.extend(
                    _render_screenshots_section(
                        [{"path": p, "caption": s.get("name")} for p in sir_screens if isinstance(p, str)],
                        doc_path=sir_doc_path,
                        docs_root=docs_dir,
                    )
                )
                body_lines.append("")
            elif (s.get("props") or {}).get("ui_snapshot"):
                body_lines.append("## Screenshots")
                body_lines.extend(
                    _render_screenshots_section(
                        [{"path": (s.get("props") or {}).get("ui_snapshot"), "caption": s.get("name")}],
                        doc_path=sir_doc_path,
                        docs_root=docs_dir,
                    )
                )
                body_lines.append("")

            # Embed any SVGs produced for this SIR
            # Known path: docs/assets/graphs/<group_slug>/<component_slug>/*.svg
            comp_slug = _safe_slug(s.get("name") or s.get("id") or sir_slug)
            candidate_dir = docs_dir / "assets" / "graphs" / gid_slug / comp_slug
            if candidate_dir.exists():
                for svg in sorted(candidate_dir.glob("*.svg")):
                    rel = svg.relative_to(docs_dir).as_posix()
                    body_lines.append(f"![Flow]({_resolve_asset_path(rel, doc_path=sir_doc_path, docs_root=docs_dir)})")
                    body_lines.append("")

            # Combine and write
            content = "\n".join(fm_lines + body_lines)
            _write_markdown_file(sir_doc_path, content)


def build_mkdocs_site(out_base: Path) -> None:
    """
    Attempt to build a MkDocs static site for the generated docs/ tree.
    This is best-effort: we try to run 'mkdocs build' with cwd=out_base.
    """
    out_base = Path(out_base).resolve()
    cmd = ["mkdocs", "build", "-d", str(out_base / "site")]
    # If mkdocs.yml exists at out_base, allow mkdocs to pick it up.
    if shutil.which("mkdocs") is None:
        rprint("[yellow]MkDocs CLI not found on PATH. Install mkdocs-material (e.g., via scripts/setup_wsl.sh) "
               "to enable static site builds.[/yellow]")
        return
    try:
        subprocess.run(cmd, cwd=str(out_base), check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        print(f"MkDocs site built at {out_base / 'site'}")
    except FileNotFoundError:
        rprint("[yellow]mkdocs CLI not found. Install mkdocs to build the site (pip install mkdocs).[/yellow]")
    except subprocess.CalledProcessError as e:
        # Show a short diagnostic but don't raise to avoid hard failure
        print(f"mkdocs build failed: returncode={e.returncode}; stdout/stderr suppressed.")
    except Exception as e:
        print(f"mkdocs build encountered an error: {e}")
def _render_explanation(md_lines: List[str], explanation: Dict[str, Any]) -> None:
    if not explanation:
        return
    if explanation.get("what_it_does"):
        md_lines.append("## What it does")
        for item in explanation["what_it_does"]:
            md_lines.append(f"- {item}")
        md_lines.append("")
    if explanation.get("why_it_matters"):
        md_lines.append("## Why it matters")
        for item in explanation["why_it_matters"]:
            md_lines.append(f"- {item}")
        md_lines.append("")
    if explanation.get("interfaces"):
        md_lines.append("## Interfaces exposed")
        for itf in explanation["interfaces"]:
            md_lines.append(f"- {itf.get('kind','service')}: {itf.get('method','ANY')} {itf.get('endpoint','unknown')}")
        md_lines.append("")
    if explanation.get("invokes"):
        md_lines.append("## Invokes / Dependencies")
        for inv in explanation["invokes"]:
            md_lines.append(f"- {inv.get('kind','component')} → {inv.get('target','unknown')}")
        md_lines.append("")
    inter = explanation.get("interdependencies") or {}
    if any(inter.get(k) for k in ("calls", "called_by", "shared_identifiers_with", "shared_datastores_with")):
        md_lines.append("## Interdependency map")
        for key in ("calls", "called_by", "shared_identifiers_with", "shared_datastores_with"):
            if inter.get(key):
                label = key.replace("_", " ").title()
                md_lines.append(f"- {label}: {', '.join(inter[key])}")
        md_lines.append("")
    inputs_fmt = _as_text_list(explanation.get("key_inputs"))
    outputs_fmt = _as_text_list(explanation.get("key_outputs"))
    if inputs_fmt or outputs_fmt:
        md_lines.append("## Key inputs & outputs")
        if inputs_fmt:
            md_lines.append(f"- Inputs: {', '.join(inputs_fmt)}")
        if outputs_fmt:
            md_lines.append(f"- Outputs: {', '.join(outputs_fmt)}")
        md_lines.append("")
    eal = explanation.get("errors_and_logging") or {}
    if eal.get("errors") or eal.get("logging"):
        md_lines.append("## Errors & Logging")
        if eal.get("errors"):
            md_lines.append(f"- Errors: {', '.join(eal['errors'])}")
        if eal.get("logging"):
            md_lines.append(f"- Logging: {', '.join(eal['logging'])}")
        md_lines.append("")
    if explanation.get("extrapolations"):
        md_lines.append("## Extrapolations")
        for hyp in explanation["extrapolations"]:
            rationale = hyp.get("rationale")
            note = f" _(because {rationale})_" if rationale else ""
            md_lines.append(f"- {hyp.get('hypothesis','Hypothesis')}{note}")
        md_lines.append("")


def _as_text_list(values: Any) -> List[str]:
    out: List[str] = []
    for item in values or []:
        if isinstance(item, dict):
            name = str(item.get("name") or "").strip()
            desc = str(item.get("description") or "").strip()
            if name and desc and desc.lower() not in {name.lower(), "-"}:
                out.append(f"{name}: {desc}")
            elif name:
                out.append(name)
            elif desc:
                out.append(desc)
            else:
                out.append(str(item))
        else:
            out.append(str(item))
    return out
