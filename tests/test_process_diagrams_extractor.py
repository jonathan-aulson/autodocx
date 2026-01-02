from __future__ import annotations

from pathlib import Path

from autodocx.extractors.process_diagrams import ProcessDiagramExtractor
from autodocx.scaffold.signal_scaffold import build_scaffold


def test_process_diagram_name_detection(tmp_path: Path) -> None:
    source = tmp_path / "billing.bpmn"
    source.write_text(
        """<bpmn:definitions xmlns:bpmn="http://www.omg.org/spec/BPMN/20100524/MODEL">
  <bpmn:process name="Billing Flow">
    <bpmn:dataStoreReference id="Store_1" name="InvoiceTable" />
    <bpmn:callActivity id="CallPayments" calledElement="PaymentsFlow" />
    <bpmn:dataObjectReference id="Obj_1" name="CustomerId" />
  </bpmn:process>
</bpmn:definitions>
""",
        encoding="utf-8",
    )
    extractor = ProcessDiagramExtractor()
    signals = list(extractor.extract(source))
    assert signals
    props = signals[0].props
    assert props["name"] == "Billing Flow"
    assert "InvoiceTable" in props.get("datasource_tables", [])
    assert "PaymentsFlow" in props.get("process_calls", [])
    assert "CustomerId" in props.get("identifier_hints", [])
    scaffold = build_scaffold(signals[0])
    assert scaffold["io_summary"]["identifiers"]
    assert scaffold["dependencies"]["datastores"]
    assert scaffold["dependencies"]["processes"]


def test_process_diagram_drawio_hints(tmp_path: Path) -> None:
    drawio = tmp_path / "legacy.drawio"
    drawio.write_text(
        """<mxfile>
  <diagram>
    <mxGraphModel>
      <root>
        <mxCell id="0" />
        <mxCell id="1" parent="0" />
        <mxCell id="2" value="Datastore Accounts" style="shape=datastore" vertex="1" parent="1" />
        <mxCell id="3" value="Call LegacyBatch" style="shape=process;labelBackgroundColor=#fff" vertex="1" parent="1" />
        <mxCell id="4" value="CustomerCode" style="shape=rectangle" vertex="1" parent="1" />
      </root>
    </mxGraphModel>
  </diagram>
</mxfile>
""",
        encoding="utf-8",
    )
    extractor = ProcessDiagramExtractor()
    signals = list(extractor.extract(drawio))
    assert signals
    props = signals[0].props
    assert any("Datastore Accounts" == ds for ds in props.get("datasource_tables", []))
    assert any("Call LegacyBatch" == proc for proc in props.get("process_calls", []))
    assert "CustomerCode" in props.get("identifier_hints", [])
    scaffold = build_scaffold(signals[0])
    assert scaffold["io_summary"]["identifiers"]
    assert scaffold["dependencies"]["datastores"]
    assert scaffold["dependencies"]["processes"]
