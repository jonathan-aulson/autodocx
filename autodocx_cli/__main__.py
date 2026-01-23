# autodocx_cli/__main__.py
from __future__ import annotations
import os

# 1) Load .env early (so OPENAI_API_KEY / pipeline switches are available)
from autodocx.utils.environment import load_project_dotenv

load_project_dotenv()

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
    if os.getenv("AUTODOCX_SKIP_PYCLEAN", "").lower() in {"1", "true", "yes", "on"}:
        print("[init] AUTODOCX_SKIP_PYCLEAN set; skipping repo-wide __pycache__ cleanup.")
    else:
        _clean_pycache()
except Exception:
    # Don't fail the run just because cleanup hit a permission edge case
    pass

# 3) Helper to clean out/ while preserving site/ and mkdocs.yml
from pathlib import Path
import shutil
import yaml
import csv


def clean_out_dir_preserve_site_and_mkdocs(out_dir: str | Path) -> None:
    """
    Reset the output directory before each scan:
      - Remove primary pipeline subdirectories (docs/site/flows/etc.).
      - Recreate empty docs/ directory.
      - Write a minimal mkdocs.yml stub so nav regeneration starts fresh.
      - Preserve metrics/llm_usage.csv for long-running telemetry.
    """
    p = Path(out_dir).resolve()
    p.mkdir(parents=True, exist_ok=True)

    targets = ["docs", "site", "flows", "assets", "sir_v2", "rollup", "logs", "graphs"]
    for sub in targets:
        shutil.rmtree(p / sub, ignore_errors=True)

    (p / "docs").mkdir(parents=True, exist_ok=True)

    mkdocs_stub = {"site_name": "AutoDocX", "theme": {"name": "material"}, "nav": []}
    mkdocs_path = p / "mkdocs.yml"
    try:
        mkdocs_path.write_text(yaml.safe_dump(mkdocs_stub, sort_keys=False), encoding="utf-8")
    except Exception:
        mkdocs_path.write_text("site_name: AutoDocX\nnav: []\n", encoding="utf-8")

    metrics_dir = p / "metrics"
    if metrics_dir.exists():
        for child in metrics_dir.iterdir():
            if child.name == "llm_usage.csv":
                continue
            if child.is_dir():
                shutil.rmtree(child, ignore_errors=True)
            else:
                child.unlink(missing_ok=True)
    else:
        metrics_dir.mkdir(parents=True, exist_ok=True)


def _humanize_title(stem: str) -> str:
    txt = stem.replace("_", " ").replace("-", " ").strip()
    return txt.title() if txt else stem


def regenerate_mkdocs_config(out_dir: Path, debug: bool = False) -> None:
    """
    Build a mkdocs.yml file whose navigation mirrors the unified docs layout.
    """
    docs_dir = Path(out_dir) / "docs"
    docs_dir.mkdir(parents=True, exist_ok=True)

    index_path = docs_dir / "index.md"
    if not index_path.exists():
        repo_doc = docs_dir / "repo_comprehensive.md"
        index_lines = [
            "# AutoDocX Documentation",
            "",
            "Use the navigation to browse generated docs by component, quality, and RAG topics.",
            "",
        ]
        if repo_doc.exists():
            index_lines.append(f"- Start with [Repository Overview]({repo_doc.relative_to(docs_dir).as_posix()})")
        index_path.write_text(decorate_markdown("\n".join(index_lines) + "\n"), encoding="utf-8")

    def _nav_for_folder(folder: Path) -> List[Dict[str, Any]]:
        entries: List[Dict[str, Any]] = []
        if not folder.exists():
            return entries

        md_files = [p for p in folder.iterdir() if p.is_file() and p.suffix.lower() == ".md"]

        def _file_rank(path: Path) -> tuple:
            stem = path.stem.lower()
            if stem in {"index", "readme", folder.name.lower()}:
                return (0, stem)
            return (1, stem)

        for md_file in sorted(md_files, key=_file_rank):
            rel_path = md_file.relative_to(docs_dir).as_posix()
            entries.append({_humanize_title(md_file.stem): rel_path})

        skip_dirs = {"assets", "__pycache__", ".git", ".github", ".idea", ".vscode"}
        for child in sorted([p for p in folder.iterdir() if p.is_dir() and p.name not in skip_dirs]):
            child_entries = _nav_for_folder(child)
            if child_entries:
                entries.append({_humanize_title(child.name): child_entries})
        return entries

    nav: List[Dict[str, Any]] = _nav_for_folder(docs_dir)

    if not nav:
        placeholder = docs_dir / "README.md"
        if not placeholder.exists():
            placeholder.write_text("# AutoDocX\n\nDocumentation will appear here after the next scan.\n", encoding="utf-8")
        nav.append({"Home": placeholder.relative_to(docs_dir).as_posix()})

    mkdocs_cfg = {
        "site_name": "AutoDocX",
        "theme": {
            "name": "material",
            "palette": [
                {"scheme": "default", "primary": "blue grey", "accent": "teal"},
            ],
            "features": [
                "navigation.instant",
                "navigation.tabs",
                "navigation.sections",
                "navigation.top",
                "content.code.copy",
                "content.tabs.link",
                "search.highlight",
                "search.share",
            ],
        },
        "plugins": ["search"],
        "markdown_extensions": [
            "admonition",
            "attr_list",
            "footnotes",
            "md_in_html",
            "tables",
            "pymdownx.details",
            "pymdownx.emoji",
            "pymdownx.superfences",
            "pymdownx.tabbed",
            {"pymdownx.tasklist": {"custom_checkbox": True}},
            {"pymdownx.highlight": {"anchor_linenums": True}},
            "pymdownx.inlinehilite",
        ],
        "nav": nav,
    }
    mkdocs_path = Path(out_dir) / "mkdocs.yml"
    mkdocs_text = yaml.safe_dump(mkdocs_cfg, sort_keys=False)
    mkdocs_text = mkdocs_text.replace(
        "- pymdownx.emoji\n",
        "- pymdownx.emoji:\n"
        "    emoji_index: !!python/name:material.extensions.emoji.twemoji\n"
        "    emoji_generator: !!python/name:material.extensions.emoji.to_svg\n",
    )
    mkdocs_path.write_text(mkdocs_text, encoding="utf-8")
    if debug:
        rprint(f"[blue]Regenerated mkdocs.yml with {len(nav)} nav entries[/blue]")


def sync_docs_assets(out_dir: Path) -> None:
    """
    Copy support directories into docs/ so MkDocs can serve diagrams/evidence.
    """
    out_dir = Path(out_dir)

    def _copy_tree(name: str, *, dst_suffix: str | None = None) -> None:
        src = out_dir / name
        dst = out_dir / "docs" / (dst_suffix or name)
        shutil.rmtree(dst, ignore_errors=True)
        if src.exists():
            dst.parent.mkdir(parents=True, exist_ok=True)
            shutil.copytree(src, dst)

    _copy_tree("evidence", dst_suffix="evidence/packets")


def write_evidence_manifest_doc(
    out_dir: Path,
    packet_index: Dict[str, str],
    constellations: Sequence[Dict[str, Any]],
) -> None:
    """
    Generate a curated Markdown page linking to every evidence packet.
    """
    manifest_dir = out_dir / "docs" / "evidence"
    manifest_dir.mkdir(parents=True, exist_ok=True)
    manifest_path = manifest_dir / "index.md"
    lines = [
        "---",
        'title: "Evidence Packets"',
        "---",
        "",
        "# Evidence Packets",
        "",
        "Every constellation brief links to a JSON evidence bundle containing verbatim snippets,"
        " provenance metadata, and anti-pattern findings. Use the table below to inspect or download"
        " individual packets.",
        "",
    ]
    if not packet_index:
        lines.append("_Evidence packets will appear after the next scan._")
    else:
        lines.extend(
            [
                "| Constellation | Components | Evidence Packet |",
                "|--------------|------------|-----------------|",
            ]
        )
        # keep ordering deterministic for nav stability
        for record in sorted(constellations, key=lambda r: (",".join(r.get("components", [])), r.get("slug") or r.get("id") or "")):
            cid = record.get("id") or record.get("slug") or "constellation"
            components = ", ".join(record.get("components", [])) or "-"
            packet_rel = packet_index.get(cid)
            if packet_rel:
                packet_path = Path(packet_rel)
                try:
                    packet_rel_path = packet_path.relative_to("evidence")
                except ValueError:
                    packet_rel_path = packet_path
                rel_link = (Path("packets") / packet_rel_path).as_posix()
                display = Path(packet_rel).name
                lines.append(f"| {record.get('slug') or cid} | {components} | [{display}]({rel_link}) |")
            else:
                lines.append(f"| {record.get('slug') or cid} | {components} | _pending_ |")

    manifest_path.write_text(decorate_markdown("\n".join(lines) + "\n"), encoding="utf-8")


