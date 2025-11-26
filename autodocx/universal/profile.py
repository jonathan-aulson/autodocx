from __future__ import annotations
from typing import Dict, Any, List, Tuple
from collections import defaultdict
from autodocx.types import Node, Edge

# Normalized role taxonomy (universal)
UNIVERSAL_ROLES = {
    "interface.receive", "interface.reply", "interface.soap",
    "invoke.process", "invoke.service",
    "data.jdbc", "messaging.jms",
    "transform.mapper", "schedule.timer",
    "ops.log", "error.throw",
}

def build_component_profile(service_name: str, nodes: List[Node], edges: List[Edge]) -> Dict[str, Any]:
    """
    Build a universal profile for one component/service (or process).
    Input: the EG subgraph for that component (nodes/edges filtered by service/component), plus enriched props.
    Output: ComponentProfile JSON (universal) with evidence-first sections.
    """
    profile: Dict[str, Any] = {
        "name": service_name,
        "roles": sorted(set()),
        "interfaces": [],
        "invokes": [],
        "data": {"inputs": [], "outputs": [], "identifiers": [], "schemas": []},
        "dependencies": {"processes": [], "services": [], "datastores": []},
        "operations": {"slo_sla": {"availability": "", "latency": ""}, "runbooks": []},
        "errors_and_logging": {"errors": [], "logging": []},
        "environment": {"cloud": "", "regions": [], "ci": [], "environments": []},
        "observability": {"metrics": [], "logs": [], "alerts": [], "dashboards": []},
        "security": {"auth": [], "secrets": [], "compliance": []},
        "interdependencies": {"related": [], "calls": [], "called_by": [], "shared_identifiers_with": [], "shared_datastores_with": []},
        "evidence": [],
        "version": ""
    }

    # Sweep nodes for roles, interfaces, etc.
    for n in nodes:
        k = n.type
        p = n.props or {}
        # Interfaces
        if k == "Operation":
            m = (p.get("method") or "").upper()
            path = p.get("path") or ""
            if m in ["GET","POST","PUT","DELETE","PATCH","HEAD","OPTIONS"] and path:
                profile["interfaces"].append({"kind":"REST", "method": m, "endpoint": path, "operation": "", "evidence": " ; ".join(n.evidence or [])})
        # Workflow-specific hints (works for BW, Logic Apps, Functions)
        if k == "Workflow":
            # timers / schedules
            for t in (p.get("triggers") or []):
                tt = (t.get("type") or "").lower()
                if tt == "recurrence" or "schedule" in tt:
                    sch = t.get("schedule") or {}
                    freq = sch.get("frequency") or ""
                    intr = sch.get("interval") or ""
                    profile["interfaces"].append({"kind":"Timer", "method":"", "endpoint": f"{freq}/{intr}".strip("/"), "operation": "", "evidence": " ; ".join(n.evidence or [])})
            # data schemas from request triggers
            for sch in (p.get("data_schemas") or []):
                profile["data"]["schemas"].append({"name": sch.get("name"), "format": "json", "evidence": sch.get("evidence","")})
            # connector roles (e.g., data.jdbc, messaging.jms, http)
            for s in (p.get("steps") or []):
                conn = (s.get("connector") or "").lower()
                if conn.startswith("shared_sql") or conn == "jdbc":
                    profile["roles"].append("data.jdbc")
                if conn.startswith("shared_servicebus") or conn.startswith("shared_eventhubs") or conn == "jms":
                    profile["roles"].append("messaging.jms")
        # Datastores/events
        if k == "Datastore":
            profile["dependencies"]["datastores"].append(p.get("engine") or p.get("name") or "db")
        if k == "MessageTopic":
            profile["interfaces"].append({"kind":"JMS", "method":"", "endpoint": p.get("topic_or_queue",""), "operation":"", "evidence": " ; ".join(n.evidence or [])})

        # Environment (cloud/ci/environments) can be added via artifact merge step outside this builder

        # Collect evidence
        if n.evidence:
            profile["evidence"].append({"path": (n.evidence[0].split(":")[0] if isinstance(n.evidence[0], str) else ""), "lines": "", "snippet": ""})

    # Edges for interdependencies (calls)
    for e in edges:
        if e.type == "calls":
            # If source is this service and target is another component’s Operation/Workflow, it’s an invoke
            profile["invokes"].append({"kind":"REST|Process|Other", "target": e.target, "operation":"", "evidence": " ; ".join(e.evidence or [])})
            profile["interdependencies"]["calls"].append(e.target)

    # Normalize roles
    profile["roles"] = sorted(set([r for r in profile["roles"] if r in UNIVERSAL_ROLES]))
    return profile
