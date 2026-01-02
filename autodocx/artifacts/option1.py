
from __future__ import annotations
from collections import defaultdict
from pathlib import Path
from typing import Dict, Any, Iterable
from autodocx.types import Signal
from autodocx.utils.components import derive_component
from autodocx.artifacts.experience_packs import build_experience_pack

# -------- helpers --------

def _lang_for(path: str) -> str:
    p = (path or "").lower()
    if p.endswith((".yaml", ".yml")): return "yaml"
    if p.endswith(".json"): return "json"
    if p.endswith(".tf"): return "hcl2"
    if p.endswith(".sql"): return "sql"
    if p.endswith(".md") or p.endswith(".markdown"): return "markdown"
    if p.endswith(".js"): return "javascript"
    if p.endswith(".ts"): return "typescript"
    if p.endswith(".bicep"): return "bicep"
    if p.endswith(".cs"): return "csharp"
    return "unknown"

def _relationship_matrix(relationships: Any) -> Dict[str, Dict[str, int]]:
    matrix: Dict[str, Dict[str, int]] = defaultdict(lambda: defaultdict(int))
    for rel in relationships or []:
        target = ((rel or {}).get("target") or {}).get("kind") or "unknown"
        op = ((rel or {}).get("operation") or {}).get("type") or "unknown"
        matrix[target][op] += 1
    return {k: dict(v) for k, v in matrix.items()}


def _apply_narrative_sections(base: Dict[str, Any], props: Dict[str, Any]) -> None:
    story = props.get("user_story")
    if story:
        journeys = base.setdefault("primary_journeys", [])
        if not any(j.get("story") == story for j in journeys):
            journeys.append({"story": story, "evidence": props.get("file", "")})
    touchpoints = props.get("journey_touchpoints")
    if touchpoints:
        summaries = base.setdefault("ux_summaries", [])
        if not any(s.get("touchpoints") == touchpoints for s in summaries):
            summaries.append({"touchpoints": touchpoints, "evidence": props.get("file", "")})
    roles = props.get("roles") or []
    for role in roles[:2]:
        base.setdefault("personas", []).append({"name": role, "goals": story or "", "evidence": props.get("file", "")})
    if props.get("inputs_example"):
        base.setdefault("data_examples", []).append({"inputs": props["inputs_example"]})
    if props.get("outputs_example"):
        base.setdefault("data_examples", []).append({"outputs": props["outputs_example"]})
    if props.get("data_samples"):
        base.setdefault("data_examples", []).extend(props["data_samples"])


def _extend_unique(target: List[Any], values: Iterable[Any]) -> None:
    for value in values or []:
        if not value:
            continue
        if value not in target:
            target.append(value)


def _apply_dependency_hints(base: Dict[str, Any], props: Dict[str, Any]) -> None:
    datastores = props.get("datasource_tables") or props.get("datastores") or []
    services = props.get("service_dependencies") or []
    processes = props.get("process_calls") or props.get("calls_flows") or []
    identifiers = props.get("identifier_hints") or []
    _extend_unique(base["dependencies"]["datastores"], datastores)
    _extend_unique(base["dependencies"]["external_services"], services)
    if processes:
        slots = base.setdefault("process_dependencies", [])
        _extend_unique(slots, processes)
    if identifiers:
        identifier_block = base.setdefault("data", {}).setdefault("identifier_fields", [])
        _extend_unique(identifier_block, identifiers)

# -------- main mapper --------

