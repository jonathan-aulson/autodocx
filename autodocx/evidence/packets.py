from __future__ import annotations

import json
import re
import time
from pathlib import Path
from typing import Any, Dict, Iterable, List, Sequence, Tuple

MAX_SNIPPET_LINES = 80
EVIDENCE_PATTERN = re.compile(r"^(?P<path>.+?)(?::(?P<start>\d+)(?:-(?P<end>\d+))?)?$")


def _slugify(value: str) -> str:
    return re.sub(r"[^a-z0-9]+", "-", (value or "").lower()).strip("-") or "packet"


def _rel_path(path: Path, base: Path) -> str:
    try:
        return path.relative_to(base).as_posix()
    except ValueError:
        return path.as_posix()


def _resolve_path(repo_root: Path, candidate: str | None) -> Path | None:
    if not candidate:
        return None
    path = Path(candidate)
    if not path.is_absolute():
        path = repo_root / path
    if path.exists():
        return path
    return None


def _load_lines(path: Path) -> List[str]:
    try:
        return path.read_text(encoding="utf-8").splitlines()
    except Exception:
        return []


def _clamp_line_range(total: int, start: int, end: int) -> Tuple[int, int]:
    start = max(1, start)
    end = max(start, end)
    if end - start > MAX_SNIPPET_LINES:
        end = start + MAX_SNIPPET_LINES
    if end > total:
        end = total
    return start, end


def _slice_lines(lines: List[str], start: int, end: int) -> str:
    if not lines:
        return ""
    start_idx = max(0, start - 1)
    end_idx = min(len(lines), end)
    return "\n".join(lines[start_idx:end_idx])


def _extract_snippet_from_evidence(
    repo_root: Path,
    entry: str,
) -> Dict[str, Any] | None:
    match = EVIDENCE_PATTERN.match(entry.strip())
    if not match:
        return None
    src_path = _resolve_path(repo_root, match.group("path"))
    if not src_path:
        return None
    lines = _load_lines(src_path)
    if not lines:
        return None
    start = int(match.group("start") or 1)
    end = int(match.group("end") or start + MAX_SNIPPET_LINES)
    start, end = _clamp_line_range(len(lines), start, end)
    snippet = _slice_lines(lines, start, end)
    if not snippet.strip():
        return None
    return {
        "path": _rel_path(src_path, repo_root),
        "start_line": start,
        "end_line": end,
        "text": snippet,
    }


def _fallback_snippet(repo_root: Path, props: Dict[str, Any]) -> Dict[str, Any] | None:
    file_hint = props.get("file")
    candidate = _resolve_path(repo_root, file_hint)
    if not candidate:
        return None
    lines = _load_lines(candidate)
    if not lines:
        return None
    end_line = min(len(lines), MAX_SNIPPET_LINES)
    text = _slice_lines(lines, 1, end_line)
    if not text.strip():
        return None
    return {
        "path": _rel_path(candidate, repo_root),
        "start_line": 1,
        "end_line": end_line,
        "text": text,
    }


def _collect_snippets_for_sir(repo_root: Path, sir_obj: Dict[str, Any]) -> List[Dict[str, Any]]:
    snippets: List[Dict[str, Any]] = []
    for entry in sir_obj.get("evidence") or []:
        snippet = _extract_snippet_from_evidence(repo_root, entry)
        if snippet:
            snippets.append(snippet)
        if len(snippets) >= 4:
            break
    if not snippets:
        props = sir_obj.get("props") or {}
        fallback = _fallback_snippet(repo_root, props)
        if fallback:
            snippets.append(fallback)
    return snippets


def build_evidence_packets(
    out_base: Path,
    repo_root: Path,
    constellations: Sequence[Dict[str, Any]],
    sir_records: Sequence[Tuple[Dict[str, Any], Path]],
    anti_patterns_by_constellation: Dict[str, List[Dict[str, Any]]],
) -> Dict[str, str]:
    out_base = Path(out_base)
    repo_root = Path(repo_root)
    packet_dir = out_base / "evidence" / "constellations"
    packet_dir.mkdir(parents=True, exist_ok=True)

    sir_lookup: Dict[str, Dict[str, Any]] = {}
    for sir_obj, sir_path in sir_records:
        rel = _rel_path(sir_path, out_base)
        sir_lookup[rel] = sir_obj

    packet_index: Dict[str, str] = {}
    for record in constellations:
        slug = record.get("slug") or _slugify(record["id"])
        packet_path = packet_dir / f"{slug}.json"
        snippets: List[Dict[str, Any]] = []
        for sir_file in record.get("sir_files", []):
            sir_obj = sir_lookup.get(sir_file)
            if not sir_obj:
                continue
            for snippet in _collect_snippets_for_sir(repo_root, sir_obj):
                snippet["sir_id"] = sir_obj.get("id")
                snippet["sir_name"] = sir_obj.get("name")
                snippet["source_file"] = sir_file
                snippets.append(snippet)
                if len(snippets) >= 50:
                    break
            if len(snippets) >= 50:
                break

        packet = {
            "constellation_id": record["id"],
            "slug": slug,
            "components": record.get("components", []),
            "summary": {
                "score": record.get("score"),
                "node_count": record.get("node_count"),
                "edge_count": record.get("edge_count"),
            },
            "entry_points": record.get("entry_points", []),
            "sir_files": record.get("sir_files", []),
            "snippets": snippets,
            "anti_patterns": anti_patterns_by_constellation.get(record["id"], []),
            "generated_at": time.time(),
        }
        packet_path.write_text(json.dumps(packet, indent=2), encoding="utf-8")
        packet_index[record["id"]] = packet_path.relative_to(out_base).as_posix()
    return packet_index
