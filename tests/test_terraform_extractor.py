from pathlib import Path

import pytest

from autodocx.extractors import terraform as terraform_module
from autodocx.extractors.terraform import TerraformExtractor

TF_SAMPLE = """
resource "aws_db_instance" "orders" {
  name = "orders-db"
}

resource "aws_lambda_function" "processor" {
  function_name = "processor"
}
"""


@pytest.mark.skipif(terraform_module.hcl2 is None, reason="python-hcl2 is required for Terraform extractor")
def test_terraform_extractor_emits_hints(tmp_path: Path) -> None:
    tf_file = tmp_path / "main.tf"
    tf_file.write_text(TF_SAMPLE, encoding="utf-8")

    extractor = TerraformExtractor()
    signals = list(extractor.extract(tf_file))
    datastore_signal = next(sig for sig in signals if sig.props.get("name") == "orders")
    service_signal = next(sig for sig in signals if sig.props.get("name") == "processor")
    assert "orders-db" in (datastore_signal.props.get("datasource_tables") or [])
    assert "processor" in (service_signal.props.get("service_dependencies") or [])