def to_option1_artifact(signal: Signal, repo_root: Path) -> Dict[str, Any]:
    # Always get props and repo_path first; guard against weird plugins
    props: Dict[str, Any] = signal.props if isinstance(signal.props, dict) else {}
    repo_path: str = str(props.get("file") or "")

    # Component/service inferred from first path segment beneath scan root
    component = derive_component(repo_root, props)

    # Base skeleton per universal schema
    base: Dict[str, Any] = {
        "artifact_type": "other",
        "name": props.get("name") or signal.kind,
        "description": "",
        "repo_path": repo_path,
        "language_or_format": _lang_for(repo_path),
        "component_or_service": component,
        "version": props.get("version",""),
        "capabilities": [],
        "entry_points": [],
        "interfaces": {
            "http_endpoints": [],
            "grpc_services": [],
            "graphql": {"queries": [], "mutations": [], "subscriptions": [], "evidence": ""},
            "events": [],
        },
        "workflows": [],
        "data": {
            "schemas": [],
            "tables_or_collections": [],
            "pii_categories": [],
        },
        "infrastructure": {
            "cloud": "",
            "regions": [],
            "k8s": {"namespaces": [], "deployments": []},
            "serverless": [],
            "terraform_modules": [],
        },
        "build_and_deploy": {
            "ci": [],
            "artifacts": [],
            "environments": [],
        },
        "security": {"auth": [], "secrets": [], "compliance": []},
        "observability": {"metrics": [], "logs": [], "alerts": [], "dashboards": []},
        "dependencies": {"internal_services": [], "external_services": [], "datastores": []},
        "operations": {"slo_sla": {"availability": "", "latency": ""}, "runbooks": []},
        "risk_and_gaps": [],
        "assumptions": [],
        "ui_components": [],
        "integrations": [],
        "process_diagrams": [],
        "business_entities": [],
        "code_entities": [],
        "personas": [],
        "primary_journeys": [],
        "ux_summaries": [],
        "before_after": [],
        "screenshots": [],
        "experience_pack": {},
        "data_examples": [],
        "confidence": round(
            min(1.0, float(signal.subscores.get("parsed", 0.0)) + 0.5 * float(signal.subscores.get("schema_evidence", 0.0))),
            3,
        ),
        "evidence": [{"path": repo_path, "lines": "", "snippet": ""}] if repo_path else [],
    }
    _apply_narrative_sections(base, props)

    # Optional fallback for OpenAPI spec-only version
    if not base["version"] and "spec_version" in props:
        base["version"] = props["spec_version"]

    relationships = props.get("relationships") or []
    if relationships:
        base["relationships"] = relationships
        base["relationship_matrix"] = _relationship_matrix(relationships)
        base["capabilities"].append("documents dependencies")

    k = signal.kind

    # ---- API spec (OpenAPI/Swagger) ----
    if k == "api":
        base["artifact_type"] = "api_spec"
        base["capabilities"].append("exposes REST API")
        servers = props.get("servers") or []
        if servers:
            base["entry_points"].append({"type": "http", "value": ",".join(servers), "evidence": f"{repo_path}:1-50"})

    # ---- Operations / routes ----
    elif k in ["op", "route"]:
        base["artifact_type"] = "route_code"
        method = props.get("method", "")
        pathv = props.get("path", "")
        base["interfaces"]["http_endpoints"].append({
            "method": method,
            "path": pathv,
            "summary": props.get("summary", ""),
            "auth": "none",
            "request_schema_ref": "",
            "response_schema_ref": "",
            "status_codes": [],
            "evidence": f"{repo_path}:ref",
        })
        base["capabilities"].append("exposes REST API")

    # ---- Workflows (Logic Apps / Power Automate / Tibco BW etc.) ----
    elif k == "workflow":
        base["artifact_type"] = "workflow_dag"
        base["capabilities"].append("orchestrates workflow")

        # Cloud hint for Logic Apps / Power Automate
        wf_kind = (props.get("wf_kind") or "").lower()
        engine = (props.get("engine") or "").lower()
        if wf_kind in {"logicapps_standard", "logicapps_consumption", "power_automate", "power_automate_desktop"} or \
           engine in {"logicapps", "logicapps/powerautomate"}:
            base["infrastructure"]["cloud"] = "azure"

        # Entry points from triggers
        for t in props.get("triggers") or []:
            ttype = (t.get("type") or "").lower()
            if ttype in ["request", "http", "httpwebhook"]:
                base["entry_points"].append({"type": "http", "value": t.get("name"), "evidence": f"{repo_path}:triggers"})
            if ttype == "recurrence":
                sch = t.get("schedule") or {}
                base["entry_points"].append({
                    "type": "schedule",
                    "value": f"{sch.get('frequency')}/{sch.get('interval')}",
                    "evidence": f"{repo_path}:triggers",
                })
            if t.get("schema_props"):
                base["data"]["schemas"].append({"name": f"{t.get('name')}_request_schema", "format": "json", "evidence": f"{repo_path}:triggers"})

        # Workflow step summary
        parts = []
        for s in (props.get("steps") or [])[:40]:
            tag = s.get("connector") or s.get("type") or "step"
            parts.append(f"{s.get('name')}[{tag}]")
        wf_entry = {
            "name": props.get("name"),
            "kind": props.get("wf_kind") or "other",
            "trigger": "http|schedule|event",
            "steps_summary": " -> ".join(parts),
            "evidence": f"{repo_path}:actions",
        }

        # Cross-flow calls recorded as HTTP endpoints for visibility
        for u in props.get("calls_flows") or []:
            base["interfaces"]["http_endpoints"].append({
                "method": "POST",
                "path": u,
                "summary": "Calls another flow trigger",
                "auth": "custom",
                "request_schema_ref": "",
                "response_schema_ref": "",
                "status_codes": [],
                "evidence": f"{repo_path}:actions",
            })

        # Connector categorization → business context
        connectors_raw = []
        for s in props.get("steps") or []:
            raw_conn = (s.get("connector") or s.get("type") or "").strip()
            if raw_conn:
                connectors_raw.append(raw_conn)
        connectors = sorted(set(connectors_raw))
        if connectors:
            wf_entry["connectors"] = connectors
        datastore_list = [str(d) for d in (props.get("datasource_tables") or []) if d]
        if datastore_list:
            wf_entry["datastores"] = sorted(set(datastore_list))
        service_list = [str(s) for s in (props.get("service_dependencies") or []) if s]
        if service_list:
            wf_entry["service_dependencies"] = sorted(set(service_list))
        identifier_list = [str(i) for i in (props.get("identifier_hints") or []) if i]
        if identifier_list:
            wf_entry["identifier_hints"] = sorted(set(identifier_list))

        msg_keys = {"shared_servicebus", "shared_eventhubs", "shared_kafka"}
        db_keys = {"shared_sql", "shared_azuresql", "shared_commondataservice", "shared_commondataserviceforapps", "shared_postgresql", "shared_mysql"}
        storage_keys = {"shared_azureblob", "shared_onedriveforbusiness", "shared_sharepointonline"}
        http_like = {"http", "shared_http", "apim", "apimmanaged"}

        extra_datastores = set()
        extra_external = set()

        for c in connectors:
            c_lower = c.lower()
            if c_lower in msg_keys:
                base["interfaces"]["events"].append({
                    "topic_or_channel": "",
                    "direction": "publishes|subscribes",
                    "schema_ref": "",
                    "broker": "other",
                    "evidence": f"{repo_path}:actions",
                })
            elif c_lower in db_keys:
                extra_datastores.add("sql")
            elif c_lower in storage_keys:
                extra_external.add(c)
            elif c_lower in http_like:
                base["capabilities"].append("calls external HTTP APIs")
            elif c_lower.startswith("shared_") or ":" in c_lower:
                extra_external.add(c)

        if extra_datastores:
            _extend_unique(base["dependencies"]["datastores"], sorted(extra_datastores))
        if extra_external:
            _extend_unique(base["dependencies"]["external_services"], sorted(extra_external))

        base["workflows"].append(wf_entry)
        if props.get("user_story"):
            base["primary_journeys"].append({"story": props["user_story"], "evidence": repo_path})
        if props.get("journey_touchpoints"):
            base["ux_summaries"].append({"touchpoints": props["journey_touchpoints"], "evidence": repo_path})

    # ---- Infra (K8s/Terraform/Bicep/ARM general) ----
    elif k == "infra":
        rk = props.get("resource_kind")
        rt = props.get("resource_type")
        if rk:
            base["artifact_type"] = "k8s_manifest"
        elif rt:
            base["artifact_type"] = "terraform" if ".tf" in repo_path else "other"
        base["capabilities"].append("defines infrastructure")

    # ---- Events (general) ----
    elif k == "event":
        base["artifact_type"] = "event_definition"
        base["interfaces"]["events"].append({
            "topic_or_channel": props.get("topic_or_queue", ""),
            "direction": props.get("direction", "unknown"),
            "schema_ref": "",
            "broker": props.get("broker", "other"),
            "evidence": f"{repo_path}:ref",
        })
        base["capabilities"].append("publishes/subscribes events")

    # ---- Data / DB schemas ----
    elif k == "db":
        base["artifact_type"] = "db_schema"
        if props.get("table"):
            base["data"]["tables_or_collections"].append(props["table"])
        if props.get("engine"):
            base["dependencies"]["datastores"].append(props["engine"])
        if props.get("columns"):
            base["data"]["schemas"].append({"name": props.get("table"), "columns": props["columns"]})
        base["capabilities"].append("stores structured data")
        if props.get("data_samples"):
            base["data_examples"].extend(props["data_samples"])

    # ---- CI/CD pipelines (this is the branch you were missing) ----
    elif k == "job":
        base["artifact_type"] = "ci_pipeline"
        # schedules → optional workflow block
        sched = props.get("schedules") or []
        if sched:
            base["workflows"].append({
                "name": props.get("name"),
                "kind": "other",
                "trigger": "schedule",
                "steps_summary": "",
                "evidence": f"{repo_path}:1-60",
            })
        base["capabilities"].append("runs CI/CD pipeline")
        # CI system
        ci = (props.get("ci_system") or "").strip()
        if ci:
            base["build_and_deploy"]["ci"].append(ci)
        # environments inferred by the extractor
        envs = props.get("environments") or []
        if envs:
            base["build_and_deploy"]["environments"].extend(envs)

    # ---- Markdown docs etc. ----
    elif k == "doc":
        base["artifact_type"] = "design_doc"
        base["capabilities"].append("provides human documentation")

    # ---- Code entities (tree-sitter) ----
    elif k == "code_entity":
        base["artifact_type"] = "code_entity"
        doc = props.get("docstring") or ""
        base["description"] = doc or base["description"]
        base["capabilities"].append("exposes code component")
        verbs = props.get("business_verbs") or []
        entry = {
            "name": props.get("name"),
            "entity_type": props.get("entity_type"),
            "language": props.get("language"),
            "docstring": doc,
            "start_line": props.get("start_line"),
            "end_line": props.get("end_line"),
            "business_verbs": verbs,
        }
        if verbs:
            base["capabilities"].append(f"handles {', '.join(verbs[:3])}")
        base["code_entities"].append(entry)

    # ---- UI components ----
    elif k == "ui_component":
        base["artifact_type"] = "ui_component"
        base["capabilities"].append("exposes UI entry point")
        entry = {
            "name": props.get("name"),
            "framework": props.get("framework"),
            "routes": props.get("routes") or [],
            "selector": props.get("selector"),
            "template_url": props.get("template_url"),
        }
        base["ui_components"].append(entry)
        screenshots = props.get("screenshots") or ([] if not props.get("ui_snapshot") else [props.get("ui_snapshot")])
        for shot in screenshots:
            base["screenshots"].append({"path": shot, "caption": props.get("name")})

    # ---- Integrations / imports ----
    elif k == "integration":
        base["artifact_type"] = "integration_signal"
        base["capabilities"].append("calls external systems")
        entry = {
            "library": props.get("library"),
            "integration_kind": props.get("integration_kind"),
            "language": props.get("language"),
        }
        base["integrations"].append(entry)

    # ---- Process diagrams ----
    elif k == "process_diagram":
        base["artifact_type"] = "process_diagram"
        entry = {"name": props.get("name")}
        base["process_diagrams"].append(entry)

    elif k == "business_entity":
        base["artifact_type"] = "business_entity"
        base["business_entities"].append({"name": props.get("name")})

    # ---- other kinds fall through ----

    _apply_dependency_hints(base, props)

    pack = build_experience_pack(signal)
    if pack:
        base["experience_pack"] = pack

    return base
