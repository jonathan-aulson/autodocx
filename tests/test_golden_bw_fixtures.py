from __future__ import annotations

from pathlib import Path


def test_bw_golden_fixtures_present() -> None:
    """
    Basic presence checks for BW golden outputs so regressions are caught early.
    """
    base = Path("out/fixtures/bw-golden")
    if not base.exists():
        # Skip if fixtures are not available in this checkout/CI run.
        import pytest

        pytest.skip("bw-golden fixtures not present under out/fixtures/bw-golden")

    # Signals
    sir_dir = base / "signals" / "sir_v2"
    assert sir_dir.exists() and any(sir_dir.glob("*.json")), "Expected sir_v2 JSON fixtures"
    interdeps = base / "signals" / "interdeps.json"
    assert interdeps.exists(), "Expected interdeps.json in signals fixtures"

    # Docs
    docs_dir = base / "docs"
    assert docs_dir.exists() and any(docs_dir.rglob("*.md")), "Expected docs fixtures"

    # Diagrams
    diag_dir = base / "diagrams"
    assert diag_dir.exists(), "Expected diagrams directory in fixtures"
    assert (diag_dir / "flows_json").exists(), "Expected flows_json in diagrams fixtures"

    # Manifests
    manifests_dir = base / "manifests"
    assert manifests_dir.exists(), "Expected manifests directory in fixtures"

    # Reports
    reports_dir = base / "reports"
    assert reports_dir.exists(), "Expected reports directory in fixtures"
