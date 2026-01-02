#!/usr/bin/env python3
# tools/wipeout.py
# Safely delete files under ./out while keeping the directory structure.

import argparse, sys
from pathlib import Path

HERE = Path(__file__).resolve()
REPO_ROOT = HERE.parents[1]
OUT = REPO_ROOT / "out"

def safe():
    # Safety guard: only operate on "<repo>/out"
    if OUT.name != "out" or OUT.parent != REPO_ROOT:
        print("[wipeout] Safety check failed; refusing to run.")
        sys.exit(2)

def wipe_files_only(root: Path, dry_run: bool):
    if not root.exists():
        print("[wipeout] Nothing to delete; out/ does not exist.")
        return
    count = 0
    for p in root.rglob("*"):
        if p.is_file():
            count += 1
            if dry_run:
                print(f"[DRY] delete {p}")
            else:
                try:
                    p.unlink()
                except Exception as e:
                    print(f"[wipeout][WARN] Could not delete {p}: {e}")
    print(f"[wipeout] Files deleted: {count}")

def wipe_all_contents(root: Path, dry_run: bool):
    # remove files and empty dirs, keep top-level out/
    if not root.exists():
        print("[wipeout] Nothing to delete; out/ does not exist.")
        return
    # delete files first
    wipe_files_only(root, dry_run=dry_run)
    # then prune empty dirs (bottom-up)
    for p in sorted(root.rglob("*"), key=lambda x: len(x.parts), reverse=True):
        if p.is_dir():
            try:
                if dry_run:
                    print(f"[DRY] rmdir  {p}")
                else:
                    p.rmdir()
            except OSError:
                # not empty, ignore
                pass
    print("[wipeout] Directory cleanup complete (kept out/).")

def main():
    ap = argparse.ArgumentParser(description="Delete files from ./out safely.")
    ap.add_argument("--mode", choices=["files", "all"], default="files",
                    help="'files' = delete files only; 'all' = delete files then empty dirs (keep out/).")
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()

    safe()
    OUT.mkdir(parents=True, exist_ok=True)

    if args.mode == "files":
        wipe_files_only(OUT, dry_run=args.dry_run)
    else:
        wipe_all_contents(OUT, dry_run=args.dry_run)

if __name__ == "__main__":
    sys.exit(main())
