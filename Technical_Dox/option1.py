
from __future__ import annotations
from pathlib import Path
from typing import Dict, Any
from autodocx.types import Signal

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

def _service_from_repo_path(repo_root: Path, abs_path: str) -> str:
    try:
        p = Path(abs_path)
        rel = p.resolve().relative_to(repo_root.resolve())
        parts = rel.parts
        if len(parts) > 0:
            return parts[0]
    except Exception:
        pass
    return repo_root.name

# -------- main mapper --------

def to_option1_artifact(signal: Signal, repo_root: Path) -> Dict[str, Any]:
    # Always get props and repo_path first; guard against weird plugins
    props: Dict[str, Any] = signal.props if isinstance(signal.props, dict) else {}
    repo_path: str = str(props.get("file") or "")

    # Component/service inferred from first path segment beneath scan root
    component = _service_from_repo_path(repo_root, repo_path) if repo_path else repo_root.name

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
        "confidence": round(
            min(1.0, float(signal.subscores.get("parsed", 0.0)) + 0.5 * float(signal.subscores.get("schema_evidence", 0.0))),
            3,
        ),
        "evidence": [{"path": repo_path, "lines": "", "snippet": ""}] if repo_path else [],
    }
    # Optional fallback for OpenAPI spec-only version
    if not base["version"] and "spec_version" in props:
        base["version"] = props["spec_version"]

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
        base["workflows"].append({
            "name": props.get("name"),
            "kind": props.get("wf_kind") or "other",
            "trigger": "http|schedule|event",
            "steps_summary": " -> ".join(parts),
            "evidence": f"{repo_path}:actions",
        })

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
        connectors = []
        for s in props.get("steps") or []:
            c = (s.get("connector") or "").strip().lower()
            if c:
                connectors.append(c)
        connectors = sorted(set(connectors))

        msg_keys = {"shared_servicebus", "shared_eventhubs", "shared_kafka"}
        db_keys = {"shared_sql", "shared_azuresql", "shared_commondataservice", "shared_commondataserviceforapps", "shared_postgresql", "shared_mysql"}
        storage_keys = {"shared_azureblob", "shared_onedriveforbusiness", "shared_sharepointonline"}
        http_like = {"http", "shared_http", "apim", "apimmanaged"}

        for c in connectors:
            if c in msg_keys:
                base["interfaces"]["events"].append({
                    "topic_or_channel": "",
                    "direction": "publishes|subscribes",
                    "schema_ref": "",
                    "broker": "other",
                    "evidence": f"{repo_path}:actions",
                })
            elif c in db_keys:
                base["dependencies"]["datastores"].append("sql")
            elif c in storage_keys:
                base["dependencies"]["external_services"].append(c)
            elif c in http_like:
                base["capabilities"].append("calls external HTTP APIs")

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
        base["capabilities"].append("stores structured data")

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

    # ---- other kinds fall through ----

    return base
