from __future__ import annotations

from pathlib import Path
from typing import Iterable, List
import json

from autodocx.types import Signal
from autodocx.extractors.logicapps import LogicAppsWDLExtractor


class PowerAutomateExtractor:
    """Extractor for Power Automate solution export JSON files."""

    name = "power_automate"
    patterns = ["**/Workflows/*.json", "**/*powerautomate*.json"]

    def __init__(self) -> None:
        self._logic = LogicAppsWDLExtractor()

    def detect(self, repo: Path) -> bool:
        return any(repo.glob("**/Workflows/*.json"))

    def discover(self, repo: Path) -> Iterable[Path]:
        seen: set[Path] = set()
        for pattern in self.patterns:
            for candidate in repo.glob(pattern):
                if candidate in seen or not candidate.is_file():
                    continue
                seen.add(candidate)
                try:
                    text = candidate.read_text(encoding="utf-8", errors="ignore")
                except Exception:
                    continue
                if "\"triggers\"" in text and "\"actions\"" in text:
                    yield candidate

    def extract(self, path: Path) -> Iterable[Signal]:
        try:
            data = json.loads(path.read_text(encoding="utf-8", errors="ignore"))
        except Exception:
            return []

        props_root = data.get("properties") or {}
        definition = props_root.get("definition") or data.get("definition")
        if not isinstance(definition, dict):
            return []

        connection_refs = props_root.get("connectionReferences") or {}
        flow_name = props_root.get("displayName") or data.get("name") or path.stem
        parsed = self._logic._parse_definition(definition, connection_refs=connection_refs, source_path=path)
        parsed.update(self._logic._augment_workflow_props(flow_name, parsed))

        props = {
            "name": flow_name,
            "file": str(path),
            "engine": "power_automate",
            "wf_kind": props_root.get("state") or "power_automate",
            "environment": props_root.get("environment", {}).get("name"),
            "solution": self._solution_name(path),
            **parsed,
        }
        evidence = [f"{path}:definition"]
        subscores = {"parsed": 1.0, "schema_evidence": 0.5 if parsed.get("triggers") else 0.2}
        return [Signal(kind="workflow", props=props, evidence=evidence, subscores=subscores)]

    def _solution_name(self, path: Path) -> str:
        try:
            parts = path.parts
            if "Workflows" in parts:
                idx = parts.index("Workflows")
                return parts[idx - 1]
        except Exception:
            pass
        return path.parent.name
