#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
set_homepage.py — Utility to set any generated .md file as the MkDocs homepage.

Usage:
    python .roo/tools/bw/set_homepage.py out/docs/<file>.md
"""

import sys
import shutil
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
    docs_dir = repo_root / "out" / "docs"
    docs_dir.mkdir(exist_ok=True)

    # Copy chosen file to docs/index.md
    index_md = docs_dir / "index.md"
    shutil.copyfile(chosen_file, index_md)
    print(f"[INFO] Set {chosen_file} as homepage (copied to out/docs/index.md)")

    # Do NOT modify mkdocs.yml anymore
    print("[INFO] mkdocs.yml left unchanged. Ensure 'index.md' is referenced in your nav if needed.")
    print("[NEXT] Run `mkdocs serve` or `mkdocs gh-deploy` to rebuild the site.")

if __name__ == "__main__":
    main()