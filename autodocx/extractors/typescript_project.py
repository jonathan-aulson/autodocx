from __future__ import annotations

import json
from pathlib import Path
from typing import Any, Dict, Iterable, List

from autodocx.types import Signal


class TypeScriptProjectExtractor:
    """
    Emits doc signals describing TypeScript project settings pulled from tsconfig.json
    and package.json dependencies/scripts.
    """

    name = "typescript_project"
    patterns = ["**/tsconfig.json", "**/package.json"]

    def detect(self, repo: Path) -> bool:
        return any(repo.glob(pattern) for pattern in self.patterns)

    def discover(self, repo: Path) -> Iterable[Path]:
        seen = set()
        for pattern in self.patterns:
            for candidate in repo.glob(pattern):
                if candidate.is_file() and candidate not in seen:
                    seen.add(candidate)
                    yield candidate

    def extract(self, path: Path) -> Iterable[Signal]:
        if path.name == "tsconfig.json":
            return self._from_tsconfig(path)
        if path.name == "package.json":
            return self._from_package_json(path)
        return []

    # ---------------- helpers ----------------

    def _json_from_file(self, path: Path) -> Dict[str, Any]:
        try:
            return json.loads(path.read_text(encoding="utf-8"))
        except Exception:
            return {}

    def _from_tsconfig(self, path: Path) -> Iterable[Signal]:
        data = self._json_from_file(path)
        if not data:
            return []
        compiler = data.get("compilerOptions") or {}
        alias_count = 0
        if isinstance(compiler.get("paths"), dict):
            for arr in compiler["paths"].values():
                alias_count += len(arr or [])
        props = {
            "name": f"tsconfig::{path.parent.name or path.stem}",
            "file": str(path),
            "kind": "tsconfig",
            "root_dir": compiler.get("rootDir"),
            "out_dir": compiler.get("outDir"),
            "module": compiler.get("module"),
            "target": compiler.get("target"),
            "strict": compiler.get("strict"),
            "jsx": compiler.get("jsx"),
            "paths": list((compiler.get("paths") or {}).keys()),
            "alias_count": alias_count,
            "references": data.get("references"),
            "include": data.get("include"),
            "exclude": data.get("exclude"),
        }
        signal = Signal(
            kind="doc",
            props=props,
            evidence=[f"{path}:1"],
            subscores={"parsed": 0.9},
        )
        return [signal]

    def _from_package_json(self, path: Path) -> Iterable[Signal]:
        data = self._json_from_file(path)
        if not data:
            return []
        deps = self._collect_dependencies(data)
        if "typescript" not in deps:
            return []
        frameworks = self._detect_frameworks(deps)
        scripts = self._important_scripts(data.get("scripts") or {})
        props = {
            "name": data.get("name") or path.parent.name or "typescript-project",
            "file": str(path),
            "kind": "package_json",
            "typescript_version": deps.get("typescript"),
            "frameworks": frameworks,
            "scripts": scripts,
            "linters": [k for k in deps if k in {"eslint", "tslint", "@angular/cli"}],
        }
        signal = Signal(
            kind="doc",
            props=props,
            evidence=[f"{path}:name"],
            subscores={"parsed": 0.8},
        )
        return [signal]

    def _collect_dependencies(self, data: Dict[str, Any]) -> Dict[str, str]:
        deps: Dict[str, str] = {}
        for section in ("dependencies", "devDependencies", "peerDependencies"):
            section_data = data.get(section) or {}
            if isinstance(section_data, dict):
                for name, version in section_data.items():
                    if name not in deps:
                        deps[name] = str(version)
        return deps

    def _detect_frameworks(self, deps: Dict[str, str]) -> List[str]:
        frameworks = []
        candidates = {
            "nest": "@nestjs/core",
            "next": "next",
            "angular": "@angular/core",
            "react": "react",
            "aws-cdk": "aws-cdk-lib",
        }
        for name, pkg in candidates.items():
            if pkg in deps:
                frameworks.append(name)
        return frameworks

    def _important_scripts(self, scripts: Dict[str, Any]) -> List[Dict[str, str]]:
        highlights = []
        for script, cmd in scripts.items():
            if any(keyword in cmd for keyword in ("ts-node", "tsc", "next", "nest", "vite", "webpack")):
                highlights.append({"name": script, "command": cmd})
        return highlights[:10]
