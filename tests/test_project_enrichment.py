from pathlib import Path

from autodocx.enrichers.project_enrichment import enrich_project_artifacts


def test_project_enrichment_detects_openapi_wsdl_xsd(tmp_path: Path) -> None:
    openapi = tmp_path / "api.json"
    openapi.write_text(
        """
{
  "openapi": "3.0.0",
  "paths": {
    "/orders": {
      "get": {
        "operationId": "listOrders",
        "summary": "List orders"
      }
    }
  }
}
""",
        encoding="utf-8",
    )
    wsdl = tmp_path / "service.wsdl"
    wsdl.write_text(
        """
<definitions xmlns="http://schemas.xmlsoap.org/wsdl/">
  <portType name="OrderPort">
    <operation name="CreateOrder" />
  </portType>
</definitions>
""",
        encoding="utf-8",
    )
    xsd = tmp_path / "schema.xsd"
    xsd.write_text(
        """
<xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
  <xs:element name="Order" type="xs:string"/>
  <xs:complexType name="OrderType"/>
  <xs:simpleType name="Status">
    <xs:restriction base="xs:string">
      <xs:enumeration value="OPEN"/>
    </xs:restriction>
  </xs:simpleType>
</xs:schema>
""",
        encoding="utf-8",
    )
    ini_file = tmp_path / "pb.ini"
    ini_file.write_text(
        """
[http]
endpoint=https://pb.example.com/service
""",
        encoding="utf-8",
    )
    enrichment = enrich_project_artifacts(tmp_path)
    assert "rest_endpoints" in enrichment
    assert enrichment["rest_endpoints"][0]["path"] == "/orders"
    assert "wsdl_operations" in enrichment
    assert enrichment["wsdl_operations"][0]["operation"] == "CreateOrder"
    assert "xsd_glossary" in enrichment
    assert enrichment["xsd_glossary"]["elements"][0]["name"] == "Order"
    assert "pb_project_endpoints" in enrichment
    assert any("pb.example.com" in entry["endpoint"] for entry in enrichment["pb_project_endpoints"])
