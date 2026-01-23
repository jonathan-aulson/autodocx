from __future__ import annotations

import json
from pathlib import Path
from typing import Iterable, Dict, Any

from autodocx.types import Signal


class BwServiceDescriptorExtractor:
    name = "bw_service_descriptor"
    patterns = ["**/*.Process-*.json", "**/*.process-*.json", "**/*Process-*.json"]

    def detect(self, repo: Path) -> bool:
        repo = Path(repo)
        return any(repo.glob(p) for p in self.patterns)

    def discover(self, repo: Path) -> Iterable[Path]:
        repo = Path(repo)
        for pat in self.patterns:
            yield from repo.glob(pat)

    def extract(self, path: Path) -> Iterable[Signal]:
        path = Path(path)
        props: Dict[str, Any] = {"file": str(path), "kind": "service_descriptor", "name": path.stem}
        try:
            data = json.loads(path.read_text(encoding="utf-8", errors="ignore"))
        except Exception:
            data = {}
        # Attempt to read REST/SOAP contract fields
        iface = data.get("interface") or {}
        rest = iface.get("rest") or data.get("rest") or {}
        bw_services = []
        if rest:
            props["endpoint"] = rest.get("path") or rest.get("endpoint")
            props["method"] = rest.get("method") or rest.get("httpMethod")
            props["inputs"] = rest.get("inputs") or rest.get("request")
            props["outputs"] = rest.get("outputs") or rest.get("response")
            bw_services.append(
                {
                    "kind": "REST",
                    "connector": "http",
                    "endpoint": props.get("endpoint"),
                    "method": props.get("method"),
                    "operation": rest.get("operationId"),
                    "evidence": f"{path}:1-1",
                }
            )
        soap = iface.get("soap") or data.get("soap") or {}
        if soap and not props.get("endpoint"):
            props["endpoint"] = soap.get("location")
            bw_services.append(
                {
                    "kind": "SOAP",
                    "connector": "soap",
                    "endpoint": props.get("endpoint"),
                    "method": soap.get("method"),
                    "operation": soap.get("operation"),
                    "evidence": f"{path}:1-1",
                }
            )
        evidence = [f"{path}:1-1"]
        props["enrichment"] = {"bw_services": bw_services}
        yield Signal(kind="interface", props=props, evidence=evidence, subscores={"parsed": 0.7})
