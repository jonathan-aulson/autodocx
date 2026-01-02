from autodocx.narratives.deterministic import compose_process_explanation


def test_compose_process_explanation_uses_scaffold() -> None:
    sir = {
        "name": "OrderFlow",
        "business_scaffold": {
            "interfaces": [{"kind": "REST", "endpoint": "/orders", "method": "POST", "evidence": "trigger"}],
            "invocations": [{"kind": "Process", "target": "ValidateOrder"}],
            "dependencies": {"datastores": ["OrdersDB"], "processes": ["ValidateOrder"]},
            "io_summary": {"identifiers": ["OrderId"], "outputs": ["Confirmation"]},
            "errors": [{"condition": "invalid"}],
            "logging": [{"name": "LogResult", "message_hint": "Logs outcome"}],
            "traceability": ["trigger:Receive", "step:ValidateOrder"],
        },
        "interdependencies_slice": {"calls": ["ValidateOrder"], "shared_datastores_with": ["ProcessB"]},
        "extrapolations": [{"hypothesis": "Likely orchestrates downstream validation.", "rationale": "Calls ValidateOrder."}],
    }
    explanation = compose_process_explanation(sir)
    assert "what_it_does" in explanation
    assert explanation["interfaces"][0]["endpoint"] == "/orders"
    assert explanation["interdependencies"]["calls"] == ["ValidateOrder"]
    assert explanation["extrapolations"][0]["hypothesis"].startswith("Likely orchestrates")
