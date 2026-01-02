from __future__ import annotations

import hashlib
import shutil
import zipfile
from pathlib import Path
from typing import Dict, List, Tuple


ARCHIVE_EXTENSIONS = (".ear", ".par", ".zip", ".war", ".sar")


def collect_scan_roots(
    repo_root: Path,
    out_base: Path,
    include_archives: bool = True,
) -> Tuple[List[Path], Dict[str, List[Dict[str, str]]]]:
    """
    Return a list of roots to scan (primary repo + extracted archives) and a manifest describing the work.
    """
    repo_root = repo_root.resolve()
    extracted_roots: List[Path] = []
    manifest: Dict[str, List[Dict[str, str]]] = {"archives": [], "warnings": []}
    if not include_archives:
        return [repo_root], manifest

    archive_tmp = out_base / "tmp" / "archives"
    archive_tmp.mkdir(parents=True, exist_ok=True)

    for ext in ARCHIVE_EXTENSIONS:
        for archive in repo_root.rglob(f"*{ext}"):
            if not zipfile.is_zipfile(archive):
                manifest["warnings"].append(f"Skipped {archive} (not a valid zip archive)")
                continue
            identifier = hashlib.sha256(str(archive.resolve()).encode("utf-8")).hexdigest()[:12]
            dest = archive_tmp / f"{archive.stem}_{identifier}"
            if not dest.exists():
                dest.mkdir(parents=True, exist_ok=True)
                try:
                    with zipfile.ZipFile(archive, "r") as zf:
                        zf.extractall(dest)
                except Exception as exc:
                    shutil.rmtree(dest, ignore_errors=True)
                    manifest["warnings"].append(f"Failed to extract {archive}: {exc}")
                    continue
            extracted_roots.append(dest)
            manifest["archives"].append(
                {
                    "archive": str(archive.resolve()),
                    "extracted_to": str(dest.resolve()),
                    "id": identifier,
                }
            )
    roots = extracted_roots + [repo_root]
    return roots, manifest
