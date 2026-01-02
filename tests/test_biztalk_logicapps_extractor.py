from pathlib import Path

import pytest

pytest.importorskip("lxml")

from autodocx.extractors.biztalk_logicapps import BizTalkLogicAppsExtractor


WORKFLOW_JSON = {
    "definition": {
        "triggers": {
            "manual": {
                "type": "Request",
                "kind": "Http",
                "inputs": {"method": "POST", "path": "/manual"},
            }
        },
        "actions": {
            "CallApi": {
                "type": "Http",
                "inputs": {"method": "GET", "uri": "https://example.com/api"},
            },
            "WriteSql": {
                "type": "ApiConnection",
                "inputs": {
                    "host": {"connection": {"name": "sql"}},
                    "path": "/tables/Orders/items",
                },
            },
            "InvokeChild": {
                "type": "Workflow",
                "workflow": "ChildWorkflow",
            },
        },
    }
}


def test_logicapps_standard_extractor(tmp_path: Path) -> None:
    workflow = tmp_path / "workflow.json"
    workflow.write_text(__import__("json").dumps(WORKFLOW_JSON), encoding="utf-8")

    extractor = BizTalkLogicAppsExtractor()
    signals = list(extractor.extract(workflow))
    assert signals
    props = signals[0].props
    assert props["triggers"]
    assert any("orders" in table.lower() for table in props.get("datasource_tables", []))
    assert "ChildWorkflow" in props.get("process_calls", [])
