# autodocx/utils/redaction.py
from __future__ import annotations
import re
from typing import List, Pattern

# Centralized secret patterns (extend as needed)
SECRET_PATTERNS: List[Pattern] = [
    re.compile(r"(?i)\b(api[_-]?key|secret|token|password|passwd)\s*[:=]\s*([^\s\"']{8,})"),
    re.compile(r"AKIA[0-9A-Z]{16}"),                      # AWS-ish access key id
    re.compile(r"(?i)(?:aws)?_?secret(?:_?access)?_?key\s*[:=]\s*([A-Za-z0-9\/+=]{8,})"),
    re.compile(r"([?&]sig=)[^&\s]+"),                     # SAS sig tokens in URLs
    re.compile(r"eyJ[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+"),  # JWT-like
]

def redact(text: str) -> str:
    """
    Replace obvious secret patterns with safe placeholders.
    Keep function simple and deterministic.
    """
    if not isinstance(text, str):
        try:
            text = str(text)
        except Exception:
            return "***REDACTED***"

    red = text
    for pat in SECRET_PATTERNS:
        try:
            if pat.pattern.startswith("([?&]sig="):
                red = pat.sub(r"\1***REDACTED***", red)
            else:
                # Replace captured secret with placeholder; preserve the left-hand label if present.
                # If pattern has groups, substitute the last group.
                # For generality use a safe mask
                red = pat.sub(lambda m: (m.group(1) + ": ***REDACTED***") if m.groups() else "***REDACTED***", red)
        except Exception:
            # on any pattern error, skip it
            continue
    return red
