# autodocx/llm/evidence_index.py
from __future__ import annotations
import json
from pathlib import Path
from typing import Dict, Any

def build_evidence_index(out_dir: str | Path) -> Dict[str, Dict[str, Any]]:
    out_dir = Path(out_dir)
    idx: Dict[str, Dict[str, Any]] = {}
    # Load SIRs
    sir_dir = out_dir / "sir"
    if sir_dir.exists():
        for p in sorted(sir_dir.glob("*.json")):
            try:
                j = json.loads(p.read_text(encoding="utf-8"))
            except Exception:
                continue
            sid = j.get("id") or p.stem
            # SIR-level evidence entries
            evids = j.get("evidence", []) or []
            # also include roles_evidence
            roles_ev = j.get("roles_evidence", {}) or {}
            cid = 0
            for e in evids:
                key = f"{sid}#e{cid}"
                idx[key] = e
                cid += 1
            for role, evs in roles_ev.items():
                for e in evs:
                    key = f"{sid}#role-{role}-e{cid}"
                    idx[key] = e
                    cid += 1
    # Load artifacts
    a_file = out_dir / "artifacts.json"
    if a_file.exists():
        try:
            arts = json.loads(a_file.read_text(encoding="utf-8"))
            for a in arts:
                aid = a.get("name") or Path(a.get("repo_path","")).name or "artifact"
                for i, e in enumerate(a.get("evidence", []) or []):
                    key = f"artifact:{aid}#e{i}"
                    # ensure snippet trimmed
                    idx[key] = e
        except Exception:
            pass

    # Persist index
    (out_dir / "evidence_index.json").write_text(json.dumps(idx, indent=2), encoding="utf-8")
    return idx
