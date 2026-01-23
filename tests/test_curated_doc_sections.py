import re
from pathlib import Path

DOCS_DIR = Path("out/docs")
REQUIRED_SECTIONS = [
    "## Interfaces exposed",
    "## Invokes / Dependencies",
    "## Key inputs & outputs",
    "## Errors & Logging",
]


def test_curated_docs_have_required_sections():
    import os
    import pytest

    min_coverage = float(os.getenv("AUTODOCX_MIN_DOC_SECTION_COVERAGE", "0.8"))
    if not DOCS_DIR.exists():
        pytest.skip("Docs not found; run a scan first to generate out/docs")
    total = 0
    with_required = 0
    missing = []
    for md in DOCS_DIR.glob("*/components/*.md"):
        if md.name == "overview.md":
            continue
        total += 1
        text = md.read_text(encoding="utf-8")
        missing_sections = [section for section in REQUIRED_SECTIONS if section not in text]
        if missing_sections:
            missing.append((md.name, missing_sections))
        else:
            with_required += 1
    coverage = (with_required / total) if total else 0.0
    if coverage < min_coverage:
        raise AssertionError(
            f"Doc section coverage {coverage:.2f} below threshold {min_coverage:.2f}; missing={missing[:10]}"
        )
