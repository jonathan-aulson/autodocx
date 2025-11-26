# autodocx/features/distance_features.py
from __future__ import annotations

import math
import time
from typing import Any, Dict, Iterable, List, Mapping, Optional, Sequence, Tuple

try:
    import networkx as nx
except Exception as e:
    raise RuntimeError("networkx is required for distance-features. Please install 'networkx'.") from e


# ----------------------------
# Defaults and configuration
# ----------------------------

_DEFAULT_CFG: Dict[str, Any] = {
    "enabled": True,
    "edge_weights": {
        # Generic defaults; use labels from your builder
        "calls": 1.0,
        "exposes": 1.0,
        "depends_on": 0.5,
        "publishes_to": 1.5,
        "consumes_from": 1.5,
        "evidence_of": 0.25,
        # Fallback weight if label not listed
        "__default__": 1.0,
    },
    # Marker selection
    "marker_strategy": {
        "business_anchors": True,
        "structural_anchors": True,
        "domain_anchors": True,
        "k_per_strategy": 3,
        "overrides": {
            "include_ids": [],  # explicit node ids to force include
            "exclude_ids": [],  # explicit node ids to exclude
        },
        # optional: restrict candidate types for markers
        "candidate_types": ["Workflow", "API", "Operation"],
    },
    # Distance features
    "radius": 4,
    # Computation
    "compute": {
        "parallel": False,  # single-process for simplicity/portability
        "backend": "auto",
    },
}


# ----------------------------
# Public API
# ----------------------------

def compute_graph_features(
    nodes: Sequence[Any],
    edges: Sequence[Any],
    settings: Optional[Mapping[str, Any]] = None,
) -> Dict[str, Dict[str, Any]]:
    """
    Compute distance-driven graph features for nodes in a graph.

    Inputs
    - nodes: iterable of Node-like objects with attributes: id, type, name, props
    - edges: iterable of Edge-like objects with attributes: src, dst, label, props
    - settings: optional dict; we will look for settings.get('distance_features', {})

    Output
    - features_map: { node_id: graph_features_dict }
    """
    cfg = _derive_cfg(settings)

    if not cfg.get("enabled", True):
        return {}

    G = build_nx_graph_from_nodes_edges(nodes, edges, cfg)
    if G.number_of_nodes() == 0:
        return {}

    markers, marker_meta = select_markers(G, cfg)
    if not markers:
        # no markers -> still return empty feature map
        return {}

    features = compute_distance_vectors(G, markers, cfg)

    # Decorate each node's features with shared meta/config
    decorated: Dict[str, Dict[str, Any]] = {}
    for nid, f in features.items():
        f2 = dict(f)
        f2["marker_strategy"] = marker_meta.get("strategy", "mixed")
        f2["markers"] = marker_meta.get("markers", [])
        f2["edge_weights"] = cfg.get("edge_weights", {})
        f2["parameters"] = {
            "radius": int(cfg.get("radius", 4)),
            "marker_count": len(markers),
            "compute_ms": int(marker_meta.get("compute_ms", 0)),
        }
        decorated[nid] = f2
    return decorated


# ----------------------------
# Graph building
# ----------------------------

def build_nx_graph_from_nodes_edges(
    nodes: Sequence[Any],
    edges: Sequence[Any],
    cfg: Mapping[str, Any],
) -> "nx.DiGraph":
    """
    Build a typed, weighted DiGraph from Node/Edge records.
    Node attributes: type, name, props (optional)
    Edge attributes: label, weight (derived from label), props (optional)
    """
    G = nx.DiGraph()

    # Add nodes with attributes
    for n in nodes:
        # Defensive access
        nid = getattr(n, "id", None) or getattr(n, "node_id", None)
        ntype = getattr(n, "type", None)
        nname = getattr(n, "name", None)
        nprops = getattr(n, "props", None) or {}
        if not nid:
            # skip malformed nodes
            continue
        G.add_node(nid, type=ntype, name=nname, props=nprops)

    # Edge weights map
    ew = cfg.get("edge_weights", {}) or {}
    default_w = float(ew.get("__default__", 1.0))

    def _w(label: Optional[str]) -> float:
        if not label:
            return default_w
        return float(ew.get(str(label), default_w))

    # Add edges with label/weight
    for e in edges:
        src = getattr(e, "src", None) or getattr(e, "source", None)
        dst = getattr(e, "dst", None) or getattr(e, "target", None)
        lab = getattr(e, "label", None) or getattr(e, "type", None)
        if not src or not dst:
            continue
        G.add_edge(src, dst, label=lab, weight=_w(lab), props=getattr(e, "props", {}) or {})

    return G


