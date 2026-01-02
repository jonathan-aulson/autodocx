from __future__ import annotations
from pathlib import Path
from typing import Iterable, List, Dict, Any, Optional, Tuple, Set
import hashlib
import json, yaml, re
from autodocx.types import Signal

CONNECTOR_ALIASES = {
    "shared_sql": "SQL Server",
    "sql": "SQL Server",
    "shared_commondataserviceforapps": "Dataverse",
    "shared_office365": "Office 365 Outlook",
    "shared_sharepointonline": "SharePoint Online",
    "shared_powerbi": "Power BI",
    "shared_azureblob": "Azure Blob Storage",
    "shared_http": "HTTP",
    "shared_hhttp": "HTTP",
    "shared_bs-5fsendmailbyserviceprincipal-5f4f4d8f3ae476f46d": "TownePark SendMail Service",
    "response": "Power App response",
    "http": "HTTP",
}

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

            conn_refs_root = self._normalize_connection_refs(
                doc.get("connectionReferences") or (doc.get("properties") or {}).get("connectionReferences")
            )

            # A: direct top-level triggers/actions
            if self._has_top_level_wdl(doc):
                return self._emit_from_definition(path, raw, doc, root_label="root", connection_refs=conn_refs_root)

            # B: top-level "definition"
            if isinstance(doc.get("definition"), dict) and self._has_top_level_wdl(doc["definition"]):
                return self._emit_from_definition(path, raw, doc["definition"], root_label="definition", connection_refs=conn_refs_root)

            # C: "properties.definition" (Power Automate export)
            if isinstance(doc.get("properties"), dict) and isinstance(doc["properties"].get("definition"), dict):
                d = doc["properties"]["definition"]
                if self._has_top_level_wdl(d):
                    refs = self._normalize_connection_refs((doc.get("properties") or {}).get("connectionReferences")) or conn_refs_root
                    return self._emit_from_definition(path, raw, d, root_label="properties.definition", connection_refs=refs)

            # D: ARM resources array
            if isinstance(doc.get("resources"), list):
                out: List[Signal] = []
                for res in doc["resources"]:
                    if not isinstance(res, dict): continue
                    if res.get("type") == "Microsoft.Logic/workflows":
                        definition = (res.get("properties") or {}).get("definition") or {}
                        refs = self._normalize_connection_refs((res.get("properties") or {}).get("connectionReferences")) or conn_refs_root
                        if isinstance(definition, dict) and self._has_top_level_wdl(definition):
                            out.extend(self._emit_from_definition(path, raw, definition, root_label="resources.definition", connection_refs=refs))
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

    def _normalize_connection_refs(self, value: Any) -> Dict[str, Any]:
        if isinstance(value, dict):
            return value
        return {}

    def _parse_definition(
        self,
        definition: dict,
        connection_refs: Optional[Dict[str, Any]] = None,
        source_path: Optional[Path] = None,
    ) -> Dict[str, Any]:
        connection_refs = connection_refs if isinstance(connection_refs, dict) else {}
        triggers = []
        for name, body in (definition.get("triggers") or {}).items():
            ttype = (body or {}).get("type")
            trig = {"name": name, "type": ttype}
            inputs = (body or {}).get("inputs") or {}
            params = inputs.get("parameters")
            if isinstance(params, dict):
                trig["parameter_keys"] = sorted(list(params.keys()))[:10]
            if ttype and ttype.lower() in ["request", "http"]:
                schema = inputs.get("schema") or {}
                props = sorted(list((schema.get("properties") or {}).keys())) if isinstance(schema, dict) else []
                if props:
                    trig["schema_props"] = props
            if ttype and ttype.lower() == "recurrence":
                rec = inputs.get("recurrence") or {}
                trig["schedule"] = {"frequency": rec.get("frequency"), "interval": rec.get("interval")}
            triggers.append(trig)

        actions_data = self._flatten_actions(
            definition.get("actions") or {},
            connection_refs=connection_refs,
            source_path=source_path,
        )
        relationships = actions_data["relationships"]
        steps = actions_data["steps"]
        calls_flows = actions_data["calls_flows"]
        relationships.extend(self._trigger_action_relationships(triggers, steps, source_path=source_path))
        identifier_hints = set(actions_data.get("identifier_hints") or [])
        identifier_hints.update(self._identifier_tokens_from_triggers(triggers))
        return {
            "triggers": triggers,
            "steps": steps,
            "calls_flows": sorted(set(calls_flows)),
            "relationships": relationships,
            "control_edges": actions_data["control_edges"],
            "datasource_tables": sorted(actions_data.get("datastores") or []),
            "service_dependencies": sorted(actions_data.get("services") or []),
            "identifier_hints": sorted(identifier_hints),
            "process_calls": sorted(actions_data.get("process_calls") or []),
        }

    def _emit_from_definition(self, path: Path, raw: str, definition: dict, root_label: str, connection_refs: Optional[Dict[str, Any]] = None) -> List[Signal]:
        wf_kind = self._guess_kind(raw, path, is_arm=False)
        engine = "logicapps" if "logicapps" in wf_kind else "power_automate"
        name = self._name_from_path(path)
        parsed = self._parse_definition(definition, connection_refs=connection_refs, source_path=path)
        parsed.update(self._augment_workflow_props(name, parsed))
        content_version = definition.get("contentVersion") or "" 
        return [
            Signal(
                kind="workflow",
                props={
                    "name": name,
                    "file": str(path),
                    "engine": engine,
                    "wf_kind": wf_kind,
                    "version": content_version,
                    **parsed,
                },
                evidence=[f"{path}:{root_label}"],
                subscores={"parsed": 1.0, "schema_evidence": 0.4 if parsed.get("triggers") else 0.1},
            )
        ]

    def _name_from_path(self, path: Path) -> str:
        parts_lower = [p.lower() for p in path.parts]
        if "workflows" in parts_lower:
            try:
                idx = parts_lower.index("workflows")
                return path.parts[idx + 1]
            except Exception:
                pass
        return path.stem

    # ---- relationship helpers ----

    def _relationships_from_action(
        self,
        step: Dict[str, Any],
        inputs: Dict[str, Any],
        source_path: Optional[Path],
        connection_refs: Optional[Dict[str, Any]],
    ) -> List[Dict[str, Any]]:
        rels: List[Dict[str, Any]] = []
        target_kind, target_ref, target_display, context = self._infer_target_details(step, inputs, connection_refs or {})
        connector = step.get("connector") or ""
        detail = step.get("operation_detail")
        if target_kind and target_ref:
            rels.append(
                self._format_relationship(
                    rel_id=self._relationship_id(step.get("name"), target_kind, target_ref),
                    source_type="action",
                    source_name=step.get("name"),
                    target_kind=target_kind,
                    target_ref=target_ref,
                    target_display=target_display,
                    method=step.get("method"),
                    connector=connector,
                    direction="outbound",
                    context=context,
                    auth=self._auth_block(step, connection_refs or {}),
                    evidence=self._relationship_evidence(source_path, step.get("name")),
                    detail=detail,
                )
            )

        if step.get("child_workflow_uri"):
            rels.append(
                self._format_relationship(
                    rel_id=self._relationship_id(step.get("name"), "workflow", step["child_workflow_uri"]),
                    source_type="action",
                    source_name=step.get("name"),
                    target_kind="workflow",
                    target_ref=step["child_workflow_uri"],
                    target_display=step["child_workflow_uri"],
                    method="POST",
                    connector=connector or "http",
                    direction="outbound",
                    context={"child_workflow": step["child_workflow_uri"]},
                    auth=self._auth_block(step, connection_refs or {}),
                    evidence=self._relationship_evidence(source_path, step.get("name")),
                    detail=detail or f"Invoke {step['child_workflow_uri']}",
                )
        )
        return rels

    # ---- control-aware helpers ----

    def _flatten_actions(
        self,
        actions: Dict[str, Any],
        connection_refs: Optional[Dict[str, Any]],
        source_path: Optional[Path],
        parent: Optional[str] = None,
        branch: Optional[str] = None,
    ) -> Dict[str, List[Any]]:
        flat_steps: List[Dict[str, Any]] = []
        relationships: List[Dict[str, Any]] = []
        control_edges: List[Dict[str, Any]] = []
        calls_flows: List[str] = []
        datastores: Set[str] = set()
        services: Set[str] = set()
        identifier_hints: Set[str] = set()
        process_calls: Set[str] = set()

        for name, node in (actions or {}).items():
            (
                info,
                rels,
                child_flow,
                step_datastores,
                step_services,
                step_identifiers,
                step_process_refs,
            ) = self._build_step_info(
                name, node, connection_refs=connection_refs, source_path=source_path
            )
            info["parent_step"] = parent
            info["branch"] = branch
            flat_steps.append(info)
            relationships.extend(rels)
            if child_flow:
                calls_flows.append(child_flow)
                process_calls.add(child_flow)
            datastores.update(step_datastores)
            services.update(step_services)
            identifier_hints.update(step_identifiers)
            process_calls.update(step_process_refs)

            branches = self._control_branches_for_action(node, info.get("control_type"))
            if branches:
                for label, child_actions in branches.items():
                    control_edges.append({
                        "parent": name,
                        "branch": label,
                        "children": list(child_actions.keys()),
                    })
                    nested = self._flatten_actions(
                        child_actions,
                        connection_refs=connection_refs,
                        source_path=source_path,
                        parent=name,
                        branch=label,
                    )
                    flat_steps.extend(nested["steps"])
                    relationships.extend(nested["relationships"])
                    control_edges.extend(nested["control_edges"])
                    calls_flows.extend(nested["calls_flows"])
                    datastores.update(nested.get("datastores") or [])
                    services.update(nested.get("services") or [])
                    identifier_hints.update(nested.get("identifier_hints") or [])
                    process_calls.update(nested.get("process_calls") or [])

        return {
            "steps": flat_steps,
            "relationships": relationships,
            "control_edges": control_edges,
            "calls_flows": calls_flows,
            "datastores": sorted(datastores),
            "services": sorted(services),
            "identifier_hints": sorted(identifier_hints),
            "process_calls": sorted(process_calls),
        }

    def _build_step_info(
        self,
        aname: str,
        node: Dict[str, Any],
        connection_refs: Optional[Dict[str, Any]],
        source_path: Optional[Path],
    ) -> Tuple[
        Dict[str, Any],
        List[Dict[str, Any]],
        Optional[str],
        Set[str],
        Set[str],
        Set[str],
        Set[str],
    ]:
        atype = (node or {}).get("type")
        inputs = (node or {}).get("inputs") or {}
        if not isinstance(inputs, dict):
            inputs = {}
        host = inputs.get("host") or {}
        connection_name = host.get("connectionName")
        api_id = host.get("apiId")
        info = {
            "name": aname,
            "type": atype,
            "connector": None,
            "method": None,
            "url_or_path": None,
            "inputs_keys": [],
            "run_after": sorted((node.get("runAfter") or {}).keys()),
            "connection_name": connection_name,
            "child_workflow_uri": None,
            "control_type": atype if self._is_control_action(atype) else None,
            "control_expression": node.get("expression"),
            "kind": node.get("kind"),
        }
        conn_meta = self._resolve_connection_metadata(connection_name, connection_refs or {}, host)
        param_keys = self._collect_object_keys(inputs.get("parameters"))
        body_fields = self._collect_object_keys(inputs.get("body"))
        schema_props = self._collect_schema_props(inputs.get("schema"))
        info["parameter_keys"] = param_keys
        info["body_fields"] = body_fields
        info["schema_properties"] = schema_props
        info["connection_display"] = conn_meta.get("display")
        info["connection_logical_name"] = conn_meta.get("logical_name")
        info["api_display"] = conn_meta.get("api_display")
        info["host_api_id"] = conn_meta.get("api_id")

        calls_flow = None
        step_datastores: Set[str] = set()
        step_services: Set[str] = set()
        step_identifier_hints: Set[str] = set()
        step_process_refs: Set[str] = set()

        params = inputs.get("parameters") if isinstance(inputs.get("parameters"), dict) else {}
        table_candidate = (
            params.get("entityName")
            or params.get("table")
            or params.get("dataset")
            or inputs.get("table")
            or inputs.get("entityName")
            or inputs.get("dataset")
        )
        if isinstance(table_candidate, str):
            info["datasource_table"] = table_candidate

        if atype in ["OpenApiConnection", "ApiConnection", "ApiConnectionWebhook", "ServiceProvider"]:
            info["connector"] = (((inputs.get("host") or {}).get("connection") or {}).get("name") or "").strip()
            info["method"] = (inputs.get("method") or "").upper()
            info["url_or_path"] = inputs.get("path")
            if isinstance(inputs.get("body"), dict):
                info["inputs_keys"] = sorted(list(inputs["body"].keys()))
            if not info.get("datasource_table"):
                info["datasource_table"] = (inputs.get("body") or {}).get("table") or params.get("table")

        if atype in ["Http", "HttpWebhook"]:
            info["connector"] = "http"
            info["method"] = (inputs.get("method") or "").upper()
            info["url_or_path"] = inputs.get("uri")
            if isinstance(inputs.get("body"), dict):
                info["inputs_keys"] = sorted(list(inputs["body"].keys()))
            uri = inputs.get("uri") or ""
            if "/workflows/" in (uri or "") and "/triggers/" in (uri or "") and "/run" in (uri or ""):
                calls_flow = uri
                info["child_workflow_uri"] = uri
            if uri:
                step_services.add(uri)
                info["service_dependency"] = uri

        if not info["connector"]:
            resolved = None
            if connection_refs and connection_name and connection_name in connection_refs:
                ref = connection_refs.get(connection_name) or {}
                api_info = ref.get("api") or {}
                resolved = api_info.get("logicalName") or api_info.get("name")
            if not resolved and connection_name:
                resolved = connection_name
            if not resolved and api_id:
                resolved = api_id.split("/")[-1]
            if resolved:
                info["connector"] = resolved
        if not info["connector"] and atype:
            info["connector"] = atype

        friendly, op_detail = self._build_action_summary(
            info,
            conn_meta,
            body_fields,
            schema_props,
            param_keys,
        )
        if friendly:
            info["friendly_display"] = friendly
        if op_detail:
            info["operation_detail"] = op_detail

        rels = self._relationships_from_action(info, inputs, source_path=source_path, connection_refs=connection_refs)
        step_datastores.update(self._datastores_from_step(info, inputs, rels))
        step_services.update(self._services_from_step(info, inputs, rels))
        step_identifier_hints.update(self._identifier_tokens_from_list(body_fields))
        step_identifier_hints.update(self._identifier_tokens_from_list(schema_props))
        step_identifier_hints.update(self._identifier_tokens_from_list(info.get("inputs_keys") or []))
        for rel in rels:
            target_kind = (rel.get("target") or {}).get("kind")
            display = (rel.get("target") or {}).get("display") or (rel.get("target") or {}).get("ref")
            if target_kind in {"workflow", "process"} and display:
                step_process_refs.add(display)
        return info, rels, calls_flow, step_datastores, step_services, step_identifier_hints, step_process_refs

    def _is_control_action(self, atype: Optional[str]) -> bool:
        if not atype:
            return False
        return atype in {"If", "Switch", "Foreach", "Scope", "Until", "Parallel", "ParallelBranch"}

    def _control_branches_for_action(self, node: Dict[str, Any], control_type: Optional[str]) -> Dict[str, Dict[str, Any]]:
        if not control_type:
            return {}
        control_type = control_type or ""
        branches: Dict[str, Dict[str, Any]] = {}
        base_actions = (node.get("actions") or {}) if isinstance(node.get("actions"), dict) else {}
        if control_type == "If":
            if base_actions:
                branches["If.True"] = base_actions
            else_actions = ((node.get("else") or {}).get("actions") or {})
            if else_actions:
                branches["If.False"] = else_actions
        elif control_type == "Switch":
            for case_name, case_body in (node.get("cases") or {}).items():
                case_actions = (case_body or {}).get("actions") or {}
                if case_actions:
                    branches[f"Switch.{case_name}"] = case_actions
            default_actions = ((node.get("default") or {}).get("actions") or {})
            if default_actions:
                branches["Switch.Default"] = default_actions
        elif control_type in {"Foreach", "Parallel"}:
            if base_actions:
                branches[f"{control_type}.Body"] = base_actions
        elif control_type in {"Scope", "Until"}:
            if base_actions:
                branches[f"{control_type}.Body"] = base_actions
        elif control_type == "ParallelBranch":
            for idx, branch_actions in enumerate(base_actions.get("branches") or []):
                if isinstance(branch_actions, dict):
                    actions = branch_actions.get("actions") or {}
                    branches[f"Parallel.Branch{idx+1}"] = actions
        else:
            if base_actions:
                branches["Control.Body"] = base_actions
        return branches

    def _resolve_connection_metadata(
        self,
        connection_name: Optional[str],
        connection_refs: Dict[str, Any],
        host: Dict[str, Any],
    ) -> Dict[str, Any]:
        ref = {}
        if connection_name:
            ref = (connection_refs or {}).get(connection_name) or {}
        api_meta = ref.get("api") or {}
        connection_meta = ref.get("connection") or {}
        display = (ref.get("displayName") or ref.get("apiDisplayName") or api_meta.get("displayName") or "").strip()
        api_name = api_meta.get("name") or ""
        if not display and api_name:
            display = self._connector_alias(api_name)
        if not display and connection_name:
            display = self._connector_alias(connection_name)
        logical_name = connection_meta.get("connectionReferenceLogicalName")
        api_id = (host or {}).get("apiId")
        op_id = (host or {}).get("operationId")
        api_display = display or self._connector_alias(api_name)
        return {
            "display": display,
            "logical_name": logical_name,
            "api_id": api_id,
            "operation_id": op_id,
            "api_display": api_display,
        }

    def _collect_object_keys(self, value: Any) -> List[str]:
        if isinstance(value, dict):
            keys = [str(k) for k in value.keys()]
            return sorted(keys)
        return []

    def _collect_schema_props(self, schema: Any) -> List[str]:
        if not isinstance(schema, dict):
            return []
        props = schema.get("properties")
        if isinstance(props, dict):
            return sorted([str(k) for k in props.keys()])
        return []

    def _clean_label(self, raw: Optional[str]) -> str:
        if not raw:
            return ""
        text = raw.replace("_", " ").replace("-", " ").replace(".", " ")
        text = re.sub(r"\s{2,}", " ", text)
        return text.strip().title()

    def _connector_alias(self, connector: Optional[str]) -> str:
        if not connector:
            return ""
        base = connector.lower()
        if base in CONNECTOR_ALIASES:
            return CONNECTOR_ALIASES[base]
        if connector.startswith("shared_"):
            return connector.replace("shared_", "").replace("_", " ").title()
        return connector.replace("_", " ").title()

    def _build_action_summary(
        self,
        info: Dict[str, Any],
        conn_meta: Dict[str, Any],
        body_fields: List[str],
        schema_props: List[str],
        param_keys: List[str],
    ) -> (str, str):
        action_label = self._clean_label(info.get("name") or info.get("type") or "Step")
        target_label = (
            conn_meta.get("display")
            or conn_meta.get("api_display")
            or self._connector_alias(info.get("connector"))
            or self._clean_label(info.get("kind"))
        )
        method = (info.get("method") or "").upper()
        friendly = action_label
        if target_label and target_label.lower() not in action_label.lower():
            friendly = f"{action_label} → {target_label}"
        if method and method not in friendly.upper():
            friendly = f"{method} {friendly}"

        detail_bits: List[str] = []
        url = info.get("url_or_path")
        if url:
            detail_bits.append(url)
        fields = schema_props or body_fields or param_keys or info.get("inputs_keys") or []
        if fields:
            detail_bits.append("fields {" + ", ".join(fields[:4]) + "}")
        op_id = conn_meta.get("operation_id")
        if op_id and op_id not in detail_bits:
            detail_bits.append(op_id)
        operation_detail = " – ".join([bit for bit in detail_bits if bit])
        return friendly.strip(), operation_detail.strip()

    # ---- narrative helpers ----

    def _augment_workflow_props(self, name: str, parsed: Dict[str, Any]) -> Dict[str, Any]:
        triggers = parsed.get("triggers") or []
        steps = parsed.get("steps") or []
        additions: Dict[str, Any] = {}
        user_story = self._build_user_story(name, triggers, steps)
        if user_story:
            additions["user_story"] = user_story
        inputs_example = self._build_inputs_example(triggers, steps)
        if inputs_example:
            additions["inputs_example"] = inputs_example
        outputs_example = self._build_outputs_example(steps)
        if outputs_example:
            additions["outputs_example"] = outputs_example
        latency = self._build_latency_hint(triggers)
        if latency:
            additions["latency_hints"] = latency
        step_labels = self._collect_step_display_names(steps)
        if step_labels:
            additions["step_display_names"] = step_labels
            additions["step_display_name"] = " -> ".join(step_labels[:6])
        journey = self._build_touchpoints_summary(step_labels)
        if journey:
            additions["journey_touchpoints"] = journey
        return additions

    def _build_user_story(self, name: str, triggers: List[Dict[str, Any]], steps: List[Dict[str, Any]]) -> str:
        if not triggers and not steps:
            return ""
        trig_parts = []
        for trig in triggers:
            ttype = (trig.get("type") or "").lower()
            if ttype in {"request", "http"}:
                trig_parts.append("an HTTP request is received")
            elif ttype == "recurrence":
                trig_parts.append("a scheduled timer fires")
            elif ttype:
                trig_parts.append(f"{ttype} events occur")
        if not trig_parts:
            trig_parts.append("the workflow is invoked")
        step_actions = []
        for step in steps[:4]:
            connector = (step.get("connector") or step.get("type") or "").replace("_", " ")
            label = step.get("name") or connector or "a step"
            if connector:
                step_actions.append(f"{label} via {connector}")
            else:
                step_actions.append(label)
        if not step_actions:
            step_actions.append("processes the payload")
        return f"When {', '.join(trig_parts)}, the {name} workflow {', then '.join(step_actions)}."

    def _build_inputs_example(self, triggers: List[Dict[str, Any]], steps: List[Dict[str, Any]]) -> Dict[str, Any]:
        for trig in triggers:
            schema_props = trig.get("schema_props")
            if schema_props:
                return {
                    "trigger": trig.get("name") or trig.get("type"),
                    "fields": schema_props[:10],
                }
            param_keys = trig.get("parameter_keys")
            if param_keys:
                return {
                    "trigger": trig.get("name") or trig.get("type"),
                    "fields": param_keys[:10],
                }
        for step in steps:
            keys = step.get("inputs_keys")
            if keys:
                return {
                    "step": step.get("name") or step.get("connector"),
                    "fields": keys[:10],
                }
        return {}

    def _build_outputs_example(self, steps: List[Dict[str, Any]]) -> Dict[str, Any]:
        candidates = []
        for step in steps:
            connector = (step.get("connector") or "").lower()
            if connector in {"response", "http", "apim"} or "response" in (step.get("name") or "").lower():
                candidates.append(step.get("name") or connector)
        if not candidates:
            return {}
        return {"responses": candidates[:5]}

    def _datastores_from_step(self, step: Dict[str, Any], inputs: Dict[str, Any], relationships: List[Dict[str, Any]]) -> Set[str]:
        targets: Set[str] = set()
        connector = (step.get("connector") or "").lower()
        candidate = step.get("datasource_table") or inputs.get("table") or inputs.get("entityName")
        if any(key in connector for key in ["sql", "dataverse", "commondataservice", "sharepoint", "azureblob", "cosmos"]):
            if candidate:
                targets.add(candidate)
        for rel in relationships:
            target = (rel.get("target") or {}).get("display")
            kind = (rel.get("target") or {}).get("kind")
            if kind in {"sql", "dataverse", "sharepoint"} and target:
                targets.add(target)
        return targets

    def _services_from_step(self, step: Dict[str, Any], inputs: Dict[str, Any], relationships: List[Dict[str, Any]]) -> Set[str]:
        services: Set[str] = set()
        url = step.get("url_or_path") or inputs.get("uri")
        if url and url.startswith("http"):
            services.add(url)
        for rel in relationships:
            target = (rel.get("target") or {}).get("display")
            kind = (rel.get("target") or {}).get("kind")
            if kind in {"http", "service", "queue", "servicebus"} and target:
                services.add(target)
        return services

    def _identifier_tokens_from_list(self, values: List[str]) -> Set[str]:
        tokens: Set[str] = set()
        for value in values or []:
            if not value:
                continue
            for token in re.findall(r"[A-Za-z0-9_]+", str(value)):
                lower = token.lower()
                if lower.endswith(("id", "key", "code", "number")):
                    tokens.add(token)
        return tokens

    def _identifier_tokens_from_triggers(self, triggers: List[Dict[str, Any]]) -> Set[str]:
        hints: Set[str] = set()
        for trig in triggers:
            hints.update(self._identifier_tokens_from_list(trig.get("schema_props") or []))
            hints.update(self._identifier_tokens_from_list(trig.get("parameter_keys") or []))
        return hints

    def _build_latency_hint(self, triggers: List[Dict[str, Any]]) -> Dict[str, Any]:
        for trig in triggers:
            sched = trig.get("schedule")
            if sched:
                return {
                    "frequency": sched.get("frequency"),
                    "interval": sched.get("interval"),
                }
        return {}

    def _collect_step_display_names(self, steps: List[Dict[str, Any]]) -> List[str]:
        labels: List[str] = []
        for step in steps:
            name = step.get("name")
            connector = step.get("connector") or step.get("type")
            if name and connector:
                labels.append(f"{name} ({connector})")
            elif name:
                labels.append(name)
            elif connector:
                labels.append(connector)
        return labels[:20]

    def _build_touchpoints_summary(self, labels: List[str]) -> List[str]:
        if not labels:
            return []
        limit = 12
        if len(labels) <= limit:
            return labels
        return labels[:limit]

    def _trigger_action_relationships(
        self, triggers: List[Dict[str, Any]], steps: List[Dict[str, Any]], source_path: Optional[Path]
    ) -> List[Dict[str, Any]]:
        starters = [s for s in steps if not (s.get("run_after") or [])]
        rels: List[Dict[str, Any]] = []
        if not starters:
            return rels
        for trig in triggers or []:
            for step in starters:
                rels.append(
                    self._format_relationship(
                        rel_id=self._relationship_id(trig.get("name") or trig.get("type"), "action", step.get("name")),
                        source_type="trigger",
                        source_name=trig.get("name") or trig.get("type"),
                        target_kind="action",
                        target_ref=step.get("name"),
                        target_display=step.get("name"),
                        method=None,
                        connector=trig.get("type") or "trigger",
                        direction="inbound",
                        context={"trigger_type": trig.get("type")},
                        auth={},
                        evidence=self._relationship_evidence(source_path, f"trigger:{trig.get('name')}"),
                    )
                )
        return rels

    def _infer_target_details(
        self,
        step: Dict[str, Any],
        inputs: Dict[str, Any],
        connection_refs: Dict[str, Any],
    ) -> Tuple[Optional[str], Optional[str], str, Dict[str, Any]]:
        connector = (step.get("connector") or "").lower()
        uri = (step.get("url_or_path") or inputs.get("uri") or inputs.get("path") or "") or ""
        params = inputs.get("parameters") if isinstance(inputs.get("parameters"), dict) else {}
        body = inputs.get("body") if isinstance(inputs.get("body"), dict) else {}
        context: Dict[str, Any] = {}
        display = ""

        if uri and isinstance(uri, str) and uri.startswith("http"):
            context["url_or_resource"] = uri
            display = uri.split("?")[0]
            return "http", uri, display, context

        if connector in {"http", "shared_http", "apim", "apimmanaged"}:
            context["url_or_resource"] = uri or connector
            display = (uri or step.get("name") or connector)
            return "http", (uri or connector), display, context

        if "commondataservice" in connector or "dataverse" in connector:
            table = params.get("entityName") or params.get("table") or body.get("table") or uri or connector
            context["table"] = table
            display = table
            return "dataverse", table, display, context

        if "sharepoint" in connector:
            dataset = params.get("dataset") or params.get("site") or uri or connector
            context["site"] = dataset
            display = dataset
            return "sharepoint", dataset, display, context

        if "sql" in connector:
            proc = body.get("storedProcedure") or body.get("procedure") or params.get("procedure") or params.get("table") or uri or connector
            context["operation"] = body.get("operation") or ""
            context["table"] = params.get("table")
            display = proc
            return "sql", proc, display, context

        if connector.startswith("shared_"):
            display = connector
            return connector.replace("shared_", ""), connector, display, context

        if uri:
            display = uri
            context["url_or_resource"] = uri
            return "http", uri, display, context

        if connector:
            display = connector
            return connector, connector, display, context

        return None, None, "", context

    def _format_relationship(
        self,
        *,
        rel_id: str,
        source_type: str,
        source_name: Optional[str],
        target_kind: Optional[str],
        target_ref: Optional[str],
        target_display: Optional[str],
        method: Optional[str],
        connector: str,
        direction: str,
        context: Optional[Dict[str, Any]],
        auth: Dict[str, Any],
        evidence: List[str],
        detail: Optional[str] = None,
    ) -> Dict[str, Any]:
        op_type = self._infer_operation_type(target_kind, source_name, method)
        rel = {
            "id": rel_id,
            "source": {"type": source_type, "name": source_name, "step_id": source_name},
            "target": {"kind": target_kind, "ref": target_ref, "display": target_display or target_ref},
            "operation": {
                "type": op_type,
                "verb": method or "",
                "crud": self._crud_from_operation(op_type),
                "protocol": self._protocol_for_kind(target_kind),
            },
            "connector": connector,
            "direction": direction,
            "context": self._clean_context(context),
            "roles": self._roles_for_relationship(target_kind, op_type),
            "evidence": evidence,
            "confidence": 0.9,
        }
        if detail:
            rel["operation"]["detail"] = detail
        if auth:
            rel["auth"] = auth
        return rel

    def _auth_block(self, step: Dict[str, Any], connection_refs: Dict[str, Any]) -> Dict[str, Any]:
        block: Dict[str, Any] = {}
        conn_name = step.get("connection_name")
        if conn_name:
            block["connection_reference"] = conn_name
            ref_meta = (connection_refs or {}).get(conn_name) or {}
            display = (ref_meta.get("displayName") or ref_meta.get("apiDisplayName") or "").strip()
            if not display:
                display = step.get("connection_display") or self._connector_alias(conn_name)
            if display:
                block["connection_display_name"] = display
        return block

    def _relationship_id(self, source: Optional[str], target_kind: Optional[str], target_ref: Optional[str]) -> str:
        base = f"{source or 'rel'}::{target_kind or 'unknown'}::{target_ref or ''}"
        digest = hashlib.sha1(base.encode("utf-8")).hexdigest()[:8]
        clean_source = re.sub(r"[^a-z0-9]+", "_", (source or "step").lower()).strip("_") or "step"
        clean_kind = re.sub(r"[^a-z0-9]+", "_", (target_kind or "target").lower()).strip("_") or "target"
        return f"{clean_source}_{clean_kind}_{digest}"

    def _relationship_evidence(self, source_path: Optional[Path], label: Optional[str]) -> List[str]:
        if not source_path:
            return []
        return [f"{source_path}:{label or 'step'}"]

    def _infer_operation_type(self, target_kind: Optional[str], source_name: Optional[str], method: Optional[str]) -> str:
        if target_kind in {"workflow"}:
            return "invokes"
        if target_kind in {"http"}:
            return "calls"
        if target_kind in {"sql", "dataverse", "sharepoint"}:
            name = (source_name or "").lower()
            if any(k in name for k in ["create", "insert", "add", "update", "patch", "set", "write"]):
                return "writes"
            if any(k in name for k in ["delete", "remove", "drop"]):
                return "deletes"
            return "reads"
        if method and method.upper() == "GET":
            return "reads"
        if method and method.upper() in {"POST", "PUT", "PATCH"}:
            return "writes"
        return "calls"

    def _crud_from_operation(self, op_type: str) -> str:
        return {
            "reads": "read",
            "writes": "update",
            "deletes": "delete",
            "invokes": "execute",
            "calls": "execute",
        }.get(op_type, "")

    def _protocol_for_kind(self, target_kind: Optional[str]) -> str:
        return {
            "http": "https",
            "workflow": "https",
            "sql": "sql",
            "dataverse": "dataverse",
            "sharepoint": "sharepoint",
        }.get(target_kind or "", target_kind or "")

    def _roles_for_relationship(self, target_kind: Optional[str], op_type: str) -> List[str]:
        if target_kind in {"sql", "dataverse", "sharepoint"}:
            if op_type == "reads":
                return ["data.reads"]
            if op_type in {"writes", "deletes"}:
                return ["data.mutates"]
        if target_kind == "http":
            return ["interface.calls"]
        if target_kind == "workflow":
            return ["workflow.invokes"]
        if target_kind == "action":
            return ["workflow.triggers"]
        return []

    def _clean_context(self, ctx: Optional[Dict[str, Any]]) -> Dict[str, Any]:
        cleaned = {}
        for key, value in (ctx or {}).items():
            if value not in (None, "", []):
                cleaned[key] = value
        return cleaned
