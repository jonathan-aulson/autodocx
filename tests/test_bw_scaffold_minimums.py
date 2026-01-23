import json
from pathlib import Path


FIXTURE_ROOT = Path("out/fixtures/bw-golden/signals/sir_v2")


def test_bw_fixture_io_presence_minimums():
    assert FIXTURE_ROOT.exists(), "Fixture SIR directory missing; run a bw-samples scan to refresh fixtures."
    files = list(FIXTURE_ROOT.glob("*.json"))
    assert files, "No SIR fixtures found under bw-golden."

    nonempty_io = 0
    missing_scaffold = 0
    for f in files:
        data = json.loads(f.read_text(encoding="utf-8"))
        scaffold = data.get("business_scaffold")
        if not scaffold:
            missing_scaffold += 1
            continue
        io = scaffold.get("io_summary") or {}
        if (io.get("inputs") or []) or (io.get("outputs") or []) or (io.get("identifiers") or []):
            nonempty_io += 1

    # Guardrails: ensure we keep at least the current level of populated IO scaffolds
    assert missing_scaffold <= 5, f"Too many fixtures missing business_scaffold ({missing_scaffold})"
    assert nonempty_io >= 5, f"Expected at least 5 fixtures with populated IO fields, found {nonempty_io}"
