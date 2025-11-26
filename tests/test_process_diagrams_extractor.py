from __future__ import annotations

from pathlib import Path

from autodocx.extractors.process_diagrams import ProcessDiagramExtractor


def test_process_diagram_name_detection(tmp_path: Path) -> None:
    source = tmp_path / "billing.bpmn"
    source.write_text('<bpmn:process name="Billing Flow"></bpmn:process>', encoding="utf-8")
    extractor = ProcessDiagramExtractor()
    signals = list(extractor.extract(source))
    assert signals
    assert signals[0].props["name"] == "Billing Flow"
