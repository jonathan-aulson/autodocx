from pathlib import Path

from autodocx.scaffold.signal_scaffold import build_scaffold
from autodocx.sir.v2 import build_sir_v2
from autodocx.types import Signal


def test_build_sir_v2_from_workflow(tmp_path: Path) -> None:
    src = tmp_path / "flows" / "demo" / "process.json"
    src.parent.mkdir(parents=True, exist_ok=True)
    src.write_text("{}", encoding="utf-8")
    signal = Signal(
        kind="workflow",
        props={
            "name": "OrderFlow",
            "file": str(src),
            "component_or_service": "orders",
            "triggers": [{"name": "When Request Received", "type": "http"}],
            "steps": [
                {"name": "Validate Order", "connector": "function"},
                {"name": "Persist Order", "connector": "jdbc", "run_after": ["Validate Order"]},
            ],
            "relationships": [
                {"source": {"name": "Persist Order"}, "target": {"display": "JDBC:Orders"}, "connector": "jdbc"}
            ],
        },
        evidence=["process.json:1-10"],
        subscores={"parsed": 1.0},
    )
    scaffold = build_scaffold(signal)
    sir = build_sir_v2(
        signal,
        tmp_path,
        business_scaffold=scaffold,
        graph_features={"degree": 1},
        roles=["trigger"],
        roles_evidence={"trigger": [{"connector": "http"}]},
        doc_slug="orderflow",
    )
    assert sir is not None
    assert sir["process_name"] == "OrderFlow"
    assert sir["component_or_service"] == "orders"
    assert sir["doc_slug"] == "orderflow"
    assert sir["graph_features"]["degree"] == 1
    assert sir["roles"] == ["trigger"]
    assert "trigger" in sir["roles_evidence"]
    assert sir["provenance"]
    assert len(sir["activities"]) >= 3  # trigger + two steps + external
    names = [a["name"] for a in sir["activities"]]
    assert "Validate Order" in names
    assert "Persist Order" in names
    assert any(t["from"] == "Validate Order" and t["to"] == "Persist Order" for t in sir["transitions"])
    assert sir["source"]["hash_sha256"] is not None
    assert "logging" in sir["resources"]
    assert "interdependencies_slice" in sir
