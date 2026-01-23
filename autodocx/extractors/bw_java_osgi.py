from __future__ import annotations

import re
from pathlib import Path
from typing import Iterable, Dict, Any

from autodocx.types import Signal


class BwJavaOsgiComponentExtractor:
    name = "bw_java_osgi_component"
    patterns = ["**/*.java", "**/*.class", "**/MANIFEST.MF", "**/*.properties"]

    def detect(self, repo: Path) -> bool:
        repo = Path(repo)
        return any(repo.glob("**/*.java")) or any(repo.glob("**/MANIFEST.MF"))

    def discover(self, repo: Path) -> Iterable[Path]:
        repo = Path(repo)
        for pat in self.patterns:
            yield from repo.glob(pat)

    def extract(self, path: Path) -> Iterable[Signal]:
        path = Path(path)
        text = path.read_text(encoding="utf-8", errors="ignore")
        props: Dict[str, Any] = {
            "name": path.stem,
            "file": str(path),
            "kind": "bw_java_osgi",
        }
        if path.name.upper() == "MANIFEST.MF":
            bsn = self._extract_manifest_field(text, "Bundle-SymbolicName")
            activator = self._extract_manifest_field(text, "Bundle-Activator")
            classpath = self._extract_manifest_field(text, "Bundle-ClassPath")
            if bsn:
                props["bundle_symbolic_name"] = bsn
            if activator:
                props["bundle_activator"] = activator
            if classpath:
                props["bundle_classpath"] = classpath
        evidence = [f"{path}:1-1"]
        props["enrichment"] = {"bw_services": [], "bw_invocations": []}
        yield Signal(kind="adapter", props=props, evidence=evidence, subscores={"parsed": 0.6})

    def _extract_manifest_field(self, text: str, field: str) -> str | None:
        pattern = re.compile(rf"{re.escape(field)}:\s*(.+)", re.IGNORECASE)
        for line in text.splitlines():
            m = pattern.match(line.strip())
            if m:
                return m.group(1).strip()
        return None
