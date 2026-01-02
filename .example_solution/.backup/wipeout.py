#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
wipeout.py — delete all files (not folders) under ./out/ safely,
and fully clear ./out/tmp/archives (files + subfolders).
"""

import os
import sys
import shutil
from pathlib import Path

def wipeout(out_dir: Path):
    if not out_dir.exists():
        print(f"[INFO] No out/ directory found at {out_dir}")
        return

    deleted = 0
    # Delete all files under out/ (but not folders)
    for root, dirs, files in os.walk(out_dir):
        for f in files:
            file_path = Path(root) / f
            try:
                file_path.unlink()
                print(f"[DELETE] {file_path}")
                deleted += 1
            except Exception as e:
                print(f"[ERROR] Could not delete {file_path}: {e}")

    # Special handling: clear out/tmp/archives completely (files + subfolders)
    archives_dir = out_dir / "tmp" / "archives"
    if archives_dir.exists() and archives_dir.is_dir():
        for item in archives_dir.iterdir():
            try:
                if item.is_file():
                    item.unlink()
                    print(f"[DELETE] {item}")
                    deleted += 1
                elif item.is_dir():
                    shutil.rmtree(item)
                    print(f"[RMDIR] {item}")
            except Exception as e:
                print(f"[ERROR] Could not remove {item}: {e}")

    print(f"[INFO] Wipeout complete. Deleted {deleted} files. Cleared subfolders in out/tmp/archives.")

def main():
    repo_root = Path(__file__).resolve().parents[3]
    out_dir = repo_root / "out"
    wipeout(out_dir)

if __name__ == "__main__":
    main()
