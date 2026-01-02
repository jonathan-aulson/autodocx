from __future__ import annotations

import json
from pathlib import Path
from typing import Iterable, List, Dict, Any, Optional, Tuple, Set

from lxml import etree

from autodocx.types import Signal

NS = {
    "om": "http://schemas.microsoft.com/BizTalk/2003/DesignerData",
    "xsd": "http://www.w3.org/2001/XMLSchema",
}


class BizTalkLogicAppsExtractor:
    """Parses BizTalk orchestrations (.odx) and Logic Apps Standard/Durable workflow JSON."""

    name = "biztalk_logicapps"
    patterns = ["**/*.odx", "**/*.workflow.json", "**/workflow.json", "**/*.durable.json"]

    def detect(self, repo: Path) -> bool:
        return any(repo.glob("**/*.odx")) or any(repo.glob("**/*.workflow.json")) or any(repo.glob("**/workflow.json"))

    def discover(self, repo: Path) -> Iterable[Path]:
        seen = set()
        for pattern in self.patterns:
            for candidate in repo.glob(pattern):
                if candidate.is_file() and candidate not in seen:
                    seen.add(candidate)
                    yield candidate

    def extract(self, path: Path) -> Iterable[Signal]:
        suffix = path.suffix.lower()
        name = path.name.lower()
        if suffix == ".odx":
            return self._extract_odx(path)
        if name.endswith("workflow.json") or path.name.lower() == "workflow.json":
            return self._extract_logicapps_workflow(path)
        if name.endswith("durable.json"):
            return self._extract_durable_json(path)
        return []

    # ---------------- BizTalk ODX -----------------

    def _extract_odx(self, path: Path) -> Iterable[Signal]:
        try:
            tree = etree.parse(str(path))
        except Exception as exc:
            return [
                Signal(
                    kind="doc",
                    props={"name": path.stem, "file": str(path), "note": f"BizTalk ODX parse error: {exc}"},
                    evidence=[{"path": str(path), "lines": "1-1", "snippet": ""}],
                    subscores={"parsed": 0.1},
                )
            ]
        root = tree.getroot()
        processes = root.xpath(".//om:Element[@Type='Microsoft.BizTalk.BpelModel.BpelProcess']", namespaces=NS)
        if not processes:
            return []
        signals: List[Signal] = []
        for process in processes:
            name = process.get("Name") or path.stem
            triggers: List[Dict[str, Any]] = []
            steps: List[Dict[str, Any]] = []
            relationships: List[Dict[str, Any]] = []
            datastores: Set[str] = set()
            services: Set[str] = set()
            process_calls: Set[str] = set()
            identifier_hints: Set[str] = set()

            shapes = process.xpath(".//om:Element", namespaces=NS)
            for shape in shapes:
                shape_type = (shape.get("Type") or "").lower()
                shape_name = shape.get("Name") or shape.get("ID") or shape_type.split(".")[-1]
                properties = {
                    prop.get("Name"): prop.get("Value")
                    for prop in shape.xpath(".//om:Property", namespaces=NS)
                }
                step = {"name": shape_name, "connector": shape_type.split(".")[-1], "evidence": {"path": str(path), "lines": "", "snippet": shape_type}}
                if "receive" in shape_type:
                    port = properties.get("PortName") or properties.get("OperationName")
                    triggers.append({"name": shape_name, "type": "biztalk_receive", "method": "RECEIVE", "path": port, "evidence": step["evidence"]})
                if "send" in shape_type:
                    port = properties.get("PortName") or properties.get("OperationName")
                    services.add(port or "SendPort")
                    relationships.append(
                        self._relationship(
                            source=shape_name,
                            kind="service",
                            ref=port or "SendPort",
                            detail=f"Send {port or ''}".strip(),
                            operation="calls",
                        )
                    )
                if "sql" in shape_type or "database" in shape_type:
                    table = properties.get("Table") or properties.get("Document")
                    if table:
                        datastores.add(table)
                        step["datasource_table"] = table
                        relationships.append(
                            self._relationship(
                                source=shape_name,
                                kind="sql",
                                ref=table,
                                detail=f"SQL {table}",
                                operation="writes",
                            )
                        )
                if "callsuborchestration" in shape_type or "callprocess" in shape_type:
                    target = properties.get("CalledOrchestration") or properties.get("Process")
                    if target:
                        process_calls.add(target)
                        relationships.append(
                            self._relationship(
                                source=shape_name,
                                kind="workflow",
                                ref=target,
                                detail=f"Invoke {target}",
                                operation="calls",
                            )
                        )
                steps.append(step)
                for prop_val in properties.values():
                    identifier_hints.update(self._identifier_tokens(prop_val))

            if not steps and not triggers:
                continue
            props = {
                "name": name,
                "file": str(path),
                "engine": "biztalk",
                "wf_kind": "biztalk_odx",
                "triggers": triggers,
                "steps": steps,
                "relationships": relationships,
                "datasource_tables": sorted(datastores),
                "service_dependencies": sorted(services),
                "process_calls": sorted(process_calls),
                "identifier_hints": sorted(identifier_hints),
            }
            signals.append(
                Signal(
                    kind="workflow",
                    props=props,
                    evidence=steps[0 : min(10, len(steps))],
                    subscores={"parsed": 0.8},
                )
            )
        return signals

    # --------------- Logic Apps Standard Workflow JSON ----------------

    def _extract_logicapps_workflow(self, path: Path) -> Iterable[Signal]:
        try:
            data = json.loads(path.read_text(encoding="utf-8"))
        except Exception as exc:
            return [
                Signal(
                    kind="doc",
                    props={"name": path.stem, "file": str(path), "note": f"workflow.json parse error: {exc}"},
                    evidence=[{"path": str(path), "lines": "1-1", "snippet": ""}],
                    subscores={"parsed": 0.1},
                )
            ]
        definition = data.get("definition") or data
        triggers_def = definition.get("triggers") or {}
        actions_def = definition.get("actions") or {}
        if not (triggers_def or actions_def):
            return []

        triggers = [self._logicapps_trigger(name, trig) for name, trig in triggers_def.items()]
        steps, relationships, datastores, services, process_calls, identifier_hints = self._logicapps_actions(actions_def)

        props = {
            "name": definition.get("metadata", {}).get("workflowName") or path.stem,
            "file": str(path),
            "engine": "logicapps_standard",
            "wf_kind": "logicapps_workflow",
            "triggers": [t for t in triggers if t],
            "steps": steps,
            "relationships": relationships,
            "datasource_tables": sorted(datastores),
            "service_dependencies": sorted(services),
            "process_calls": sorted(process_calls),
            "identifier_hints": sorted(identifier_hints),
        }
        return [Signal(kind="workflow", props=props, evidence=relationships[:10], subscores={"parsed": 0.9})]

    def _logicapps_trigger(self, name: str, trig: Dict[str, Any]) -> Dict[str, Any]:
        trig_type = trig.get("type") or trig.get("kind") or "trigger"
        inputs = trig.get("inputs") or {}
        method = (inputs.get("method") or inputs.get("schema", {}).get("method") or "POST").upper()
        path = inputs.get("path") or inputs.get("relativePath")
        return {"name": name, "type": trig_type, "method": method, "path": path, "evidence": {"path": "", "lines": "", "snippet": trig_type}}

    def _logicapps_actions(self, actions: Dict[str, Any]) -> Tuple[List[Dict[str, Any]], List[Dict[str, Any]], Set[str], Set[str], Set[str], Set[str]]:
        steps: List[Dict[str, Any]] = []
        relationships: List[Dict[str, Any]] = []
        datastores: Set[str] = set()
        services: Set[str] = set()
        process_calls: Set[str] = set()
        identifier_hints: Set[str] = set()
        for name, action in actions.items():
            action_type = (action.get("type") or "action").lower()
            inputs = action.get("inputs") or {}
            connector = action.get("kind") or action.get("type") or "action"
            step = {"name": name, "connector": connector, "evidence": {"path": "", "lines": "", "snippet": action_type}}
            if action_type == "workflow":
                workflow = action.get("workflow") or action.get("inputs", {}).get("workflow")
                if workflow:
                    process_calls.add(workflow)
                    relationships.append(self._relationship(source=name, kind="workflow", ref=workflow, detail=f"Run {workflow}", operation="calls"))
            if action_type in {"http", "apicall"} or inputs.get("uri"):
                uri = inputs.get("uri") or inputs.get("host", {}).get("connection", {}).get("name")
                services.add(uri or name)
                relationships.append(self._relationship(source=name, kind="http", ref=uri or name, detail=uri or name, operation="calls"))
            if action_type in {"apiconnection", "apiconnectionwebhook"}:
                path_segment = inputs.get("path") or inputs.get("relativePath")
                if path_segment:
                    identifier_hints.update(self._identifier_tokens(path_segment))
                host = inputs.get("host", {}).get("connection", {}).get("name")
                if host and any(key in host.lower() for key in ("sql", "dataverse", "sharepoint", "sqlserver")):
                    table = self._extract_table_from_path(path_segment)
                    if table:
                        datastores.add(table)
                        step["datasource_table"] = table
            steps.append(step)
        return steps, relationships, datastores, services, process_calls, identifier_hints

    # --------------- Durable orchestrations JSON ----------------

    def _extract_durable_json(self, path: Path) -> Iterable[Signal]:
        try:
            data = json.loads(path.read_text(encoding="utf-8"))
        except Exception as exc:
            return [
                Signal(
                    kind="doc",
                    props={"name": path.stem, "file": str(path), "note": f"durable.json parse error: {exc}"},
                    evidence=[{"path": str(path), "lines": "1-1", "snippet": ""}],
                    subscores={"parsed": 0.1},
                )
            ]
        orch_list = data.get("orchestrations") or []
        signals: List[Signal] = []
        for orch in orch_list:
            name = orch.get("name") or path.stem
            activities = orch.get("activities") or []
            triggers = orch.get("triggers") or ["DurableTrigger"]
            steps = [{"name": act.get("name"), "connector": act.get("type") or "activity", "evidence": {"path": str(path), "lines": "", "snippet": act.get("name")}} for act in activities]
            relationships = []
            datastores: Set[str] = set()
            services: Set[str] = set()
            for act in activities:
                target = act.get("target") or act.get("queue") or act.get("uri")
                if not target:
                    continue
                kind = "queue" if "queue" in (act.get("type") or "").lower() else "service"
                relationships.append(self._relationship(source=act.get("name") or "activity", kind=kind, ref=target, detail=target, operation="calls"))
                if kind == "queue":
                    services.add(target)
            props = {
                "name": name,
                "file": str(path),
                "engine": "durable_functions",
                "wf_kind": "durable_orchestration",
                "triggers": [{"name": t if isinstance(t, str) else t.get("name"), "type": "durable", "method": "event", "path": None, "evidence": {"path": str(path), "lines": "", "snippet": ""}} for t in triggers],
                "steps": steps,
                "relationships": relationships,
                "datasource_tables": sorted(datastores),
                "service_dependencies": sorted(services),
                "process_calls": [],
                "identifier_hints": [],
            }
            signals.append(Signal(kind="workflow", props=props, evidence=steps[:10], subscores={"parsed": 0.7}))
        return signals

    # --------------- helpers ---------------

    def _relationship(self, source: str, kind: str, ref: Optional[str], detail: str, operation: str) -> Dict[str, Any]:
        return {
            "id": f"biztalk_rel_{source}_{ref}",
            "source": {"type": "activity", "name": source},
            "target": {"kind": kind, "ref": ref, "display": ref},
            "operation": {"type": operation, "crud": "execute", "protocol": kind},
            "connector": kind,
            "direction": "outbound",
            "context": {"detail": detail},
            "roles": [],
            "evidence": [],
        }

    def _identifier_tokens(self, value: Optional[str]) -> Set[str]:
        tokens: Set[str] = set()
        if not value:
            return tokens
        for token in value.replace("/", " ").split():
            cleaned = token.strip("{}[]()\"'")
            if cleaned.lower().endswith("id") and len(cleaned) <= 64:
                tokens.add(cleaned)
        return tokens

    def _extract_table_from_path(self, path: Optional[str]) -> Optional[str]:
        if not path:
            return None
        segments = [seg for seg in path.split("/") if seg]
        for idx, seg in enumerate(segments):
            if seg.lower() in {"tables", "entities"} and idx + 1 < len(segments):
                return segments[idx + 1]
        return None

