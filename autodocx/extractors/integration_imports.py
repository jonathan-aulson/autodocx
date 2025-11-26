from __future__ import annotations

import re
from pathlib import Path
from typing import Iterable, List

from autodocx.types import Signal


class IntegrationImportsExtractor:
    name = "integration_imports"
    patterns = ["**/*.ts", "**/*.tsx", "**/*.js", "**/*.cs"]

    JS_IMPORT_RE = re.compile(r'import\s+(?:.+?\s+from\s+)?["\'](?P<module>[^"\']+)["\']')
    CS_USING_RE = re.compile(r'^\s*using\s+(?P<namespace>[A-Za-z0-9_.]+);', re.MULTILINE)
    SDK_HINTS = {
        "axios": "http_client",
        "node-fetch": "http_client",
        "@microsoft/microsoft-graph-client": "microsoft_graph",
        "@azure": "azure_sdk",
        "@azure/storage-blob": "azure_storage",
        "aws-sdk": "aws_sdk",
        "pg": "postgres",
        "mysql2": "mysql",
        "mssql": "sql_server",
        "redis": "redis",
        "mongodb": "mongodb",
        "mongoose": "mongodb",
        "twilio": "communication",
        "sendgrid": "communication",
        "mailchimp": "marketing",
        "System.Net.Http": "http_client",
        "Microsoft.PowerPlatform": "power_platform",
        "Microsoft.Graph": "microsoft_graph",
        "System.Data.SqlClient": "sql_server",
        "MongoDB.Driver": "mongodb",
        "Azure.Storage.Blobs": "azure_storage",
        "SendGrid": "communication",
    }

    def detect(self, repo: Path) -> bool:
        return any(repo.glob("**/*.ts")) or any(repo.glob("**/*.cs")) or any(repo.glob("**/*.js"))

    def discover(self, repo: Path) -> Iterable[Path]:
        for pattern in self.patterns:
            yield from repo.glob(pattern)

    def extract(self, path: Path) -> Iterable[Signal]:
        try:
            text = path.read_text(encoding="utf-8", errors="ignore")
        except Exception:
            return []

        suffix = path.suffix.lower()
        modules: List[str] = []
        if suffix in {".ts", ".tsx", ".js"}:
            modules = [m.group("module") for m in self.JS_IMPORT_RE.finditer(text)]
        elif suffix == ".cs":
            modules = [m.group("namespace") for m in self.CS_USING_RE.finditer(text)]

        signals: List[Signal] = []
        for module in modules:
            tag = self._classify_module(module)
            if not tag:
                continue
            signals.append(
                Signal(
                    kind="integration",
                    props={
                        "library": module,
                        "integration_kind": tag,
                        "file": str(path),
                        "language": suffix.lstrip("."),
                    },
                    evidence=[f"{path}:import:{module}"],
                    subscores={"parsed": 0.5},
                )
            )
        return signals

    def _classify_module(self, module: str) -> str | None:
        module_lower = module.lower()
        for hint, tag in self.SDK_HINTS.items():
            if hint.lower() in module_lower:
                return tag
        return None