def _log_environment_readiness(repo: Path, out_base: Path, archive_manifest: Dict[str, Any]) -> None:
    """Emit a readiness summary so runs capture roots + tool availability."""
    try:
        import shutil
        dot_available = shutil.which("dot") is not None
    except Exception:
        dot_available = False
    important_env = {key: bool(os.getenv(key)) for key in ("OPENAI_API_KEY", "ANTHROPIC_API_KEY", "AZURE_OPENAI_API_KEY")}
    roots = [str(repo.resolve())] + [
        entry.get("extracted_to") for entry in archive_manifest.get("archives", []) if entry.get("extracted_to")
    ]
    summary = {
        "repo_root": str(repo.resolve()),
        "archive_roots": [r for r in roots if r],
        "dot_available": dot_available,
        "env_present": important_env,
        "out_dir": str(out_base),
    }
    try:
        rprint(f"[cyan][init] Readiness: {summary}[/cyan]")
    except Exception:
        print(f"[init] Readiness: {summary}")


def _path_is_relative_to(path: Path, ancestor: Path) -> bool:
    try:
        path.resolve().relative_to(ancestor.resolve())
        return True
    except Exception:
        return False


def _infer_family_from_path(path: Path) -> Optional[str]:
    parts = [p for p in path.parts if p and p not in {"META-INF"}]
    if not parts:
        return None
    if len(parts) >= 2:
        return parts[-2].lower()
    return parts[-1].lower()


def _build_packaging_manifest(
    scan_roots: Sequence[Path], archive_manifest: Dict[str, Any]
) -> Tuple[Dict[str, Any], Dict[Path, Dict[str, Any]], List[Dict[str, Any]]]:
    """
    Traverse scan roots to map BW packaging artifacts to modules/families so Signals
    can be traced back to their module ownership.
    Returns (manifest_payload, file_lookup, module_roots).
    """
    entries: List[Dict[str, Any]] = []
    file_lookup: Dict[Path, Dict[str, Any]] = {}
    module_roots: List[Dict[str, Any]] = []

    archive_lookup = {
        Path(entry.get("extracted_to", "")).resolve(): entry
        for entry in (archive_manifest.get("archives") or [])
        if entry.get("extracted_to")
    }

    def _origin_for(path: Path) -> Optional[Dict[str, Any]]:
        for dest, meta in archive_lookup.items():
            if _path_is_relative_to(path, dest):
                return meta
        return None

    def _register(file_path: Path, kind: str, module_root: Path) -> None:
        module_root = module_root.resolve()
        module_name = module_root.name or module_root.stem
        family = _infer_family_from_path(module_root) or module_name.lower()
        entry = {
            "file": str(file_path.resolve()),
            "kind": kind,
            "module": module_name,
            "module_root": str(module_root),
            "family": family,
            "root": str(module_root),
        }
        origin = _origin_for(file_path)
        if origin:
            entry["archive"] = {"archive": origin.get("archive"), "id": origin.get("id")}
        entries.append(entry)
        file_lookup[file_path.resolve()] = entry
        module_roots.append({"root": module_root, "module": module_name, "family": family})

    for root in scan_roots:
        root = Path(root).resolve()
        for tibco_xml in root.rglob("META-INF/TIBCO.xml"):
            module_root = tibco_xml.parent.parent if tibco_xml.parent.name == "META-INF" else tibco_xml.parent
            _register(tibco_xml, "tibco_xml", module_root)
        for module_file in root.rglob("*.bwm"):
            _register(module_file, "module_descriptor", module_file.parent)
        for substvar in root.rglob("*.substvar"):
            _register(substvar, "substvar", substvar.parent)

    seen_roots: Set[Path] = set()
    unique_roots: List[Dict[str, Any]] = []
    for entry in module_roots:
        root = entry["root"]
        if root in seen_roots:
            continue
        seen_roots.add(root)
        unique_roots.append(entry)

    manifest_payload = {"entries": entries}
    return manifest_payload, file_lookup, unique_roots



# 4) Now import the rest of your CLI after cleanup helpers
import argparse
import json
import time
import sys
import re
from typing import Any, Dict, List, Optional, Sequence, Set, Tuple
from rich import print as rprint

from autodocx.registry import load_extractors
from autodocx.graph.builder import build_graph
from autodocx.artifacts.option1 import to_option1_artifact
from autodocx.artifacts.validator import validate_artifacts_file
from autodocx.render.mkdocs import build_mkdocs_site
from autodocx.utils.roles import map_connectors_to_roles_with_evidence
from autodocx.utils.components import derive_component
from collections import Counter, defaultdict
from autodocx.visuals.flow_export import export_workflow_graphs
from autodocx.visuals.flow_renderer import render_flow_diagrams
from autodocx.visuals.llm_flow_diagrams import generate_llm_workflow_diagrams
from autodocx.rag import EmbeddingService, generate_xml_doc_plan, build_rag_docs
from autodocx.sir.v2 import build_sir_v2
from autodocx.utils.archive_discovery import collect_scan_roots
from autodocx.enrichers.project_enrichment import enrich_project_artifacts
from autodocx.enrichers.process_enrichment import enrich_signal_metadata
from autodocx.scaffold.signal_scaffold import build_scaffold
from autodocx.scaffold.backfill import ensure_business_scaffold_inputs
from autodocx.interdeps.builder import build_interdependencies, slice_interdependencies
from autodocx.narratives.deterministic import compose_process_explanation
from autodocx.narratives.extrapolations import extrapolate_context
from autodocx.docplan import draft_doc_plan, fulfill_doc_plan
from autodocx.docplan.plan import PLAN_FILENAME
from autodocx.constellations import build_constellations, persist_constellations
from autodocx.evidence import build_evidence_packets
from autodocx.quality import run_anti_pattern_scans
from autodocx.utils.provenance import build_provenance_entries
from autodocx.orchestrator import plan_extractions

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
from autodocx.render.markdown_style import decorate_markdown

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
PROJECT_ROOT = Path(__file__).resolve().parents[1]
ANALYSIS_DIR = PROJECT_ROOT / "analysis"
SNAPSHOT_PATH = ANALYSIS_DIR / "scan_snapshot.json"
DEFAULT_MIN_WORDS = 50
COMPONENT_SUBDIR = "components"
MAX_SOURCE_CHARS = 6000


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


def _init_coverage_entry(extractor_name: str) -> Dict[str, Any]:
    return {
        "name": extractor_name,
        "detected": False,
        "files_considered": 0,
        "signals_emitted": 0,
        "errors": [],
        "samples": [],
        "root_hits": {},
    }


def _append_coverage_samples(samples: List[Dict[str, Any]], new_signals: List[Any], limit: int = 3) -> None:
    remaining = max(limit - len(samples), 0)
    if remaining <= 0:
        return
    for sig in new_signals:
        try:
            props = sig.props if isinstance(sig.props, dict) else {}
        except Exception:
            props = {}
        sample = {
            "kind": getattr(sig, "kind", "unknown"),
            "name": props.get("name") or props.get("file") or getattr(sig, "kind", ""),
            "file": props.get("file"),
            "evidence": list(getattr(sig, "evidence", []) or [])[:1],
        }
        samples.append(sample)
        remaining -= 1
        if remaining <= 0:
            break


def _ensure_scaffold_stats(entry: Dict[str, Any]) -> Dict[str, Any]:
    stats = entry.get("business_scaffold")
    if stats:
        return stats
    stats = {
        "tracked_signals": 0,
        "with_identifiers": 0,
        "with_datastores": 0,
        "with_processes": 0,
        "with_calls": 0,
        "with_logging": 0,
        "with_errors": 0,
        "missing_samples": [],
    }
    entry["business_scaffold"] = stats
    return stats


def _summarize_scaffold_stats(rows: List[Dict[str, Any]]) -> Dict[str, int]:
    """
    Build lightweight metrics from audit rows (each row is a missing-fields record).
    This is a best-effort summary to drive thresholds; signals without missing rows
    are not represented here, so "with_*" counts are conservative.
    """
    metrics = {
        "missing_scaffold": 0,
        "with_io": 0,
        "with_calls": 0,
        "with_logging": 0,
        "with_errors": 0,
    }
    for row in rows:
        missing = set(row.get("missing") or [])
        if not missing:
            continue
        # If everything is missing, treat as missing scaffold
        if missing.issuperset({"identifiers", "datastores", "processes", "calls", "logging", "errors"}):
            metrics["missing_scaffold"] += 1
        if not missing.intersection({"identifiers", "datastores", "processes"}):
            metrics["with_io"] += 1
        if "calls" not in missing:
            metrics["with_calls"] += 1
        if "logging" not in missing:
            metrics["with_logging"] += 1
        if "errors" not in missing:
            metrics["with_errors"] += 1
    return metrics