# ----------------------------
# Marker selection
# ----------------------------

def select_markers(
    G: "nx.DiGraph",
    cfg: Mapping[str, Any],
) -> Tuple[List[str], Dict[str, Any]]:
    """
    Select marker nodes using a mixed strategy:
    - Include overrides
    - Structural anchors: top-k by betweenness centrality
    - Business/domain anchors: filter by node type and domain if available
    """
    start = time.time()
    strat = (cfg.get("marker_strategy") or {})
    k = int(strat.get("k_per_strategy", 3))
    include_ids = list((strat.get("overrides") or {}).get("include_ids", []))
    exclude_ids = set((strat.get("overrides") or {}).get("exclude_ids", []))
    candidate_types = set(strat.get("candidate_types") or [])

    markers: List[str] = []

    # Always include explicit overrides that exist
    for nid in include_ids:
        if nid in G and nid not in exclude_ids:
            markers.append(nid)

    # Structural anchors (betweenness centrality)
    if strat.get("structural_anchors", True):
        # Compute on an undirected shadow for robustness
        try:
            und = G.to_undirected()
            bc = nx.betweenness_centrality(und, k=None, normalized=True)  # exact for mid-size graphs
            top_struct = sorted((n for n in bc.keys() if n not in exclude_ids),
                                key=lambda x: bc[x],
                                reverse=True)[:k]
            for nid in top_struct:
                if nid not in markers:
                    markers.append(nid)
        except Exception:
            pass

    # Business/domain anchors (heuristic)
    if strat.get("business_anchors", True) or strat.get("domain_anchors", True):
        # Prefer candidate types; fallback to all nodes
        candidates = [n for n, d in G.nodes(data=True)]
        if candidate_types:
            candidates = [n for n, d in G.nodes(data=True) if (d.get("type") in candidate_types)]
        # degree centrality as proxy (directed out+in)
        deg = {n: (G.in_degree(n) + G.out_degree(n)) for n in candidates if n not in exclude_ids}
        top_bus = sorted(deg.keys(), key=lambda x: deg[x], reverse=True)[:k]
        for nid in top_bus:
            if nid not in markers:
                markers.append(nid)

    # Deduplicate, cap to reasonable size
    markers = [m for m in markers if m in G and m not in exclude_ids]
    # If still empty, fall back to the top-degree nodes globally
    if not markers:
        deg_all = {n: (G.in_degree(n) + G.out_degree(n)) for n in G.nodes()}
        fallback = sorted(deg_all.keys(), key=lambda x: deg_all[x], reverse=True)[:max(1, k)]
        markers = fallback

    meta = {
        "strategy": "mixed",
        "markers": [{"id": m, "type": G.nodes[m].get("type")} for m in markers],
        "compute_ms": int((time.time() - start) * 1000),
    }
    return markers, meta


# ----------------------------
# Distance computation
# ----------------------------

