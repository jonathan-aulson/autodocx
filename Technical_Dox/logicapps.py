from __future__ import annotations
from pathlib import Path
from typing import Iterable, List, Dict, Any, Optional, Tuple
import json, yaml, re
from autodocx.types import Signal

class LogicAppsWDLExtractor:
    name = "logicapps_wdl"
    patterns = [
        "**/workflow.json",
        "**/definition.json",
        "**/*flow*.json",
        "**/*logicapp*.json",
        "**/azuredeploy*.json",
        "**/template*.json",
        "**/arm*.json",
        "**/*.json",
    ]

    def detect(self, repo: Path) -> bool:
        # Be generous for PoC; discover() will filter by content
        return any(repo.glob("**/*.json"))

    def discover(self, repo: Path) -> Iterable[Path]:
        seen = set()
        for pat in self.patterns:
            for p in repo.glob(pat):
                if p in seen or not p.is_file():
                    continue
                seen.add(p)
                try:
                    t = p.read_text(encoding="utf-8", errors="ignore")
                except Exception:
                    continue
                if self._looks_wdl_text(t):
                    yield p

    def extract(self, path: Path) -> Iterable[Signal]:
        signals: List[Signal] = []
        try:
            raw = path.read_text(encoding="utf-8", errors="ignore")
            try:
                doc = json.loads(raw)
            except Exception:
                doc = yaml.safe_load(raw)
            if not isinstance(doc, dict):
                return signals

            # A: direct top-level triggers/actions
            if self._has_top_level_wdl(doc):
                return self._emit_from_definition(path, raw, doc, root_label="root")

            # B: top-level "definition"
            if isinstance(doc.get("definition"), dict) and self._has_top_level_wdl(doc["definition"]):
                return self._emit_from_definition(path, raw, doc["definition"], root_label="definition")

            # C: "properties.definition" (Power Automate export)
            if isinstance(doc.get("properties"), dict) and isinstance(doc["properties"].get("definition"), dict):
                d = doc["properties"]["definition"]
                if self._has_top_level_wdl(d):
                    return self._emit_from_definition(path, raw, d, root_label="properties.definition")

            # D: ARM resources array
            if isinstance(doc.get("resources"), list):
                out: List[Signal] = []
                for res in doc["resources"]:
                    if not isinstance(res, dict): continue
                    if res.get("type") == "Microsoft.Logic/workflows":
                        definition = (res.get("properties") or {}).get("definition") or {}
                        if isinstance(definition, dict) and self._has_top_level_wdl(definition):
                            out.extend(self._emit_from_definition(path, raw, definition, root_label="resources.definition"))
                return out

            return signals
        except Exception as e:
            signals.append(Signal(
                kind="doc",
                props={"name": path.name, "file": str(path), "note": f"LogicApps/Flow parse error: {e}"},
                evidence=[f"{path}:1-1"],
                subscores={"parsed": 0.1}
            ))
            return signals

    # ---------------- internals ----------------

    def _looks_wdl_text(self, text: str) -> bool:
        t = text[:200000]
        return ('"triggers"' in t and '"actions"' in t) or ("Microsoft.Logic/workflows" in t) or ("workflowdefinition.json" in t)

    def _has_top_level_wdl(self, d: dict) -> bool:
        return isinstance(d.get("triggers"), dict) and isinstance(d.get("actions"), dict)

    def _guess_kind(self, raw: str, path: Path, is_arm: bool) -> str:
        if is_arm: return "logicapps_consumption"
        p = str(path).lower()
        if "solutions" in p or "/flows/" in p or "microsoft.flow" in raw.lower() or "connectionreferences" in raw.lower():
            return "power_automate"
        if "/workflows/" in p or p.endswith("/workflow.json"):
            return "logicapps_standard"
        return "logicapps_standard"

    def _parse_definition(self, definition: dict) -> Dict[str, Any]:
        triggers = []
        for name, body in (definition.get("triggers") or {}).items():
            ttype = (body or {}).get("type")
            trig = {"name": name, "type": ttype}
            inputs = (body or {}).get("inputs") or {}
            if ttype and ttype.lower() in ["request", "http"]:
                schema = inputs.get("schema") or {}
                props = sorted(list((schema.get("properties") or {}).keys())) if isinstance(schema, dict) else []
                if props:
                    trig["schema_props"] = props
            if ttype and ttype.lower() == "recurrence":
                rec = inputs.get("recurrence") or {}
                trig["schedule"] = {"frequency": rec.get("frequency"), "interval": rec.get("interval")}
            triggers.append(trig)

        steps = []
        calls_flows = []
        for aname, node in (definition.get("actions") or {}).items():
            atype = (node or {}).get("type")
            info = {"name": aname, "type": atype, "connector": None, "method": None, "url_or_path": None, "inputs_keys": []}
            inputs = (node or {}).get("inputs") or {}

            # Connector actions (OpenApiConnection/ApiConnection)
            if atype in ["OpenApiConnection", "ApiConnection", "ApiConnectionWebhook", "ServiceProvider"]:
                info["connector"] = (((inputs.get("host") or {}).get("connection") or {}).get("name") or "").strip()
                info["method"] = (inputs.get("method") or "").upper()
                info["url_or_path"] = inputs.get("path")
                if isinstance(inputs.get("body"), dict):
                    info["inputs_keys"] = sorted(list(inputs["body"].keys()))

            # HTTP
            if atype in ["Http", "HttpWebhook"]:
                info["connector"] = "http"
                info["method"] = (inputs.get("method") or "").upper()
                info["url_or_path"] = inputs.get("uri")
                if isinstance(inputs.get("body"), dict):
                    info["inputs_keys"] = sorted(list(inputs["body"].keys()))
                uri = inputs.get("uri") or ""
                if "/workflows/" in (uri or "") and "/triggers/" in (uri or "") and "/run" in (uri or ""):
                    calls_flows.append(uri)

            steps.append(info)

        return {"triggers": triggers, "steps": steps, "calls_flows": sorted(set(calls_flows))}

    def _emit_from_definition(self, path: Path, raw: str, definition: dict, root_label: str) -> List[Signal]:
        wf_kind = self._guess_kind(raw, path, is_arm=False)
        engine = "logicapps" if "logicapps" in wf_kind else "power_automate"
        name = self._name_from_path(path)
        parsed = self._parse_definition(definition)
        content_version = definition.get("contentVersion") or "" 
        return [Signal(
            kind="workflow",
            props={
                "name": name,
                "file": str(path),
                "engine": engine,
                "wf_kind": wf_kind,
                "version": content_version, 
                **parsed
            },
            evidence=[f"{path}:{root_label}"],
            subscores={"parsed": 1.0, "schema_evidence": 0.4 if parsed.get("triggers") else 0.1}
        )]

    def _name_from_path(self, path: Path) -> str:
        parts_lower = [p.lower() for p in path.parts]
        if "workflows" in parts_lower:
            try:
                idx = parts_lower.index("workflows")
                return path.parts[idx + 1]
            except Exception:
                pass
        return path.stem
