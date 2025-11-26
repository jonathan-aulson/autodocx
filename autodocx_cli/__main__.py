# autodocx_cli/__main__.py
from __future__ import annotations

# 1) Load .env early (so OPENAI_API_KEY is available)
try:
    from dotenv import load_dotenv, find_dotenv
    load_dotenv(find_dotenv(usecwd=True), override=False)
except Exception:
    pass

# 2) Clean __pycache__ and .pyc files under project root (excluding virtualenvs)
def _clean_pycache():
    import shutil
    from pathlib import Path

    # Resolve project root (assumes autodocx_cli is a top-level package in repo root)
    project_root = Path(__file__).resolve().parents[1]

    # Directories to skip (avoid nuking your venv or other external caches)
    SKIP_DIRS = {".venv", "venv", "env", ".env", "node_modules", ".git"}

    # Remove __pycache__ directories
    for p in project_root.rglob("__pycache__"):
        if any(skip in p.parts for skip in SKIP_DIRS):
            continue
        try:
            shutil.rmtree(p, ignore_errors=True)
        except Exception:
            pass

    # Remove stray .pyc files
    for p in project_root.rglob("*.pyc"):
        if any(skip in p.parts for skip in SKIP_DIRS):
            continue
        try:
            p.unlink(missing_ok=True)
        except Exception:
            pass

try:
    _clean_pycache()
except Exception:
    # Don't fail the run just because cleanup hit a permission edge case
    pass

# 3) Helper to clean out/ while preserving site/ and mkdocs.yml
from pathlib import Path
import shutil


def clean_out_dir_preserve_site_and_mkdocs(out_dir: str | Path) -> None:
    """
    Recursively delete all files and subdirectories inside out_dir
    EXCEPT:
      - mkdocs.yml (at the root of out_dir)
      - site/ (and everything under it)
      - metrics/llm_usage.csv (preserve the running telemetry log)
    Creates out_dir if it doesn't exist.
    """
    p = Path(out_dir).resolve()
    p.mkdir(parents=True, exist_ok=True)

    preserved_root = {"site", "mkdocs.yml", "metrics"}  # keep 'metrics' dir but prune inside it selectively

    for child in p.iterdir():
        # Preserve /out/site and /out/mkdocs.yml
        if child.name in {"site", "mkdocs.yml"}:
            continue

        # Handle /out/metrics
        if child.name == "metrics" and child.is_dir():
            # Ensure the directory exists (it does if we're here), then remove everything except llm_usage.csv
            for m in child.iterdir():
                if m.name == "llm_usage.csv":
                    continue
                try:
                    if m.is_dir():
                        shutil.rmtree(m, ignore_errors=True)
                    else:
                        m.unlink(missing_ok=True)
                except Exception:
                    pass
            # Done pruning /metrics; move on to next root child
            continue

        # Remove anything else at out/ root
        try:
            if child.is_dir():
                shutil.rmtree(child, ignore_errors=True)
            else:
                child.unlink(missing_ok=True)
        except Exception:
            # Don't fail the run for a stubborn file; continue best-effort cleanup
            pass

    # If metrics dir does not exist, create it so future appends succeed
    (p / "metrics").mkdir(parents=True, exist_ok=True)



# 4) Now import the rest of your CLI after cleanup helpers
import argparse
import json
import time
import sys
import os
from typing import Any, Dict, List
from rich import print as rprint

from autodocx.registry import load_extractors
from autodocx.graph.builder import build_graph
from autodocx.artifacts.option1 import to_option1_artifact
from autodocx.artifacts.validator import validate_artifacts_file
from autodocx.render.mkdocs import render_docs, build_mkdocs_site
from autodocx.utils.roles import map_connectors_to_roles_with_evidence
from autodocx.utils.components import derive_component
from collections import Counter
from autodocx.visuals.flow_export import export_workflow_graphs
from autodocx.visuals.flow_renderer import render_flow_diagrams

# NEW: distance-features
try:
    from autodocx.features.distance_features import compute_graph_features
except Exception as _df_err:
    compute_graph_features = None

# LLM/evidence pieces
from autodocx.config_loader import load_config, get_all_settings
from autodocx.llm.evidence_index import build_evidence_index
from autodocx.llm.grouping import group_by_component
from autodocx.llm.rollup import rollup_group_and_persist

