from __future__ import annotations

from pathlib import Path

from autodocx.extractors.integration_imports import IntegrationImportsExtractor


def test_js_import_detection(tmp_path: Path) -> None:
    source = tmp_path / "client.ts"
    source.write_text('import axios from "axios";', encoding="utf-8")
    extractor = IntegrationImportsExtractor()
    signals = list(extractor.extract(source))
    assert signals
    assert signals[0].props["integration_kind"] == "http_client"


def test_csharp_using_detection(tmp_path: Path) -> None:
    source = tmp_path / "Function.cs"
    source.write_text(
        """
using Microsoft.PowerPlatform.Dataverse.Client;
using System;
""",
        encoding="utf-8",
    )
    extractor = IntegrationImportsExtractor()
    signals = list(extractor.extract(source))
    kinds = {sig.props["integration_kind"] for sig in signals}
    assert "power_platform" in kinds
