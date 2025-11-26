from __future__ import annotations

import re
from pathlib import Path
from typing import Iterable, List, Dict

from autodocx.types import Signal


class UIComponentsExtractor:
    name = "ui_components"
    patterns = [
        "**/*.tsx",
        "**/*.jsx",
        "**/*.cshtml",
        "**/*.razor",
        "**/*.component.ts",
        "**/*.component.html",
    ]

    RE_FUNCTION_COMPONENT = re.compile(r"\bfunction\s+(?P<name>[A-Z][A-Za-z0-9_]*)\s*\(", re.MULTILINE)
    RE_CONST_COMPONENT = re.compile(r"\bconst\s+(?P<name>[A-Z][A-Za-z0-9_]*)\s*=\s*\(", re.MULTILINE)
    RE_ROUTE_DECORATOR = re.compile(r'@page\s+"(?P<route>[^"]+)"')
    RE_ANGULAR_COMPONENT = re.compile(
        r"@Component\s*\(\s*{(?P<body>.*?)}\s*\)\s*export\s+class\s+(?P<class>[A-Za-z0-9_]+)",
        re.DOTALL,
    )

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
        text = ""
        try:
            text = path.read_text(encoding="utf-8", errors="ignore")
        except Exception:
            return []

        signals: List[Signal] = []
        suffix = path.suffix.lower()
        if suffix in {".tsx", ".jsx"}:
            comps = self._react_components(text)
            for comp in comps:
                screenshots = self._find_screenshots(path, comp)
                signals.append(
                    Signal(
                        kind="ui_component",
                        props={
                            "framework": "react",
                            "name": comp,
                            "file": str(path),
                            "routes": self._guess_routes(text, comp),
                            "route_hierarchy": self._derive_route_hierarchy(path),
                            "entry_points": self._build_entry_points(comp, self._guess_routes(text, comp)),
                            "user_story": self._build_ui_story(comp, self._guess_routes(text, comp)),
                            "screenshots": screenshots,
                            "ui_snapshot": screenshots[0] if screenshots else "",
                        },
                        evidence=[f"{path}:component:{comp}"],
                        subscores={"parsed": 0.6},
                    )
                )
        elif suffix in {".cshtml", ".razor"}:
            route = self._razor_route(text)
            screenshots = self._find_screenshots(path, path.stem)
            signals.append(
                Signal(
                    kind="ui_component",
                    props={
                        "framework": "razor",
                        "name": path.stem,
                        "file": str(path),
                        "routes": [route] if route else [],
                        "route_hierarchy": self._derive_route_hierarchy(path),
                        "entry_points": self._build_entry_points(path.stem, [route] if route else []),
                        "user_story": self._build_ui_story(path.stem, [route] if route else []),
                        "screenshots": screenshots,
                        "ui_snapshot": screenshots[0] if screenshots else "",
                    },
                    evidence=[f"{path}:1-20"],
                    subscores={"parsed": 0.6},
                )
            )
        elif path.name.endswith(".component.ts"):
            components = self._angular_components(text)
            for comp in components:
                screenshots = self._find_screenshots(path, comp["class"])
                signals.append(
                    Signal(
                        kind="ui_component",
                        props={
                            "framework": "angular",
                            "name": comp["class"],
                            "file": str(path),
                            "selector": comp.get("selector"),
                            "template_url": comp.get("template_url"),
                            "route_hierarchy": self._derive_route_hierarchy(path),
                            "entry_points": self._build_entry_points(comp["class"], []),
                            "user_story": self._build_ui_story(comp["class"], []),
                            "screenshots": screenshots,
                            "ui_snapshot": screenshots[0] if screenshots else "",
                        },
                        evidence=[f"{path}:component:{comp['class']}"],
                        subscores={"parsed": 0.6},
                    )
                )
        elif path.name.endswith(".component.html"):
            component_name = path.name.replace(".component.html", "")
            routes = self._angular_template_routes(text)
            screenshots = self._find_screenshots(path, component_name)
            signals.append(
                Signal(
                    kind="ui_component",
                    props={
                        "framework": "angular",
                        "name": component_name,
                        "file": str(path),
                        "selector": "",
                        "template_url": path.name,
                        "routes": routes,
                        "route_hierarchy": self._derive_route_hierarchy(path),
                        "entry_points": self._build_entry_points(component_name, routes),
                        "user_story": self._build_ui_story(component_name, routes),
                        "screenshots": screenshots,
                        "ui_snapshot": screenshots[0] if screenshots else "",
                    },
                    evidence=[f"{path}:template"],
                    subscores={"parsed": 0.4},
                )
            )
        return [s for s in signals if s]

    def _react_components(self, text: str) -> List[str]:
        names = set(match.group("name") for match in self.RE_FUNCTION_COMPONENT.finditer(text))
        names.update(match.group("name") for match in self.RE_CONST_COMPONENT.finditer(text))
        return sorted(names)

    def _guess_routes(self, text: str, component: str) -> List[str]:
        matches = re.findall(rf'<Route[^>]*component\s*=\s*{{{component}}}[^>]*path="([^"]+)"', text)
        if matches:
            return matches
        return []

    def _razor_route(self, text: str) -> str:
        match = self.RE_ROUTE_DECORATOR.search(text)
        return match.group("route") if match else ""

    def _angular_components(self, text: str) -> List[Dict[str, str]]:
        components: List[Dict[str, str]] = []
        for match in self.RE_ANGULAR_COMPONENT.finditer(text):
            body = match.group("body")
            selector = self._extract_metadata_value(body, "selector")
            template_url = self._extract_metadata_value(body, "templateUrl")
            components.append({"class": match.group("class"), "selector": selector, "template_url": template_url})
        return components

    def _extract_metadata_value(self, body: str, key: str) -> str:
        pattern = re.compile(rf"{key}\s*:\s*['\"]([^'\"]+)['\"]")
        match = pattern.search(body)
        return match.group(1) if match else ""

    def _angular_template_routes(self, text: str) -> List[str]:
        routes = set()
        routes.update(re.findall(r'routerLink="([^"]+)"', text))
        routes.update(re.findall(r'\[routerLink\]="[^"]*([^"\]]+)"', text))
        return sorted(routes)

    def _derive_route_hierarchy(self, path: Path) -> List[str]:
        try:
            parts = list(path.relative_to(path.anchor).parts)
        except Exception:
            parts = list(path.parts)
        filtered = [p for p in parts if p.lower() not in {"src", "app", "apps"}]
        return filtered[-4:]

    def _build_entry_points(self, name: str, routes: List[str]) -> List[Dict[str, str]]:
        entries: List[Dict[str, str]] = []
        for route in routes or []:
            entries.append({"component": name, "route": route})
        if not entries:
            entries.append({"component": name, "route": ""})
        return entries

    def _build_ui_story(self, name: str, routes: List[str]) -> str:
        if routes:
            return f"{name} renders the {routes[0]} view and guides the user through on-screen actions."
        return f"{name} renders a reusable UI surface."

    def _find_screenshots(self, path: Path, name: str) -> List[str]:
        screenshots: List[str] = []
        slug = name.lower()
        for candidate in path.parent.glob("*.png"):
            if slug in candidate.stem.lower():
                screenshots.append(str(candidate))
        screenshots.sort()
        return screenshots[:3]
