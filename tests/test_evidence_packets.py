import json

from autodocx.evidence import build_evidence_packets


def test_build_evidence_packets_emits_snippets(tmp_path):
    out_dir = tmp_path / "out"
    repo_root = tmp_path / "repo"
    source_dir = repo_root / "svc"
    source_dir.mkdir(parents=True)
    file_path = source_dir / "orders.py"
    file_path.write_text(
        "def handler(event):\n"
        "    total = sum(event['values'])\n"
        "    print(total)\n",
        encoding="utf-8",
    )

    sir_dir = out_dir / "signals" / "sir_v2"
    sir_dir.mkdir(parents=True)
    sir_obj = {
        "id": "workflow:orders",
        "name": "Orders Flow",
        "component_or_service": "orders",
        "props": {"steps": [{"name": "handler"}], "file": "svc/orders.py"},
        "business_scaffold": {"io_summary": {"identifiers": [], "inputs": [], "outputs": []}, "dependencies": {}},
        "evidence": ["svc/orders.py:1-2"],
    }
    sir_path = sir_dir / "orders.json"
    sir_path.write_text(json.dumps(sir_obj), encoding="utf-8")

    constellations = [
        {
            "id": "constellation_1",
            "slug": "constellation-1",
            "components": ["orders"],
            "entry_points": [],
            "sir_files": ["signals/sir_v2/orders.json"],
            "score": 0.5,
            "node_count": 2,
            "edge_count": 1,
            "graph_file": "signals/constellations/constellation-1.json",
        }
    ]
    anti_patterns = {"constellation_1": [{"rule_id": "missing_logging"}]}

    packet_index = build_evidence_packets(
        out_dir,
        repo_root,
        constellations,
        [(sir_obj, sir_path)],
        anti_patterns,
    )

    packet_path = out_dir / packet_index["constellation_1"]
    data = json.loads(packet_path.read_text(encoding="utf-8"))
    assert data["snippets"], "expected snippet content"
    assert "handler(event)" in data["snippets"][0]["text"]
    assert data["anti_patterns"][0]["rule_id"] == "missing_logging"