def compute_distance_vectors(
    G: "nx.DiGraph",
    markers: Sequence[str],
    cfg: Mapping[str, Any],
) -> Dict[str, Dict[str, Any]]:
    """
    Compute per-node distance features to the given markers using Dijkstra on edge weights.
    Returns { node_id: {nearest_marker_id, nearest_marker_distance, avg_distance_to_markers, ...} }
    """
    radius = int(cfg.get("radius", 4))

    # Precompute single-source distances from each marker
    # Using the edge attribute 'weight'
    dist_maps: Dict[str, Dict[str, float]] = {}
    for m in markers:
        try:
            dist_maps[m] = nx.single_source_dijkstra_path_length(G, m, weight="weight", cutoff=None)
        except Exception:
            dist_maps[m] = {}

    # Collect per-node vector of distances to markers
    features: Dict[str, Dict[str, Any]] = {}
    for nid in G.nodes():
        # Gather distances to each marker
        dists: List[Tuple[str, float]] = []
        for m in markers:
            dm = dist_maps.get(m, {})
            if nid in dm:
                dists.append((m, float(dm[nid])))
            else:
                # unreachable -> treat as +inf; do not include in averages
                pass

        if not dists:
            # No reachable markers
            features[nid] = {
                "nearest_marker_id": None,
                "nearest_marker_distance": math.inf,
                "avg_distance_to_markers": math.inf,
                "distance_percentiles": {"p50": math.inf, "p90": math.inf},
                "anchor_coverage": {
                    "anchors_within_r": 0,
                    "min_anchor_distance": math.inf,
                    "avg_anchor_distance": math.inf,
                    "radius": radius,
                },
                "type_degrees": _type_degrees(G, nid),
                "risk_flags": _risk_flags(G, nid),
            }
            continue

        # Sort by distance
        dists_sorted = sorted(dists, key=lambda t: t[1])
        distances_only = [d for _, d in dists_sorted]

        nearest_marker_id, nearest_marker_distance = dists_sorted[0]
        avg_distance = sum(distances_only) / max(1, len(distances_only))

        p50 = _percentile(distances_only, 50)
        p90 = _percentile(distances_only, 90)

        within = [d for d in distances_only if d <= radius]
        anchor_coverage = {
            "anchors_within_r": len(within),
            "min_anchor_distance": min(distances_only) if distances_only else math.inf,
            "avg_anchor_distance": (sum(within) / len(within)) if within else math.inf,
            "radius": radius,
        }

        features[nid] = {
            "nearest_marker_id": nearest_marker_id,
            "nearest_marker_distance": nearest_marker_distance,
            "avg_distance_to_markers": avg_distance,
            "distance_percentiles": {"p50": p50, "p90": p90},
            "anchor_coverage": anchor_coverage,
            "type_degrees": _type_degrees(G, nid),
            "risk_flags": _risk_flags(G, nid),
        }

    return features


# ----------------------------
# Helpers
# ----------------------------

def _derive_cfg(settings: Optional[Mapping[str, Any]]) -> Dict[str, Any]:
    if not settings:
        return dict(_DEFAULT_CFG)
    # settings may contain a root or a nested "distance_features"
    df = settings.get("distance_features") if hasattr(settings, "get") else None
    if not df:
        return dict(_DEFAULT_CFG)
    # overlay user cfg
    cfg = dict(_DEFAULT_CFG)
    for k, v in dict(df).items():
        if isinstance(v, dict) and isinstance(cfg.get(k), dict):
            cfg[k] = {**cfg[k], **v}
        else:
            cfg[k] = v
    return cfg


def _percentile(values: List[float], p: float) -> float:
    if not values:
        return math.inf
    xs = sorted(values)
    if len(xs) == 1:
        return xs[0]
    k = (len(xs) - 1) * (p / 100.0)
    f = math.floor(k)
    c = math.ceil(k)
    if f == c:
        return xs[int(k)]
    d0 = xs[f] * (c - k)
    d1 = xs[c] * (k - f)
    return d0 + d1


def _type_degrees(G: "nx.DiGraph", nid: str) -> Dict[str, int]:
    # Summarize degree by edge label direction
    labels_out: Dict[str, int] = {}
    labels_in: Dict[str, int] = {}
    for _, v, data in G.out_edges(nid, data=True):
        lab = data.get("label") or "__any__"
        labels_out[lab] = labels_out.get(lab, 0) + 1
    for u, _, data in G.in_edges(nid, data=True):
        lab = data.get("label") or "__any__"
        labels_in[lab] = labels_in.get(lab, 0) + 1
    # Flatten with prefixes for readability
    flat: Dict[str, int] = {}
    for lab, cnt in labels_out.items():
        flat[f"{lab}_out"] = cnt
    for lab, cnt in labels_in.items():
        flat[f"{lab}_in"] = cnt
    # also include totals
    flat["deg_out"] = sum(labels_out.values())
    flat["deg_in"] = sum(labels_in.values())
    return flat


def _risk_flags(G: "nx.DiGraph", nid: str) -> Dict[str, Any]:
    # Lightweight structural risk hints; avoid expensive computations by default
    is_articulation = False
    try:
        und = G.to_undirected()
        # NetworkX articulation_points returns a generator
        # To keep cost bounded, check degree>1 before calling (quick prune)
        if und.degree(nid) > 1:
            # Using a quick check on a small subgraph could be an enhancement.
            # For now, do a targeted articulation test by removing node and checking connectivity change.
            before = nx.number_connected_components(und)
            und2 = und.copy()
            und2.remove_node(nid)
            after = nx.number_connected_components(und2)
            is_articulation = after > before
    except Exception:
        pass

    return {
        "is_articulation": bool(is_articulation),
        "is_bridge_adjacent": False,  # could be added later
        "notes": "",
    }
