from __future__ import annotations
from pathlib import Path
from typing import Iterable, List, Set
import re, json
from autodocx.types import Signal

class ExpressJSExtractor:
    name = "express"
    patterns = ["**/*.js", "**/*.ts"]
    ROUTE_RE = re.compile(
        r"""(?P<obj>\bapp\b|\brouter\b)\.(?P<method>get|post|put|delete|patch|options|head)\s*\(\s*([`'"])(?P<path>.+?)\3""",
        re.IGNORECASE | re.DOTALL
    )
    PARAM_RE = re.compile(r":([A-Za-z0-9_]+)")
    DB_COLLECTION_RE = re.compile(r"db\.collection\(\s*['\"]([^'\"]+)['\"]", re.IGNORECASE)
    MONGOOSE_MODEL_RE = re.compile(r"mongoose\.model\(\s*['\"]([^'\"]+)['\"]", re.IGNORECASE)
    KNEX_TABLE_RE = re.compile(r"knex\(\s*['\"]([^'\"]+)['\"]\)", re.IGNORECASE)
    HTTP_CALL_RE = re.compile(r"(axios|superagent)\.(get|post|put|delete|patch)\(\s*['\"](https?://[^'\"]+)['\"]", re.IGNORECASE)
    FETCH_CALL_RE = re.compile(r"fetch\(\s*['\"](https?://[^'\"]+)['\"]", re.IGNORECASE)

    def detect(self, repo: Path) -> bool:
        pkg = repo / "package.json"
        if pkg.exists():
            try:
                j = json.loads(pkg.read_text(encoding="utf-8", errors="ignore"))
                deps = {**(j.get("dependencies") or {}), **(j.get("devDependencies") or {})}
                if "express" in deps:
                    return True
            except Exception:
                pass
        for p in repo.glob("**/*.js"):
            try:
                head = p.read_text(encoding="utf-8", errors="ignore")[:4096]
                if "express" in head or "app.get(" in head or "router.get(" in head:
                    return True
            except Exception:
                continue
        for p in repo.glob("**/*.ts"):
            try:
                head = p.read_text(encoding="utf-8", errors="ignore")[:4096]
                if "express" in head or "app.get(" in head or "router.get(" in head:
                    return True
            except Exception:
                continue
        return False

    def discover(self, repo: Path) -> Iterable[Path]:
        for pat in self.patterns:
            yield from repo.glob(pat)

    def extract(self, path: Path) -> Iterable[Signal]:
        signals: List[Signal] = []
        try:
            content = path.read_text(encoding="utf-8", errors="ignore")
            datastores = self._datastore_hints(content)
            services = self._service_hints(content)
            for m in self.ROUTE_RE.finditer(content):
                method = m.group("method").upper()
                route = m.group("path")
                ln = content[:m.start()].count("\n") + 1
                identifiers = self.PARAM_RE.findall(route) or []
                props = {
                    "method": method,
                    "path": route,
                    "file": str(path),
                    "datasource_tables": sorted(datastores),
                    "service_dependencies": sorted(services),
                    "steps": [{"name": route, "connector": "express", "operation": method}],
                }
                if identifiers:
                    props["identifier_hints"] = sorted(set(identifiers))
                if services:
                    props["process_calls"] = sorted(services)
                signals.append(Signal(kind="route", props=props, evidence=[f"{path}:{ln}-{ln+2}"], subscores={"parsed": 1.0}))
        except Exception as e:
            signals.append(Signal(kind="doc", props={"name": path.name, "file": str(path), "note": f"Express parse error: {e}"}, evidence=[f"{path}:1-1"], subscores={"parsed": 0.1}))
        return signals

    def _datastore_hints(self, content: str) -> Set[str]:
        hints: Set[str] = set(m.group(1) for m in self.DB_COLLECTION_RE.finditer(content))
        hints.update(m.group(1) for m in self.MONGOOSE_MODEL_RE.finditer(content))
        hints.update(m.group(1) for m in self.KNEX_TABLE_RE.finditer(content))
        return {h for h in hints if h}

    def _service_hints(self, content: str) -> Set[str]:
        hints: Set[str] = set(m.group(3) for m in self.HTTP_CALL_RE.finditer(content))
        hints.update(m.group(1) for m in self.FETCH_CALL_RE.finditer(content))
        return {h for h in hints if h}
