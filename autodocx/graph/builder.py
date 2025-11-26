from __future__ import annotations
from typing import List, Dict, Tuple, Set
from autodocx.types import Signal, Node, Edge

def build_graph(signals: List[Signal]) -> Tuple[List[Node], List[Edge]]:
    nodes: Dict[str, Node] = {}
    component_nodes: Dict[str, Node] = {}
    edges: List[Edge] = []

    def add_edge(edge: Edge) -> None:
        edges.append(edge)

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
                add_edge(Edge(nid("API", p["api"]), key, "exposes", {}, ev, subs))
        elif k == "workflow":
            key = nid("Workflow", p.get("name","workflow"))
            nodes[key] = Node(key, "Workflow", p.get("name","workflow"), p, ev, subs)
            # Link calls between flows if URLs look like workflow triggers
            for u in p.get("calls_flows", []) or []:
                import re
                m = re.search(r"/workflows/([^/]+)/triggers/([^/]+)/run", u or "")
                if m:
                    target = nid("Workflow", m.group(1))
                    add_edge(Edge(key, target, "calls", {"via": "http"}, ev, subs))
        elif k == "doc":
            key = nid("Doc", p.get("name","doc"))
            nodes[key] = Node(key, "Doc", p.get("name","doc"), p, ev, subs)

    # Create component hub nodes so SIRs in the same component connect
    for node in list(nodes.values()):
        comp = (node.props or {}).get("component_or_service") or (node.props or {}).get("service")
        if not comp:
            continue
        comp_id = nid("Component", comp)
        if comp_id not in component_nodes:
            component_nodes[comp_id] = Node(comp_id, "Component", comp, {"component_or_service": comp}, [], {})
        add_edge(Edge(comp_id, node.id, "owns", {}, [], {}))
        add_edge(Edge(node.id, comp_id, "member_of", {}, [], {}))

    all_nodes: List[Node] = list(nodes.values()) + list(component_nodes.values())

    # Joiners (run once after base graph constructed)
    try:
        from autodocx.joiners.http_calls import link_workflows_to_openapi
        all_nodes, edges = link_workflows_to_openapi(all_nodes, edges)
    except Exception:
        pass

    return all_nodes, edges
