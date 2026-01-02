#!/usr/bin/env python3
# tools/mkdocs_bootstrap.py
# Write mkdocs.yml EXACTLY as requested at repo root and ensure homepage.

import sys, argparse
from pathlib import Path

HERE = Path(__file__).resolve()
REPO_ROOT = HERE.parents[3]                 # <repo>/
OUT_DIR   = REPO_ROOT / "out"
DOCS_DIR  = OUT_DIR / "docs"
MKDOCS_YML = REPO_ROOT / "mkdocs.yml"

EXACT_CFG = """site_name: Concentra BW Docs
site_description: Auto-generated business docs for TIBCO BW services
site_url: https://jonathan-aulson.github.io/Concentra-Tibco-Context/
docs_dir: out/docs
use_directory_urls: true
theme:
  name: material
  features:
  - navigation.instant
plugins:
- search
- privacy
markdown_extensions:
- attr_list
- admonition
- tables
- attr_list
- md_in_html
- footnotes
- pymdownx.details
- pymdownx.tabbed
- pymdownx.superfences
- pymdownx.snippets
- pymdownx.emoji:
    emoji_index: !!python/name:material.extensions.emoji.twemoji
    emoji_generator: !!python/name:material.extensions.emoji.to_svg
- toc:
    permalink: true
"""

def write_text(p: Path, text: str) -> None:
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(text, encoding="utf-8")

def ensure_exact_mkdocs_config(force: bool) -> None:
    """
    Always write mkdocs.yml EXACTLY as specified when --force-config is set,
    otherwise create it only if missing. Location: repo root.
    """
    if force or not MKDOCS_YML.exists():
        write_text(MKDOCS_YML, EXACT_CFG)

def ensure_homepage(force_home: bool) -> None:
    """
    Promote repo-overview.md (if present) to index.md under out/docs.
    If not present, create a small placeholder.
    """
    DOCS_DIR.mkdir(parents=True, exist_ok=True)
    repo_overview = DOCS_DIR / "repo-overview.md"
    index_md = DOCS_DIR / "index.md"
    if repo_overview.exists():
        if force_home or not index_md.exists():
            write_text(index_md, repo_overview.read_text(encoding="utf-8"))
    else:
        if force_home or not index_md.exists():
            write_text(index_md, "# Repository Overview\n\n_This homepage will be replaced after docs generation._\n")

def main():
    ap = argparse.ArgumentParser(description="Bootstrap/repair MkDocs for out/docs.")
    ap.add_argument("--force-config", action="store_true",
                    help="Overwrite mkdocs.yml at the repo root with the exact required content.")
    ap.add_argument("--force-home", action="store_true",
                    help="Overwrite out/docs/index.md from repo-overview.md if present, else create a placeholder.")
    args = ap.parse_args()

    ensure_exact_mkdocs_config(force=args.force_config)
    ensure_homepage(force_home=args.force_home)

    print(f"[mkdocs] mkdocs.yml written at: {MKDOCS_YML}")
    print(f"[mkdocs] docs_dir: {DOCS_DIR}")
    print("[mkdocs] Homepage ready (index.md).")

if __name__ == "__main__":
    sys.exit(main())
