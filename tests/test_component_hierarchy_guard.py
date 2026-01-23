import json
from pathlib import Path

DOCS_DIR = Path("out/docs")
DISALLOWED_COMPONENT_NAMES = {"dockerfile", "readme", "manifest", "manifest.mf", "issue_template"}


def test_no_artifact_only_components():
    """
    Fail if component docs exist for disallowed artifact-like names.
    """
    if not DOCS_DIR.exists():
        import pytest
        pytest.skip("Docs not present; run scan first.")
    offenders = []
    for child in DOCS_DIR.iterdir():
        if not child.is_dir():
            continue
        md = child / f"{child.name}.md"
        if not md.exists():
            continue
        stem = child.name.lower()
        if stem in DISALLOWED_COMPONENT_NAMES:
            offenders.append(md.as_posix())
    assert not offenders, f"Artifact-only component docs found: {offenders}"


def test_sir_has_ownership_links():
    """
    Ensure SIRs carry component/family/module metadata.
    """
    sir_dir = Path("out/signals/sir_v2")
    if not sir_dir.exists():
        import pytest
        pytest.skip("Signals not present; run scan first.")
    missing = []
    for sir_path in sir_dir.glob("*.json"):
        data = json.loads(sir_path.read_text(encoding="utf-8"))
        comp = data.get("component_or_service") or data.get("component") or (data.get("props") or {}).get("component")
        family = data.get("family") or (data.get("props") or {}).get("family")
        # skip manifest/meta files
        if sir_path.name.startswith("_interdeps") or sir_path.name.startswith("_project_enrichment"):
            continue
        if not comp and not family:
            missing.append(sir_path.name)
    assert not missing, f"SIRs missing ownership metadata: {missing[:10]} (total {len(missing)})"
