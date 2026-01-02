from autodocx.scaffold.signal_scaffold import build_scaffold
from autodocx.types import Signal


def test_build_scaffold_collects_cross_tech_signals() -> None:
    signal = Signal(
        kind="workflow",
        props={
            "name": "orders.SubmitOrder",
            "triggers": [
                {
                    "name": "ReceiveOrder",
                    "type": "http:ReceiveHTTP",
                    "path": "/orders",
                    "method": "POST",
                }
            ],
            "steps": [
                {"name": "Call Validator", "connector": "process:CallProcess", "called_process": "ValidateOrder"},
                {"name": "Write Orders", "connector": "jdbc:Insert", "datasource": "OrdersDB"},
                {"name": "Send Event", "connector": "jms:Send", "destination": "queue/orders"},
                {"name": "Log Result", "connector": "log:Info"},
                {"name": "Handle Error", "connector": "error:Throw", "condition": "invalid status"},
            ],
            "inputs_example": ["Order payload"],
            "outputs_example": ["Confirmation"],
            "enrichment": {
                "jdbc_sql": [{"activity": "Write Orders", "datasource": "OrdersDB", "sql": "INSERT"}],
                "jms_destinations": [{"activity": "Send Event", "destination": "queue/orders", "connector": "jms:Send"}],
                "mapper_hints": [{"paths": ["/order/OrderId"]}],
                "transition_conditions": [{"from": "Call Validator", "condition": "invalid"}],
            },
        },
        evidence=[],
        subscores={},
    )

    scaffold = build_scaffold(signal)
    assert scaffold["interfaces"][0]["endpoint"] == "/orders"
    assert any(inv["kind"] == "Process" and inv["target"] == "ValidateOrder" for inv in scaffold["invocations"])
    assert "OrdersDB" in scaffold["dependencies"]["datastores"]
    assert "OrderId" in scaffold["io_summary"]["identifiers"]
    assert scaffold["errors"]
    assert scaffold["logging"]


def test_scaffold_uses_hint_fields() -> None:
    signal = Signal(
        kind="workflow",
        props={
            "name": "PB.Process",
            "triggers": [],
            "steps": [],
            "relationships": [
                {
                    "type": "calls",
                    "target": {"kind": "workflow", "name": "SyncAccounts", "display": "SyncAccounts"},
                    "operation": {"type": "calls"},
                },
                {
                    "type": "writes",
                    "target": {"kind": "cosmosdb", "display": "Accounts"},
                    "operation": {"type": "writes"},
                },
            ],
            "datasource_tables": ["Accounts"],
            "process_calls": ["SyncAccounts"],
            "identifier_hints": ["AccountId"],
        },
        evidence=[],
        subscores={},
    )
    scaffold = build_scaffold(signal)
    assert "Accounts" in scaffold["dependencies"]["datastores"]
    assert "SyncAccounts" in scaffold["dependencies"]["processes"]
    assert "AccountId" in scaffold["io_summary"]["identifiers"]
