from __future__ import annotations

from pathlib import Path

SKIP_FILE_NAMES = {
    ".whitesource",
    ".whitesource.config",
    ".whitesource.config.json",
    ".ds_store",
    "thumbs.db",
    "desktop.ini",
}

SKIP_FILE_PREFIXES = (".git",)
SKIP_FILE_SUFFIXES = (":Zone.Identifier",)


def should_skip_file(path: Path) -> bool:
    """
    Return True when a filesystem entry should be ignored by extractors.
    We currently drop Windows ADS metadata files (*:Zone.Identifier), git control files,
    and other non-process artifacts that only generate empty repo_artifact signals.
    """
    name = path.name
    lower = name.lower()
    if any(name.startswith(prefix) for prefix in SKIP_FILE_PREFIXES):
        return True
    if any(name.endswith(suffix) for suffix in SKIP_FILE_SUFFIXES):
        return True
    if lower in SKIP_FILE_NAMES:
        return True
    return False
