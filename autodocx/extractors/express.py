from __future__ import annotations
from pathlib import Path
from typing import Iterable, List
import re, json
from autodocx.types import Signal

class ExpressJSExtractor:
    name = "express"
    patterns = ["**/*.js", "**/*.ts"]
    ROUTE_RE = re.compile(
        r"""(?P<obj>\bapp\b|\brouter\b)\.(?P<method>get|post|put|delete|patch|options|head)\s*\(\s*([`'"])(?P<path>.+?)\3""",
        re.IGNORECASE | re.DOTALL
    )

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
            for m in self.ROUTE_RE.finditer(content):
                method = m.group("method").upper()
                route = m.group("path")
                ln = content[:m.start()].count("\n") + 1
                signals.append(Signal(kind="route", props={"method": method, "path": route, "file": str(path)}, evidence=[f"{path}:{ln}-{ln+2}"], subscores={"parsed": 1.0}))
        except Exception as e:
            signals.append(Signal(kind="doc", props={"name": path.name, "file": str(path), "note": f"Express parse error: {e}"}, evidence=[f"{path}:1-1"], subscores={"parsed": 0.1}))
        return signals
