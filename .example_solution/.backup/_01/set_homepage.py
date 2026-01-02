#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
set_homepage.py — Utility to set any generated .md file as the MkDocs homepage.

Usage:
    python .roo/tools/bw/set_homepage.py out/docs/<file>.md
"""

import sys
import shutil
import yaml
from pathlib import Path

def main():
    if len(sys.argv) != 2:
        print("Usage: python set_homepage.py out/docs/<file>.md")
        sys.exit(1)

    chosen_file = Path(sys.argv[1])
    if not chosen_file.exists():
        print(f"[ERROR] File not found: {chosen_file}")
        sys.exit(1)

    repo_root = Path(__file__).resolve().parents[3]
    mkdocs_yml = repo_root / "mkdocs.yml"
    docs_dir = repo_root / "docs"
    docs_dir.mkdir(exist_ok=True)

    # Copy chosen file to docs/index.md
    index_md = docs_dir / "index.md"
    shutil.copyfile(chosen_file, index_md)
    print(f"[INFO] Set {chosen_file} as homepage (copied to docs/index.md)")

    # Update mkdocs.yml nav
    if mkdocs_yml.exists():
        with open(mkdocs_yml, "r", encoding="utf-8") as f:
            config = yaml.safe_load(f) or {}
    else:
        config = {}

    nav = config.get("nav", [])
    if not nav:
        nav = [{"Home": "index.md"}]
    else:
        # Replace first entry with Home -> index.md
        nav[0] = {"Home": "index.md"}
    config["nav"] = nav

    with open(mkdocs_yml, "w", encoding="utf-8") as f:
        yaml.safe_dump(config, f, sort_keys=False)

    print(f"[INFO] Updated mkdocs.yml to set homepage as {chosen_file.name}")
    print("[NEXT] Run `mkdocs serve` or `mkdocs gh-deploy` to rebuild the site.")

if __name__ == "__main__":
    main()