# scoring helper (ensure this exists in your repo)
try:
    from autodocx.scoring.facets import rollup_facets as compute_facets
except Exception:
    def compute_facets(nodes, edges):
        counts = {
            "ops": sum(1 for n in nodes if getattr(n, "type", "") == "Operation"),
            "apis": sum(1 for n in nodes if getattr(n, "type", "") == "API"),
            "events": sum(1 for n in nodes if getattr(n, "type", "") == "MessageTopic"),
            "dbs": sum(1 for n in nodes if getattr(n, "type", "") == "Datastore"),
            "infra": sum(1 for n in nodes if getattr(n, "type", "") == "InfraResource"),
            "docs": sum(1 for n in nodes if getattr(n, "type", "") == "Doc"),
        }
        parsed = 1.0 if nodes else 0.0
        endpoint_or_op_coverage = min(1.0, counts["ops"] / max(1, counts["ops"] + counts["apis"]))
        schema_evidence = min(1.0, (counts["apis"] + counts["dbs"]) / max(1, len(nodes)))
        link_integrity = min(1.0, len(edges) / max(1, counts["ops"]))
        runtime_alignment = min(1.0, counts["infra"] / max(1, len(nodes)))
        test_alignment = 0.0
        doc_alignment = min(1.0, counts["docs"] / max(1, len(nodes)))
        inferred_fraction = 0.1
        score = min(1.0,
                    0.40 * endpoint_or_op_coverage +
                    0.20 * schema_evidence +
                    0.15 * link_integrity +
                    0.10 * runtime_alignment +
                    0.05 * test_alignment +
                    0.05 * doc_alignment +
                    0.05 * parsed -
                    0.10 * inferred_fraction)
        return {"score": round(score, 3),
                "parsed": parsed,
                "endpoint_or_op_coverage": round(endpoint_or_op_coverage, 3),
                "schema_evidence": round(schema_evidence, 3),
                "link_integrity": round(link_integrity, 3),
                "runtime_alignment": round(runtime_alignment, 3),
                "test_alignment": test_alignment,
                "doc_alignment": round(doc_alignment, 3),
                "inferred_fraction": inferred_fraction,
                **counts}


CFG = load_config()


def _safe_filename(name: str) -> str:
    import re
    return re.sub(r"[^A-Za-z0-9_.-]+", "_", name).strip("_")[:200]


def _graph_node_id_for_signal_kind_and_props(kind: str, props: Dict[str, Any]) -> str:
    """
    Mirror the logic in autodocx.graph.builder.build_graph to construct the graph node id
    for a given signal (so we can attach graph_features to corresponding SIRs).
    """
    def nid(prefix: str, name: str) -> str:
        return f"{prefix}:{name}"

    kind = (kind or "").lower()
    if kind == "api":
        return nid("API", props.get("name", "api"))
    elif kind == "op":
        nm = f"{props.get('method','OP')} {props.get('path','')}"
        return nid("Operation", nm)
    elif kind == "workflow":
        return nid("Workflow", props.get("name", "workflow"))
    elif kind == "doc":
        return nid("Doc", props.get("name", "doc"))
    # Fallback (unlikely)
    return nid(kind.capitalize() or "Node", props.get("name", kind or "node"))


def call_render_docs(out_base: Path, nodes, edges, artifacts, facets, mkdocs_build: bool, debug: bool = False):
    """
    Attempt to call render_docs with several possible signatures to be compatible
    with different render_docs versions across projects.
    """
    tried = []
    # Try several signatures
    try:
        render_docs(out_base, nodes, edges, artifacts, facets)
        if debug:
            rprint("[green]render_docs called with signature (out, nodes, edges, artifacts, facets)[/green]")
    except TypeError as te:
        tried.append(("out,nodes,edges,artifacts,facets", te))
        try:
            render_docs(out_base, nodes, edges, artifacts)
            if debug:
                rprint("[green]render_docs called with signature (out, nodes, edges, artifacts)[/green]")
        except TypeError as te2:
            tried.append(("out,nodes,edges,artifacts", te2))
            try:
                render_docs(out_base, artifacts)
                if debug:
                    rprint("[green]render_docs called with signature (out, artifacts)[/green]")
            except TypeError as te3:
                tried.append(("out,artifacts", te3))
                try:
                    render_docs(out_base)
                    if debug:
                        rprint("[green]render_docs called with signature (out)[/green]")
                except Exception as final_e:
                    tried.append(("out", final_e))
                    if debug:
                        rprint("[yellow]render_docs failed for all tried signatures[/yellow]")
                        for sig, err in tried:
                            rprint(f"[yellow] tried {sig}: {err}[/yellow]")
                    raise final_e
    if mkdocs_build:
        try:
            build_mkdocs_site(out_base)
            rprint("[green]MkDocs site built at out/site[/green]")
        except Exception as e:
            rprint(f"[yellow]MkDocs build failed: {e}[/yellow]")


