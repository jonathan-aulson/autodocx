import json
from pathlib import Path

from autodocx.extractors.power_automate import PowerAutomateExtractor
from autodocx.scaffold.signal_scaffold import build_scaffold


def test_power_automate_extractor_parses_flow() -> None:
    repo = Path("repos/Towne-Park-Billing-Source-Code/Towne-Park-Billing-PA-Solution")
    sample = repo / "BillingSystemMonitoring/Workflows/DataflowMonitor-25A66470-1D5C-F011-BEC1-7C1E5202188C.json"
    assert sample.exists(), "expected Power Automate workflow fixture"

    extractor = PowerAutomateExtractor()
    assert extractor.detect(repo)

    signals = list(extractor.extract(sample))
    assert signals, "extractor should emit at least one workflow signal"
    sig = signals[0]

    assert sig.kind == "workflow"
    assert sig.props.get("engine") == "power_automate"
    assert sig.props.get("user_story"), "workflow user_story should be populated"
    assert sig.props.get("inputs_example"), "inputs_example should exist"
    assert sig.props.get("journey_touchpoints"), "journey touchpoints should be derived"


def test_power_automate_extractor_populates_scaffold_hints(tmp_path: Path) -> None:
    content = {
        "properties": {
            "displayName": "Dataverse Sync",
            "definition": {
                "triggers": {
                    "manual": {
                        "type": "Request",
                        "inputs": {"schema": {"properties": {"accountId": {"type": "string"}}}},
                    }
                },
                "actions": {
                    "Dataverse_Read": {
                        "type": "OpenApiConnection",
                        "inputs": {
                            "host": {"connection": {"name": "shared_commondataserviceforapps"}},
                            "method": "get",
                            "path": "/datasets/default/tables/accounts",
                            "parameters": {"entityName": "accounts"},
                        },
                    },
                    "Call_Subflow": {
                        "type": "Http",
                        "inputs": {
                            "method": "post",
                            "uri": "https://prod-00.westus.logic.azure.com/workflows/someflow/triggers/manual/run",
                        },
                    },
                },
            },
            "connectionReferences": {
                "shared_commondataserviceforapps": {
                    "api": {"name": "shared_commondataserviceforapps"},
                    "connection": {"connectionReferenceLogicalName": "dataverse"},
                }
            },
        }
    }
    flow = tmp_path / "Workflows" / "Flow.json"
    flow.parent.mkdir(parents=True, exist_ok=True)
    flow.write_text(json.dumps(content), encoding="utf-8")

    extractor = PowerAutomateExtractor()
    signals = list(extractor.extract(flow))
    assert signals, "Expected workflow signal"
    props = signals[0].props
    assert "accounts" in (props.get("datasource_tables") or [])
    assert props.get("service_dependencies"), "HTTP child flow should register as service dependency"
    assert props.get("process_calls"), "child workflow URI should populate process_calls"
    assert "accountId" in (props.get("identifier_hints") or [])
    scaffold = build_scaffold(signals[0])
    assert scaffold["io_summary"]["identifiers"]
    assert scaffold["dependencies"]["datastores"]
    assert scaffold["dependencies"]["processes"]
