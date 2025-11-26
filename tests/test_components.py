from pathlib import Path

import pytest

from autodocx.utils.components import (
    derive_component,
    derive_component_from_path,
    normalize_component_name,
)


def test_normalize_component_name_replaces_invalid_chars():
    assert normalize_component_name("Billing Service") == "Billing_Service"
    assert normalize_component_name("🚀-Orders") == "Orders"


def test_derive_component_from_path_prefers_domain_folder(tmp_path: Path):
    repo = tmp_path / "repo"
    nested = repo / "services" / "orders" / "src" / "main.py"
    nested.parent.mkdir(parents=True, exist_ok=True)
    nested.write_text("# demo", encoding="utf-8")

    component = derive_component_from_path(repo, str(nested))
    assert component == "orders"


def test_derive_component_uses_explicit_value(tmp_path: Path):
    repo = tmp_path / "repo"
    file_path = repo / "app.py"
    file_path.parent.mkdir(parents=True, exist_ok=True)
    file_path.write_text("# demo", encoding="utf-8")

    props = {"file": str(file_path), "component_or_service": "Finance-Core"}
    component = derive_component(repo, props)
    assert component == "Finance-Core"


def test_derive_component_falls_back_to_repo_name(tmp_path: Path):
    repo = tmp_path / "billing-platform"
    file_path = repo / "README.md"
    file_path.parent.mkdir(parents=True, exist_ok=True)
    file_path.write_text("# demo", encoding="utf-8")

    component = derive_component(repo, {"file": str(file_path)})
    assert component == "billing-platform"