def normalize_group_obj(gobj: Dict[str, Any], debug: bool = False) -> Dict[str, Any]:
    """
    Ensure group object has dicts for artifacts and sirs.
    Convert strings to dict placeholders; normalize nested lists recursively.
    """
    def norm_item(x):
        # If already a dict, ensure nested lists normalized
        if isinstance(x, dict):
            for k, v in list(x.items()):
                if isinstance(v, list):
                    x[k] = [norm_item(i) for i in v]
            return x
        # If list, normalize each element
        if isinstance(x, list):
            return [norm_item(i) for i in x]
        # Otherwise, convert to minimal dict
        return {"name": str(x)}

    if not isinstance(gobj, dict):
        return {"artifacts": [], "sirs": []}
    gobj.setdefault("artifacts", [])
    gobj.setdefault("sirs", [])
    gobj["artifacts"] = [norm_item(a) for a in gobj["artifacts"]]
    gobj["sirs"] = [norm_item(s) for s in gobj["sirs"]]
    if debug:
        # show a couple names for quick sanity
        try:
            arts = [a.get("name") or a.get("artifact_type") or str(a) for a in gobj["artifacts"][:3]]
            sids = [s.get("id") or s.get("name") or str(s) for s in gobj["sirs"][:3]]
            rprint(f"[blue]Normalized group (sample) artifacts: {arts} sirs: {sids}[/blue]")
        except Exception:
            pass
    return gobj


def _load_json(path: Path) -> Any:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except Exception:
        return None


def gather_scan_stats(out_dir: Path) -> Dict[str, Any]:
    out_dir = Path(out_dir)
    sir_dir = out_dir / "sir"
    artifacts_file = out_dir / "artifacts.json"

    component_counts: Counter[str] = Counter()
    kind_counts: Counter[str] = Counter()
    connector_counts: Counter[str] = Counter()
    orphan_sirs: List[str] = []

    if sir_dir.exists():
        for sir_path in sir_dir.glob("*.json"):
            data = _load_json(sir_path)
            if not isinstance(data, dict):
                continue
            kind = str(data.get("kind") or "")
            kind_counts[kind] += 1
            comp = data.get("component_or_service") or (data.get("props") or {}).get("component_or_service") or "ungrouped"
            component_counts[str(comp)] += 1
            if not comp:
                orphan_sirs.append(sir_path.name)
            props = data.get("props") or {}
            for step in props.get("steps") or []:
                conn = (step.get("connector") or step.get("type") or "").strip()
                if conn:
                    connector_counts[conn.lower()] += 1

    artifacts = _load_json(artifacts_file)
    artifact_count = len(artifacts or []) if isinstance(artifacts, list) else 0

    return {
        "out_dir": str(out_dir),
        "sir_total": sum(kind_counts.values()),
        "artifact_total": artifact_count,
        "components": component_counts.most_common(),
        "kinds": kind_counts.most_common(),
        "connectors": connector_counts.most_common(10),
        "orphans": orphan_sirs,
    }


def show_stats(out_dir: Path, *, as_json: bool = False) -> None:
    stats = gather_scan_stats(out_dir)
    if stats["sir_total"] == 0:
        rprint(f"[yellow][stats] No SIRs found in {out_dir}. Run a scan first.[/yellow]")
        return

    if as_json:
        rprint(json.dumps(stats, indent=2))
        return

    rprint(f"[cyan][stats] Output directory:[/cyan] {stats['out_dir']}")
    rprint(f"[cyan][stats] SIR count:[/cyan] {stats['sir_total']} | [cyan]Artifacts:[/cyan] {stats['artifact_total']}")

    rprint("[green]Top components:[/green]")
    for comp, count in stats["components"]:
        rprint(f"  - {comp or 'ungrouped'}: {count}")

    rprint("[green]Signal kinds:[/green]")
    for kind, count in stats["kinds"]:
        rprint(f"  - {kind or 'unknown'}: {count}")

    if stats["connectors"]:
        rprint("[green]Top connectors (workflow steps):[/green]")
        for conn, count in stats["connectors"]:
            rprint(f"  - {conn}: {count}")

    if stats["orphans"]:
        rprint(f"[yellow][stats] Orphan SIRs (no component):[/yellow] {len(stats['orphans'])}")


