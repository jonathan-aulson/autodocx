from pathlib import Path

from autodocx.enrichers.process_enrichment import enrich_signal_metadata
from autodocx.types import Signal


def test_process_enrichment_from_steps(tmp_path: Path) -> None:
    src = tmp_path / "process.json"
    src.write_text("{}", encoding="utf-8")
    signal = Signal(
        kind="workflow",
        props={
            "file": str(src),
            "steps": [
                {"name": "Run SQL", "connector": "jdbc:Select", "sql": "SELECT * FROM Orders"},
                {"name": "Queue Message", "connector": "jms:Send", "destination": "queue/orders"},
                {"name": "Timer", "connector": "timer:Event", "cron": "rate(5 minutes)"},
                {
                    "name": "Branch",
                    "connector": "mapper:Mapper",
                    "inputs_keys": ["/order/id"],
                    "functions": ["concat"],
                },
                {"name": "Invoke Child", "connector": "process:CallProcess", "called_process": "SyncOrder"},
            ],
            "control_edges": [{"parent": "Decision", "branch": "approved", "children": ["ApproveOrder"]}],
        },
        evidence=[],
        subscores={},
    )
    enrichment = enrich_signal_metadata(signal, tmp_path)
    assert enrichment["jdbc_sql"][0]["activity"] == "Run SQL"
    assert enrichment["jms_destinations"][0]["destination"] == "queue/orders"
    assert enrichment["timers"][0]["fields"]["cron"] == "rate(5 minutes)"
    assert enrichment["transition_conditions"][0]["condition"] == "approved"
    assert enrichment["mapper_hints"][0]["paths"] == ["/order/id"]
    assert any("orders" in table.lower() for table in enrichment["datasource_tables"])
    assert "SyncOrder" in enrichment["process_calls"]
    assert enrichment["identifier_hints"]
