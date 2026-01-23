from __future__ import annotations

import json
from pathlib import Path
from typing import Iterable, List

from autodocx.types import Signal


class BwModuleManifestExtractor:
    name = "bw_module_manifest"
    patterns = ["**/*.jsv", "**/*.msv", "**/*.bwm"]

    def detect(self, repo: Path) -> bool:
        repo = Path(repo)
        return any(repo.glob("**/*.jsv")) or any(repo.glob("**/*.msv")) or any(repo.glob("**/*.bwm"))

    def discover(self, repo: Path) -> Iterable[Path]:
        repo = Path(repo)
        for pattern in self.patterns:
            yield from repo.glob(pattern)

    def extract(self, path: Path) -> Iterable[Signal]:
        path = Path(path)
        text = path.read_text(encoding="utf-8", errors="ignore")
        data = self._parse_manifest(text)

        module_name = data.get("module") or data.get("name") or path.stem
        processes: List[str] = data.get("processes") or data.get("services") or []
        shared_resources: List[str] = data.get("resources") or data.get("shared_resources") or []
        bindings: List[str] = data.get("bindings") or []

        props = {
            "name": module_name,
            "module_name": module_name,
            "file": str(path),
            "kind": "bw_module_manifest",
            "processes": processes,
            "shared_resources": shared_resources,
            "bindings": bindings,
        }

        evidence = [f"{path}:1-1"]
        yield Signal(kind="manifest", props=props, evidence=evidence, subscores={"parsed": 1.0})

    def _parse_manifest(self, text: str) -> dict:
        """
        Best-effort parser: try JSON first, otherwise fall back to a minimal line-based guess.
        """
        try:
            return json.loads(text)
        except Exception:
            pass

        processes: List[str] = []
        resources: List[str] = []
        bindings: List[str] = []
        module_name = None
        for line in text.splitlines():
            line = line.strip()
            if not line:
                continue
            if line.lower().startswith("module"):
                _, _, val = line.partition("=")
                module_name = val.strip() or module_name
            if "process" in line.lower():
                processes.append(line)
            if "resource" in line.lower():
                resources.append(line)
            if "binding" in line.lower():
                bindings.append(line)
        return {
            "module": module_name,
            "processes": processes,
            "resources": resources,
            "bindings": bindings,
        }
