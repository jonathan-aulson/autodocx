#!/usr/bin/env python3
"""
Normalize the ./out layout to match README_LAYOUT.md:
  - Only mkdocs.yml is allowed at the out/ root.
  - Artifacts/docs/diagrams/manifests/reports/signals/tmp/... hold all other files.
Steps:
  1) Apply known relocations (e.g., rollup → signals/rollup, doc_context → signals/).
  2) Walk remaining root items and flag anything outside the allowed dirs.
  3) Log every move plus source-code locations that reference the moved basename.
"""
from __future__ import annotations

from datetime import datetime, timezone
import shutil
import subprocess
from pathlib import Path
from typing import Dict, Iterable, List, Tuple

REPO_ROOT = Path(__file__).resolve().parents[1]
OUT_ROOT = REPO_ROOT / "out"
LOG_PATH = OUT_ROOT / "logs" / "out_reorg.log"

ALLOWED_ROOT_DIRS = {
    "artifacts",
    "diagrams",
    "docs",
    "evidence",
    "fixtures",
    "logs",
    "manifests",
    "reports",
    "signals",
    "site",
    "tmp",
}
ALLOWED_ROOT_FILES = {"mkdocs.yml"}

# Explicit path relocations (relative to out/)
PATH_TARGETS: Dict[Path, Path] = {
    Path("artifacts.json"): Path("artifacts/artifacts.json"),
    Path("artifacts.jsonl"): Path("artifacts/artifacts.jsonl"),
    Path("doc_context.json"): Path("signals/doc_context.json"),
    Path("graph.json"): Path("signals/graph.json"),
    Path("coverage.json"): Path("manifests/coverage.json"),
    Path("scaffold_coverage.csv"): Path("manifests/scaffold_coverage.csv"),
    Path("scaffold_coverage.json"): Path("manifests/scaffold_coverage.json"),
    Path("assignment_manifest.json"): Path("manifests/assignment_manifest.json"),
    Path("packaging_manifest.json"): Path("manifests/packaging_manifest.json"),
    Path("scan_manifest.json"): Path("manifests/scan_manifest.json"),
    Path("changelog.json"): Path("reports/changelog.json"),
    Path("component_changes.json"): Path("reports/component_changes.json"),
    Path("README_LAYOUT.md"): Path("docs/README_LAYOUT.md"),
    Path("flows"): Path("diagrams/flows_json"),
    Path("assets/diagrams"): Path("diagrams/deterministic_svg"),
    Path("assets/diagrams_llm"): Path("diagrams/llm_svg"),
    Path("sir_v2"): Path("signals/sir_v2"),
    Path("sir"): Path("signals/sir_v1"),
    Path("constellations"): Path("signals/constellations"),
    Path("rollup"): Path("signals/rollup"),
    Path("docs/rollup"): Path("signals/rollup"),
    Path("quality"): Path("reports/quality"),
    Path("metrics"): Path("reports/metrics"),
    Path("evidence_index.json"): Path("evidence/evidence_index.json"),
    Path("docs/dox_draft_plan.md"): Path("docs/plan/dox_draft_plan.md"),
}

GLOB_TARGETS: List[Tuple[str, Path]] = [
    ("file_classifier*.jsonl", Path("manifests")),
]


def _log(entry: str) -> None:
    LOG_PATH.parent.mkdir(parents=True, exist_ok=True)
    with LOG_PATH.open("a", encoding="utf-8") as fh:
        fh.write(entry.rstrip() + "\n")


def _find_producers(token: str, limit: int = 10) -> List[str]:
    """
    Best-effort search for code that creates/writes the token (basename).
    """
    try:
        res = subprocess.run(
            ["rg", "-n", token, str(REPO_ROOT), "--glob", "!out/**", "--hidden"],
            check=False,
            capture_output=True,
            text=True,
        )
        lines = [ln for ln in res.stdout.splitlines() if ln.strip()]
        return lines[:limit]
    except Exception:
        return []


def _move(src: Path, dst: Path, reason: str) -> None:
    if not src.exists():
        return
    dst.parent.mkdir(parents=True, exist_ok=True)
    if dst.exists():
        if dst.is_dir():
            shutil.rmtree(dst, ignore_errors=True)
        else:
            dst.unlink(missing_ok=True)
    shutil.move(str(src), str(dst))
    producers = _find_producers(src.name)
    entry = f"MOVE {src.relative_to(OUT_ROOT)} -> {dst.relative_to(OUT_ROOT)} | reason={reason}"
    if producers:
        entry += " | producers=" + "; ".join(producers)
    _log(entry)


def apply_explicit_moves() -> None:
    for rel_src, rel_dst in PATH_TARGETS.items():
        src = OUT_ROOT / rel_src
        dst = OUT_ROOT / rel_dst
        if src.exists() and src.resolve() != dst.resolve():
            _move(src, dst, f"explicit mapping {rel_src} -> {rel_dst}")

    for pattern, rel_dst_dir in GLOB_TARGETS:
        for src in OUT_ROOT.glob(pattern):
            dst = OUT_ROOT / rel_dst_dir / src.name
            if src.resolve() != dst.resolve():
                _move(src, dst, f"glob mapping {pattern} -> {rel_dst_dir}")


def flag_root_orphans() -> None:
    """
    After explicit moves, warn about anything still living directly under out/
    that isn't allowed.
    """
    for entry in OUT_ROOT.iterdir():
        name = entry.name
        if entry.is_file():
            if name in ALLOWED_ROOT_FILES:
                continue
            _log(f"ORPHAN file at root: {entry.relative_to(OUT_ROOT)} | producers={'; '.join(_find_producers(name))}")
        elif entry.is_dir():
            if name in ALLOWED_ROOT_DIRS:
                continue
            _log(f"ORPHAN dir at root: {entry.relative_to(OUT_ROOT)} (not in allowed set)")


def remove_empty_disallowed_dirs() -> None:
    """
    Clean up empty containers that are no longer allowed at root (e.g., assets/ after moves).
    """
    for entry in list(OUT_ROOT.iterdir()):
        if entry.is_dir() and entry.name not in (ALLOWED_ROOT_DIRS | {"logs"}):
            try:
                next(entry.rglob("*"))
            except StopIteration:
                shutil.rmtree(entry, ignore_errors=True)


def main() -> None:
    if not OUT_ROOT.exists():
        raise SystemExit("out/ directory not found; run this from repo root.")

    _log(f"--- run {datetime.now(timezone.utc).isoformat()} ---")
    apply_explicit_moves()
    flag_root_orphans()
    remove_empty_disallowed_dirs()
    _log("--- end ---")


if __name__ == "__main__":
    main()