def run_scan(repo: Path, out: Path, debug: bool = False, mkdocs_build: bool = False, llm_rollup: bool = False):
    # Load .env at scan root so OPENAI_API_KEY is available via os.getenv
    try:
        from dotenv import load_dotenv
        env_path = repo / ".env"
        if env_path.exists():
            load_dotenv(dotenv_path=str(env_path), override=False)
    except Exception:
        pass

    # Load extractors
    extractors = load_extractors()
    if debug:
        rprint(f"[cyan]Loaded extractors:[/cyan] {[getattr(e,'name',str(e)) for e in extractors]}")

    # Extraction
    signals = []
    for ex in extractors:
        try:
            det = ex.detect(repo)
            if debug:
                rprint(f"[cyan]{getattr(ex,'name','unknown')} detect -> {det}[/cyan]")
            if det:
                count = 0
                for p in ex.discover(repo):
                    count += 1
                    if debug:
                        rprint(f"  -> candidate: {p}")
                    try:
                        extracted = list(extract for extract in ex.extract(p))
                        signals.extend(extracted)
                    except Exception as ee:
                        rprint(f"[yellow]Extractor {getattr(ex,'name','unknown')} failed on {p}: {ee}[/yellow]")
                if debug:
                    rprint(f"[green]{getattr(ex,'name','unknown')} discovered {count} file(s)[/green]")
        except Exception as e:
            rprint(f"[yellow]Plugin {getattr(ex,'name','unknown')} failed to run detect/discover: {e}[/yellow]")

    if debug:
        rprint(f"[green]Signals extracted:[/green] {len(signals)}")

    # Attach component identifiers early so graph/artifact stages can use them
    for sig in signals:
        try:
            props = sig.props if isinstance(sig.props, dict) else {}
            component = derive_component(repo, props)
            if isinstance(props, dict):
                props.setdefault("component_or_service", component)
        except Exception:
            continue

    # Build transient graph (once) for distance-features and later persistence
    nodes, edges = build_graph(signals)
    if debug:
        rprint(f"[green]Built transient graph ({len(nodes)} nodes, {len(edges)} edges) for distance-features[/green]")

    # Distance-features (optional; requires networkx; safe no-op if module missing)
    features_map: Dict[str, Dict[str, Any]] = {}
    if compute_graph_features is not None:
        try:
            SETTINGS = get_all_settings()
        except Exception:
            SETTINGS = {}
        try:
            features_map = compute_graph_features(nodes, edges, SETTINGS.get("distance_features") and SETTINGS or CFG)
            if debug:
                rprint(f"[green]Computed distance-features for {len(features_map)} node(s)[/green]")
        except Exception as e:
            rprint(f"[yellow]Distance-features computation failed: {e}[/yellow]")
            features_map = {}
    else:
        if debug:
            rprint("[yellow]distance_features module not available; skipping graph features[/yellow]")

    # Central SIR writer (now with graph_features)
    out_base = Path(out).resolve()
    sir_out_dir = out_base / "sir"
    sir_out_dir.mkdir(parents=True, exist_ok=True)

    for idx, sig in enumerate(signals):
        try:
            kind = getattr(sig, "kind", "unknown")
            props = sig.props if isinstance(sig.props, dict) else {}
            name = props.get("name") or props.get("file") or f"{kind}_{idx}"
            safe = _safe_filename(name)

            connectors_with_evidence = []
            for s in (props.get("steps") or []):
                conn = s.get("connector") or s.get("type") or s.get("name")
                ev = s.get("evidence") or {}
                if conn:
                    connectors_with_evidence.append((conn, ev))
            for t in (props.get("triggers") or []):
                connectors_with_evidence.append((t.get("type") or t.get("path") or "", t.get("evidence") or {}))

            try:
                role_evidence = map_connectors_to_roles_with_evidence(connectors_with_evidence)
            except Exception:
                role_evidence = {}

            # Attach graph_features when we can map the signal to its graph node id
            graph_node_id = _graph_node_id_for_signal_kind_and_props(kind, props)
            graph_features = features_map.get(graph_node_id)

            component = derive_component(repo, props)
            if isinstance(props, dict):
                props.setdefault("component_or_service", component)

            sir_obj = {
                "id": f"{kind}:{safe}",
                "kind": kind,
                "name": name,
                "file": props.get("file", ""),
                "component_or_service": component,
                "props": props,
                "roles": sorted(list(role_evidence.keys())),
                "roles_evidence": role_evidence,
                "evidence": sig.evidence if hasattr(sig, "evidence") else [],
                "subscores": sig.subscores if hasattr(sig, "subscores") else {},
                "graph_features": graph_features or {},  # NEW
                "generated_at": time.time()
            }
            (sir_out_dir / f"{safe}.json").write_text(json.dumps(sir_obj, indent=2), encoding="utf-8")
            if debug:
                rprint(f"[blue]Wrote SIR:[/blue] {sir_out_dir / f'{safe}.json'} (graph_features={'yes' if graph_features else 'no'})")
        except Exception as e:
            # Be careful: name may not be defined if error occurs earlier
            try:
                rprint(f"[yellow]SIR write failed for {name}: {e}[/yellow]")
            except Exception:
                rprint(f"[yellow]SIR write failed: {e}[/yellow]")

    try:
        flow_graph_paths = export_workflow_graphs(signals, out_base)
        if debug:
            rprint(f"[green]Exported workflow graphs to {out_base / 'flows'} ({len(flow_graph_paths)} files)[/green]")
        if flow_graph_paths:
            render_flow_diagrams(flow_graph_paths, out_base)
            if debug:
                rprint(f"[green]Rendered workflow diagrams under {out_base / 'assets' / 'diagrams'}[/green]")
    except Exception as e:
        rprint(f"[yellow]Workflow graph export/render failed: {e}[/yellow]")

    # Persist graph to disk (reuse the transient graph)
    out_base.mkdir(parents=True, exist_ok=True)
    (out_base / "graph.json").write_text(json.dumps({
        "nodes": [n.__dict__ for n in nodes],
        "edges": [e.__dict__ for e in edges],
        "generated_at": time.time()
    }, indent=2), encoding="utf-8")
    if debug:
        rprint(f"[green]Wrote graph.json ({len(nodes)} nodes, {len(edges)} edges)[/green]")

    # Facets
    try:
        facets = compute_facets(nodes, edges)
    except Exception:
        facets = {"score": 0.0}
    if debug:
        rprint(f"[green]Computed facets: {facets}[/green]")

    # Artifacts
    artifacts = []
    artifacts_jsonl = out_base / "artifacts.jsonl"
    with artifacts_jsonl.open("w", encoding="utf-8") as fjsonl:
        for s in signals:
            try:
                a = to_option1_artifact(s, repo)
                artifacts.append(a)
                fjsonl.write(json.dumps(a) + "\n")
            except Exception as e:
                rprint(f"[yellow]Artifact mapping failed for signal {getattr(s,'props',{})}: {e}[/yellow]")

    artifacts_path = out_base / "artifacts.json"
    artifacts_path.write_text(json.dumps(artifacts, indent=2), encoding="utf-8")
    if debug:
        rprint(f"[green]Wrote artifacts.json ({len(artifacts)} items) and artifacts.jsonl[/green]")
    try:
        validate_artifacts_file(artifacts_path)
    except Exception as e:
        raise RuntimeError(f"Artifact validation failed: {e}") from e

    # Render docs (robust)
    try:
        call_render_docs(out_base, nodes, edges, artifacts, facets, mkdocs_build, debug)
    except Exception as e:
        rprint(f"[yellow]Docs render failed (final): {e}[/yellow]")

    # Evidence index & grouping
    try:
        evidence_index = build_evidence_index(out_base)
        if debug:
            rprint(f"[green]Built evidence_index with {len(evidence_index)} items[/green]")
    except Exception as e:
        rprint(f"[yellow]Evidence index build failed: {e}[/yellow]")
        evidence_index = {}

    try:
        groups = group_by_component(out_base, repo_root=repo)
        if debug:
            rprint(f"[green]Grouped into {len(groups)} component(s): {list(groups.keys())}[/green]")
    except Exception as e:
        rprint(f"[yellow]Grouping failed: {e}[/yellow]")
        groups = {}

    # LLM rollup
    if llm_rollup:
        if not os.getenv("OPENAI_API_KEY"):
            rprint("[yellow]OPENAI_API_KEY not set — skipping LLM rollup[/yellow]")
        else:
            for gid, gobj in groups.items():
                # Normalize and coerce group object into expected shape
                if not isinstance(gobj, dict):
                    if isinstance(gobj, list):
                        if debug:
                            rprint(f"[yellow]Coercing group '{gid}' list -> dict with artifacts (len={len(gobj)})[/yellow]")
                        gobj = {"artifacts": gobj, "sirs": []}
                    else:
                        rprint(f"[yellow]Skipping group '{gid}' — unexpected type: {type(gobj)}[/yellow]")
                        continue

                # Defensive normalization (deep)
                gobj = normalize_group_obj(gobj, debug=debug)

                try:
                    if debug:
                        rprint(f"[cyan]Rolling up group {gid} via LLM... (artifacts={len(gobj.get('artifacts',[]))}, sirs={len(gobj.get('sirs',[]))})[/cyan]")
                    resp = rollup_group_and_persist(gid, gobj, out_dir=out_base)
                    if debug:
                        rprint(f"[green]Rollup done for {gid}: llm_subscore={resp.get('llm_subscore')} approved={resp.get('approved')}[/green]")
                except Exception as e:
                    sample = {}
                    try:
                        sample["artifacts_sample"] = [a.get("name") or a.get("artifact_type") or str(a) for a in gobj.get("artifacts", [])[:3]]
                        sample["sirs_sample"] = [s.get("id") or s.get("name") or str(s) for s in gobj.get("sirs", [])[:3]]
                    except Exception:
                        sample = {"note": "failed to extract sample"}
                    rprint(f"[yellow]LLM rollup failed for group {gid}: {e} — sample={sample}[/yellow]")

    rprint(f"[green]Done.[/green] Outputs in: {out_base}")


