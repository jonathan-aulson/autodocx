# tests/test_visual_marker_matching.py
import pytest

from autodocx.visuals.graphviz_flows import (
    _safe_slug,
    _get_visuals_cfg,
    _marker_match,
)

def test_exact_policy_matches_when_slugs_equal():
    visuals_cfg = _get_visuals_cfg({"visuals": {"match_policy": "exact", "slug_match_strip_prefix": True}})
    marker_id = "Workflow:MyProc"
    # Simulate Graphviz node slug for the full id
    node_slug_full = _safe_slug("Workflow:MyProc").lower()
    assert _marker_match(node_slug_full, marker_id, visuals_cfg) is True

    # Simulate Graphviz node slug for the suffix only (strip prefix)
    node_slug_suffix = _safe_slug("MyProc").lower()
    # exact policy should also match if whitelist prefixes are supplied and strip logic applied
    # By default whitelist is empty; this asserts exact does NOT match suffix-only unless exact slugified forms coincide
    assert _marker_match(node_slug_suffix, marker_id, visuals_cfg) is False

    # If whitelist prefixes include 'workflow:', allow prefix-stripped exact match
    visuals_cfg_pfx = dict(visuals_cfg)
    visuals_cfg_pfx["match_whitelist_prefixes"] = ["Workflow:"]
    assert _marker_match(node_slug_suffix, marker_id, visuals_cfg_pfx) is True


def test_slug_policy_matches_suffix_or_full_slug():
    visuals_cfg = _get_visuals_cfg({"visuals": {"match_policy": "slug", "slug_match_strip_prefix": True}})
    marker_id = "Workflow:OrderProcessor"
    node_slug1 = _safe_slug("OrderProcessor").lower()
    node_slug2 = _safe_slug("Workflow:OrderProcessor").lower()
    assert _marker_match(node_slug1, marker_id, visuals_cfg) is True
    assert _marker_match(node_slug2, marker_id, visuals_cfg) is True

    # partial substring match should also succeed in slug mode
    node_slug_short = _safe_slug("order-processor-step").lower()
    assert _marker_match(node_slug_short, marker_id, visuals_cfg) is True


def test_fuzzy_policy_respects_threshold():
    # Lower threshold to make a fuzzy match easier
    visuals_cfg = _get_visuals_cfg({"visuals": {"match_policy": "fuzzy", "fuzzy_threshold": 0.6}})
    marker_id = "SearchMovies"
    # Simulate a misspelled node slug
    node_slug = _safe_slug("SreachMovies").lower()
    assert _marker_match(node_slug, marker_id, visuals_cfg) is True

    # With a higher threshold it should fail
    visuals_cfg_strict = _get_visuals_cfg({"visuals": {"match_policy": "fuzzy", "fuzzy_threshold": 0.95}})
    assert _marker_match(node_slug, marker_id, visuals_cfg_strict) is False


def test_unknown_policy_falls_back_to_slug():
    visuals_cfg = _get_visuals_cfg({"visuals": {"match_policy": "nonexistent-policy"}})
    marker_id = "API:Orders"
    node_slug = _safe_slug("Orders").lower()
    assert _marker_match(node_slug, marker_id, visuals_cfg) is True
