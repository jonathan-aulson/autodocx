from __future__ import annotations

import json
import time
from collections import Counter, defaultdict, deque
from pathlib import Path
from typing import Any, Dict, Iterable, List, Sequence, Tuple

from autodocx.types import Edge, Node


def _slugify(value: str) -> str:
    import re

    slug = re.sub(r"[^a-z0-9]+", "-", (value or "").lower()).strip("-")
    return slug or "constellation"


def _rel_path(path: Path, base: Path) -> str:
    try:
        return path.relative_to(base).as_posix()
    except ValueError:
        return path.as_posix()


def _index_sirs(
    sir_records: Sequence[Tuple[Dict[str, Any], Path]],
    out_base: Path,
) -> Dict[str, List[Dict[str, Any]]]:
    index: Dict[str, List[Dict[str, Any]]] = defaultdict(list)
    for sir_obj, sir_path in sir_records:
        comp = sir_obj.get("component_or_service") or "ungrouped"
        entry = {
            "id": sir_obj.get("id"),
            "name": sir_obj.get("name"),
            "path": _rel_path(sir_path, out_base),
            "props": sir_obj.get("props") or {},
            "evidence": sir_obj.get("evidence") or [],
        }
        index[comp].append(entry)
    return index


CALL_EDGE_TYPES = {
    "calls",
    "invokes",
    "triggers",
    "publishes",
    "subscribes",
    "depends_on",
}


def _edge_nodes(edge: Edge) -> Tuple[str | None, str | None]:
    src = getattr(edge, "source", None) or getattr(edge, "src", None)
    dst = getattr(edge, "target", None) or getattr(edge, "dst", None)
    return src, dst


def _edge_type(edge: Edge) -> str:
    return (getattr(edge, "type", None) or getattr(edge, "label", "") or "").lower()


def _repo_bucket(node: Node) -> str | None:
    file_path = (node.props or {}).get("file")
    if not file_path:
        return None
    parts = Path(file_path).parts
    if not parts:
        return None
    if len(parts) == 1:
        return parts[0]
    return "/".join(parts[:2])


def _gather_components(node: Node) -> Iterable[str]:
    props = node.props or {}
    candidates = [
        props.get("component_or_service"),
        props.get("service"),
        node.name if (node.type or "").lower() == "component" else None,
    ]
    for entry in candidates:
        if entry:
            yield entry