def _append_missing_scaffold_sample(samples: List[Dict[str, Any]], sig: Any, missing: List[str], limit: int = 5) -> None:
    if len(samples) >= limit:
        return
    try:
        props = sig.props if isinstance(sig.props, dict) else {}
    except Exception:
        props = {}
    samples.append(
        {
            "kind": getattr(sig, "kind", "unknown"),
            "name": props.get("name") or props.get("file") or getattr(sig, "kind", ""),
            "file": props.get("file"),
            "missing": missing,
        }
    )


def _record_scaffold_coverage(entry: Dict[str, Any], sig: Any, scaffold: Dict[str, Any], audit_rows: List[Dict[str, Any]]) -> None:
    stats = _ensure_scaffold_stats(entry)
    stats["tracked_signals"] += 1
    io_summary = (scaffold.get("io_summary") or {}) if scaffold else {}
    dependencies = (scaffold.get("dependencies") or {}) if scaffold else {}
    identifiers = list(io_summary.get("identifiers") or [])
    datastores = list(dependencies.get("datastores") or [])
    processes = list(dependencies.get("processes") or [])
    calls = list(dependencies.get("services") or []) + list(dependencies.get("processes") or [])
    logging_entries = list(scaffold.get("logging") or [])
    error_entries = list(scaffold.get("errors") or [])
    if identifiers:
        stats["with_identifiers"] += 1
    if datastores:
        stats["with_datastores"] += 1
    if processes:
        stats["with_processes"] += 1
    if calls:
        stats["with_calls"] += 1
    if logging_entries:
        stats["with_logging"] += 1
    if error_entries:
        stats["with_errors"] += 1
    missing: List[str] = []
    if not identifiers:
        missing.append("identifiers")
    if not datastores:
        missing.append("datastores")
    if not processes:
        missing.append("processes")
    if not calls:
        missing.append("calls")
    if not logging_entries:
        missing.append("logging")
    if not error_entries:
        missing.append("errors")
    if missing:
        props = {}
        if hasattr(sig, "props") and isinstance(sig.props, dict):
            props = sig.props
        if os.getenv("AUTODOCX_DEBUG_SCAFFOLD", "0") == "1":
            rprint(
                f"[magenta][scaffold] {entry.get('name')} emitted {props.get('name') or getattr(sig, 'kind', 'signal')} "
                f"without {', '.join(missing)}[/magenta]"
            )
        audit_rows.append(
            {
                "extractor": entry.get("name"),
                "kind": getattr(sig, "kind", "unknown"),
                "name": props.get("name") or props.get("file") or getattr(sig, "kind", ""),
                "file": props.get("file"),
                "missing": missing,
            }
        )
        _append_missing_scaffold_sample(stats["missing_samples"], sig, missing)


def _write_scaffold_gap_reports(out_base: Path, audit_rows: List[Dict[str, Any]]) -> int:
    gaps = len(audit_rows or [])
    data = {"gaps_recorded": gaps, "rows": audit_rows or []}
    manif_dir = out_base / "manifests"
    manif_dir.mkdir(parents=True, exist_ok=True)
    report_path = manif_dir / "scaffold_coverage.json"
    report_path.write_text(json.dumps(data, indent=2), encoding="utf-8")
    csv_path = manif_dir / "scaffold_coverage.csv"
    with csv_path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle)
        writer.writerow(["extractor", "kind", "name", "file", "missing_fields"])
        for row in audit_rows:
            writer.writerow([row.get("extractor"), row.get("kind"), row.get("name"), row.get("file"), ",".join(row.get("missing") or [])])
    # Optional thresholds
    thresholds = {
        "min_io": int(os.getenv("AUTODOCX_MIN_IO_WITH_DETAILS", "5")),
        "max_no_scaffold": int(os.getenv("AUTODOCX_MAX_MISSING_SCAFFOLD", "5")),
        "min_calls": int(os.getenv("AUTODOCX_MIN_WITH_CALLS", "3")),
        "min_logging": int(os.getenv("AUTODOCX_MIN_WITH_LOGGING", "1")),
        "min_errors": int(os.getenv("AUTODOCX_MIN_WITH_ERRORS", "1")),
    }
    metrics = _summarize_scaffold_stats(data.get("rows") or [])
    coverage_report = {
        "gaps": gaps,
        "metrics": metrics,
        "thresholds": thresholds,
        "failures": [],
    }
    # Evaluate thresholds
    if metrics["missing_scaffold"] > thresholds["max_no_scaffold"]:
        coverage_report["failures"].append(f"missing_scaffold>{thresholds['max_no_scaffold']}")
    if metrics["with_io"] < thresholds["min_io"]:
        coverage_report["failures"].append(f"with_io<{thresholds['min_io']}")
    if metrics["with_calls"] < thresholds["min_calls"]:
        coverage_report["failures"].append(f"with_calls<{thresholds['min_calls']}")
    if metrics["with_logging"] < thresholds["min_logging"]:
        coverage_report["failures"].append(f"with_logging<{thresholds['min_logging']}")
    if metrics["with_errors"] < thresholds["min_errors"]:
        coverage_report["failures"].append(f"with_errors<{thresholds['min_errors']}")
    (manif_dir / "scaffold_coverage_meta.json").write_text(json.dumps(coverage_report, indent=2), encoding="utf-8")
    return gaps


def _write_coverage_report(out_base: Path, coverage_entries: List[Dict[str, Any]], repo: Path, scan_roots: List[Path]) -> None:
    report = {
        "repo": str(repo),
        "generated_at": time.time(),
        "scan_roots": [str(root) for root in scan_roots],
        "extractors": coverage_entries,
    }
    manif_dir = out_base / "manifests"
    manif_dir.mkdir(parents=True, exist_ok=True)
    (manif_dir / "coverage.json").write_text(json.dumps(report, indent=2), encoding="utf-8")


def _summarize_artifacts_for_snapshot(artifacts: List[Dict[str, Any]]) -> Dict[str, Dict[str, Any]]:
    summary: Dict[str, Dict[str, Any]] = {"workflows": {}, "integrations": {}}
    for art in artifacts or []:
        a_type = art.get("artifact_type")
        comp = art.get("component_or_service") or "ungrouped"
        name = art.get("name") or art.get("repo_path") or a_type
        key = f"{comp}::{name}"
        if a_type == "workflow_dag":
            summary["workflows"][key] = {
                "component": comp,
                "name": name,
                "steps_summary": (art.get("workflows") or [{}])[0].get("steps_summary") if art.get("workflows") else "",
                "relationships": len(art.get("relationships") or []),
            }
        elif a_type in {"integration_signal", "integration"}:
            summary["integrations"][key] = {
                "component": comp,
                "name": name,
                "library": art.get("library") or art.get("integration_kind"),
            }
    return summary


def _diff_section(prev: Dict[str, Any], curr: Dict[str, Any]) -> Dict[str, List[Dict[str, Any]]]:
    added = [curr[key] for key in curr.keys() - prev.keys()]
    removed = [prev[key] for key in prev.keys() - curr.keys()]
    modified: List[Dict[str, Any]] = []
    for key in curr.keys() & prev.keys():
        if curr[key] != prev[key]:
            entry = dict(curr[key])
            entry["previous"] = prev[key]
            modified.append(entry)
    return {"added": added, "removed": removed, "modified": modified}


def _aggregate_component_changes(diff: Dict[str, Dict[str, List[Dict[str, Any]]]]) -> Dict[str, Dict[str, List[Dict[str, Any]]]]:
    per_component: Dict[str, Dict[str, List[Dict[str, Any]]]] = {}
    for bucket, payload in diff.items():
        for change_type in ("added", "removed", "modified"):
            for entry in payload.get(change_type, []):
                comp = entry.get("component") or "ungrouped"
                comp_bucket = per_component.setdefault(
                    comp,
                    {
                        "added_workflows": [],
                        "removed_workflows": [],
                        "modified_workflows": [],
                        "added_integrations": [],
                        "removed_integrations": [],
                        "modified_integrations": [],
                    },
                )
                key = f"{change_type}_{'workflows' if bucket == 'workflows' else 'integrations'}"
                comp_bucket.setdefault(key, []).append(entry)
    return per_component


