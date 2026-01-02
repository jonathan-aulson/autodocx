# autodocx/utils/roles.py
from __future__ import annotations
import json
from pathlib import Path
from typing import Dict, List, Iterable, Tuple, Set

_ROLE_MAP = None

def _load_role_map() -> Dict[str, List[str]]:
    global _ROLE_MAP
    if _ROLE_MAP is not None:
        return _ROLE_MAP
    try:
        roles_file = Path(__file__).resolve().parents[1] / "roles" / "roles.json"
        if roles_file.exists():
            with roles_file.open("r", encoding="utf-8") as fh:
                j = json.load(fh)
                _ROLE_MAP = j.get("prefix_roles", {}) or {}
                return _ROLE_MAP
    except Exception:
        pass
    _ROLE_MAP = {}
    return _ROLE_MAP

def map_connectors_to_roles_with_evidence(connectors_with_evidence: Iterable[Tuple[str, Dict]]) -> Dict[str, List[Dict]]:
    """
    Input: iterable of (connector_or_type_string, evidence_anchor_dict)
    Output: mapping role -> list of evidence anchors (deduped)
    Matching uses substring contains on connector string against role prefixes in roles.json.
    """
    role_map = _load_role_map()
    roles_to_evidence: Dict[str, List[Dict]] = {}
    if not role_map:
        return {}

    for raw, evidence in connectors_with_evidence or []:
        if not raw:
            continue
        s = str(raw).lower()
        for prefix, mapped in role_map.items():
            if not prefix:
                continue
            if prefix in s:
                for r in mapped:
                    lst = roles_to_evidence.setdefault(r, [])
                    # avoid exact duplicate evidence
                    if evidence not in lst:
                        lst.append(evidence)
    return roles_to_evidence


def map_connectors_to_roles(connectors: Iterable[str]) -> List[str]:
    """
    Lightweight helper used by extractors that only need the role labels.
    Mirrors the prefix-matching behavior of map_connectors_to_roles_with_evidence
    but returns a flat, deduped list without evidence payloads.
    """
    role_map = _load_role_map()
    if not role_map:
        return []
    roles: List[str] = []
    seen: Set[str] = set()
    for raw in connectors or []:
        if not raw:
            continue
        s = str(raw).lower()
        for prefix, mapped in role_map.items():
            if not prefix:
                continue
            if prefix in s:
                for r in mapped:
                    if r not in seen:
                        seen.add(r)
                        roles.append(r)
    return roles
