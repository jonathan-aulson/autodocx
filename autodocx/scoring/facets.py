from __future__ import annotations

from typing import Any, Dict, List, Mapping, Sequence, Tuple

# Optional networkx-based structural features
try:
    import networkx as nx  # type: ignore
    _NX_OK = True
except Exception:
    _NX_OK = False


def rollup_facets(nodes, edges) -> dict:
    """
    Compute documentation rollup facets with light distance- and structure-aware metrics.
    - Signature stays the same (nodes, edges) to preserve compatibility.
    - If networkx is unavailable, returns baseline counts and a basic score.

    Returns a dict including:
      - score (0..1)
      - counts: ops, apis, events, dbs, infra, docs
      - distance block (if computed): markers, avg_nearest_distance, anchors_within_r, reachable_ratio, radius
      - structure block (if computed): articulation_count (approx), density
    """
    # -------------------------
    # Baseline counts (existing)
    # -------------------------
    ops = sum(1 for n in nodes if getattr(n, "type", "") == "Operation")
    apis = sum(1 for n in nodes if getattr(n, "type", "") == "API")
    events = sum(1 for n in nodes if getattr(n, "type", "") == "MessageTopic")
    dbs = sum(1 for n in nodes if getattr(n, "type", "") == "Datastore")
    infra = sum(1 for n in nodes if getattr(n, "type", "") == "InfraResource")
    docs = sum(1 for n in nodes if getattr(n, "type", "") == "Doc")

    baseline_score = min(
        1.0,
        0.30 * (ops > 0)
        + 0.20 * (apis > 0)
        + 0.20 * (events > 0)
        + 0.15 * (dbs > 0)
        + 0.10 * (infra > 0)
        + 0.05 * (docs > 0),
    )

    out: Dict[str, Any] = {
        "score": round(baseline_score, 3),
        "ops": ops,
        "apis": apis,
        "events": events,
        "dbs": dbs,
        "infra": infra,
        "docs": docs,
    }

    # -------------------------
    # Distance/structure metrics
    # -------------------------
    if not _NX_OK:
        return out  # networkx not available; return baseline only

    # Build a directed graph from nodes/edges with light weights
    G = nx.DiGraph()
    node_types: Dict[str, str] = {}

    for n in nodes:
        nid = getattr(n, "id", None) or getattr(n, "node_id", None)
        ntype = getattr(n, "type", None) or ""
        nname = getattr(n, "name", None) or ""
        nprops = getattr(n, "props", None) or {}
        if not nid:
            continue
        G.add_node(nid, type=ntype, name=nname, props=nprops)
        node_types[nid] = ntype

    # Edge weights: keep simple so we don't depend on external config here
    def weight_for(label: str) -> float:
        if not label:
            return 1.0
        lab = str(label).lower()
        if lab in {"calls", "exposes", "depends_on"}:
            return 1.0
        if lab in {"publishes_to", "consumes_from"}:
            return 1.5
        if lab in {"evidence_of"}:
            return 0.25
        return 1.0

    for e in edges:
        src = getattr(e, "src", None)
        dst = getattr(e, "dst", None)
        lab = getattr(e, "label", "") or ""
        if not src or not dst:
            continue
        G.add_edge(src, dst, label=lab, weight=weight_for(lab), props=getattr(e, "props", {}) or {})

    N = G.number_of_nodes()
    E = G.number_of_edges()
    if N == 0:
        return out

    # Heuristic marker selection: prefer Workflow/API/Operation with highest degree
    preferred_types = {"Workflow", "API", "Operation"}
    candidates = [n for n in G.nodes() if node_types.get(n) in preferred_types]
    if not candidates:
        candidates = list(G.nodes())

    # Degree-based top-K markers
    deg_map = {n: (G.in_degree(n) + G.out_degree(n)) for n in candidates}
    top_k = 6  # small, fast default
    markers = sorted(deg_map.keys(), key=lambda x: deg_map[x], reverse=True)[:max(1, min(top_k, len(deg_map)))]

    # Compute distances to markers (Dijkstra on weight)
    dist_maps: Dict[str, Dict[str, float]] = {}
    for m in markers:
        try:
            dist_maps[m] = nx.single_source_dijkstra_path_length(G, m, weight="weight")
        except Exception:
            dist_maps[m] = {}

    # Aggregate per-node nearest distance
    nearest: List[float] = []
    radius = 4
    anchors_within_r = 0
    reachable = 0
    for nid in G.nodes():
        dvals = []
        for m in markers:
            dm = dist_maps.get(m, {})
            if nid in dm:
                dvals.append(dm[nid])
        if not dvals:
            continue
        reachable += 1
        dmin = min(dvals)
        nearest.append(float(dmin))
        if dmin <= radius:
            anchors_within_r += 1

    avg_nearest = (sum(nearest) / len(nearest)) if nearest else float("inf")
    reachable_ratio = (reachable / N) if N else 0.0

    # Lightweight articulation estimate (undirected check; may be expensive on very large graphs)
    articulation_count = 0
    try:
        und = G.to_undirected()
        # Guard against pathological sizes
        if und.number_of_nodes() <= 5000:
            # networkx articulation_points returns a generator
            articulation_count = sum(1 for _ in nx.articulation_points(und))
    except Exception:
        articulation_count = 0

    # Density (simple structural health signal)
    density = (E / (N * (N - 1))) if N > 1 else 0.0

    # Compose a structure-aware score bump (bounded)
    # Normalize avg distance into [0,1]: smaller distance => larger quality
    distance_quality = 0.0
    if nearest:
        distance_quality = max(0.0, min(1.0, 1.0 / (1.0 + max(0.0, avg_nearest))))

    anchor_coverage_quality = max(0.0, min(1.0, anchors_within_r / max(1, N)))
    reachable_quality = max(0.0, min(1.0, reachable_ratio))

    structure_penalty = 0.0
    if N >= 20:
        # penalize if too many articulation points relative to size
        structure_penalty = min(0.15, (articulation_count / N) * 0.5)

    # Blend into overall score conservatively to retain backward compatibility
    blended = (
        0.70 * baseline_score
        + 0.10 * distance_quality
        + 0.10 * anchor_coverage_quality
        + 0.10 * reachable_quality
        - structure_penalty
    )
    out["score"] = round(max(0.0, min(1.0, blended)), 3)

    out["distance"] = {
        "markers": markers,
        "avg_nearest_distance": (None if avg_nearest == float("inf") else round(avg_nearest, 3)),
        "anchors_within_r": anchors_within_r,
        "reachable_ratio": round(reachable_ratio, 3),
        "radius": radius,
        "node_count": N,
        "edge_count": E,
    }
    out["structure"] = {
        "articulation_count": articulation_count,
        "density": round(density, 6),
    }

    return out
