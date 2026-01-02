from pathlib import Path

import pytest

pytest.importorskip("lxml")

from autodocx.extractors.mulesoft import MuleSoftExtractor


def test_mulesoft_extractor_parses_flow(tmp_path: Path) -> None:
    content = """
    <mule xmlns="http://www.mulesoft.org/schema/mule/core"
          xmlns:http="http://www.mulesoft.org/schema/mule/http"
          xmlns:db="http://www.mulesoft.org/schema/mule/db">
      <flow name="OrderFlow">
        <http:listener config-ref="listener" path="/orders" allowedMethods="GET" />
        <db:select config-ref="dbConfig" doc:name="SelectOrders">
          <db:sql>SELECT * FROM Orders</db:sql>
        </db:select>
        <flow-ref name="AuditFlow" />
      </flow>
    </mule>
    """
    src = tmp_path / "order-flow.xml"
    src.write_text(content, encoding="utf-8")

    extractor = MuleSoftExtractor()
    signals = list(extractor.extract(src))
    assert signals
    signal = signals[0]
    props = signal.props
    assert props["triggers"]
    assert "Orders" in (props.get("datasource_tables") or [""])[0]
    assert "AuditFlow" in props.get("process_calls")
