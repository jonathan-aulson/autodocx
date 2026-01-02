from __future__ import annotations

import re
from pathlib import Path
from typing import Any, Dict, List, Sequence

EVIDENCE_PATTERN = re.compile(r"^(?P<path>.+?)(?::(?P<start>\d+)(?:-(?P<end>\d+))?)?$")


def _rel_path(repo_root: Path, candidate: Path) -> str:
    try:
        return candidate.relative_to(repo_root).as_posix()
    except (ValueError, AttributeError):
        try:
            return candidate.as_posix()
        except AttributeError:
            return "/".join(candidate.parts)


def _parse_entry(repo_root: Path, entry: str) -> Dict[str, int | str] | None:
    match = EVIDENCE_PATTERN.match(entry.strip())
    if not match:
        return None
    path_text = match.group("path")
    if not path_text:
        return None
    candidate = Path(path_text)
    if not candidate.is_absolute():
        candidate = repo_root / candidate
    start = int(match.group("start") or 1)
    end = int(match.group("end") or start)
    return {
        "path": _rel_path(repo_root, candidate),
        "start_line": start,
        "end_line": end,
    }


def build_provenance_entries(repo_root: Path, evidence: Sequence[Any], fallback_file: str | None = None) -> List[Dict[str, int | str]]:
    repo_root = Path(repo_root)
    entries: List[Dict[str, int | str]] = []
    for entry in evidence or []:
        normalized = _normalize_evidence_entry(entry)
        if not normalized:
            continue
        parsed = _parse_entry(repo_root, normalized)
        if parsed:
            entries.append(parsed)
    if not entries and fallback_file:
        candidate = Path(fallback_file)
        if not candidate.is_absolute():
            candidate = repo_root / candidate
        entries.append(
            {
                "path": _rel_path(repo_root, candidate),
                "start_line": 1,
                "end_line": 1,
            }
        )
    return entries


def _normalize_evidence_entry(entry: Any) -> str | None:
    if isinstance(entry, str):
        return entry
    if isinstance(entry, dict):
        path = entry.get("path") or entry.get("file")
        if not path:
            return None
        lines = entry.get("lines") or entry.get("line") or entry.get("span")
        if isinstance(lines, str) and lines:
            return f"{path}:{lines}"
        return str(path)
    if entry is None:
        return None
    return str(entry)
