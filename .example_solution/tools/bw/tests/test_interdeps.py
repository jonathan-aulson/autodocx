import json
from pathlib import Path

def test_creditcheck_calls_lookupdatabase():
    interdeps_path = Path("out/sir/_interdeps.json")
    assert interdeps_path.exists(), "Run the orchestrator first to build interdeps."
    data = json.loads(interdeps_path.read_text(encoding="utf-8"))
    edges = [ (e["from"], e["to"], e["kind"]) for e in data.get("edges", []) ]
    # allow either fully-qualified or simple names depending on project parse
    candidates = [
        ("creditcheckservice.Process", "creditcheckservice.LookupDatabase", "calls"),
        ("Process", "LookupDatabase", "calls")
    ]
    assert any(e for e in edges if (e[0], e[1], e[2]) in candidates), \
        f"Expected a 'calls' edge from Process to LookupDatabase, got: {edges[:10]}"
