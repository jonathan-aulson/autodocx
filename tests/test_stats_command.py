import json
from pathlib import Path

from autodocx_cli.__main__ import gather_scan_stats


def test_gather_scan_stats_counts_components(tmp_path: Path):
    out_dir = tmp_path / "out"
    sir_dir = out_dir / "sir"
    sir_dir.mkdir(parents=True)
    sample = {
        "kind": "workflow",
        "component_or_service": "BillingSystem",
        "props": {
            "steps": [
                {"name": "StepA", "connector": "shared_powerplatformadminv2"},
                {"name": "StepB", "connector": "Compose"},
            ]
        },
    }
    (sir_dir / "sample.json").write_text(json.dumps(sample), encoding="utf-8")
    (out_dir / "artifacts.json").write_text("[]", encoding="utf-8")

    stats = gather_scan_stats(out_dir)
    assert stats["sir_total"] == 1
    assert stats["components"][0][0] == "BillingSystem"
    assert any(conn == "shared_powerplatformadminv2" for conn, _ in stats["connectors"])
