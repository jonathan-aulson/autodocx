from pathlib import Path

from autodocx.extractors.powerbuilder import PowerBuilderExtractor


def test_powerbuilder_extractor_emits_workflow(tmp_path: Path) -> None:
    src = tmp_path / "u_demo.sru"
    src.write_text(
        """
        event open();
            SELECT account_id, account_name FROM dbo.Accounts;
            dw_orders.Retrieve();
            uo_helper.PostProcessAccounts();
            createobject("httpclient")
            httpclient.SendRequest("GET","https://api.example.com/orders")
        end event
        """
    , encoding="utf-8")

    extractor = PowerBuilderExtractor()
    signals = list(extractor.extract(src))
    assert signals
    wf = signals[0]
    props = wf.props
    assert "datasource_tables" in props and any("accounts" in table.lower() for table in props["datasource_tables"])
    assert "process_calls" in props and any("PostProcessAccounts" in call for call in props["process_calls"])
    assert props["identifier_hints"]
    assert any(step.get("connector") == "pb:db_exec" for step in props.get("steps", []))
    assert any(rel.get("target", {}).get("kind") == "sql" for rel in props.get("relationships", []))
    assert props.get("triggers")
    assert any("api.example.com" in dep for dep in props.get("service_dependencies", []))