def _persist_changelog(artifacts: List[Dict[str, Any]], out_base: Path) -> Dict[str, Any]:
    current = _summarize_artifacts_for_snapshot(artifacts)
    if SNAPSHOT_PATH.exists():
        try:
            previous = json.loads(SNAPSHOT_PATH.read_text(encoding="utf-8"))
        except Exception:
            previous = {"workflows": {}, "integrations": {}}
    else:
        previous = {"workflows": {}, "integrations": {}}
    diff_workflows = _diff_section(previous.get("workflows", {}), current.get("workflows", {}))
    diff_integrations = _diff_section(previous.get("integrations", {}), current.get("integrations", {}))
    diff = {
        "generated_at": time.time(),
        "workflows": diff_workflows,
        "integrations": diff_integrations,
    }
    diff["per_component"] = _aggregate_component_changes({"workflows": diff_workflows, "integrations": diff_integrations})
    (out_base / "changelog.json").write_text(json.dumps(diff, indent=2), encoding="utf-8")
    try:
        SNAPSHOT_PATH.parent.mkdir(parents=True, exist_ok=True)
        SNAPSHOT_PATH.write_text(json.dumps(current, indent=2), encoding="utf-8")
    except Exception:
        pass
    return diff


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


def run_doctor() -> bool:
    """
    Validate that key external dependencies are available.
    Returns True if required checks pass.
    """
    import shutil

    def add(checks, name, ok, remedy, optional=False, detail=""):
        checks.append({"name": name, "ok": ok, "remedy": remedy, "optional": optional, "detail": detail})

    checks = []
    add(checks, "Graphviz CLI (dot)", shutil.which("dot") is not None, "sudo apt install graphviz graphviz-dev")

    try:
        import graphviz  # noqa: F401
        graphviz_detail = ""
        ok_graphviz = True
    except Exception as exc:  # pragma: no cover
        ok_graphviz = False
        graphviz_detail = str(exc)
    add(checks, "Graphviz Python package", ok_graphviz, "pip install graphviz", detail=graphviz_detail)

    add(checks, "MkDocs CLI", shutil.which("mkdocs") is not None, "pip install mkdocs mkdocs-material", optional=False)
    add(checks, "Azure CLI (az)", shutil.which("az") is not None, "curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash", optional=True)
    add(checks, "Bicep CLI", shutil.which("bicep") is not None or shutil.which("az") is not None, "az bicep install", optional=True)
    add(checks, "OPENAI_API_KEY present", bool(os.getenv("OPENAI_API_KEY")), "Add OPENAI_API_KEY=... to .env", optional=True)

    rprint("[cyan][doctor] Environment check[/cyan]")
    for chk in checks:
        status = "[green]OK[/green]" if chk["ok"] else "[red]MISSING[/red]"
        if chk["optional"] and not chk["ok"]:
            status = "[yellow]OPTIONAL[/yellow]"
        detail = f" ({chk['detail']})" if chk["detail"] else ""
        rprint(f"- {chk['name']}: {status}{detail}")
        if not chk["ok"]:
            rprint(f"    remedy: {chk['remedy']}")

    required_failures = [c for c in checks if (not c["ok"]) and (not c["optional"])]
    if required_failures:
        rprint("[red][doctor] Missing required dependencies. See remedies above.[/red]")
        return False
    rprint("[green][doctor] All required dependencies satisfied.[/green]")
    return True


