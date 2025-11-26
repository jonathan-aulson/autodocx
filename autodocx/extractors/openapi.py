# 
from __future__ import annotations
from pathlib import Path
from typing import Iterable, List, Dict, Any
import yaml
from autodocx.types import Signal

class OpenAPIExtractor:
    name = "openapi"
    patterns = ["**/*.yaml", "**/*.yml", "**/*.json"]

    def detect(self, repo: Path) -> bool:
        for pat in self.patterns:
            for p in repo.glob(pat):
                try:
                    head = p.read_text(encoding="utf-8", errors="ignore")[:4096]
                except Exception:
                    continue
                if "openapi:" in head or "swagger:" in head:
                    return True
        return False

    def discover(self, repo: Path) -> Iterable[Path]:
        for pat in self.patterns:
            for p in repo.glob(pat):
                if not p.is_file():
                    continue
                try:
                    text = p.read_text(encoding="utf-8", errors="ignore")
                    if "openapi:" in text or "swagger:" in text:
                        yield p
                except Exception:
                    continue

    def extract(self, path: Path) -> Iterable[Signal]:
        signals: List[Signal] = []
        try:
            spec = yaml.safe_load(path.read_text(encoding="utf-8", errors="ignore"))
            if not isinstance(spec, dict):
                return signals
            version = spec.get("openapi") or spec.get("swagger")
            if not version:
                return signals
            info = spec.get("info", {}) or {}
            title = info.get("title") or path.stem
            api_version = info.get("version") or ""               # API (business) version
            servers = [s.get("url") for s in spec.get("servers", []) if isinstance(s, dict)]

            signals.append(Signal(
                kind="api",
                props={"name": title, "spec_version": version, "version": api_version, "servers": servers, "file": str(path)},
                evidence=[f"{path}:1-50"],
                subscores={"parsed": 1.0, "schema_evidence": 1.0}
            ))

            for pth, methods in (spec.get("paths", {}) or {}).items():
                if not isinstance(methods, dict):
                    continue
                for m, op in methods.items():
                    if m.lower() not in ["get","post","put","delete","patch","head","options","trace"]:
                        continue
                    summ = (op or {}).get("summary") or ""
                    signals.append(Signal(
                        kind="op",
                        props={"api": title, "method": m.upper(), "path": pth, "summary": summ, "file": str(path)},
                        evidence=[f"{path}:paths:{pth}"],
                        subscores={"parsed": 1.0, "endpoint_or_op_coverage": 1.0, "schema_evidence": 1.0}
                    ))
        except Exception as e:
            signals.append(Signal(
                kind="doc",
                props={"name": path.name, "file": str(path), "note": f"OpenAPI parse error: {e}"},
                evidence=[f"{path}:1-1"],
                subscores={"parsed": 0.1}
            ))
        return signals
