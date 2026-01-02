from pathlib import Path

from autodocx.interdeps.builder import build_interdependencies
from autodocx.render import mkdocs


def _sample_sirs():
    return [
        {
            "name": "orders.SubmitOrder",
            "component_or_service": "Orders",
            "business_scaffold": {
                "interfaces": [{"kind": "REST", "method": "POST", "endpoint": "/orders", "description": "Accepts orders"}],
                "dependencies": {"processes": ["orders.ValidateOrder", "billing.InvoiceOrder"], "datastores": ["OrdersDB"]},
                "io_summary": {"identifiers": ["OrderId"]},
            },
            "deterministic_explanation": {"one_line_summary": "Accepts incoming orders and orchestrates validation."},
        },
        {
            "name": "orders.ValidateOrder",
            "component_or_service": "Orders",
            "business_scaffold": {
                "interfaces": [],
                "dependencies": {"datastores": ["OrdersDB"]},
                "io_summary": {"identifiers": ["OrderId"]},
            },
            "deterministic_explanation": {"one_line_summary": "Validates order payloads."},
        },
        {
            "name": "billing.InvoiceOrder",
            "component_or_service": "Billing",
            "business_scaffold": {
                "interfaces": [{"kind": "REST", "method": "POST", "endpoint": "/invoice", "description": "Invoices orders"}],
                "dependencies": {"datastores": ["BillingDB"]},
                "io_summary": {"identifiers": ["InvoiceId"]},
            },
            "deterministic_explanation": {"one_line_summary": "Creates invoices for validated orders."},
        },
    ]


def _sample_interdeps():
    sirs = _sample_sirs()
    return build_interdependencies(sirs), sirs


def test_collect_family_insights_captures_members_and_calls():
    interdeps, sirs = _sample_interdeps()
    insights = mkdocs._collect_family_insights(interdeps, sirs)
    assert "orders" in insights
    orders = insights["orders"]
    assert any(entry["name"] == "orders.SubmitOrder" for entry in orders["members"])
    assert orders["shared_datastores"]["OrdersDB"] == ["orders.SubmitOrder", "orders.ValidateOrder"]
    assert any(call["target_family"] == "billing" for call in orders["cross_calls"])


def test_family_and_repo_docs_written(tmp_path: Path):
    interdeps, sirs = _sample_interdeps()
    docs_dir = tmp_path / "docs"
    docs_dir.mkdir(parents=True, exist_ok=True)
    insights = mkdocs._collect_family_insights(interdeps, sirs)
    assert mkdocs._render_family_docs(docs_dir, insights) is True
    family_md = docs_dir / "families" / "orders.md"
    assert family_md.exists()
    text = family_md.read_text(encoding="utf-8")
    assert "## Members" in text
    assert "## Cross-family calls" in text

    assert mkdocs._render_repo_overview(docs_dir, insights, interdeps) is True
    overview = (docs_dir / "repo_overview.md").read_text(encoding="utf-8")
    assert "Repository Overview" in overview
    assert "| orders | 2" in overview
