import json

from autodocx.quality.anti_patterns import run_anti_pattern_scans


def test_run_anti_pattern_scans_flags_missing_logging(tmp_path):
    out_dir = tmp_path / "out"
    sir_dir = out_dir / "signals" / "sir_v2"
    sir_dir.mkdir(parents=True)
    repo_root = tmp_path / "repo"
    repo_root.mkdir(parents=True)
    sir_obj = {
        "id": "workflow:invoice",
        "name": "Invoice Processor",
        "component_or_service": "billing",
        "props": {
            "steps": [{"name": "fetch"}, {"name": "persist"}],
            "logging": [],
        },
        "business_scaffold": {"io_summary": {"identifiers": [], "inputs": [], "outputs": []}, "dependencies": {}},
        "evidence": ["src/billing.py:5-12"],
    }
    sir_path = sir_dir / "invoice.json"
    sir_path.write_text(json.dumps(sir_obj), encoding="utf-8")

    constellations = [
        {
            "id": "constellation_1",
            "sir_files": ["signals/sir_v2/invoice.json"],
            "components": ["billing"],
        }
    ]

    findings_by_constellation, rel_path = run_anti_pattern_scans(
        out_dir,
        repo_root,
        constellations,
        [(sir_obj, sir_path)],
    )

    assert rel_path == "reports/quality/anti_patterns.json"
    assert "constellation_1" in findings_by_constellation
    assert findings_by_constellation["constellation_1"][0]["rule_id"] == "missing_logging"
