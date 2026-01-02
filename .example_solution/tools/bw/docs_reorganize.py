#!/usr/bin/env python3
# .roo/tools/bw/docs_reorganize.py
# Reorganize out/docs/*.md into subfolders.
# - Domain-prefixed files (creditapp.module.EquifaxScore.md) -> out/docs/creditapp.module/<same>.md
# - Family_* files (Family_com.tibco... .md or Family com.tibco... .md) -> out/docs/<family>/<same>.md
# Exclusions: repo-overview.md, index.md, BUSINESS_* stay at root.

import sys, argparse, shutil
from pathlib import Path

HERE = Path(__file__).resolve()
REPO_ROOT = HERE.parents[3]                 # <repo>/  (bw → tools → .roo → repo)
OUT_DIR   = REPO_ROOT / "out"
DOCS_DIR  = OUT_DIR / "docs"

# Leave BUSINESS_* at root (unchanged)
EXCLUDE_PREFIXES = ("BUSINESS_",)
EXCLUDE_EXACT = {"repo-overview.md", "REPO_OVERVIEW.md", "index.md"}  # leave as-is per your version

FAMILY_PREFIXES = ("Family_", "Family ")

def _same_content(a: Path, b: Path) -> bool:
    try:
        return a.read_text(encoding="utf-8", errors="ignore") == b.read_text(encoding="utf-8", errors="ignore")
    except Exception:
        return False

def _move(md: Path, dest: Path) -> None:
    if dest.resolve() == md.resolve():
        return
    dest.parent.mkdir(parents=True, exist_ok=True)
    if dest.exists() and _same_content(dest, md):
        md.unlink(missing_ok=True)
    else:
        shutil.move(str(md), str(dest))

def move_family_if_needed(md: Path) -> bool:
    """If filename is Family_* or Family <name>.md, move to out/docs/<name>/."""
    name = md.name
    lower = name  # case-sensitive match is fine; Family_* is how we emit
    if not name.lower().endswith(".md"):
        return False

    for pref in FAMILY_PREFIXES:
        if name.startswith(pref):
            family = name[len(pref):-3]  # strip prefix and '.md'
            if not family:
                return False
            target_dir = DOCS_DIR / family
            dest = target_dir / name
            # Instead of moving, copy so original Family_*.md remains in out/docs/
            try:
                shutil.copy2(md, dest)
            except Exception:
                pass
            _move(md, dest)
            return True
    return False

def move_if_domain_prefixed(md: Path):
    """Move file into subfolder named by its domain prefix (filename up to last dot)."""
    name = md.name
    if name in EXCLUDE_EXACT or name.startswith(EXCLUDE_PREFIXES):
        return

    # Special-case Family_* first (now we actively move these)
    if move_family_if_needed(md):
        return

    if "." not in name:
        return

    # domain prefix = filename (without extension) up to last dot
    stem = md.stem  # without .md
    last_dot = stem.rfind(".")
    if last_dot <= 0:
        return

    domain = stem[:last_dot]  # e.g., "creditapp.module"
    target_dir = DOCS_DIR / domain
    dest = target_dir / name
    _move(md, dest)

def main():
    ap = argparse.ArgumentParser(description="Reorganize out/docs into domain subfolders.")
    _ = ap.parse_args()

    DOCS_DIR.mkdir(parents=True, exist_ok=True)
    for md in DOCS_DIR.rglob("*.md"):
        # Only operate on files directly under out/docs (not already in a subfolder)
        if md.parent == DOCS_DIR:
            move_if_domain_prefixed(md)

    print("[reorg] Completed domain-based reorganization under out/docs/")
    return 0

if __name__ == "__main__":
    sys.exit(main())
