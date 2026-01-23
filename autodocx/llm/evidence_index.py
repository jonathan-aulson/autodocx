# autodocx/llm/evidence_index.py
from __future__ import annotations
import json
from pathlib import Path
from typing import Dict, Any

def build_evidence_index(out_dir: str | Path) -> Dict[str, Dict[str, Any]]:
    out_dir = Path(out_dir)
    idx: Dict[str, Dict[str, Any]] = {}
    # Load SIRs (canonical locations only)
    sir_dirs = [
        out_dir / "signals" / "sir_v2",
        out_dir / "signals" / "sir_v1",
    ]
    for sir_dir in sir_dirs:
        if not sir_dir.exists():
            continue
        for p in sorted(sir_dir.glob("*.json")):
            try:
                j = json.loads(p.read_text(encoding="utf-8"))
            except Exception:
                continue
            sid = j.get("id") or p.stem
            evids = j.get("evidence", []) or []
            roles_ev = j.get("roles_evidence", {}) or {}
            # Also index protocol-specific evidence from enrichment
            enrichment = j.get("props", {}).get("enrichment", {}) if isinstance(j.get("props"), dict) else {}
            mapper_ev = enrichment.get("mapper_evidence") or []
            jdbc_ev = enrichment.get("jdbc_sql") or enrichment.get("jdbc_evidence") or []
            wsdl_ev = enrichment.get("wsdl") or []
            xsd_ev = enrichment.get("xsd") or []
            protocol_sets = {
                "mapper": mapper_ev,
                "jdbc": jdbc_ev,
                "wsdl": wsdl_ev,
                "xsd": xsd_ev,
            }
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
            for proto, evs in protocol_sets.items():
                for e in evs or []:
                    key = f"{sid}#{proto}-e{cid}"
                    idx[key] = e
                    cid += 1
        break  # prefer the first existing dir

    # Load artifacts
    a_file = out_dir / "artifacts" / "artifacts.json"
    if not a_file.exists():
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
    evidence_dir = out_dir / "evidence"
    evidence_dir.mkdir(parents=True, exist_ok=True)
    (evidence_dir / "evidence_index.json").write_text(json.dumps(idx, indent=2), encoding="utf-8")
    return idx
