from __future__ import annotations

import re
from pathlib import Path
from typing import Dict, Iterable, List, Set

from autodocx.types import Signal


class BusinessEntityExtractor:
    name = "business_entities"
    patterns = [
        "**/*.bpmn",
        "**/*.drawio",
        "**/*.drawio.xml",
        "**/*.cs",
        "**/*.cshtml",
        "**/*.razor",
        "**/*.tsx",
        "**/*.jsx",
        "**/*.component.ts",
    ]

    LANE_RE = re.compile(r'<[\w:]*lane[^>]*name="(?P<name>[^"]+)"', re.IGNORECASE)
    AUTHORIZE_RE = re.compile(r"\[Authorize[^\]]*Roles\s*=\s*\"(?P<roles>[^\"]+)\"", re.IGNORECASE)
    COMPONENT_FUNCTION_RE = re.compile(r"\bfunction\s+(?P<name>[A-Z][A-Za-z0-9_]*)\s*\(", re.MULTILINE)
    COMPONENT_CONST_RE = re.compile(r"\bconst\s+(?P<name>[A-Z][A-Za-z0-9_]*)\s*=\s*\(", re.MULTILINE)
    COMPONENT_CLASS_RE = re.compile(r"class\s+(?P<name>[A-Z][A-Za-z0-9_]*)\s+(?:extends|implements)", re.MULTILINE)
    ANGULAR_COMPONENT_RE = re.compile(
        r"@Component\s*\(\s*{(?P<body>.*?)}\s*\)\s*export\s+class\s+(?P<class>[A-Za-z0-9_]+)",
        re.DOTALL,
    )

    COMPONENT_STOPWORDS: Set[str] = {
        "component",
        "page",
        "view",
        "screen",
        "card",
        "dialog",
        "modal",
        "form",
        "container",
        "module",
        "section",
        "area",
        "widget",
    }
    ROLE_KEYWORDS: Dict[str, str] = {
        "admin": "Administrator",
        "administrator": "Administrator",
        "manager": "Manager",
        "mgr": "Manager",
        "customer": "Customer",
        "client": "Client",
        "partner": "Partner",
        "vendor": "Vendor",
        "analyst": "Analyst",
        "agent": "Agent",
        "operator": "Operator",
        "ops": "Operations",
        "support": "Support",
        "finance": "Finance",
        "billing": "Billing",
        "sales": "Sales",
        "approver": "Approver",
        "reviewer": "Reviewer",
        "owner": "Owner",
        "lead": "Lead",
    }

    def detect(self, repo: Path) -> bool:
        for pattern in self.patterns:
            if any(repo.glob(pattern)):
                return True
        return False

    def discover(self, repo: Path) -> Iterable[Path]:
        seen = set()
        for pattern in self.patterns:
            for candidate in repo.glob(pattern):
                if candidate in seen or not candidate.is_file():
                    continue
                seen.add(candidate)
                yield candidate

    def extract(self, path: Path) -> Iterable[Signal]:
        try:
            text = path.read_text(encoding="utf-8", errors="ignore")
        except Exception:
            return []

        entities: List[Signal] = []
        lowered_name = path.name.lower()
        suffix = path.suffix.lower()

        if suffix == ".bpmn" or lowered_name.endswith(".drawio") or lowered_name.endswith(".drawio.xml"):
            entities.extend(self._lanes_from_process_diagrams(path, text))

        if suffix == ".cs":
            entities.extend(self._roles_from_authorize_attributes(path, text))

        if suffix in {".tsx", ".jsx"} or path.name.endswith(".component.ts") or suffix in {".cshtml", ".razor"}:
            entities.extend(self._roles_from_component_names(path, text))

        return entities

    def _lanes_from_process_diagrams(self, path: Path, text: str) -> List[Signal]:
        signals: List[Signal] = []
        for match in self.LANE_RE.finditer(text):
            name = match.group("name").strip()
            if not name:
                continue
            signals.append(
                Signal(
                    kind="business_entity",
                    props={
                        "name": name,
                        "file": str(path),
                        "source": "process_diagram",
                    },
                    evidence=[f"{path}:lane:{name}"],
                    subscores={"parsed": 0.5},
                )
            )
        return signals

    def _roles_from_authorize_attributes(self, path: Path, text: str) -> List[Signal]:
        signals: List[Signal] = []
        for match in self.AUTHORIZE_RE.finditer(text):
            roles_blob = match.group("roles") or ""
            for raw_role in re.split(r"[,\|;]", roles_blob):
                role = raw_role.strip()
                if not role:
                    continue
                display = role.replace("_", " ").strip()
                if not display:
                    display = role
                signals.append(
                    Signal(
                        kind="business_entity",
                        props={
                            "name": display,
                            "raw_name": role,
                            "file": str(path),
                            "source": "authorize_attribute",
                            "entity_kind": "role",
                        },
                        evidence=[f"{path}:authorize:{display}"],
                        subscores={"parsed": 0.4},
                    )
                )
        return signals

    def _roles_from_component_names(self, path: Path, text: str) -> List[Signal]:
        names = set()
        names.update(match.group("name") for match in self.COMPONENT_FUNCTION_RE.finditer(text))
        names.update(match.group("name") for match in self.COMPONENT_CONST_RE.finditer(text))
        names.update(match.group("name") for match in self.COMPONENT_CLASS_RE.finditer(text))
        for match in self.ANGULAR_COMPONENT_RE.finditer(text):
            names.add(match.group("class"))

        signals: List[Signal] = []
        seen_entities: Set[str] = set()
        for component_name in sorted(names):
            entity_name = self._humanize_component_name(component_name)
            if not entity_name or entity_name in seen_entities:
                continue
            seen_entities.add(entity_name)
            signals.append(
                Signal(
                    kind="business_entity",
                    props={
                        "name": entity_name,
                        "raw_component": component_name,
                        "file": str(path),
                        "source": "component_name",
                        "entity_kind": "role",
                    },
                    evidence=[f"{path}:component:{component_name}"],
                    subscores={"parsed": 0.35},
                )
            )
        return signals

    def _humanize_component_name(self, component_name: str) -> str:
        parts = re.findall(r"[A-Z][a-z0-9]+|[A-Z]+(?=[A-Z]|$)", component_name)
        if not parts:
            return ""
        filtered: List[str] = []
        has_role_keyword = False
        for part in parts:
            lower = part.lower()
            if lower in self.COMPONENT_STOPWORDS:
                continue
            mapped = self.ROLE_KEYWORDS.get(lower, part)
            filtered.append(mapped)
            if lower in self.ROLE_KEYWORDS:
                has_role_keyword = True
        if not has_role_keyword or not filtered:
            return ""
        return " ".join(filtered)
