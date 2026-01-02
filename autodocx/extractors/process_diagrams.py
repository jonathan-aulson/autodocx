from __future__ import annotations

import re
import xml.etree.ElementTree as ET
from html import unescape
from pathlib import Path
from typing import Iterable, List, Set, Tuple

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
        datastores, process_calls, identifier_hints = self._parse_diagram_hints(path)
        return [
            Signal(
                kind="process_diagram",
                props={
                    "name": name,
                    "file": str(path),
                    "datasource_tables": sorted(datastores),
                    "process_calls": sorted(process_calls),
                    "identifier_hints": sorted(identifier_hints),
                },
                evidence=[f"{path}:diagram"],
                subscores={"parsed": 0.5},
            )
        ]

    def _infer_name(self, path: Path, text: str) -> str:
        match = self.BPMN_NAME_RE.search(text)
        if match:
            return match.group("name")
        return path.stem

    def _parse_diagram_hints(self, path: Path) -> Tuple[Set[str], Set[str], Set[str]]:
        lower_name = path.name.lower()
        if lower_name.endswith(".bpmn"):
            return self._parse_bpmn_hints(path)
        if "drawio" in lower_name:
            return self._parse_drawio_hints(path)
        # Default to BPMN heuristics for other XML-based diagrams
        return self._parse_bpmn_hints(path)

    def _parse_bpmn_hints(self, path: Path) -> Tuple[Set[str], Set[str], Set[str]]:
        datastores: Set[str] = set()
        process_calls: Set[str] = set()
        identifier_hints: Set[str] = set()
        try:
            tree = ET.parse(path)
            root = tree.getroot()
        except Exception:
            return datastores, process_calls, identifier_hints
        for elem in root.iter():
            tag = elem.tag.lower()
            name = elem.attrib.get("name") or elem.attrib.get("{http://www.omg.org/spec/BPMN/20100524/MODEL}name")
            if "datastore" in tag and name:
                datastores.add(name)
            if "callactivity" in tag:
                called = elem.attrib.get("calledElement")
                if called:
                    process_calls.add(called)
            if name and self._looks_like_identifier(name):
                identifier_hints.add(name)
        return datastores, process_calls, identifier_hints

    def _parse_drawio_hints(self, path: Path) -> Tuple[Set[str], Set[str], Set[str]]:
        datastores: Set[str] = set()
        process_calls: Set[str] = set()
        identifier_hints: Set[str] = set()
        try:
            tree = ET.parse(path)
            root = tree.getroot()
        except Exception:
            return datastores, process_calls, identifier_hints

        for elem in root.iter():
            tag = elem.tag.lower()
            if not tag.endswith("cell"):
                continue
            if elem.attrib.get("vertex") != "1":
                continue
            raw_value = elem.attrib.get("value") or ""
            text = self._strip_markup(raw_value)
            if not text:
                continue
            style = (elem.attrib.get("style") or "").lower()
            if self._looks_like_datastore(style, text):
                datastores.add(text)
            if self._looks_like_process_call(style, text):
                process_calls.add(text)
            if self._looks_like_identifier(text):
                identifier_hints.add(text)
        return datastores, process_calls, identifier_hints

    def _strip_markup(self, value: str) -> str:
        if not value:
            return ""
        text = unescape(value)
        text = re.sub(r"<[^>]+>", " ", text)
        return re.sub(r"\s+", " ", text).strip()

    def _looks_like_datastore(self, style: str, text: str) -> bool:
        tokens = ("datastore", "database", "cylinder", "db", "datawarehouse")
        if any(token in style for token in tokens):
            return True
        label = text.lower()
        if any(label.endswith(suffix) for suffix in ("table", "db", "database")):
            return True
        return False

    def _looks_like_process_call(self, style: str, text: str) -> bool:
        label = text.lower()
        if any(token in style for token in ("call", "subprocess", "invoke")):
            return True
        if any(word in label for word in ("call", "invoke", "trigger", "launch")):
            return True
        return False

    def _looks_like_identifier(self, text: str) -> bool:
        lower = text.lower()
        return lower.endswith(("id", "code", "number", "key"))
