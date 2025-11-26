# autodocx/extractors/myplugin.py
# Example custom extractor plugin
#--------------------------------
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable, List
from autodocx.types import Signal, Subscores
from autodocx.utils.redaction import redact

class MyExtractor:
    name = "my_extractor"
    api_version = "1.0"
    plugin_version = "0.1.0"
    patterns = ["**/*.myfmt"]

    def detect(self, repo: Path) -> bool:
        return any(repo.glob(self.patterns[0]))

    def discover(self, repo: Path) -> Iterable[Path]:
        for pat in self.patterns:
            yield from repo.glob(pat)

    def extract(self, path: Path) -> Iterable[Signal]:
        text = path.read_text(encoding="utf-8", errors="ignore")
        # parse...
        return [Signal(kind="api", props={"name": path.stem, "file": str(path)}, evidence=[f"{path}:1-10"], subscores={"parsed": 1.0})]
