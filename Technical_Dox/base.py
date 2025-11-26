from __future__ import annotations
from pathlib import Path
from typing import Protocol, Iterable, List
from autodocx.types import Signal

class Extractor(Protocol):
    name: str
    patterns: List[str]
    def detect(self, repo: Path) -> bool: ...
    def discover(self, repo: Path) -> Iterable[Path]: ...
    def extract(self, path: Path) -> Iterable[Signal]: ...
