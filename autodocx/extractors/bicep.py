from __future__ import annotations
from pathlib import Path
from typing import Iterable, List, Dict, Any
import json, subprocess, yaml
from autodocx.types import Signal

class BicepExtractor:
    name = "bicep"
    patterns = ["**/*.bicep"]

    def detect(self, repo: Path) -> bool:
        return any(repo.glob("**/*.bicep"))

    def discover(self, repo: Path) -> Iterable[Path]:
        yield from repo.glob("**/*.bicep")

    def _build_to_arm(self, path: Path) -> Dict[str, Any] | None:
        # Try az bicep; fallback to bicep CLI; else None
        cmds = [
            ["az", "bicep", "build", "--file", str(path), "--stdout"],
            ["bicep", "build", str(path), "--stdout"]
        ]
        for cmd in cmds:
            try:
                out = subprocess.check_output(cmd, stderr=subprocess.STDOUT, text=True)
                doc = json.loads(out)
                return doc
            except Exception:
                continue
        return None

    def extract(self, path: Path) -> Iterable[Signal]:
        signals: List[Signal] = []
        try:
            arm = self._build_to_arm(path)
            if not isinstance(arm, dict):
                # Best-effort mark as infra doc
                signals.append(Signal(kind="doc", props={"name": path.name, "file": str(path), "note": "Bicep compile failed or CLI not available"}, evidence=[f"{path}:1-1"], subscores={"parsed": 0.2}))
                return signals

            # Generic ARM resource extraction
            resources = arm.get("resources") or []
            for res in resources:
                rtype = res.get("type")
                name = res.get("name")
                signals.append(Signal(kind="infra", props={"resource_type": rtype, "name": name, "file": str(path)}, evidence=[f"{path}:resources:{rtype}.{name}"], subscores={"parsed": 1.0}))

                # Logic Apps in ARM → parse workflows' definition (same as LogicApps)
                if rtype == "Microsoft.Logic/workflows":
                    definition = (res.get("properties") or {}).get("definition") or {}
                    if isinstance(definition, dict) and isinstance(definition.get("triggers"), dict) and isinstance(definition.get("actions"), dict):
                        # Basic parse (summary only)
                        triggers = [{"name": n, "type": (b or {}).get("type")} for n,b in (definition.get("triggers") or {}).items()]
                        steps = []
                        for an, node in (definition.get("actions") or {}).items():
                            atype = (node or {}).get("type")
                            inputs = (node or {}).get("inputs") or {}
                            conn = (((inputs.get("host") or {}).get("connection") or {}).get("name") or "").strip()
                            method = (inputs.get("method") or "")
                            uri = inputs.get("uri") or inputs.get("path")
                            steps.append({"name": an, "type": atype, "connector": conn, "method": method, "url_or_path": uri})
                        content_version = (res.get("properties") or {}).get("definition", {}).get("contentVersion") or ""
                        signals.append(Signal(
                            kind="workflow",
                            props={"name": name, "file": str(path), "engine": "logicapps", "wf_kind": "logicapps_consumption", "version": content_version, "triggers": triggers, "steps": steps, "calls_flows": []},
                            evidence=[f"{path}:resources:Microsoft.Logic/workflows:{name}"],
                            subscores={"parsed": 1.0, "schema_evidence": 0.4}
                        ))
        except Exception as e:
            signals.append(Signal(kind="doc", props={"name": path.name, "file": str(path), "note": f"Bicep parse error: {e}"}, evidence=[f"{path}:1-1"], subscores={"parsed": 0.1}))
        return signals
