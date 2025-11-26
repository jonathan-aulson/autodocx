from __future__ import annotations

import re
from pathlib import Path
from typing import Iterable, List

from autodocx.types import Signal


class ProcessDiagramExtractor:
    name = "process_diagrams"
    patterns = ["**/*.bpmn", "**/*.drawio", "**/*.drawio.xml"]

    BPMN_NAME_RE = re.compile(r'<[\w:]*process[^>]*name="(?P<name>[^"]+)"', re.IGNORECASE)

    def detect(self, repo: Path) -> bool:
        return any(repo.glob("**/*.bpmn")) or any(repo.glob("**/*.drawio")) or any(repo.glob("**/*.drawio.xml"))

    def discover(self, repo: Path) -> Iterable[Path]:
        for pattern in self.patterns:
            yield from repo.glob(pattern)

    def extract(self, path: Path) -> Iterable[Signal]:
        try:
            text = path.read_text(encoding="utf-8", errors="ignore")
        except Exception:
            return []

        name = self._infer_name(path, text)
        return [
            Signal(
                kind="process_diagram",
                props={"name": name, "file": str(path)},
                evidence=[f"{path}:diagram"],
                subscores={"parsed": 0.5},
            )
        ]

    def _infer_name(self, path: Path, text: str) -> str:
        match = self.BPMN_NAME_RE.search(text)
        if match:
            return match.group("name")
        return path.stem
