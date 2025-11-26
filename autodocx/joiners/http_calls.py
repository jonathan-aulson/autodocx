from __future__ import annotations
from typing import List, Tuple
from urllib.parse import urlparse
from autodocx.types import Node, Edge

def link_workflows_to_openapi(nodes: List[Node], edges: List[Edge]) -> Tuple[List[Node], List[Edge]]:
    # Build a map of OpenAPI Operations by (method, path)
    ops = {}
    for n in nodes:
        if n.type == "Operation":
            m = (n.props.get("method") or "").upper()
            p = n.props.get("path") or ""
            # Ignore k8s service port ops and Ingress summary-only
            if m in ["GET","POST","PUT","DELETE","PATCH","HEAD","OPTIONS"] and p.startswith("/"):
                ops[(m, p)] = n.id

    # For each Workflow with HTTP steps calling absolute URLs, link by path
    for w in [n for n in nodes if n.type == "Workflow"]:
        for s in (w.props.get("steps") or []):
            if (s.get("type") or "").lower() in ["http","httpwebhook"] or (s.get("connector") or "").lower() in ["http","shared_http"]:
                url = s.get("url_or_path") or ""
                if url and url.startswith("http"):
                    try:
                        parsed = urlparse(url)
                        path_only = parsed.path
                        method = (s.get("method") or "").upper() or "POST"
                        key = (method, path_only)
                        if key in ops:
                            edges.append(Edge(source=w.id, target=ops[key], type="calls", props={"via":"http"}, evidence=w.evidence, subscores=w.subscores))
                    except Exception:
                        continue
    return nodes, edges
