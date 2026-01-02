#!/usr/bin/env python3
# .roo/tools/bw/pb_enrich.py
from __future__ import annotations
import re, json
from pathlib import Path
from typing import Dict, Any, List

ID_TOKENS = ["SSN","CustomerId","CustomerID","AccountNumber","FICOScore","Rating","CorrelationId","MovieId","Title","Id","ID"]

def enrich_project_artifacts_pb(project_root: Path) -> Dict[str, Any]:
    # harvest .ini/.json for endpoints
    endpoints = []
    for p in list(project_root.rglob("*.ini")) + list(project_root.rglob("*.json")):
        try:
            txt = p.read_text(encoding="utf-8", errors="ignore")
        except Exception:
            continue
        for m in re.findall(r"https?://[^\s\"']{8,120}", txt, re.IGNORECASE):
            endpoints.append({"endpoint": m, "file": str(p.as_posix())})
    return {"pb_project_endpoints": endpoints[:50]} if endpoints else {}

def enrich_sir_pb(sir: Dict) -> Dict[str, Any]:
    acts = sir.get("activities", [])
    inv: List[Dict[str,str]] = []
    ids: List[str] = []
    for a in acts:
        nm, typ = a.get("name",""), a.get("type","")
        if typ == "pb:http_request":
            inv.append({"kind":"REST","target":"Unknown","operation":"Unknown","evidence": (a.get("evidence") or [{}])[0].get("selector","")})
        elif typ == "pb:soap_call":
            inv.append({"kind":"SOAP","target":"Unknown","operation":"Unknown","evidence": (a.get("evidence") or [{}])[0].get("selector","")})
        elif typ == "pb:db_exec" or typ == "pb:datawindow_op":
            inv.append({"kind":"JDBC","target":"Unknown","operation":"Unknown","evidence": (a.get("evidence") or [{}])[0].get("selector","")})
        elif typ == "pb:method_call":
            inv.append({"kind":"Process","target": nm, "operation": None, "evidence": (a.get("evidence") or [{}])[0].get("selector","")})
        # identifier scan
        for tok in ID_TOKENS:
            if tok.lower() in nm.lower():
                ids.append(tok)
    ids = sorted(list(dict.fromkeys(ids)))
    bs = {
        "interfaces": [],
        "invocations": inv[:20],
        "dependencies": {
            "processes": [i["target"] for i in inv if i["kind"] == "Process"],
            "services": [],
            "datastores": (sir.get("enrichment", {}).get("business_scaffold", {}).get("dependencies", {}).get("datastores") or [])
        },
        "io_summary": {"inputs": [], "outputs": [], "identifiers": ids[:10]},
        "errors": [],
        "logging": [],
        "traceability": [acts[0]["name"]] if acts else []
    }
    return {"business_scaffold": bs}
