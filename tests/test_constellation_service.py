from pathlib import Path
import json

from autodocx.constellations.service import build_constellations, persist_constellations
from autodocx.types import Edge, Node


def test_build_and_persist_constellations(tmp_path):
    out_dir = tmp_path / "out"
    out_dir.mkdir()
    sir_dir = out_dir / "signals" / "sir_v2"
    sir_dir.mkdir(parents=True, exist_ok=True)
    sir_obj = {
        "id": "workflow:orders",
        "name": "Orders Flow",
        "component_or_service": "orders",
        "props": {"steps": [{"name": "A"}], "file": "src/orders.py"},
        "business_scaffold": {"io_summary": {"identifiers": [], "inputs": [], "outputs": []}, "dependencies": {}},
        "evidence": ["src/orders.py:10-12"],
    }
    sir_path = sir_dir / "orders.json"
    sir_path.write_text(json.dumps(sir_obj), encoding="utf-8")

    nodes = [
        Node("Component:orders", "Component", "orders", {"component_or_service": "orders"}, [], {}),
        Node("API:getOrders", "API", "getOrders", {"component_or_service": "orders"}, ["src/orders.py:10"], {}),
    ]
    edges = [Edge(nodes[0].id, nodes[1].id, "owns", {}, [], {})]

    constellations = build_constellations(nodes, edges, [(sir_obj, sir_path)], out_dir)
    assert constellations, "Expected at least one constellation"
    record = constellations[0]
    assert record["components"] == ["orders"]
    assert record["sir_files"] == ["signals/sir_v2/orders.json"]

    manifest = persist_constellations(out_dir, constellations)
    assert manifest[0]["path"] == "signals/constellations/orders.json"
    assert Path(out_dir / manifest[0]["path"]).exists()