def main():
    p = argparse.ArgumentParser(description="autodocx - modular auto-documentation")
    sub = p.add_subparsers(dest="cmd")

    s1 = sub.add_parser("scan", help="Scan a repo (or folder of repos) and render docs")
    s1.add_argument("repo", help="Path to a repository root (or a folder of repos)")
    s1.add_argument("--out", default=None, help="Output directory (defaults to YAML llm.out_dir)")
    s1.add_argument("--debug", action="store_true", help="Verbose discovery/extraction logs")
    s1.add_argument("--mkdocs-build", action="store_true", help="Build a MkDocs static site (out/site)")
    s1.add_argument("--llm-rollup", action="store_true", help="Run LLM rollup to synthesize higher-level docs (requires OPENAI_API_KEY)")

    s_stats = sub.add_parser("stats", help="Display summary statistics for an existing scan output")
    s_stats.add_argument("--out", default=None, help="Output directory to inspect (defaults to YAML llm.out_dir)")
    s_stats.add_argument("--json", action="store_true", help="Emit stats as JSON instead of human-readable text")

    # Default command to 'scan' if none supplied
    if len(sys.argv) > 1 and sys.argv[1] not in {"scan", "-h", "--help"}:
        # leave user-specified args as-is
        pass
    elif len(sys.argv) == 1:
        sys.argv.insert(1, "scan")

    args = p.parse_args()

    # Load settings and determine effective out dir
    SETTINGS = get_all_settings()
    default_out = SETTINGS["out_dir"]
    effective_out = Path(args.out or default_out).resolve()

    if args.cmd == "stats":
        show_stats(effective_out, as_json=getattr(args, "json", False))
        return

    # Clean the out/ directory while preserving site/ and mkdocs.yml
    clean_out_dir_preserve_site_and_mkdocs(effective_out)
    rprint(f"[blue][init] Cleaned {effective_out} (preserved mkdocs.yml and site/)[/blue]")

    if args.cmd == "scan":
        run_scan(Path(args.repo).resolve(), effective_out, debug=args.debug, mkdocs_build=args.mkdocs_build, llm_rollup=args.llm_rollup)
    else:
        p.print_help()


if __name__ == "__main__":
    main()


if __name__ == "__main__":
    main()
