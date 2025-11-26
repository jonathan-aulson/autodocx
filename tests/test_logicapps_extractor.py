from pathlib import Path
import json

from autodocx.extractors.logicapps import LogicAppsWDLExtractor


def test_logicapps_extractor_enriches_connectors(tmp_path: Path):
    content = {
        "properties": {
            "connectionReferences": {
                "shared_powerplatformadminv2-1": {
                    "api": {"name": "shared_powerplatformadminv2"},
                    "connection": {"connectionReferenceLogicalName": "ref_powerplatform"},
                }
            },
            "definition": {
                "triggers": {
                    "Recurrence": {
                        "type": "Recurrence",
                        "inputs": {"recurrence": {"frequency": "Day", "interval": 1}},
                    }
                },
                "actions": {
                    "GetTenantCapacity": {
                        "type": "OpenApiConnection",
                        "inputs": {
                            "host": {
                                "connectionName": "shared_powerplatformadminv2-1",
                                "apiId": "/providers/Microsoft.PowerApps/apis/shared_powerplatformadminv2",
                            },
                            "method": "get",
                            "path": "/tenantCapacity",
                        },
                    }
                },
            },
        }
    }
    file_path = tmp_path / "flow.json"
    file_path.write_text(json.dumps(content), encoding="utf-8")

    extractor = LogicAppsWDLExtractor()
    signals = list(extractor.extract(file_path))
    assert signals, "Expected extractor to emit a workflow signal"
    workflow = signals[0]
    steps = workflow.props["steps"]  # type: ignore[attr-defined]
    connector_names = {step.get("connector") for step in steps}
    assert "shared_powerplatformadminv2" in connector_names or "shared_powerplatformadminv2-1" in connector_names
