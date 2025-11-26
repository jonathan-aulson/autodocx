from __future__ import annotations
def safe_head(s: str, n: int = 4096) -> str:
    return s[:n] if isinstance(s, str) else ""
