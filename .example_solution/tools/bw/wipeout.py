#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
wipeout.py — delete all files and folders under ./out/ safely,
EXCEPT for specific preserved directories (which are kept but emptied).
"""

import os
import shutil
from pathlib import Path

# Directories to preserve (keep folder, delete contents)
PRESERVE_DIRS = [
    Path("docs/assets/graphs"),
    Path("logs"),
    Path("sir"),
    Path("tmp/archives"),
]

def clear_directory_contents(dir_path: Path, deleted: int) -> int:
    """Delete all files and subfolders inside a directory, but keep the directory itself."""
    for item in dir_path.iterdir():
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
    return deleted

def wipeout(out_dir: Path):
    if not out_dir.exists():
        print(f"[INFO] No out/ directory found at {out_dir}")
        return

    deleted = 0
    preserve_paths = {out_dir / p for p in PRESERVE_DIRS}

    for item in out_dir.iterdir():
        if item.is_file():
            # Delete files directly under ./out
            try:
                item.unlink()
                print(f"[DELETE] {item}")
                deleted += 1
            except Exception as e:
                print(f"[ERROR] Could not delete {item}: {e}")
        elif item.is_dir():
            if item in preserve_paths:
                # Keep folder, clear its contents
                deleted = clear_directory_contents(item, deleted)
            else:
                # Remove entire directory
                try:
                    shutil.rmtree(item)
                    print(f"[RMDIR] {item}")
                except Exception as e:
                    print(f"[ERROR] Could not remove {item}: {e}")

    print(f"[INFO] Wipeout complete. Deleted {deleted} files. Preserved directories emptied.")

def main():
    repo_root = Path(__file__).resolve().parents[3]
    out_dir = repo_root / "out"
    wipeout(out_dir)

if __name__ == "__main__":
    main()