def build_constellations(
    nodes: Sequence[Node],
    edges: Sequence[Edge],
    sir_records: Sequence[Tuple[Dict[str, Any], Path]],
    out_base: Path,
) -> List[Dict[str, Any]]:
    """
    Assemble connected subgraphs (constellations) with lightweight heuristics.
    """
    out_base = Path(out_base)
    node_map: Dict[str, Node] = {n.id: n for n in nodes if getattr(n, "id", None)}
    adjacency: Dict[str, set[str]] = defaultdict(set)
    component_map: Dict[str, List[str]] = defaultdict(list)
    repo_map: Dict[str, List[str]] = defaultdict(list)
    for edge in edges:
        src, dst = _edge_nodes(edge)
        if not src or not dst:
            continue
        adjacency[src].add(dst)
        adjacency[dst].add(src)
    for node_id in node_map:
        adjacency.setdefault(node_id, set())
        node = node_map[node_id]
        for comp in _gather_components(node):
            component_map[comp].append(node_id)
        bucket = _repo_bucket(node)
        if bucket:
            repo_map[bucket].append(node_id)

    def _connect_bucket(bucket_nodes: List[str]) -> None:
        if len(bucket_nodes) < 2:
            return
        for idx in range(len(bucket_nodes) - 1):
            a = bucket_nodes[idx]
            b = bucket_nodes[idx + 1]
            adjacency[a].add(b)
            adjacency[b].add(a)

    for members in component_map.values():
        _connect_bucket(members)
    for members in repo_map.values():
        _connect_bucket(members)

    sir_index = _index_sirs(sir_records, out_base)
    constellations: List[Dict[str, Any]] = []
    visited: set[str] = set()

    for node_id in sorted(node_map.keys()):
        if node_id in visited:
            continue
        queue = deque([node_id])
        members: List[str] = []
        while queue:
            cur = queue.popleft()
            if cur in visited:
                continue
            visited.add(cur)
            members.append(cur)
            queue.extend(n for n in adjacency[cur] if n not in visited)

        member_nodes = [node_map[mid] for mid in members]
        node_types = Counter((n.type or "unknown") for n in member_nodes)
        components = sorted({comp for node in member_nodes for comp in _gather_components(node)})
        if not components:
            components = ["ungrouped"]
        repo_buckets = Counter(filter(None, (_repo_bucket(node) for node in member_nodes)))

        entry_points = [
            {
                "id": n.id,
                "name": n.name,
                "type": n.type,
                "component": (n.props or {}).get("component_or_service"),
            }
            for n in member_nodes
            if (n.type or "") in {"API", "Workflow", "Operation"}
        ]
        datastores = [
            {
                "id": n.id,
                "name": n.name,
                "kind": n.type,
                "component": (n.props or {}).get("component_or_service"),
            }
            for n in member_nodes
            if (n.type or "").lower() in {"datastore", "database"}
        ]

        sir_refs: List[str] = []
        for comp in components:
            for sir_entry in sir_index.get(comp, []):
                sir_refs.append(sir_entry["path"])
        sir_refs = sorted(dict.fromkeys(sir_refs))

        member_set = set(members)
        edge_count = 0
        call_edge_count = 0
        for edge in edges:
            src, dst = _edge_nodes(edge)
            if src in member_set and dst in member_set:
                edge_count += 1
                if _edge_type(edge) in CALL_EDGE_TYPES:
                    call_edge_count += 1
        evidence_samples = []
        for node in member_nodes:
            for ev in (node.evidence or [])[:2]:
                evidence_samples.append(ev)
            if len(evidence_samples) >= 6:
                break

        entry_score = 0.4 if entry_points else 0.1
        component_score = 0.3 if len(components) > 1 else 0.15
        size_score = min(0.3, len(members) / 20.0)
        score = round(min(1.0, entry_score + component_score + size_score), 3)

        constellations.append(
            {
                "id": f"constellation_{len(constellations) + 1}",
                "components": components,
                "node_ids": sorted(members),
                "node_count": len(members),
                "edge_count": edge_count,
                "node_types": node_types,
                "entry_points": entry_points,
                "datastores": datastores,
                "sir_files": sir_refs,
                "score": score,
                "heuristics": {
                    "entry_points": len(entry_points),
                    "component_count": len(components),
                    "size": len(members),
                    "call_edges": call_edge_count,
                    "repo_bucket_diversity": len(repo_buckets),
                    "reason": "graph + repo proximity",
                },
                "evidence_samples": evidence_samples[:6],
                "generated_at": time.time(),
            }
        )

    return constellations


def persist_constellations(out_base: Path, constellations: Sequence[Dict[str, Any]]) -> List[Dict[str, Any]]:
    out_base = Path(out_base)
    const_dir = out_base / "signals" / "constellations"
    const_dir.mkdir(parents=True, exist_ok=True)
    manifest: List[Dict[str, Any]] = []
    for record in constellations:
        slug_hint = "-".join(record.get("components", []))
        slug = record.get("slug") or _slugify(slug_hint or record["id"])
        payload = dict(record)
        payload["slug"] = slug
        payload["graph_file"] = f"signals/constellations/{slug}.json"
        target = const_dir / f"{slug}.json"
        target.write_text(json.dumps(payload, indent=2), encoding="utf-8")
        manifest.append(
            {
                "id": record["id"],
                "slug": slug,
                "path": payload["graph_file"],
                "components": record.get("components", []),
                "score": record.get("score"),
                "node_count": record.get("node_count"),
                "sir_files": record.get("sir_files", []),
            }
        )
    (const_dir / "manifest.json").write_text(
        json.dumps({"generated_at": time.time(), "constellations": manifest}, indent=2),
        encoding="utf-8",
    )
    return manifest
