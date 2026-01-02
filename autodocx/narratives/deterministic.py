from __future__ import annotations

from typing import Any, Dict, List


def compose_process_explanation(sir: Dict[str, Any]) -> Dict[str, Any]:
    scaffold = sir.get("business_scaffold") or {}
    interdeps = sir.get("interdependencies_slice") or {}
    io = scaffold.get("io_summary") or {}
    dependencies = scaffold.get("dependencies") or {}
    interfaces = scaffold.get("interfaces") or []
    invocations = scaffold.get("invocations") or []
    what_it_does = _build_what_it_does(scaffold)
    why_it_matters = _build_why_it_matters(scaffold)
    key_inputs = _stringify_io_entries(io.get("inputs") or io.get("identifiers") or [])
    key_outputs = _stringify_io_entries(io.get("outputs") or [])
    errors = [
        err.get("condition") or err.get("label") or err.get("activity") or err for err in scaffold.get("errors") or []
    ]
    logging = [
        log.get("activity") or log.get("name") or log.get("message_hint") or "Log activity"
        for log in scaffold.get("logging") or []
    ]
    trace = scaffold.get("traceability") or []
    extrapolations = sir.get("extrapolations")
    if not extrapolations:
        extrapolations = _build_extrapolations(scaffold, interdeps)
    return {
        "process_name": sir.get("name") or sir.get("process_name"),
        "one_line_summary": _compose_one_liner(interfaces, invocations, io),
        "what_it_does": what_it_does,
        "why_it_matters": why_it_matters,
        "interfaces": _normalize_interfaces(interfaces),
        "invokes": invocations,
        "key_inputs": key_inputs,
        "key_outputs": key_outputs,
        "errors_and_logging": {"errors": errors, "logging": logging},
        "traceability": trace,
        "interdependencies": interdeps,
        "extrapolations": extrapolations,
    }


def _build_what_it_does(scaffold: Dict[str, Any]) -> List[str]:
    bullets: List[str] = []
    interfaces = scaffold.get("interfaces") or []
    if interfaces:
        first = interfaces[0]
        bullets.append(
            f"Receives {first.get('kind','interface')} traffic ({first.get('method','ANY')} {first.get('endpoint','unknown')})."
        )
    for inv in scaffold.get("invocations") or []:
        kind = inv.get("kind") or "component"
        target = inv.get("target") or inv.get("connector")
        bullets.append(f"Invokes {kind.lower()} `{target}` as part of the main flow.")
    deps = scaffold.get("dependencies") or {}
    if deps.get("datastores"):
        bullets.append(f"Reads/writes datastores: {', '.join(deps['datastores'])}.")
    if not bullets:
        bullets.append("Executes business logic and returns a structured response.")
    return bullets[:7]


def _build_why_it_matters(scaffold: Dict[str, Any]) -> List[str]:
    reasons: List[str] = []
    if scaffold.get("interfaces"):
        reasons.append("Exposes an entry point relied on by upstream applications or channels.")
    deps = scaffold.get("dependencies") or {}
    if deps.get("processes"):
        reasons.append("Coordinates downstream subprocesses to complete the transaction.")
    if deps.get("datastores"):
        reasons.append("Keeps authoritative data synchronized across systems.")
    if not reasons:
        reasons.append("Delivers traceable automation for a business workflow.")
    return reasons[:3]


def _compose_one_liner(interfaces: List[Dict[str, Any]], invocations: List[Dict[str, Any]], io: Dict[str, Any]) -> str:
    subject = "This process"
    if interfaces:
        first = interfaces[0]
        subject = f"This {first.get('kind','service')} endpoint ({first.get('method','ANY')} {first.get('endpoint','unknown')})"
    verbs = []
    if invocations:
        verbs.append("delegates to subprocesses" if any(inv.get("kind") == "Process" for inv in invocations) else "")
        verbs.append("touches data stores" if any(inv.get("kind") == "JDBC" for inv in invocations) else "")
        verbs.append("exchanges messages" if any(inv.get("kind") == "JMS" for inv in invocations) else "")
    verbs = [v for v in verbs if v]
    io_part = ""
    ids = io.get("identifiers") or []
    if ids:
        io_part = f" while tracking identifiers such as {', '.join(ids[:3])}"
    return f"{subject} {' and '.join(verbs) if verbs else 'handles business logic'}{io_part}."


def _build_extrapolations(scaffold: Dict[str, Any], interdeps: Dict[str, Any]) -> List[Dict[str, Any]]:
    hyps: List[Dict[str, Any]] = []
    deps = scaffold.get("dependencies") or {}
    if deps.get("processes"):
        hyps.append(
            {
                "hypothesis": "Likely orchestrates helper processes to complete its workflow.",
                "rationale": "Detected subprocess invocations in the scaffold.",
                "hypothesis_score": 0.35,
            }
        )
    if interdeps.get("shared_datastores_with"):
        hyps.append(
            {
                "hypothesis": "Shares datastore usage with peer processes, suggesting a common domain model.",
                "rationale": f"Shared datastores: {', '.join(interdeps['shared_datastores_with'])}",
                "hypothesis_score": 0.3,
            }
        )
    return hyps[:5]


def _stringify_io_entries(entries: List[Any]) -> List[str]:
    results: List[str] = []
    for entry in entries or []:
        if isinstance(entry, dict):
            name = entry.get("name") or entry.get("identifier") or entry.get("field")
            desc = entry.get("description") or entry.get("detail")
            if name and desc:
                results.append(f"{name}: {desc}")
            elif name:
                results.append(str(name))
            elif desc:
                results.append(str(desc))
            else:
                results.append(str(entry))
        else:
            results.append(str(entry))
    return results


def _normalize_interfaces(interfaces: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
    normalized: List[Dict[str, Any]] = []
    for idx, itf in enumerate(interfaces or []):
        entry = dict(itf)
        entry.setdefault("name", entry.get("kind") or f"Interface {idx+1}")
        entry.setdefault("kind", "service")
        entry.setdefault("endpoint", entry.get("endpoint") or "Unknown")
        entry.setdefault("method", entry.get("method") or "ANY")
        entry.setdefault("description", entry.get("description") or entry.get("summary") or "Description pending.")
        entry.setdefault("evidence_ids", entry.get("evidence_ids") or [])
        normalized.append(entry)
    return normalized
