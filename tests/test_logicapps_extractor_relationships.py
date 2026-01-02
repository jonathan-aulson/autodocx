from __future__ import annotations

import json
from pathlib import Path

from autodocx.extractors.logicapps import LogicAppsWDLExtractor
from autodocx.scaffold.signal_scaffold import build_scaffold


def test_logicapps_extractor_emits_relationships(tmp_path: Path) -> None:
    definition = {
        "triggers": {
            "manual": {
                "type": "Request",
                "inputs": {
                    "schema": {
                        "properties": {
                            "siteId": {"type": "string"},
                        }
                    }
                },
            }
        },
        "actions": {
            "Call_API": {
                "type": "Http",
                "inputs": {"method": "post", "uri": "https://example.com/api/resource"},
                "runAfter": {},
            },
            "Read_SQL": {
                "type": "OpenApiConnection",
                "inputs": {
                    "host": {"connection": {"name": "shared_sql"}},
                    "method": "get",
                    "path": "/queries/read",
                    "body": {"table": "[dbo].[Invoices]"},
                },
                "runAfter": {"Call_API": ["Succeeded"]},
            },
            "InvokeChild": {
                "type": "Http",
                "inputs": {
                    "method": "post",
                    "uri": "https://prod-00.westus.logic.azure.com/workflows/childFlow/triggers/manual/run",
                },
                "runAfter": {"Read_SQL": ["Succeeded"]},
            },
        },
    }

    flow_path = tmp_path / "sample_workflow.json"
    flow_path.write_text(json.dumps(definition), encoding="utf-8")

    extractor = LogicAppsWDLExtractor()
    signals = list(extractor.extract(flow_path))
    assert signals, "Expected workflow signal from sample definition"
    workflow = signals[0]
    relationships = workflow.props.get("relationships") or []

    assert len(relationships) >= 3, "Should emit relationships for trigger, HTTP action, and SQL action"
    kinds = {rel["target"]["kind"] for rel in relationships}
    assert "http" in kinds
    assert "sql" in kinds
    assert any(rel["source"]["type"] == "trigger" for rel in relationships)
    assert "[dbo].[Invoices]" in (workflow.props.get("datasource_tables") or []), "datastores should capture SQL tables"
    services = workflow.props.get("service_dependencies") or []
    assert any(dep.startswith("https://example.com") for dep in services)
    assert "siteId" in (workflow.props.get("identifier_hints") or [])
    scaffold = build_scaffold(workflow)
    assert scaffold["io_summary"]["identifiers"], "expected identifiers in scaffold"
    assert scaffold["dependencies"]["datastores"], "expected datastores in scaffold"
    assert scaffold["dependencies"]["processes"], "expected process dependencies in scaffold"
