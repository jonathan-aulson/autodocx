# autodocx/render/mkdocs.py
from __future__ import annotations
from pathlib import Path
from typing import Any, Dict, List, Sequence
import json
import shutil
import subprocess
import sys

from autodocx.render import business_renderer
from autodocx.render.business_renderer import _aggregate_graph_features
from autodocx.visuals.graphviz_flows import ensure_assets_dir

# Helpers
def _safe_slug(s: str) -> str:
    import re
    if not s:
        return "unnamed"
    return re.sub(r"[^A-Za-z0-9._-]+", "_", s).strip("_")[:120]


def _copy_assets_into_docs(out_base: Path, docs_dir: Path) -> None:
    """
    Copy the repo-level assets (out_base/assets) into docs/ so MkDocs will include them.
    Overwrites destination if present.
    """
    src = out_base / "assets"
    dst = docs_dir / "assets"
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


def _read_sirs(out_base: Path) -> List[Dict[str, Any]]:
    sir_dir = out_base / "sir"
    if not sir_dir.exists():
        return []
    out = []
    for f in sorted(sir_dir.glob("*.json")):
        try:
            j = json.loads(f.read_text(encoding="utf-8"))
            out.append(j)
        except Exception:
            continue
    return out


def _group_sirs_by_component(sirs: Sequence[Dict[str, Any]]) -> Dict[str, List[Dict[str, Any]]]:
    groups: Dict[str, List[Dict[str, Any]]] = {}
    for s in sirs:
        gid = s.get("component_or_service") or s.get("props", {}).get("component") or "ungrouped"
        groups.setdefault(gid or "ungrouped", []).append(s)
    return groups


def _write_markdown_file(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
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


def render_docs(out_base: Path, nodes: Sequence[Any], edges: Sequence[Any], artifacts: Sequence[Any], facets: Dict[str, Any]) -> None:
    """
    Render a minimal MkDocs-ready docs/ tree with:
      - docs/index.md summarizing facets
      - per-component docs under docs/components/<group>/<component>.md
      - assets copied under docs/assets (so SVGs produced earlier are available)
      - YAML front-matter is emitted at the top of each component page with facets/distance metadata
    """
    out_base = Path(out_base).resolve()
    docs_dir = out_base / "docs"
    docs_dir.mkdir(parents=True, exist_ok=True)

    # Copy assets so MkDocs can serve them
    _copy_assets_into_docs(out_base, docs_dir)

    # Read SIRs to discover components and their graph_features
    sirs = _read_sirs(out_base)
    groups = _group_sirs_by_component(sirs)

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
        index_lines.append(f"- [{gid}](/components/{gid_slug}/{gid_slug}.md) — {len(sirs_in_group)} SIR(s)")
    _write_markdown_file(docs_dir / "index.md", "\n".join(index_lines))

    # Per-group and per-SIR pages
    for gid, sirs_in_group in groups.items():
        gid_slug = _safe_slug(gid)
        group_dir = docs_dir / "components" / gid_slug
        group_dir.mkdir(parents=True, exist_ok=True)

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
        group_md.append(f"- SIR count: {len(sirs_in_group)}")
        group_md.append("")
        group_md.append("## Graph Insights (component)")
        group_md.append("")
        group_md.append(_make_compact_table_from_agg(agg))
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
                group_md.append(f"![Flow]({rel})")
                group_md.append("")

        _write_markdown_file(group_dir / f"{gid_slug}.md", "\n".join(group_md))

        # Per-SIR pages
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

            # Embed any SVGs produced for this SIR
            # Known path: docs/assets/graphs/<group_slug>/<component_slug>/*.svg
            comp_slug = _safe_slug(s.get("name") or s.get("id") or sir_slug)
            candidate_dir = docs_dir / "assets" / "graphs" / gid_slug / comp_slug
            if candidate_dir.exists():
                for svg in sorted(candidate_dir.glob("*.svg")):
                    rel = svg.relative_to(docs_dir).as_posix()
                    body_lines.append(f"![Flow]({rel})")
                    body_lines.append("")

            # Combine and write
            content = "\n".join(fm_lines + body_lines)
            _write_markdown_file(group_dir / f"{sir_slug}.md", content)


def build_mkdocs_site(out_base: Path) -> None:
    """
    Attempt to build a MkDocs static site for the generated docs/ tree.
    This is best-effort: we try to run 'mkdocs build' with cwd=out_base.
    """
    out_base = Path(out_base).resolve()
    cmd = ["mkdocs", "build", "-d", str(out_base / "site")]
    # If mkdocs.yml exists at out_base, allow mkdocs to pick it up.
    try:
        subprocess.run(cmd, cwd=str(out_base), check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        print(f"MkDocs site built at {out_base / 'site'}")
    except FileNotFoundError:
        print("mkdocs CLI not found. Install mkdocs to build the site (pip install mkdocs).")
    except subprocess.CalledProcessError as e:
        # Show a short diagnostic but don't raise to avoid hard failure
        print(f"mkdocs build failed: returncode={e.returncode}; stdout/stderr suppressed.")
    except Exception as e:
        print(f"mkdocs build encountered an error: {e}")
