from __future__ import annotations

import json
from pathlib import Path
from typing import Iterable, List

from autodocx.types import Signal


class BwTestSuiteExtractor:
    name = "bw_test_suite"
    patterns = ["**/*.bwt", "**/*.ml"]

    def detect(self, repo: Path) -> bool:
        repo = Path(repo)
        return any(repo.glob("**/*.bwt")) or any(repo.glob("**/*.ml"))

    def discover(self, repo: Path) -> Iterable[Path]:
        repo = Path(repo)
        for pat in self.patterns:
            yield from repo.glob(pat)

    def extract(self, path: Path) -> Iterable[Signal]:
        path = Path(path)
        name = path.stem
        props = {
            "name": name,
            "file": str(path),
            "kind": "bw_test",
        }
        # Try to parse minimal JSON if present (some .ml files are JSON-like)
        content = path.read_text(encoding="utf-8", errors="ignore")
        try:
            data = json.loads(content)
            props["inputs"] = data.get("inputs") or data.get("request") or data.get("payload")
            props["expected"] = data.get("expected") or data.get("response")
        except Exception:
            pass
        evidence = [f"{path}:1-1"]
        props["enrichment"] = {"bw_invocations": [{"connector": "test", "target": props.get("name"), "evidence": evidence[0]}]}
        yield Signal(kind="test", props=props, evidence=evidence, subscores={"parsed": 0.6})
