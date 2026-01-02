#!/usr/bin/env python3
# tools/mkdocs_bootstrap.py
# Bootstrap/repair MkDocs to serve docs from out/docs and promote REPO_OVERVIEW.md to the homepage.

import sys, os, shutil, argparse
from pathlib import Path
import yaml

HERE = Path(__file__).resolve()
REPO_ROOT = HERE.parents[1]         # repo/
OUT_DIR   = REPO_ROOT / "out"
DOCS_DIR  = OUT_DIR / "docs"
MKDOCS_YML = REPO_ROOT / "mkdocs.yml"

def write_text(p: Path, text: str):
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(text, encoding="utf-8")

def load_yaml(p: Path):
    try:
        return yaml.safe_load(p.read_text(encoding="utf-8"))
    except Exception:
        return {}

def dump_yaml(data) -> str:
    return yaml.safe_dump(data, sort_keys=False)

def ensure_mkdocs_config(site_name: str, force: bool = False):
    cfg = {}
    if MKDOCS_YML.exists() and not force:
        cfg = load_yaml(MKDOCS_YML)

    # sane defaults; keep user overrides if present
    cfg.setdefault("site_name", site_name or REPO_ROOT.name)
    cfg.setdefault("docs_dir", "out/docs")     # serve content from out/docs
    cfg.setdefault("site_dir", "out/site")     # built site goes to out/site
    cfg.setdefault("theme", "mkdocs")          # keep default theme (user can change later)

    # if nav missing, let MkDocs auto-discover (index.md first)
    # (do not write nav to keep it resilient while out/docs churns)
    cfg.pop("nav", None)

    write_text(MKDOCS_YML, dump_yaml(cfg))

def ensure_homepage(force_home: bool = False):
    """Make out/docs/index.md from REPO_OVERVIEW.md (or a stub) every time."""
    DOCS_DIR.mkdir(parents=True, exist_ok=True)
    repo_overview = DOCS_DIR / "REPO_OVERVIEW.md"
    index_md = DOCS_DIR / "index.md"

    if repo_overview.exists():
        # copy as homepage (avoid symlink for Windows)
        if force_home or (not index_md.exists()):
            write_text(index_md, repo_overview.read_text(encoding="utf-8"))
    else:
        # minimal fallback if the overview hasn't been generated yet
        if force_home or (not index_md.exists()):
            write_text(index_md, "# Repository Overview\n\n_This homepage will be replaced after docs generation._\n")

def main():
    ap = argparse.ArgumentParser(description="Bootstrap/repair MkDocs for out/docs.")
    ap.add_argument("--site-name", default="")
    ap.add_argument("--force-config", action="store_true", help="Rewrite mkdocs.yml even if it exists.")
    ap.add_argument("--force-home", action="store_true", help="Overwrite index.md from REPO_OVERVIEW.md if present.")
    args = ap.parse_args()

    ensure_mkdocs_config(args.site_name, force=args.force_config)
    ensure_homepage(force_home=args.force_home)

    print(f"[mkdocs] mkdocs.yml at {MKDOCS_YML}")
    print(f"[mkdocs] docs_dir: {DOCS_DIR}")
    print("[mkdocs] Homepage ready (index.md).")

if __name__ == "__main__":
    sys.exit(main())
