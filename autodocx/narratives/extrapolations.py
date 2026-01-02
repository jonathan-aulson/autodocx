from __future__ import annotations

from typing import Any, Dict, List


def extrapolate_context(sir: Dict[str, Any], interdeps_slice: Dict[str, Any]) -> List[Dict[str, Any]]:
    scaffold = sir.get("business_scaffold") or {}
    deps = scaffold.get("dependencies") or {}
    interfaces = scaffold.get("interfaces") or []
    datastores = deps.get("datastores") or []
    services = deps.get("services") or []
    processes = deps.get("processes") or []
    name = (sir.get("name") or sir.get("process_name") or "").lower()
    peers = [peer.lower() for peer in interdeps_slice.get("family_peers") or []]
    shared_data = interdeps_slice.get("shared_datastores_with") or []
    shared_ids = interdeps_slice.get("shared_identifiers_with") or []

    hypotheses: List[Dict[str, Any]] = []
    if len(processes) >= 2:
        hypotheses.append(
            {
                "hypothesis": "Likely orchestrates multiple subprocesses to complete a transaction.",
                "rationale": f"Detected downstream calls: {', '.join(processes[:4])}.",
                "hypothesis_score": 0.45,
            }
        )
    if shared_data:
        hypotheses.append(
            {
                "hypothesis": "Shares datastore usage with adjacent processes, suggesting a common data model.",
                "rationale": f"Shared datastores with: {', '.join(shared_data[:5])}.",
                "hypothesis_score": 0.35,
            }
        )
    if shared_ids:
        hypotheses.append(
            {
                "hypothesis": "Probably exchanges identifiers with peer flows to keep customer context aligned.",
                "rationale": f"Shared identifiers with: {', '.join(shared_ids[:5])}.",
                "hypothesis_score": 0.3,
            }
        )
    if "search" in name and any("sort" in peer or "get" in peer for peer in peers):
        hypotheses.append(
            {
                "hypothesis": "Acts as the entry point of a Search → Get → Sort experience for the same domain.",
                "rationale": f"Family peers: {', '.join(interdeps_slice.get('family_peers') or [])}.",
                "hypothesis_score": 0.33,
            }
        )
    if interfaces and services:
        interfaces_summary = ", ".join(f"{itf.get('method','ANY')} {itf.get('endpoint','')}".strip() for itf in interfaces[:2])
        hypotheses.append(
            {
                "hypothesis": "Serves as an integration boundary that receives requests and publishes events/messages.",
                "rationale": f"Interfaces ({interfaces_summary}) fan out to services {', '.join(services[:3])}.",
                "hypothesis_score": 0.32,
            }
        )
    if not hypotheses and datastores:
        hypotheses.append(
            {
                "hypothesis": "Maintains authoritative records even when source XML omits business commentary.",
                "rationale": f"Datastores touched: {', '.join(datastores[:3])}.",
                "hypothesis_score": 0.25,
            }
        )
    return hypotheses[:5]