def run_scan(
    repo: Path,
    out: Path,
    debug: bool = False,
    mkdocs_build: bool = False,
    llm_rollup: Optional[bool] = None,
    include_archives: Optional[bool] = None,
    rag_docs: Optional[bool] = None,
    settings: Optional[Dict[str, Any]] = None,
    scan_timeout: Optional[int] = None,
):
    if settings is None:
        settings = get_all_settings()

    # Load .env at scan root so OPENAI_API_KEY is available via os.getenv
    try:
        from dotenv import load_dotenv
        env_path = repo / ".env"
        if env_path.exists():
            load_dotenv(dotenv_path=str(env_path), override=False)
    except Exception:
        pass

    out_base = Path(out).resolve()

    pipeline_cfg = settings.get("pipeline") or {}
    env_include_archives = pipeline_cfg.get("include_archives", True)
    env_rag_docs = pipeline_cfg.get("rag_docs", False)
    env_llm_rollup = pipeline_cfg.get("llm_rollup", False)
    env_plan_refresh = pipeline_cfg.get("doc_plan_refresh", True)
    env_plan_fulfill = pipeline_cfg.get("doc_plan_fulfill", True)
    timeout_seconds = scan_timeout or pipeline_cfg.get("timeout_seconds") or 0

    archives_enabled = env_include_archives if include_archives is None else include_archives
    rag_docs_enabled = env_rag_docs if rag_docs is None else rag_docs
    llm_rollup_enabled = env_llm_rollup if llm_rollup is None else llm_rollup
    scan_roots, archive_manifest = collect_scan_roots(repo, out_base, archives_enabled)
    (out_base / "scan_manifest.json").write_text(json.dumps({"primary_root": str(repo), **archive_manifest}, indent=2), encoding="utf-8")
    packaging_manifest, packaging_lookup, module_roots = _build_packaging_manifest(scan_roots, archive_manifest)
    (out_base / "packaging_manifest.json").write_text(json.dumps(packaging_manifest, indent=2), encoding="utf-8")
    _log_environment_readiness(repo, out_base, archive_manifest)
    warnings = archive_manifest.get("warnings") or []
    if archives_enabled and warnings:
        for warning in warnings:
            rprint(f"[red][archives] {warning}[/red]")
        raise RuntimeError("Archive extraction failed. Resolve the warnings above or re-run with --skip-archives to proceed without unpacking.")

    # Load extractors
    extractors = load_extractors()
    if debug:
        rprint(f"[cyan]Loaded extractors:[/cyan] {[getattr(e,'name',str(e)) for e in extractors]}")

    extractor_map: Dict[str, object] = {}
    coverage_entries: List[Dict[str, Any]] = []
    coverage_index: Dict[str, Dict[str, Any]] = {}
    for ex in extractors:
        ex_name = getattr(ex, "name", "unknown")
        extractor_map[ex_name] = ex
        coverage_entry = _init_coverage_entry(ex_name)
        coverage_entries.append(coverage_entry)
        coverage_index[ex_name] = coverage_entry
        for root in scan_roots:
            root_str = str(root)
            coverage_entry.setdefault("root_hits", {}).setdefault(root_str, {"detected": False, "files": 0, "signals": 0})
            try:
                det = ex.detect(root)
                if debug:
                    rprint(f"[cyan]{ex_name} detect@{root_str} -> {det}[/cyan]")
                if det:
                    coverage_entry["detected"] = True
                    coverage_entry["root_hits"][root_str]["detected"] = True
            except Exception as e:
                rprint(f"[yellow]Plugin {getattr(ex,'name','unknown')} failed during detect at {root_str}: {e}[/yellow]")
                coverage_entry["errors"].append(f"{root_str}: {e}")

    assignments, router_errors = plan_extractions(scan_roots, extractors, packaging_lookup)
    if router_errors:
        for err in router_errors:
            rprint(f"[yellow][router] {err}[/yellow]")

    root_paths = [root.resolve() for root in scan_roots]

    def _root_key_for(path: Path) -> str:
        for root in root_paths:
            try:
                path.relative_to(root)
                return str(root)
            except ValueError:
                continue
        return "unknown"

    def _module_for(path: Path) -> Optional[Dict[str, Any]]:
        resolved = path.resolve()
        for entry in module_roots:
            root = entry.get("root")
            if root and _path_is_relative_to(resolved, root):
                return entry
        return packaging_lookup.get(resolved)

    # Persist router assignments with module/family context for downstream consumers
    assignment_manifest: Dict[str, Any] = {"files": []}
    for file_path, extractor_list in sorted(assignments.items()):
        module_info = _module_for(file_path) or {}
        assignment_manifest["files"].append(
            {
                "file": str(file_path),
                "extractors": extractor_list,
                "module": module_info.get("module"),
                "family": module_info.get("family"),
                "module_root": str(module_info.get("root") or module_info.get("module_root") or ""),
            }
        )
    (out_base / "assignment_manifest.json").write_text(json.dumps(assignment_manifest, indent=2), encoding="utf-8")

    # Extraction driven by deterministic router
    signals: List[Any] = []
    signal_origin: Dict[int, str] = {}
    scaffold_gap_rows: List[Dict[str, Any]] = []

    deadline = time.time() + timeout_seconds if timeout_seconds else None

    for file_path in sorted(assignments.keys()):
        if deadline and time.time() > deadline:
            raise SystemExit(f"Scan timeout exceeded ({timeout_seconds}s); last file processed: {file_path}")
        assigned_extractors = assignments[file_path]
        if debug:
            rprint(f"[magenta]Router assignment:[/magenta] {file_path} -> {assigned_extractors or ['(none)']}")
        for ex_name in assigned_extractors:
            ex = extractor_map.get(ex_name)
            if ex is None:
                continue
            coverage_entry = coverage_index.get(ex_name)
            if coverage_entry is None:
                continue
            root_key = _root_key_for(file_path)
            root_stats = coverage_entry.setdefault("root_hits", {}).setdefault(root_key, {"detected": True, "files": 0, "signals": 0})
            root_stats["detected"] = True
            coverage_entry["detected"] = True
            coverage_entry["files_considered"] += 1
            root_stats["files"] += 1
            try:
                module_info = _module_for(file_path)
                extracted = list(ex.extract(file_path))
                for sig in extracted:
                    try:
                        props = sig.props if isinstance(sig.props, dict) else {}
                        if module_info and isinstance(props, dict):
                            props.setdefault("module_name", module_info.get("module"))
                            props.setdefault("module_root", str(module_info.get("root") or ""))
                            props.setdefault("family", module_info.get("family"))
                            packaging_copy = {k: (str(v) if isinstance(v, Path) else v) for k, v in module_info.items() if k != "root"}
                            if packaging_copy:
                                props.setdefault("packaging", packaging_copy)
                        enrichments = enrich_signal_metadata(sig, repo)
                        if enrichments:
                            props = sig.props if isinstance(sig.props, dict) else {}
                            props.setdefault("enrichment", {}).update(enrichments)
                    except Exception as enrich_err:
                        rprint(f"[yellow]Enrichment failed for {getattr(sig,'props',{}).get('name')}: {enrich_err}[/yellow]")
                    try:
                        ensure_business_scaffold_inputs(sig)
                    except Exception as scaffold_err:
                        rprint(f"[yellow]Failed to backfill scaffold fields for {getattr(sig,'props',{}).get('name')}: {scaffold_err}[/yellow]")
                    signal_origin[id(sig)] = ex_name
                signals.extend(extracted)
                emitted = len(extracted)
                coverage_entry["signals_emitted"] += emitted
                root_stats["signals"] += emitted
                _append_coverage_samples(coverage_entry["samples"], extracted, limit=5)
            except Exception as ee:
                rprint(f"[yellow]Extractor {ex_name} failed on {file_path}: {ee}[/yellow]")
                coverage_entry["errors"].append(f"{file_path}: {ee}")

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
    signals_dir = out_base / "signals"
    sir_v2_dir = signals_dir / "sir_v2"
    sir_v2_dir.mkdir(parents=True, exist_ok=True)

    project_enrichment = enrich_project_artifacts(repo)
    if project_enrichment:
        (sir_v2_dir / "_project_enrichment.json").write_text(json.dumps(project_enrichment, indent=2), encoding="utf-8")
    try:
        _write_coverage_report(out_base, coverage_entries, repo, scan_roots)
    except Exception as e:
        rprint(f"[yellow]Failed to write coverage report: {e}[/yellow]")

    sir_records: List[Tuple[Dict[str, Any], Path]] = []
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

            scaffold = build_scaffold(sig)
            props.setdefault("business_scaffold", scaffold)
            origin = signal_origin.get(id(sig))
            if origin:
                entry = coverage_index.get(origin)
                if entry is not None:
                    _record_scaffold_coverage(entry, sig, scaffold, scaffold_gap_rows)
            try:
                provenance_entries = build_provenance_entries(
                    repo,
                    sig.evidence if hasattr(sig, "evidence") else [],
                    props.get("file"),
                )
                sir_v2 = build_sir_v2(
                    sig,
                    repo,
                    component=component,
                    business_scaffold=scaffold,
                    graph_features=graph_features,
                    roles=sorted(role_evidence.keys()),
                    roles_evidence=role_evidence,
                    provenance=provenance_entries,
                    doc_slug=safe,
                ) or {}
                if not sir_v2:
                    sir_v2 = {
                        "process_name": name,
                        "component_or_service": component,
                        "signal_kind": kind,
                        "kind": kind,
                        "resources": {
                            "triggers": props.get("triggers") or [],
                            "steps": props.get("steps") or [],
                            "journey_touchpoints": props.get("journey_touchpoints") or [],
                            "logging": props.get("logging") or [],
                        },
                        "activities": props.get("steps") or [],
                        "transitions": props.get("transitions") or [],
                        "relationships": props.get("relationships") or [],
                        "doc_slug": safe,
                        "_doc_slug": safe,
                        "business_scaffold": scaffold,
                        "provenance": provenance_entries,
                        "graph_features": graph_features or {},
                        "roles": sorted(role_evidence.keys()),
                        "roles_evidence": role_evidence,
                        "interdependencies_slice": {},
                        "extrapolations": [],
                        "deterministic_explanation": None,
                        "props": props,
                        "evidence": sig.evidence if hasattr(sig, "evidence") else [],
                        "subscores": sig.subscores if hasattr(sig, "subscores") else {},
                        "enrichment": props.get("enrichment") or {},
                    }
                sir_v2_name = sir_v2.get("doc_slug") or _safe_filename(sir_v2.get("process_name") or safe)
                sir_v2_path = sir_v2_dir / f"{sir_v2_name}.json"
                sir_v2.setdefault("metadata", {})
                sir_v2["metadata"].setdefault("generated_at", time.time())
                if sir_v2.get("transitions") and not props.get("transitions"):
                    props["transitions"] = sir_v2["transitions"]
                if sir_v2.get("activities") and not props.get("activities"):
                    props["activities"] = sir_v2["activities"]
                sir_v2_path.write_text(json.dumps(sir_v2, indent=2), encoding="utf-8")
                sir_records.append((sir_v2, sir_v2_path))
                if debug:
                    rprint(f"[blue]Prepared SIR v2:[/blue] {sir_v2_path} (graph_features={'yes' if graph_features else 'no'})")
            except Exception as sir_err:
                rprint(f"[yellow]SIR v2 build failed for {name}: {sir_err}[/yellow]")
        except Exception as e:
            # Be careful: name may not be defined if error occurs earlier
            try:
                rprint(f"[yellow]SIR write failed for {name}: {e}[/yellow]")
            except Exception:
                rprint(f"[yellow]SIR write failed: {e}[/yellow]")

    interdeps = build_interdependencies([sir for sir, _ in sir_records])
    interdeps_path = signals_dir / "interdeps.json"
    interdeps_path.parent.mkdir(parents=True, exist_ok=True)
    interdeps_path.write_text(json.dumps(interdeps, indent=2), encoding="utf-8")

    for sir_obj, sir_path in sir_records:
        process_name = sir_obj.get("name") or sir_obj.get("process_name")
        if process_name:
            slice_data = slice_interdependencies(interdeps, process_name)
            sir_obj["interdependencies_slice"] = slice_data
            sir_obj["extrapolations"] = extrapolate_context(sir_obj, slice_data)
            sir_obj["deterministic_explanation"] = compose_process_explanation(sir_obj)
        sir_obj["business_scaffold"] = sir_obj.get("business_scaffold") or {}
        sir_path.write_text(json.dumps(sir_obj, indent=2), encoding="utf-8")

    gaps = _write_scaffold_gap_reports(out_base, scaffold_gap_rows)
    if os.getenv("AUTODOCX_FAIL_ON_SCAFFOLD_GAPS", "0").lower() in {"1", "true", "yes"}:
        if gaps:
            raise SystemExit(f"Scaffold coverage gaps detected: {gaps}")
        # Also honor detailed failures in scaffold_coverage_meta.json
        meta_path = out_base / "manifests" / "scaffold_coverage_meta.json"
        try:
            meta = json.loads(meta_path.read_text(encoding="utf-8"))
            failures = meta.get("failures") or []
            if failures:
                raise SystemExit(f"Scaffold coverage thresholds failed: {', '.join(failures)}")
        except FileNotFoundError:
            pass

    try:
        flow_graph_paths = export_workflow_graphs(signals, out_base)
        if debug:
            rprint(f"[green]Exported workflow graphs to {out_base / 'diagrams' / 'flows_json'} ({len(flow_graph_paths)} files)[/green]")
        if flow_graph_paths:
            render_flow_diagrams(flow_graph_paths, out_base)
            if debug:
                rprint(f"[green]Rendered workflow diagrams under {out_base / 'diagrams' / 'deterministic_svg'}[/green]")
    except Exception as e:
        rprint(f"[yellow]Workflow graph export/render failed: {e}[/yellow]")

    # Persist graph to disk (reuse the transient graph)
    out_base.mkdir(parents=True, exist_ok=True)
    (signals_dir / "graph.json").write_text(json.dumps({
        "nodes": [n.__dict__ for n in nodes],
        "edges": [e.__dict__ for e in edges],
        "generated_at": time.time()
    }, indent=2), encoding="utf-8")
    if debug:
        rprint(f"[green]Wrote graph.json ({len(nodes)} nodes, {len(edges)} edges) to {signals_dir / 'graph.json'}[/green]")

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
    try:
        changelog = _persist_changelog(artifacts, out_base)
        component_changes = changelog.get("per_component", {})
        (out_base / "component_changes.json").write_text(json.dumps(component_changes, indent=2), encoding="utf-8")
    except Exception as e:
        rprint(f"[yellow]Failed to compute changelog: {e}[/yellow]")

    # Constellations, evidence packets, and anti-pattern scaffolding (optional)
    enable_constellations = os.getenv("AUTODOCX_ENABLE_CONSTELLATIONS", "").lower() in {"1", "true", "yes", "on"}
    constellations_for_context: List[Dict[str, Any]] = []
    if enable_constellations:
        constellations = build_constellations(nodes, edges, sir_records, out_base)
        constellation_manifest = persist_constellations(out_base, constellations)
        manifest_lookup = {entry["id"]: entry for entry in constellation_manifest}
        for record in constellations:
            entry = dict(record)
            manifest_entry = manifest_lookup.get(record["id"])
            if manifest_entry:
                entry["slug"] = manifest_entry.get("slug")
                entry["graph_file"] = manifest_entry.get("path")
            constellations_for_context.append(entry)

    anti_patterns_by_constellation, anti_patterns_rel_path = run_anti_pattern_scans(
        out_base, repo, constellations_for_context, sir_records
    )
    evidence_packets: Dict[str, str] = {}
    if enable_constellations:
        evidence_packets = build_evidence_packets(
            out_base,
            repo,
            constellations_for_context,
            sir_records,
            anti_patterns_by_constellation,
        )
        if evidence_packets:
            write_evidence_manifest_doc(out_base, evidence_packets, constellations_for_context)

    # Build doc-context for LLM-authored docs
    doc_context = build_doc_context(
        out_base,
        sir_records,
        artifacts,
        interdeps,
        facets,
        constellations=constellations_for_context,
        evidence_packets=evidence_packets,
        anti_patterns=anti_patterns_by_constellation,
        anti_patterns_file=anti_patterns_rel_path,
    )
    context_path = out_base / "signals" / "doc_context.json"
    context_path.parent.mkdir(parents=True, exist_ok=True)
    context_path.write_text(json.dumps(doc_context, indent=2), encoding="utf-8")
    if debug:
        rprint(f"[green]Doc context saved to {context_path}[/green]")
    try:
        llm_diagram_map = generate_llm_workflow_diagrams(out_base, doc_context)
        if llm_diagram_map:
            _inject_llm_diagrams(doc_context, llm_diagram_map)
            context_path.write_text(json.dumps(doc_context, indent=2), encoding="utf-8")
            if debug:
                rprint(f"[blue]LLM-generated diagrams added for {len(llm_diagram_map)} component(s)[/blue]")
    except Exception as diagram_err:
        rprint(f"[yellow]LLM diagram generation skipped: {diagram_err}[/yellow]")

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

    plan_path = None
    plan_file_path = out_base / "docs" / PLAN_FILENAME
    if env_plan_refresh:
        try:
            plan_path = draft_doc_plan(out_base, context=doc_context)
            rprint(f"[green]Documentation plan refreshed at {plan_path}[/green]")
        except Exception as e:
            rprint(f"[yellow]Doc plan generation skipped: {e}[/yellow]")
            plan_path = plan_file_path if plan_file_path.exists() else None
    else:
        if plan_file_path.exists():
            plan_path = plan_file_path
            rprint(f"[yellow]Doc plan refresh disabled; using existing plan at {plan_file_path}[/yellow]")
        else:
            rprint("[yellow]Doc plan refresh disabled and no existing plan found; skipping plan generation.[/yellow]")

    openai_key_present = bool(os.getenv("OPENAI_API_KEY"))

    if plan_path and env_plan_fulfill:
        if not openai_key_present:
            rprint("[yellow]OPENAI_API_KEY not set — skipping doc plan fulfillment.[/yellow]")
        else:
            min_words = _read_min_words_setting()
            try:
                processed = fulfill_doc_plan(out_base, context=doc_context, min_words_per_section=min_words)
                rprint(f"[green]Doc plan fulfillment complete ({processed} doc(s) generated, min {min_words} words/section)[/green]")
            except Exception as e:
                rprint(f"[yellow]Doc plan fulfillment failed: {e}[/yellow]")
    elif env_plan_fulfill:
        rprint("[yellow]Doc plan missing; skipping LLM fulfillment.[/yellow]")
    else:
        rprint("[yellow]Doc plan fulfillment disabled; skipping LLM authoring.[/yellow]")

    if rag_docs_enabled:
        if not openai_key_present:
            rprint("[yellow]OPENAI_API_KEY not set — skipping RAG docs.[/yellow]")
        else:
            try:
                run_rag_pipeline(repo, out_base, artifacts, doc_context, debug=debug)
            except Exception as rag_err:
                rprint(f"[yellow]RAG pipeline failed: {rag_err}[/yellow]")

    if llm_rollup_enabled:
        if not openai_key_present:
            rprint("[yellow]OPENAI_API_KEY not set — skipping LLM rollup[/yellow]")
        else:
            docs_root = out_base / "docs"
            for gid, gobj in groups.items():
                if not isinstance(gobj, dict):
                    if isinstance(gobj, list):
                        if debug:
                            rprint(f"[yellow]Coercing group '{gid}' list -> dict with artifacts (len={len(gobj)})[/yellow]")
                        gobj = {"artifacts": gobj, "sirs": []}
                    else:
                        rprint(f"[yellow]Skipping group '{gid}' — unexpected type: {type(gobj)}[/yellow]")
                        continue
                gobj = normalize_group_obj(gobj, debug=debug)
                slug = _slugify(gid)
                curated_doc = docs_root / slug / f"{slug}.md"
                if curated_doc.exists():
                    snippets = gobj.setdefault("curated_docs", [])
                    snippets.append(
                        {
                            "path": curated_doc.relative_to(out_base).as_posix(),
                            "content": curated_doc.read_text(encoding="utf-8")[:MAX_SOURCE_CHARS],
                        }
                    )
                try:
                    if debug:
                        rprint(f"[cyan]Rolling up group {gid} via LLM (curated docs attached)...[/cyan]")
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

    try:
        sync_docs_assets(out_base)
        regenerate_mkdocs_config(out_base, debug=debug)
    except Exception as cfg_err:
        rprint(f"[yellow]MkDocs nav regeneration failed: {cfg_err}[/yellow]")

    if mkdocs_build:
        try:
            build_mkdocs_site(out_base)
            rprint("[green]MkDocs site built at out/site[/green]")
        except Exception as e:
            rprint(f"[yellow]MkDocs build failed: {e}[/yellow]")

    _apply_out_layout(out_base)
    rprint(f"[green]Done.[/green] Outputs in: {out_base}")


