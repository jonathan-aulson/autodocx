from __future__ import annotations

import os
from pathlib import Path
from typing import Optional

from dotenv import load_dotenv

_ENV_LOADED = False
_ENV_PATH: Optional[Path] = None


def _resolve_env_path(env_file: Optional[str] = None) -> Optional[Path]:
    candidates = []
    if env_file:
        candidates.append(Path(env_file))
    if os.getenv("AUTODOCX_ENV_FILE"):
        candidates.append(Path(os.getenv("AUTODOCX_ENV_FILE", "")))
    # project root relative to this module (../.. from autodocx/utils)
    candidates.append(Path(__file__).resolve().parents[2] / ".env")
    # cwd fallback
    candidates.append(Path.cwd() / ".env")
    for candidate in candidates:
        if candidate and not candidate.is_absolute():
            candidate = candidate.resolve()
        if candidate.exists():
            return candidate
    return None


def load_project_dotenv(env_file: Optional[str] = None, *, override: bool = False) -> Optional[Path]:
    """
    Ensure .env is loaded exactly once so CLI + scripts share the same configuration-driven environment.
    Returns the path that was loaded (if any).
    """
    global _ENV_LOADED, _ENV_PATH
    if _ENV_LOADED and not override:
        return _ENV_PATH
    path = _resolve_env_path(env_file)
    if not path:
        return None
    load_dotenv(dotenv_path=path, override=override)
    _ENV_LOADED = True
    _ENV_PATH = path
    return path
