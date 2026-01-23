from pathlib import Path
import json

from autodocx.extractors.bw_module_manifest import BwModuleManifestExtractor
from autodocx.extractors.bw_resources import BwResourceBindingExtractor, BwSubstitutionVarExtractor


def test_bw_module_manifest_parses_json(tmp_path: Path) -> None:
    manifest = {
        "module": "OrdersModule",
        "processes": ["Orders.process", "Billing.process"],
        "resources": ["HttpClientResource1"],
        "bindings": ["RestBinding"],
    }
    path = tmp_path / "module.jsv"
    path.write_text(json.dumps(manifest), encoding="utf-8")

    ex = BwModuleManifestExtractor()
    signals = list(ex.extract(path))
    assert signals, "expected a manifest signal"
    sig = signals[0]
    props = sig.props
    assert props["module_name"] == "OrdersModule"
    assert "Orders.process" in props.get("processes", [])
    assert props.get("shared_resources") == ["HttpClientResource1"]
    assert props.get("bindings") == ["RestBinding"]
    assert sig.kind == "manifest"
    assert sig.evidence and str(path) in sig.evidence[0]


def test_bw_module_manifest_fallback_parses_lines(tmp_path: Path) -> None:
    content = """
    module=CreditModule
    process:CheckCredit
    resource:JdbcResource1
    binding:JmsBinding
    """
    path = tmp_path / "module.msv"
    path.write_text(content, encoding="utf-8")

    ex = BwModuleManifestExtractor()
    signals = list(ex.extract(path))
    sig = signals[0]
    props = sig.props
    assert props["module_name"] == "CreditModule"
    assert any("process" in p.lower() for p in props.get("processes", []))
    assert any("resource" in r.lower() for r in props.get("shared_resources", []))


def test_bw_resource_binding_http(tmp_path: Path) -> None:
    xml = """<httpClientResource host="api.example.com" port="443" baseURI="https://api.example.com" method="GET"/>"""
    path = tmp_path / "Credit.httpClientResource"
    path.write_text(xml, encoding="utf-8")
    ex = BwResourceBindingExtractor()
    sig = next(iter(ex.extract(path)))
    props = sig.props
    assert props["endpoint"].startswith("https://")
    assert props["method"] == "GET"
    assert sig.kind == "resource"


def test_bw_resource_binding_jdbc(tmp_path: Path) -> None:
    xml = """<jdbcResource databaseName="OrdersDB" host="orders-db" port="5432"/>"""
    path = tmp_path / "Orders.jdbcResource"
    path.write_text(xml, encoding="utf-8")
    ex = BwResourceBindingExtractor()
    sig = next(iter(ex.extract(path)))
    props = sig.props
    assert props["datastore"] == "OrdersDB"
    assert "datasource_tables" in props


def test_bw_substvar_extractor(tmp_path: Path) -> None:
    content = """HOST=prod.example.com\nQUEUE=orders.q\n"""
    path = tmp_path / "default.substvar"
    path.write_text(content, encoding="utf-8")
    ex = BwSubstitutionVarExtractor()
    sig = next(iter(ex.extract(path)))
    assert sig.props["variables"]["HOST"] == "prod.example.com"
    assert sig.props["variables"]["QUEUE"] == "orders.q"
