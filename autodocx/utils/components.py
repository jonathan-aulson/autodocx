from __future__ import annotations

from pathlib import Path
from typing import Dict, Optional

_IGNORE_PARTS = {
    "",
    ".",
    "..",
    "__pycache__",
    "src",
    "source",
    "app",
    "apps",
    "services",
    "service",
    "modules",
    "module",
    "lib",
    "libs",
    "code",
    "dist",
    "build",
    "bin",
    "config",
}


def _candidate_from_parts(parts: tuple[str, ...]) -> Optional[str]:
    """
    Choose a stable component name from the provided path parts.
    Prefers the first directory outside of ignored tokens; falls back to the final directory.
    """
    filtered = []
    for p in parts:
        if not p:
            continue
        low = p.lower()
        if low in _IGNORE_PARTS:
            continue
        if "." in p:
            # likely a filename with extension; skip when searching for component dirs
            continue
        filtered.append(p)
    if not filtered:
        return None
    # Prefer the first element; if it still looks generic, fall back to the next one
    first = filtered[0]
    if first.lower() in {"src", "app", "apps", "services", "service"} and len(filtered) > 1:
        return filtered[1]
    return first


def normalize_component_name(raw: str) -> str:
    """
    Produce a filesystem-friendly, stable component identifier.
    """
    safe_chars: list[str] = []
    for ch in raw.strip():
        if ch.isalnum() or ch in {"-", "_"}:
            safe_chars.append(ch)
        else:
            safe_chars.append("_")
    normalized = "".join(safe_chars)
    normalized = normalized.strip("_-")
    if not normalized:
        return "component"
    return normalized


def derive_component_from_path(
    repo_root: Path,
    file_path: str,
    *,
    explicit: Optional[str] = None,
    hint: Optional[str] = None,
) -> str:
    """
    Derive a component identifier using (in priority order):
      1. Explicit value supplied by extractor (explicit / hint).
      2. Repo-relative path segments.
      3. Repository folder name.
    """
    if explicit:
        return normalize_component_name(explicit)
    if hint:
        return normalize_component_name(hint)

    if not file_path:
        return normalize_component_name(repo_root.name)

    path = Path(file_path)
    try:
        rel = path.resolve().relative_to(repo_root.resolve())
    except Exception:
        # Attempt fallback by walking up until we can relativize
        rel = path
        for parent in path.parents:
            try:
                rel = path.relative_to(parent)
                break
            except Exception:
                continue

    candidate = _candidate_from_parts(rel.parts[:-1]) or _candidate_from_parts(rel.parts)
    if not candidate:
        candidate = repo_root.name
    return normalize_component_name(candidate)


def derive_component(
    repo_root: Path,
    props: Dict[str, object],
    *,
    default_hint: Optional[str] = None,
) -> str:
    """
    High-level helper for callers holding a signal props dict.
    Respects existing component/service values but normalizes them.
    """
    explicit = None
    hint = None
    if isinstance(props, dict):
        explicit = props.get("component_or_service") or props.get("service")
        hint = props.get("component") or props.get("module") or props.get("app")
        file_path = props.get("file")
    else:
        file_path = None
    if not explicit and default_hint:
        hint = hint or default_hint
    return derive_component_from_path(
        repo_root,
        str(file_path or ""),
        explicit=str(explicit) if explicit else None,
        hint=str(hint) if hint else None,
    )
