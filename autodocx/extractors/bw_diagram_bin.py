from __future__ import annotations

from pathlib import Path
from typing import Iterable

from autodocx.types import Signal


class BwDiagramBinaryExtractor:
    name = "bw_diagram_binary"
    patterns = ["**/*.bwd"]

    def detect(self, repo: Path) -> bool:
        repo = Path(repo)
        return any(repo.glob("**/*.bwd"))

    def discover(self, repo: Path) -> Iterable[Path]:
        repo = Path(repo)
        yield from repo.glob("**/*.bwd")

    def extract(self, path: Path) -> Iterable[Signal]:
        path = Path(path)
        props = {
            "name": path.stem,
            "file": str(path),
            "kind": "bw_diagram",
            "binary_diagram": True,
        }
        evidence = [f"{path}:1-1"]
        yield Signal(kind="diagram", props=props, evidence=evidence, subscores={"parsed": 0.2})
