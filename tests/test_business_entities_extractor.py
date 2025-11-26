from __future__ import annotations

from pathlib import Path

from autodocx.extractors.business_entities import BusinessEntityExtractor


def test_business_entities_from_bpmn_lanes(tmp_path: Path) -> None:
    source = tmp_path / "process.bpmn"
    source.write_text(
        '<bpmn:laneSet><bpmn:lane name="Finance"/><bpmn:lane name="Operations"/></bpmn:laneSet>',
        encoding="utf-8",
    )
    extractor = BusinessEntityExtractor()
    signals = list(extractor.extract(source))
    names = {sig.props["name"] for sig in signals}
    assert {"Finance", "Operations"} <= names


def test_business_entities_from_authorize_roles(tmp_path: Path) -> None:
    cs_file = tmp_path / "SecuredController.cs"
    cs_file.write_text(
        """
using Microsoft.AspNetCore.Authorization;

[Authorize(Roles="BillingAdmin,OpsManager")]
public class SecuredController { }
""",
        encoding="utf-8",
    )
    extractor = BusinessEntityExtractor()
    signals = list(extractor.extract(cs_file))
    names = {sig.props["name"] for sig in signals}
    assert "BillingAdmin" in names
    assert "OpsManager" in names


def test_business_entities_from_component_roles(tmp_path: Path) -> None:
    tsx_file = tmp_path / "BillingAdminPage.tsx"
    tsx_file.write_text(
        """
function BillingAdminPage() {
  return <div />;
}

const CustomerPortal = () => <div />;
""",
        encoding="utf-8",
    )
    extractor = BusinessEntityExtractor()
    signals = list(extractor.extract(tsx_file))
    names = {sig.props["name"] for sig in signals}
    assert "Billing Administrator" in names
    assert "Customer Portal" in names
