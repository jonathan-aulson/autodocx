from __future__ import annotations

from pathlib import Path

import pytest

from autodocx.extractors.tree_sitter_code import TreeSitterCodeExtractor
from autodocx.tree_sitter_support import tree_sitter_available


PYTHON_SAMPLE = '''
def greet(name):
    """Return a greeting."""
    return f"Hello {name}"

class BillingService:
    """Handles billing workflows."""
    def run(self):
        pass
'''


@pytest.mark.skipif(not tree_sitter_available(), reason="tree-sitter not installed")
def test_tree_sitter_extractor_emits_code_entities(tmp_path: Path) -> None:
    source = tmp_path / "module.py"
    source.write_text(PYTHON_SAMPLE, encoding="utf-8")

    extractor = TreeSitterCodeExtractor()
    signals = list(extractor.extract(source))
    assert len(signals) >= 2

    names = {sig.props["name"] for sig in signals}
    assert "greet" in names
    assert "BillingService" in names

    greet_signal = next(sig for sig in signals if sig.props["name"] == "greet")
    assert greet_signal.props["entity_type"] == "function"
    assert "greeting" in greet_signal.props["docstring"]
