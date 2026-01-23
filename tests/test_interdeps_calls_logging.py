import json
from pathlib import Path

SIR_DIR = Path("out/signals/sir_v2")


def test_sir_have_calls_or_logging():
    import os

    min_coverage = float(os.getenv("AUTODOCX_MIN_SIR_WITH_CALLS_LOGGING", "0.04"))
    assert SIR_DIR.exists(), "Signals not found; run scan first."
    total = 0
    with_calls_logging = 0
    missing = []
    for sir_path in SIR_DIR.glob("*.json"):
        total += 1
        data = json.loads(sir_path.read_text(encoding="utf-8"))
        scaffold = data.get("business_scaffold") or {}
        deps = scaffold.get("dependencies") or {}
        calls = (deps.get("processes") or []) + (deps.get("services") or [])
        logging = scaffold.get("logging") or []
        errors = scaffold.get("errors") or []
        if calls or logging or errors:
            with_calls_logging += 1
        else:
            missing.append(sir_path.name)
    coverage = (with_calls_logging / total) if total else 0.0
    if coverage < min_coverage:
        raise AssertionError(
            f"SIR coverage (calls/logging/errors) {coverage:.2f} below threshold {min_coverage:.2f}; "
            f"missing={missing[:10]} (total {len(missing)})"
        )
