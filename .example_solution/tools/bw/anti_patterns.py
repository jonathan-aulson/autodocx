from __future__ import annotations

from pathlib import Path
from typing import Dict, List

SEVERITY_HIGH = "high"
SEVERITY_MED = "medium"
SEVERITY_LOW = "low"


def detect_anti_patterns(sir: Dict, source_text: str) -> List[Dict]:
    findings: List[Dict] = []
    scaffold = (sir.get("enrichment") or {}).get("business_scaffold", {}) or {}
    process = sir.get("process_name")

    interfaces = scaffold.get("interfaces", []) or []
    invocations = scaffold.get("invocations", []) or []
    logging_entries = scaffold.get("logging", []) or []
    errors = scaffold.get("errors", []) or []
    traceability = scaffold.get("traceability", []) or []

    if interfaces and not logging_entries:
        findings.append({
            "id": "missing_logging",
            "process": process,
            "severity": SEVERITY_MED,
            "message": "Process exposes interfaces but has no logging entries in the scaffold.",
            "suggested_fix": "Add structured logging around interface entry/exit points.",
        })

    if invocations and not errors:
        findings.append({
            "id": "missing_error_handling",
            "process": process,
            "severity": SEVERITY_MED,
            "message": "Process calls downstream components but lacks explicit error handlers.",
            "suggested_fix": "Add error transitions or exception handling before/after invocations.",
        })

    if not traceability:
        findings.append({
            "id": "no_traceability",
            "process": process,
            "severity": SEVERITY_LOW,
            "message": "No traceability metadata documented (identifiers, correlation IDs, etc.).",
            "suggested_fix": "Capture identifiers or correlation IDs for observability.",
        })

    if source_text and "http://" in source_text:
        findings.append({
            "id": "insecure_http_reference",
            "process": process,
            "severity": SEVERITY_MED,
            "message": "Source contains hard-coded http:// references; prefer https:// or configuration.",
            "suggested_fix": "Move URLs to configuration and enforce HTTPS where possible.",
        })

    return findings
