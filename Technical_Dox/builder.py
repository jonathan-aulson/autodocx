from __future__ import annotations
from typing import List, Dict, Tuple
from autodocx.types import Signal, Node, Edge

def build_graph(signals: List[Signal]) -> Tuple[List[Node], List[Edge]]:
    nodes: Dict[str, Node] = {}
    edges: List[Edge] = []

    def nid(prefix: str, name: str) -> str:
        return f"{prefix}:{name}"

    for s in signals:
        k, p, ev, subs = s.kind, s.props, s.evidence, s.subscores
        if k == "api":
            key = nid("API", p.get("name","api"))
            nodes[key] = Node(key, "API", p.get("name","api"), p, ev, subs)
        elif k == "op":
            nm = f"{p.get('method','OP')} {p.get('path','')}"
            key = nid("Operation", nm)
            nodes[key] = Node(key, "Operation", nm, p, ev, subs)
            if p.get("api"):
                edges.append(Edge(nid("API", p["api"]), key, "exposes", {}, ev, subs))
        elif k == "workflow":
            key = nid("Workflow", p.get("name","workflow"))
            nodes[key] = Node(key, "Workflow", p.get("name","workflow"), p, ev, subs)
            # Link calls between flows if URLs look like workflow triggers
            for u in p.get("calls_flows", []) or []:
                import re
                m = re.search(r"/workflows/([^/]+)/triggers/([^/]+)/run", u or "")
                if m:
                    target = nid("Workflow", m.group(1))
                    edges.append(Edge(key, target, "calls", {"via": "http"}, ev, subs))
        elif k == "doc":
            key = nid("Doc", p.get("name","doc"))
            nodes[key] = Node(key, "Doc", p.get("name","doc"), p, ev, subs)
        # Joiners
        try:
            from autodocx.joiners.http_calls import link_workflows_to_openapi
            nodes, edges = link_workflows_to_openapi(nodes, edges)
        except Exception:
            pass


    return list(nodes.values()), edges