def main():
    p = argparse.ArgumentParser(description="autodocx - modular auto-documentation")
    sub = p.add_subparsers(dest="cmd")

    s1 = sub.add_parser("scan", help="Scan a repo (or folder of repos) and render docs")
    s1.add_argument("repo", help="Path to a repository root (or a folder of repos)")
    s1.add_argument("--out", default=None, help="Output directory (defaults to YAML llm.out_dir)")
    s1.add_argument("--debug", action="store_true", help="Verbose discovery/extraction logs")
    s1.add_argument("--mkdocs-build", action="store_true", help="Build a MkDocs static site (out/site)")
    s1.add_argument(
        "--llm-rollup",
        action="store_true",
        default=None,
        help="Run LLM rollup to synthesize higher-level docs (requires OPENAI_API_KEY)",
    )
    s1.add_argument(
        "--include-archives",
        dest="include_archives",
        action="store_true",
        default=None,
        help="Unpack and scan .zip/.ear/.par archives (enabled by default; this flag is kept for compatibility).",
    )
    s1.add_argument(
        "--skip-archives",
        dest="include_archives",
        action="store_false",
        help="Skip archive extraction (archives will not be unpacked or required).",
    )
    s1.add_argument(
        "--scan-timeout",
        type=int,
        default=None,
        help="Maximum scan duration in seconds (default: unlimited or YAML pipeline.timeout_seconds).",
    )
    s1.add_argument(
        "--rag-docs",
        action="store_true",
        default=None,
        help="Generate RAG-backed docs from embeddings/Qdrant",
    )
    # Doc plan + fulfillment now run automatically every scan; no flags needed.

    s_stats = sub.add_parser("stats", help="Display summary statistics for an existing scan output")
    s_stats.add_argument("--out", default=None, help="Output directory to inspect (defaults to YAML llm.out_dir)")
    s_stats.add_argument("--json", action="store_true", help="Emit stats as JSON instead of human-readable text")

    sub.add_parser("doctor", help="Check external dependencies (Graphviz, MkDocs, Azure CLI, Bicep, OPENAI_API_KEY)")

    s_pipeline = sub.add_parser(
        "pipeline",
        help="Run the full packaging→extraction→scaffold→interdeps→doc-plan pipeline (alias for scan with defaults).",
    )
    s_pipeline.add_argument("repo", help="Path to a repository root (or a folder of repos)")
    s_pipeline.add_argument("--out", default=None, help="Output directory (defaults to YAML llm.out_dir)")
    s_pipeline.add_argument("--debug", action="store_true", help="Verbose discovery/extraction logs")

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
    effective_out = Path(getattr(args, "out", None) or default_out).resolve()

    if args.cmd == "stats":
        show_stats(effective_out, as_json=getattr(args, "json", False))
        return
    if args.cmd == "doctor":
        success = run_doctor()
        sys.exit(0 if success else 1)

    # Reset the out/ directory before scanning
    clean_out_dir_preserve_site_and_mkdocs(effective_out)
    rprint(f"[blue][init] Reset {effective_out} (cleared prior artifacts and stubbed mkdocs.yml)[/blue]")

    if args.cmd in {"scan", "pipeline"}:
        run_scan(
            Path(args.repo).resolve(),
            effective_out,
            debug=args.debug,
            mkdocs_build=args.mkdocs_build,
            llm_rollup=args.llm_rollup,
            include_archives=getattr(args, "include_archives", None),
            rag_docs=args.rag_docs,
            settings=SETTINGS,
            scan_timeout=args.scan_timeout,
        )
    else:
        p.print_help()


