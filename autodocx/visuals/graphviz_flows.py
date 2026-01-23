# autodocx/visuals/graphviz_flows.py
from __future__ import annotations
from pathlib import Path
from typing import Dict, Any, List, Optional
import re

try:
    from graphviz import Digraph
except Exception:
    Digraph = None  # We'll guard usage at call sites

word_re = re.compile(r"[A-Za-z0-9.:/\-]+")


# Default visuals config (merged with project settings when present)
_DEFAULT_VISUALS_CFG: Dict[str, Any] = {
    "marker_highlight": {
        "enabled": True,
        "color": "#FFD700",
        "legend_text": "Marker",
    },
    # match_policy: "exact" | "slug" | "fuzzy"
    "match_policy": "slug",
    "fuzzy_threshold": 0.7,
    "match_whitelist_prefixes": [],  # optional list of prefixes to prefer for exact matching
    "slug_match_strip_prefix": True,
}


def _get_visuals_cfg(settings: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
    """
    Merge provided settings (may be the whole settings dict or a visuals sub-dict)
    with module defaults and return the visuals config.
    """
    cfg = dict(_DEFAULT_VISUALS_CFG)
    if not settings:
        return cfg

    # settings may be root settings or a nested block
    vf = None
    if isinstance(settings, dict):
        vf = settings.get("distance_features", {}).get("visuals") or settings.get("visuals") or settings.get("distance_features")
        # Also accept a top-level "visuals" or "distance_features.visuals"
        if not vf:
            vf = settings.get("visuals")
    if isinstance(vf, dict):
        # shallow merge
        cfg.update({k: v for k, v in vf.items() if k in cfg})
        # merge nested marker_highlight dict if provided
        mh = vf.get("marker_highlight")
        if isinstance(mh, dict):
            cfg["marker_highlight"].update(mh)
        # override other keys explicitly if provided
        for key in ("match_policy", "fuzzy_threshold", "match_whitelist_prefixes", "slug_match_strip_prefix"):
            if key in vf:
                cfg[key] = vf[key]
    return cfg


def _safe_slug(s: Optional[str], max_len: int = 80) -> str:
    """Create a filesystem-friendly slug from a string."""
    if not s:
        return "unnamed"
    tokens = word_re.findall(str(s))
    out = "-".join(tokens).strip("-")
    return out[:max_len] if out else "unnamed"


def ensure_assets_dir(out_docs_dir: Path, group_id: str, component_key: Optional[str] = None) -> Path:
    base = out_docs_dir / "assets" / "graphs" / _safe_slug(group_id)
    if component_key:
        base = base / _safe_slug(component_key)
    base.mkdir(parents=True, exist_ok=True)
    return base


def pick_label_from_snippet(snippet: str) -> Optional[str]:
    """Look for known BW-style activity hints inside an evidence snippet."""
    s = (snippet or "").lower()

    # Known BW palette / activity hints
    if "http:sendhttprequest" in s or "sendhttprequest" in s:
        return "SendHTTPRequest"
    if "restjson#//jsonrender" in s or "jsonrender" in s:
        return "JsonRender"
    if "restjson#//jsonparser" in s or "jsonparser" in s:
        return "JsonParser"
    if "internalactivity#//end" in s or "bw.internal.end" in s or ("type href" in s and "internalactivity#//end" in s):
        return "End"
    if ("tibex:receiveevent" in s) or ("createinstance" in s and "receiveevent" in s):
        return "Start (receiveEvent)"
    if "httpparameters" in s or "httpparameters" in s:
        return "HTTP parameters"

    # Linkname is not a label, just hint ordering; skip
    if "linkname" in s:
        return None

    return None


def label_for_step(step: Dict[str, Any]) -> str:
    """
    Produce a human-friendly label for a step.
    Prefer specific activity names, otherwise use BW type hints from evidence snippets.
    """
    name = step.get("friendly_display") or step.get("name") or ""
    typ = (step.get("type") or "") or ""
    # evidence snippet may contain richer hints
    ev = step.get("evidence") or {}
    snippet = ""
    if isinstance(ev, dict):
        snippet = str(ev.get("snippet") or "")

    label_hint = None
    if snippet:
        label_hint = pick_label_from_snippet(snippet)

    if label_hint:
        return label_hint

    if name and name.lower() not in {"extensionactivity", "bwactivity", "activityconfig"}:
        return str(name)

    if typ:
        # Title-case the type for readability
        return str(typ).capitalize()

    # Fallback
    return name or "step"


def _is_wrapper_step(step: Dict[str, Any]) -> bool:
    name = str(step.get("name") or "").lower()
    typ = str(step.get("type") or "").lower()
    if name in {"extensionactivity", "bwactivity", "activityconfig"}:
        return True
    if typ in {"extensionactivity", "bwactivity", "activityconfig"}:
        return True
    if name.startswith("extensionactivity") or name.startswith("bwactivity") or name.startswith("activityconfig"):
        return True
    return False


def _step_fill_color(step: Dict[str, Any], label: str) -> str:
    tokens = " ".join(
        [
            str(step.get("connector") or ""),
            str(step.get("type") or ""),
            str(label or ""),
        ]
    ).lower()
    if "http" in tokens or "rest" in tokens:
        return "#D3F9D8"
    if "jdbc" in tokens or "sql" in tokens or "db" in tokens:
        return "#FFE8CC"
    if "json" in tokens or "parse" in tokens or "render" in tokens or "mapper" in tokens:
        return "#C3DAFE"
    if "log" in tokens or "audit" in tokens or "trace" in tokens:
        return "#FDE2E4"
    return "#E8F0FE"


def bw_detect_trigger_label(sir: Dict[str, Any]) -> Optional[str]:
    """Try to detect a trigger label from SIR triggers or step evidence (receiveEvent)."""
    for t in sir.get("triggers", []) or []:
        tname = t.get("name") or t.get("type") or "Trigger"
        return f"Trigger: {tname}"

    # Fallback: look for receiveEvent in step evidence snippets
    for st in sir.get("steps", []) or []:
        ev = st.get("evidence") or {}
        snippet = str((ev or {}).get("snippet") or "")
        if "tibex:receiveevent" in snippet.lower():
            return "Trigger: receiveEvent"

    return None


# -------------------------
# Matching helpers
# -------------------------
def _levenshtein_distance(a: str, b: str) -> int:
    """Simple Levenshtein distance (iterative DP)."""
    a = a or ""
    b = b or ""
    if a == b:
        return 0
    la, lb = len(a), len(b)
    if la == 0:
        return lb
    if lb == 0:
        return la
    # Use a memory-efficient DP
    prev = list(range(lb + 1))
    for i, ca in enumerate(a, start=1):
        curr = [i] + [0] * lb
        for j, cb in enumerate(b, start=1):
            cost = 0 if ca == cb else 1
            curr[j] = min(prev[j] + 1, curr[j - 1] + 1, prev[j - 1] + cost)
        prev = curr
    return prev[lb]


def _levenshtein_ratio(a: str, b: str) -> float:
    """Normalized similarity ratio between 0..1 (1.0 -> identical)."""
    a = (a or "").lower()
    b = (b or "").lower()
    if not a and not b:
        return 1.0
    dist = _levenshtein_distance(a, b)
    max_len = max(len(a), len(b))
    if max_len == 0:
        return 1.0
    return max(0.0, 1.0 - (dist / max_len))


def _marker_match(node_slug: str, marker_id: str, visuals_cfg: Dict[str, Any]) -> bool:
    """
    Return True if the given node_slug matches the marker_id according to visuals_cfg.
    node_slug is the slugified form of the node name (as used for Graphviz ids).
    marker_id is the marker node id from graph_features (often forms like 'Workflow:Foo' or 'API:Orders').
    visuals_cfg: dict controlling match_policy, fuzzy_threshold, slug stripping, etc.
    """
    policy = visuals_cfg.get("match_policy", "slug") or "slug"
    fuzzy_threshold = float(visuals_cfg.get("fuzzy_threshold", 0.7) or 0.7)
    strip_prefix = bool(visuals_cfg.get("slug_match_strip_prefix", True))
    whitelist_prefixes = visuals_cfg.get("match_whitelist_prefixes") or []

    # normalize inputs
    node_slug_norm = (node_slug or "").lower()
    mid = (marker_id or "") or ""
    mid_norm = mid.lower()

    # exact policy: compare the raw ids OR compare slugified forms (support both)
    if policy == "exact":
        if node_slug_norm == _safe_slug(mid).lower():
            return True
        # also accept exact string match against raw marker id (some nodes kept as raw ids)
        if node_slug_norm == mid_norm:
            return True
        # if whitelist prefixes present, allow prefix-stripped exact match
        for pfx in whitelist_prefixes:
            if mid_norm.startswith(pfx.lower()):
                stripped = mid_norm[len(pfx) :]
                if node_slug_norm == _safe_slug(stripped).lower():
                    return True
        return False

    # slug policy: strip prefix optionally and compare slug(node) == slug(marker_suffix)
    if policy == "slug":
        candidate = mid
        if ":" in mid and strip_prefix:
            candidate = mid.split(":", 1)[1]
        cand_slug = _safe_slug(candidate).lower()
        if node_slug_norm == cand_slug:
            return True
        # also accept slug of full id (in case node slugs kept prefix)
        if node_slug_norm == _safe_slug(mid).lower():
            return True
        # fallback: substring match
        if cand_slug:
            compact_node = node_slug_norm.replace("-", "").replace("_", "")
            compact_cand = cand_slug.replace("-", "").replace("_", "")
            if compact_cand and compact_cand in compact_node:
                return True
        return False

    # fuzzy policy: use levenshtein ratio against several forms
    if policy == "fuzzy":
        candidates = [mid]
        if ":" in mid and strip_prefix:
            candidates.append(mid.split(":", 1)[1])
        candidates.append(_safe_slug(mid))
        for c in candidates:
            if not c:
                continue
            ratio = _levenshtein_ratio(node_slug_norm, str(c).lower())
            if ratio >= fuzzy_threshold:
                return True
        return False

    # Unknown policy: fallback to slug
    return _marker_match(node_slug, marker_id, {**visuals_cfg, "match_policy": "slug"})


# -------------------------
# Marker collection helpers
# -------------------------
def _collect_marker_ids_from_sirs(sirs: List[Dict[str, Any]]) -> List[str]:
    """
    Collect a list of marker ids from SIRs' graph_features.markers fields.
    Markers may be either dicts like {'id': 'Workflow:Foo', 'type': 'Workflow'} or simple strings.
    """
    out: List[str] = []
    seen = set()
    for s in sirs or []:
        gf = s.get("graph_features") or {}
        markers = gf.get("markers") or []
        for m in markers or []:
            mid = m.get("id") if isinstance(m, dict) else (m or "")
            if not mid:
                continue
            if mid not in seen:
                seen.add(mid)
                out.append(mid)
    return out


# -------------------------
# Rendering functions
# -------------------------
def render_bw_process_flow_svg(
    sir: Dict[str, Any],
    out_docs_dir: Path,
    group_id: str,
    component_key: str,
    settings: Optional[Dict[str, Any]] = None,
) -> Optional[str]:
    """
    Render a simple process flow for a BW-like SIR using Graphviz.
    - Nodes: trigger (if any) + each step
    - Edges: sequential from trigger -> steps in listed order (until we have formal transitions)
    - Marker highlighting is controlled by visuals config (match policy, color, legend text).
    Returns the relative path under out_docs_dir to the generated SVG, or None if rendering isn't possible.
    """
    visuals_cfg = _get_visuals_cfg(settings)
    mh_cfg = visuals_cfg.get("marker_highlight", {})

    if Digraph is None:
        return None

    props = sir.get("props") if isinstance(sir.get("props"), dict) else {}
    steps: List[Dict[str, Any]] = list(sir.get("steps") or [])
    if not steps:
        steps = list(props.get("steps") or [])
    triggers = list(sir.get("triggers") or [])
    if not triggers:
        triggers = list(props.get("triggers") or [])
    if not steps and not triggers:
        return None  # nothing to draw

    title = sir.get("name") or sir.get("id") or "process"
    file_slug = _safe_slug(title)
    assets_dir = ensure_assets_dir(out_docs_dir, group_id, component_key)
    svg_path = assets_dir / f"{file_slug}.svg"

    dot = Digraph(name="G", format="svg")
    # Styling to resemble the prior SVG: rounded rectangles, Times font, LR layout
    dot.attr("graph", rankdir="LR")
    dot.attr("node", shape="box", style="rounded", fontname="Times New Roman")
    dot.attr("edge", fontname="Times New Roman")

    # Prepare marker ids from this single SIR (process-level markers)
    marker_ids = _collect_marker_ids_from_sirs([sir])

    # Also include markers from sir.props if present (compat)
    for key in ("marker_names", "markers"):
        cand = props.get(key)
        if cand:
            for c in (cand if isinstance(c, list) else [cand]):
                if c and c not in marker_ids:
                    marker_ids.append(str(c))

    # Trigger node (optional)
    trigger_label = bw_detect_trigger_label(sir)
    prev_id: Optional[str] = None
    if trigger_label:
        trig_id = _safe_slug(trigger_label)
        highlight = False
        if visuals_cfg.get("marker_highlight", {}).get("enabled", True):
            for mid in marker_ids:
                if _marker_match(trig_id, mid, visuals_cfg):
                    highlight = True
                    break
        if highlight:
            dot.node(trig_id, trigger_label, style="filled,rounded", fillcolor=mh_cfg.get("color", "#FFD700"))
        else:
            dot.node(trig_id, trigger_label, style="filled,rounded", fillcolor="#D3F9D8")
        prev_id = trig_id

    filtered_steps = [st for st in steps if not _is_wrapper_step(st)]
    if not filtered_steps:
        filtered_steps = steps

    # Steps in order (until formal transitions are available)
    for idx, st in enumerate(filtered_steps, start=1):
        sid = _safe_slug(st.get("name") or st.get("type") or f"step-{idx}")
        label = label_for_step(st)
        highlight = False
        if visuals_cfg.get("marker_highlight", {}).get("enabled", True):
            for mid in marker_ids:
                if _marker_match(sid, mid, visuals_cfg):
                    highlight = True
                    break
        if highlight:
            dot.node(sid, label, style="filled,rounded", fillcolor=mh_cfg.get("color", "#FFD700"))
        else:
            dot.node(sid, label, style="filled,rounded", fillcolor=_step_fill_color(st, label))
        if prev_id:
            dot.edge(prev_id, sid)
        prev_id = sid

    # Add a tiny legend for markers if any present and highlighting enabled
    if marker_ids and visuals_cfg.get("marker_highlight", {}).get("enabled", True):
        legend_id = f"legend_marker_{file_slug}"
        legend_text = mh_cfg.get("legend_text", "Marker")
        dot.node(legend_id, legend_text, shape="box", style="filled", fillcolor=mh_cfg.get("color", "#FFD700"), fontname="Times New Roman")
        try:
            if prev_id:
                dot.edge(legend_id, prev_id, style="invis")
        except Exception:
            pass

    try:
        # render: provide filename without extension
        dot.render(filename=svg_path.with_suffix("").as_posix(), format="svg", cleanup=True)
    except Exception:
        return None

    try:
        rel = svg_path.relative_to(out_docs_dir).as_posix()
    except Exception:
        # fallback to absolute path as string
        rel = str(svg_path)
    return rel


def render_relationship_sequence_svg(
    sirs: List[Dict[str, Any]],
    out_docs_dir: Path,
    group_id: str,
    component_key: str,
    settings: Optional[Dict[str, Any]] = None,
) -> Optional[str]:
    """
    Render a lightweight sequence diagram based on relationships emitted by SIRs.
    Outputs assets/graphs/<group>/<component>/relationships.svg and returns the docs-relative path.
    """
    if Digraph is None:
        return None

    rels: List[Dict[str, Any]] = []
    for sir in sirs or []:
        rels.extend(sir.get("relationships") or (sir.get("props") or {}).get("relationships") or [])

    if not rels:
        return None

    def _node_id(label: str, used: Dict[str, str]) -> str:
        if label in used:
            return used[label]
        base = _safe_slug(label) or "node"
        candidate = base
        counter = 1
        while candidate in used.values():
            candidate = f"{base}-{counter}"
            counter += 1
        used[label] = candidate
        return candidate

    visuals_cfg = _get_visuals_cfg(settings)
    dot = Digraph(name="sequence", format="svg")
    dot.attr("graph", rankdir="LR", splines="polyline", fontname="Times New Roman")
    dot.attr("node", shape="box", style="rounded", fontname="Times New Roman")
    dot.attr("edge", fontname="Times New Roman")

    node_ids: Dict[str, str] = {}
    edges_added = 0
    for rel in rels:
        if edges_added >= 20:
            break
        src = (rel.get("source") or {}).get("name") or (rel.get("source") or {}).get("type")
        tgt = (rel.get("target") or {}).get("display") or (rel.get("target") or {}).get("ref")
        if not src or not tgt:
            continue
        op = (rel.get("operation") or {}).get("type") or rel.get("connector") or ""
        src_id = _node_id(src, node_ids)
        tgt_id = _node_id(tgt, node_ids)
        dot.node(src_id, src)
        dot.node(tgt_id, tgt)
        dot.edge(src_id, tgt_id, label=op)
        edges_added += 1

    if edges_added == 0:
        return None

    assets_dir = ensure_assets_dir(out_docs_dir, group_id, component_key)
    svg_path = assets_dir / f"{_safe_slug(component_key)}-relationships.svg"
    try:
        dot.render(filename=svg_path.with_suffix("").as_posix(), format="svg", cleanup=True)
    except Exception:
        return None

    try:
        return svg_path.relative_to(out_docs_dir).as_posix()
    except Exception:
        return str(svg_path)


def render_rollup_journey_svgs(
    component_doc: Dict[str, Any],
    out_docs_dir: Path,
    group_id: str,
    component_key: str,
) -> List[Dict[str, str]]:
    """
    Render SVG journey diagrams sourced from LLM rollup blueprints. Returns
    [{"title": "...", "path": "docs-relative-path"}, ...].
    """
    if Digraph is None:
        return []

    component = component_doc.get("component") or component_doc
    blueprints = component.get("journey_blueprints") or []
    if not blueprints:
        return []

    assets_dir = ensure_assets_dir(out_docs_dir, group_id, component_key)
    outputs: List[Dict[str, str]] = []
    for idx, blueprint in enumerate(blueprints, start=1):
        steps = [str(step).strip() for step in blueprint.get("steps") or [] if str(step).strip()]
        if len(steps) < 2:
            continue
        dot = Digraph(name=f"journey_{idx}", format="svg")
        dot.attr(
            "graph",
            rankdir="LR",
            splines="spline",
            fontname="Helvetica",
            bgcolor="transparent",
            labelloc="t",
            label=blueprint.get("title") or f"Journey {idx}",
        )
        dot.attr("node", shape="box", style="rounded,filled", fontname="Helvetica", color="#273142")
        dot.attr("edge", color="#7C8FB5", penwidth="1.4", arrowsize="0.8")

        palette = ["#D3F9D8", "#E8F0FE", "#C3DAFE", "#FFE8CC", "#FDE2E4"]
        node_ids: List[str] = []
        for step_idx, label in enumerate(steps, start=1):
            node_id = f"journey_{idx}_{step_idx}"
            fill = palette[(step_idx - 1) % len(palette)]
            if step_idx == len(steps):
                fill = "#FFE8CC"
            dot.node(node_id, f"{step_idx}. {label}", fillcolor=fill)
            node_ids.append(node_id)

        for src, dst in zip(node_ids, node_ids[1:]):
            dot.edge(src, dst)

        svg_path = assets_dir / f"{_safe_slug(component_key)}-journey-{idx}.svg"
        try:
            dot.render(filename=svg_path.with_suffix("").as_posix(), format="svg", cleanup=True)
        except Exception:
            continue

        try:
            rel = svg_path.relative_to(out_docs_dir).as_posix()
        except Exception:
            rel = str(svg_path)
        outputs.append({"title": blueprint.get("title") or f"Journey {idx}", "path": rel})

    return outputs


def render_component_overview_svg(
    sirs: List[Dict[str, Any]],
    out_docs_dir: Path,
    group_id: str,
    component_key: str,
    settings: Optional[Dict[str, Any]] = None,
) -> Optional[str]:
    """
    Render a coarse module overview graph (orchestration -> sub-processes).
    Heuristic until extractor populates calls_flows:
    - Pick a SIR whose name contains 'Process' or 'Main' as orchestrator.
    - Add other SIRs as nodes.
    - Draw edges from orchestrator to others.
    - Highlight marker nodes (gold fill by default) when markers are present in SIR graph_features.
    """
    visuals_cfg = _get_visuals_cfg(settings)
    mh_cfg = visuals_cfg.get("marker_highlight", {})

    if Digraph is None or not sirs:
        return None

    # Heuristic orchestrator selection
    orchestrators = [
        s for s in sirs
        if "process" in (s.get("name") or "").lower() or "main" in (s.get("name") or "").lower()
    ]
    orch = orchestrators[0] if orchestrators else sirs[0]

    assets_dir = ensure_assets_dir(out_docs_dir, group_id, component_key)
    svg_path = assets_dir / f"{_safe_slug(component_key)}-module-overview.svg"

    dot = Digraph(name="G", format="svg")
    dot.attr("graph", rankdir="LR")
    dot.attr("node", shape="box", style="rounded", fontname="Times New Roman")
    dot.attr("edge", fontname="Times New Roman")

    # Collect global marker ids across component SIRs
    marker_ids = _collect_marker_ids_from_sirs(sirs)

    # Nodes
    nodes: Dict[str, str] = {}
    for s in sirs:
        nid = _safe_slug(s.get("name") or s.get("id") or "sir")
        label = s.get("name") or s.get("id") or nid
        nodes[s.get("name") or nid] = nid
        highlight = False
        if marker_ids and visuals_cfg.get("marker_highlight", {}).get("enabled", True):
            for mid in marker_ids:
                if _marker_match(nid, mid, visuals_cfg):
                    highlight = True
                    break
        if highlight:
            dot.node(nid, label, style="filled,rounded", fillcolor=mh_cfg.get("color", "#FFD700"))
        else:
            dot.node(nid, label)

    # Edges (heuristic from orchestrator to others)
    orch_name = orch.get("name") or orch.get("id") or _safe_slug("orchestrator")
    orch_id = nodes.get(orch.get("name")) or _safe_slug(orch_name)
    if orch_id not in nodes.values():
        # ensure orchestrator node exists
        if any(_marker_match(orch_id, mid, visuals_cfg) for mid in marker_ids):
            dot.node(orch_id, orch_name, style="filled,rounded", fillcolor=mh_cfg.get("color", "#FFD700"))
        else:
            dot.node(orch_id, orch_name)

    for s in sirs:
        sid = nodes.get(s.get("name"))
        if not sid or sid == orch_id:
            continue
        dot.edge(orch_id, sid)

    # Helper for sample edges
    names = [s.get("name", "") for s in sirs]

    def _has(n: str) -> bool:
        return any(n.lower() in (x or "").lower() for x in names)

    def _id_for(n: str) -> Optional[str]:
        for s in sirs:
            if n.lower() in (s.get("name", "") or "").lower():
                return nodes.get(s.get("name"))
        return None

    # Example edges for common sample flows (movie catalog)
    if _has("SearchMovies") and _has("GetRatings"):
        a, b = _id_for("SearchMovies"), _id_for("GetRatings")
        if a and b:
            dot.edge(a, b, label="optional enrichment")
    if _has("SearchMovies") and _has("SortMovies"):
        a, b = _id_for("SearchMovies"), _id_for("SortMovies")
        if a and b:
            dot.edge(a, b, label="list of movies")
    if _has("GetRatings") and _has("SortMovies"):
        a, b = _id_for("GetRatings"), _id_for("SortMovies")
        if a and b:
            dot.edge(a, b, label="movies with ratings")

    # Add legend if markers present
    if marker_ids and visuals_cfg.get("marker_highlight", {}).get("enabled", True):
        legend_id = f"legend_marker_{_safe_slug(component_key)}"
        legend_text = mh_cfg.get("legend_text", "Marker")
        dot.node(legend_id, legend_text, shape="box", style="filled", fillcolor=mh_cfg.get("color", "#FFD700"), fontname="Times New Roman")
        try:
            dot.edge(legend_id, orch_id, style="invis")
        except Exception:
            pass

    try:
        dot.render(filename=svg_path.with_suffix("").as_posix(), format="svg", cleanup=True)
    except Exception:
        return None

    try:
        rel = svg_path.relative_to(out_docs_dir).as_posix()
    except Exception:
        rel = str(svg_path)
    return rel
