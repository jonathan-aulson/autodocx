# autodocx/llm/grouping.py
from __future__ import annotations
import json
from pathlib import Path
from typing import Dict, Any, List, Optional

from autodocx.utils.components import derive_component_from_path, normalize_component_name

def load_artifacts(out_dir: Path) -> List[Dict[str,Any]]:
    p = Path(out_dir) / "artifacts.json"
    if not p.exists():
        return []
    try:
        return json.loads(p.read_text(encoding="utf-8"))
    except Exception:
        return []

def load_sirs(out_dir: Path) -> List[Dict[str,Any]]:
    sirs = []
    sir_dir = Path(out_dir) / "sir"
    if sir_dir.exists():
        for f in sorted(sir_dir.glob("*.json")):
            try:
                s = json.loads(f.read_text(encoding="utf-8"))
                sirs.append(s)
            except Exception:
                continue
    return sirs

def group_by_component(out_dir: str | Path, repo_root: Optional[Path] = None) -> Dict[str, Dict[str, Any]]:
    out_dir = Path(out_dir)
    repo_root = Path(repo_root).resolve() if repo_root else None
    artifacts = load_artifacts(out_dir)
    sirs = load_sirs(out_dir)
    groups: Dict[str, Dict[str, Any]] = {}
    # primary grouping by artifact.component_or_service
    for a in artifacts:
        comp = a.get("component_or_service")
        if not comp and repo_root and a.get("repo_path"):
            comp = derive_component_from_path(repo_root, a["repo_path"])
        if not comp and a.get("repo_path"):
            comp = normalize_component_name(Path(a["repo_path"]).parts[0]) if Path(a["repo_path"]).parts else ""
        comp = comp or "ungrouped"
        g = groups.setdefault(comp, {"artifacts": [], "sirs": []})
        g["artifacts"].append(a)
    # assign sirs to groups using component_or_service in SIR or fallback to artifact mapping by file path
    for s in sirs:
        comp = s.get("component_or_service") or ""
        if comp:
            groups.setdefault(comp, {"artifacts": [], "sirs": []})["sirs"].append(s)
        else:
            # try to match by file prefix to artifact repo_path
            fpath = s.get("file","")
            assigned = False
            for comp_name, group in groups.items():
                for a in group["artifacts"]:
                    rp = a.get("repo_path","")
                    if rp and fpath.startswith(rp[:min(len(rp), 100)]):
                        group["sirs"].append(s); assigned = True; break
                if assigned:
                    break
            if not assigned:
                groups.setdefault("ungrouped", {"artifacts": [], "sirs": []})["sirs"].append(s)
    return groups