def run_rag_pipeline(
    repo_root: Path,
    out_base: Path,
    artifacts: List[Dict[str, Any]],
    doc_context: Dict[str, Any],
    *,
    debug: bool = False,
) -> None:
    if not artifacts:
        rprint("[yellow]RAG pipeline skipped: no artifacts available for embeddings.[/yellow]")
        return
    if not os.getenv("OPENAI_API_KEY"):
        rprint("[yellow]RAG pipeline skipped: OPENAI_API_KEY is required for embeddings.[/yellow]")
        return
    rag_service = EmbeddingService(repo_root, out_base, debug=debug)
    rag_service.index_artifacts(artifacts)
    plan_path = generate_xml_doc_plan(repo_root, out_base, doc_context)
    generated = build_rag_docs(plan_path, rag_service, out_base, doc_context)
    if generated:
        rprint(f"[green]RAG docs generated ({len(generated)} page(s)) under docs/rag.[/green]")
    elif debug:
        rprint("[yellow]RAG plan parsed but produced no pages.[/yellow]")


def _read_min_words_setting() -> int:
    try:
        return max(1, int(os.getenv("AUTODOCX_SECTION_MIN_WORDS", str(DEFAULT_MIN_WORDS))))
    except Exception:
        return DEFAULT_MIN_WORDS


def _slugify(value: str) -> str:
    slug = re.sub(r"[^a-z0-9]+", "-", (value or "").lower()).strip("-")
    return slug or "doc"


def _gather_diagram_paths(out_base: Path) -> Dict[str, List[str]]:
    mapping: Dict[str, set] = defaultdict(set)
    diagrams_root = out_base / "diagrams"
    for sub in ("deterministic_svg", "llm_svg"):
        root = diagrams_root / sub
        if not root.exists():
            continue
        for component_dir in root.iterdir():
            if not component_dir.is_dir():
                continue
            key = component_dir.name
            for svg in component_dir.rglob("*.svg"):
                rel = svg.relative_to(out_base).as_posix()
                mapping[key].add(rel)
    return {k: sorted(v) for k, v in mapping.items()}


def _group_artifacts_by_component(artifacts: List[Dict[str, Any]]) -> Dict[str, List[Dict[str, Any]]]:
    grouped: Dict[str, List[Dict[str, Any]]] = defaultdict(list)
    for art in artifacts or []:
        comp = art.get("component_or_service") or "ungrouped"
        grouped[comp].append(art)
    return grouped


def _inject_llm_diagrams(context: Dict[str, Any], diag_map: Dict[str, List[str]]) -> None:
    if not diag_map:
        return
    components = context.get("components") or {}
    families = context.get("families") or {}
    repo = context.get("repo") or {}
    for component_name, paths in diag_map.items():
        comp_entry = components.get(component_name)
        if not comp_entry:
            continue
        existing = set(comp_entry.get("diagram_paths") or [])
        existing.update(paths)
        comp_entry["diagram_paths"] = sorted(existing)
    for fam_entry in families.values():
        diagrams = set(fam_entry.get("diagram_paths") or [])
        for comp_name in fam_entry.get("components", []):
            comp_entry = components.get(comp_name)
            if comp_entry:
                diagrams.update(comp_entry.get("diagram_paths") or [])
        fam_entry["diagram_paths"] = sorted(diagrams)
    repo_diagrams = set(repo.get("diagram_paths") or [])
    for comp_entry in components.values():
        repo_diagrams.update(comp_entry.get("diagram_paths") or [])
    repo["diagram_paths"] = sorted(repo_diagrams)


def _extract_business_scaffold(sir_obj: Dict[str, Any]) -> Dict[str, Any]:
    props = sir_obj.get("props") or {}
    return (props.get("business_scaffold") or sir_obj.get("business_scaffold") or {}) or {}


def _compute_process_quality(sir_obj: Dict[str, Any]) -> Dict[str, Any]:
    scaffold = _extract_business_scaffold(sir_obj)
    io_summary = (scaffold.get("io_summary") or {}) if scaffold else {}
    dependencies = (scaffold.get("dependencies") or {}) if scaffold else {}
    props = sir_obj.get("props") or {}
    resources = (scaffold.get("resources") or {}) if scaffold else {}
    if not resources:
        resources = {
            "triggers": props.get("triggers") or [],
            "steps": props.get("steps") or [],
        }

    metrics = {
        "identifiers": len(io_summary.get("identifiers") or []),
        "inputs": len(io_summary.get("inputs") or []),
        "outputs": len(io_summary.get("outputs") or []),
        "datastores": len(dependencies.get("datastores") or []),
        "processes": len(dependencies.get("processes") or []),
        "services": len(dependencies.get("internal_services") or [])
        + len(dependencies.get("external_services") or [])
        + len(dependencies.get("services") or []),
        "interfaces": len(scaffold.get("interfaces") or []),
        "invocations": len(scaffold.get("invocations") or []),
        "triggers": len(resources.get("triggers") or []),
        "steps": len(resources.get("steps") or []),
        "errors": len(scaffold.get("errors") or []),
        "logging": len(scaffold.get("logging") or []),
        "relationships": len(props.get("relationships") or []),
    }

    score = 0
    score += metrics["identifiers"] * 3
    score += metrics["datastores"] * 2
    score += metrics["processes"] * 2
    score += metrics["steps"]
    score += min(metrics["interfaces"], 3)
    score += min(metrics["relationships"], 3)
    if metrics["triggers"]:
        score += 2
    if metrics["invocations"]:
        score += min(metrics["invocations"], 3)
    if metrics["services"]:
        score += min(metrics["services"], 3)
    if metrics["errors"]:
        score += 1
    if metrics["logging"]:
        score += 1
    has_details = score >= 5 or bool(metrics["steps"] or metrics["relationships"])
    return {
        "score": score,
        "has_workflow_details": has_details,
        "metrics": metrics,
    }


DOC_SIGNAL_KINDS = {"workflow", "route"}


def _is_docworthy_sir(sir_obj: Dict[str, Any]) -> bool:
    kind = (sir_obj.get("kind") or sir_obj.get("signal_kind") or "").lower()
    if kind not in DOC_SIGNAL_KINDS:
        return False
    quality = _compute_process_quality(sir_obj)
    return bool(quality.get("has_workflow_details"))


