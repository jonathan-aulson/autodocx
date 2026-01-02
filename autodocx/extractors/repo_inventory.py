from __future__ import annotations

import hashlib
import os
from pathlib import Path
from typing import Iterable, List

from autodocx.types import Signal
from autodocx.utils.scan_filters import should_skip_file

SKIP_DIRS = {
    ".git",
    ".github",
    ".hg",
    ".svn",
    ".venv",
    "node_modules",
    "out",
    "dist",
    "build",
    "__pycache__",
    ".mypy_cache",
}

CODE_EXTENSIONS = {
    ".py",
    ".ts",
    ".tsx",
    ".js",
    ".cs",
    ".java",
    ".go",
    ".rs",
    ".rb",
    ".php",
}

INFRA_EXTENSIONS = {
    ".bicep",
    ".tf",
    ".tf.json",
    ".yaml",
    ".yml",
    ".json",
}


def _sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as fh:
        for chunk in iter(lambda: fh.read(65536), b""):
            h.update(chunk)
    return h.hexdigest()


def _artifact_type(rel_parts: List[str], path: Path) -> str:
    lowered_parts = [part.lower() for part in rel_parts]
    if "test" in lowered_parts or "tests" in lowered_parts or path.name.startswith("test_"):
        return "test"
    if "infra" in lowered_parts or "infrastructure" in lowered_parts or path.suffix in {".bicep", ".tf"}:
        return "infra"
    if "config" in lowered_parts or path.name.lower() in {"config.yaml", "config.json"}:
        return "config"
    if path.suffix in CODE_EXTENSIONS:
        return "code"
    if path.suffix in INFRA_EXTENSIONS:
        return "infra"
    return "artifact"


def _language_hint(path: Path) -> str | None:
    ext = path.suffix.lower()
    mapping = {
        ".py": "python",
        ".ts": "typescript",
        ".tsx": "tsx",
        ".js": "javascript",
        ".cs": "csharp",
        ".java": "java",
        ".go": "go",
        ".rb": "ruby",
        ".php": "php",
        ".tf": "terraform",
        ".bicep": "bicep",
        ".yaml": "yaml",
        ".yml": "yaml",
        ".json": "json",
    }
    return mapping.get(ext)


def _component_hint(rel_parts: List[str]) -> str | None:
    if not rel_parts:
        return None
    return rel_parts[0]


class RepoInventoryExtractor:
    name = "repo_inventory"
    patterns = ["**/*"]

    def detect(self, repo: Path) -> bool:
        self._repo_root = Path(repo)
        return self._repo_root.exists()

    def discover(self, repo: Path) -> Iterable[Path]:
        repo = Path(repo)
        self._repo_root = repo
        for root, dirs, files in os.walk(repo):
            dirs[:] = [d for d in dirs if d not in SKIP_DIRS]
            for fname in files:
                path = Path(root) / fname
                if path.is_file() and not should_skip_file(path):
                    yield path

    def extract(self, path: Path) -> Iterable[Signal]:
        return [self._build_signal(path)]

    def _build_signal(self, path: Path) -> Signal:
        repo_root = getattr(self, "_repo_root", path)
        if not path.is_relative_to(repo_root):
            repo_root = path
        rel = path.relative_to(repo_root)
        rel_parts = list(rel.parts)
        art_type = _artifact_type(rel_parts, path)
        props = {
            "file": rel.as_posix(),
            "artifact_type": art_type,
            "language": _language_hint(path),
            "component_hint": _component_hint(rel_parts),
            "size_bytes": path.stat().st_size,
            "sha256": _sha256(path),
        }
        evidence = [f"{rel.as_posix()}:1-1"]
        return Signal(kind="repo_artifact", props=props, evidence=evidence, subscores={"parsed": 0.5})
