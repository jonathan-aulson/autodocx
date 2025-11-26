from __future__ import annotations
from pathlib import Path
from typing import Iterable, List
from autodocx.types import Signal

class MarkdownDocsExtractor:
    name = "markdown_docs"
    patterns = ["**/*.md", "**/*.markdown"]

    def detect(self, repo: Path) -> bool:
        return any(repo.glob("**/*.md"))

    def discover(self, repo: Path) -> Iterable[Path]:
        for pat in self.patterns:
            yield from repo.glob(pat)

    def extract(self, path: Path) -> Iterable[Signal]:
        title = path.stem.upper()
        kind = "doc"
        if path.name.lower() in ["readme.md", "readme"]:
            title = "README"
        if "adr" in [p.lower() for p in path.parts] or path.name.lower().startswith("adr"):
            title = f"ADR: {path.stem}"
        return [Signal(kind=kind, props={"name": title, "file": str(path)}, evidence=[f"{path}:1-10"], subscores={"parsed": 0.7, "doc_alignment": 0.5})]