def build_doc_context(
    out_base: Path,
    sir_records: List[tuple],
    artifacts: List[Dict[str, Any]],
    interdeps: Dict[str, Any],
    facets: Dict[str, Any],
    *,
    constellations: Optional[List[Dict[str, Any]]] = None,
    evidence_packets: Optional[Dict[str, str]] = None,
    anti_patterns: Optional[Dict[str, List[Dict[str, Any]]]] = None,
    anti_patterns_file: Optional[str] = None,
) -> Dict[str, Any]:
    out_base = Path(out_base)
    diag_map = _gather_diagram_paths(out_base)
    art_map = _group_artifacts_by_component(artifacts)
    process_family = {
        name: node.get("family")
        for name, node in (interdeps.get("nodes") or {}).items()
    }
    components: Dict[str, Dict[str, Any]] = {}
    processes: Dict[str, Dict[str, Any]] = {}
    seen_sirs: set[str] = set()
    for sir_obj, sir_path in sir_records:
        if not _is_docworthy_sir(sir_obj):
            continue
        rel = sir_path.relative_to(out_base).as_posix()
        if rel in seen_sirs:
            continue
        seen_sirs.add(rel)
        comp = sir_obj.get("component_or_service") or "ungrouped"
        slug = _slugify(comp)
        entry = components.setdefault(
            comp,
            {
                "slug": slug,
                "sir_files": [],
                "families": set(),
                "diagram_paths": diag_map.get(slug, []),
                "artifacts": art_map.get(comp, []),
                "process_slugs": set(),
                "family_slugs": set(),
            },
        )
        entry["sir_files"].append(rel)
        fam = process_family.get(sir_obj.get("name"))
        if fam:
            entry["families"].add(fam)
            entry["family_slugs"].add(_slugify(fam))
        process_name = sir_obj.get("name") or sir_obj.get("id") or rel
        process_key = rel
        proc_slug = _slugify(f"{comp}-{process_name}")
        quality = _compute_process_quality(sir_obj)
        processes[process_key] = {
            "key": process_key,
            "slug": proc_slug,
            "name": process_name,
            "component": comp,
            "sir_file": rel,
            "families": sorted({fam} if fam else []),
            "diagram_paths": diag_map.get(slug, []),
            "quality_score": quality.get("score", 0),
            "quality": quality,
            "has_workflow_details": quality.get("has_workflow_details", False),
        }
        entry["process_slugs"].add(proc_slug)

    families: Dict[str, Dict[str, Any]] = {}
    for comp, data in components.items():
        fams = data.get("families") or {"unclassified"}
        proc_slugs = sorted(data.get("process_slugs") or [])
        for fam in fams:
            fslug = _slugify(fam)
            entry = families.setdefault(
                fam,
                {
                    "slug": fslug,
                    "components": [],
                    "sir_files": [],
                    "diagram_paths": [],
                    "process_slugs": set(),
                    "component_slugs": set(),
                },
            )
            entry["components"].append(comp)
            entry["sir_files"].extend(data["sir_files"])
            entry["diagram_paths"].extend(data.get("diagram_paths") or [])
            entry["process_slugs"].update(proc_slugs)
            entry["component_slugs"].add(data["slug"])

    repo_entry = {
        "slug": "repo-overview",
        "components": sorted(components.keys()),
        "families": sorted(families.keys()),
        "sir_files": sorted({p for data in components.values() for p in data["sir_files"]}),
        "diagram_paths": sorted({d for data in components.values() for d in data.get("diagram_paths", [])}),
        "component_slugs": sorted({data["slug"] for data in components.values()}),
        "family_slugs": sorted({_slugify(name) for name in families.keys()}),
        "process_slugs": sorted({proc["slug"] for proc in processes.values()}),
    }

    evidence_packets = evidence_packets or {}
    anti_patterns = anti_patterns or {}
    constellation_entries: Dict[str, Dict[str, Any]] = {}
    for record in constellations or []:
        cid = record.get("id")
        if not cid:
            continue
        slug = record.get("slug") or _slugify(cid)
        constellation_entries[cid] = {
            "slug": slug,
            "components": record.get("components", []),
            "sir_files": record.get("sir_files", []),
            "graph_file": record.get("graph_file"),
            "entry_points": record.get("entry_points", []),
            "score": record.get("score"),
            "evidence_packet": evidence_packets.get(cid),
            "anti_pattern_count": len(anti_patterns.get(cid, [])),
            "anti_patterns": anti_patterns.get(cid, []),
        }

    quality_block = {
        "anti_patterns_file": anti_patterns_file,
        "constellation_counts": {cid: len(entries) for cid, entries in anti_patterns.items()},
    }

    context = {
        "components": {
            name: {
                **data,
                "families": sorted(data.get("families") or []),
                "sir_files": sorted(data["sir_files"]),
                "process_slugs": sorted(data.get("process_slugs") or []),
                "family_slugs": sorted(data.get("family_slugs") or []),
            }
            for name, data in components.items()
        },
        "families": {
            name: {
                **data,
                "components": sorted(data["components"]),
                "sir_files": sorted(set(data["sir_files"])),
                "diagram_paths": sorted(set(data["diagram_paths"])),
                "process_slugs": sorted(data.get("process_slugs") or []),
                "component_slugs": sorted(data.get("component_slugs") or []),
            }
            for name, data in families.items()
        },
        "processes": {
            key: {
                **proc,
                "diagram_paths": sorted(proc.get("diagram_paths") or []),
            }
            for key, proc in processes.items()
        },
        "repo": repo_entry,
        "facets": facets,
        "constellations": constellation_entries,
        "quality": quality_block,
        "interdeps_path": "signals/interdeps.json",
        "graph_path": "signals/graph.json",
        "artifacts_file": "artifacts/artifacts.json",
    }
    return context


def _copy_path(src: Path, dst: Path) -> None:
    if not src.exists():
        return
    if src.is_dir():
        import shutil as _shutil
        _shutil.copytree(src, dst, dirs_exist_ok=True)
    else:
        dst.parent.mkdir(parents=True, exist_ok=True)
        import shutil as _shutil
        _shutil.copy2(src, dst)


def _move_path(src: Path, dst: Path) -> None:
    """
    Move a file/dir if it exists, replacing any existing target.
    """
    if not src.exists():
        return
    import shutil as _shutil
    dst.parent.mkdir(parents=True, exist_ok=True)
    if dst.exists():
        if dst.is_dir():
            _shutil.rmtree(dst, ignore_errors=True)
        else:
            dst.unlink(missing_ok=True)
    _shutil.move(str(src), str(dst))


def _apply_out_layout(out_base: Path) -> None:
    out_base = Path(out_base)
    docs_dir = out_base / "docs"
    _move_path(out_base / "README_LAYOUT.md", docs_dir / "README_LAYOUT.md")
    _move_path(docs_dir / "dox_draft_plan.md", docs_dir / "plan" / "dox_draft_plan.md")

    signals_dir = out_base / "signals"
    legacy_sir = out_base / "sir_v2"
    # Clean up any legacy SIR outputs at the root; prefer signals/ structure
    if legacy_sir.exists():
        legacy_interdeps = legacy_sir / "_interdeps.json"
        target = signals_dir / "sir_v2"
        if not target.exists():
            _move_path(legacy_sir, target)
        else:
            import shutil as _shutil
            _shutil.rmtree(legacy_sir, ignore_errors=True)
        if legacy_interdeps.exists():
            _move_path(legacy_interdeps, signals_dir / "interdeps.json")

    _move_path(out_base / "sir", signals_dir / "sir_v1")
    _move_path(out_base / "graph.json", signals_dir / "graph.json")
    _move_path(out_base / "doc_context.json", signals_dir / "doc_context.json")
    _move_path(out_base / "rollup", signals_dir / "rollup")

    artifacts_dir = out_base / "artifacts"
    _move_path(out_base / "artifacts.json", artifacts_dir / "artifacts.json")
    _move_path(out_base / "artifacts.jsonl", artifacts_dir / "artifacts.jsonl")

    diagrams_dir = out_base / "diagrams"
    _move_path(out_base / "flows", diagrams_dir / "flows_json")

    manifests_dir = out_base / "manifests"
    for name in ("scan_manifest.json", "packaging_manifest.json", "assignment_manifest.json"):
        _move_path(out_base / name, manifests_dir / name)
    _move_path(out_base / "coverage.json", manifests_dir / "coverage.json")
    _move_path(out_base / "scaffold_coverage.csv", manifests_dir / "scaffold_coverage.csv")
    _move_path(out_base / "scaffold_coverage.json", manifests_dir / "scaffold_coverage.json")
    for p in out_base.glob("file_classifier*.jsonl"):
        _move_path(p, manifests_dir / p.name)

    reports_dir = out_base / "reports"
    _move_path(out_base / "quality", reports_dir / "quality")
    _move_path(out_base / "metrics", reports_dir / "metrics")
    _move_path(out_base / "changelog.json", reports_dir / "changelog.json")
    _move_path(out_base / "component_changes.json", reports_dir / "component_changes.json")

    fixtures_dir = out_base / "fixtures"
    _move_path(out_base / "bw-golden", fixtures_dir / "bw-golden")

    layout_readme = docs_dir / "README_LAYOUT.md"
    if not layout_readme.exists():
        layout_readme.write_text(
            "\n".join(
                [
                    "# Out Directory Layout",
                    "",
                    "docs/         - unified markdown (component parents + children) and plan/",
                    "signals/      - sir_v2, interdeps.json, graph.json, doc_context, rollup",
                    "artifacts/    - artifacts.json + artifacts.jsonl",
                    "evidence/     - evidence packets/index",
                    "diagrams/     - flows_json (graph JSON) + deterministic_svg + llm_svg",
                    "manifests/    - scan/packaging/assignment manifests, coverage reports, file_classifier*.jsonl",
                    "reports/      - quality findings, metrics, changelog/component_changes",
                    "fixtures/     - golden baselines (e.g., bw-golden)",
                    "site/         - built MkDocs portal (when enabled)",
                    "logs/, tmp/   - runtime logs and scratch space",
                ]
            )
            + "\n",
            encoding="utf-8",
        )


if __name__ == "__main__":
    main()
