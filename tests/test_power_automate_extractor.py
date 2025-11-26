from pathlib import Path

from autodocx.extractors.power_automate import PowerAutomateExtractor


def test_power_automate_extractor_parses_flow() -> None:
    repo = Path("repos/Towne-Park-Billing-Source-Code/Towne-Park-Billing-PA-Solution")
    sample = repo / "BillingSystemMonitoring/Workflows/DataflowMonitor-25A66470-1D5C-F011-BEC1-7C1E5202188C.json"
    assert sample.exists(), "expected Power Automate workflow fixture"

    extractor = PowerAutomateExtractor()
    assert extractor.detect(repo)

    signals = list(extractor.extract(sample))
    assert signals, "extractor should emit at least one workflow signal"
    sig = signals[0]

    assert sig.kind == "workflow"
    assert sig.props.get("engine") == "power_automate"
    assert sig.props.get("user_story"), "workflow user_story should be populated"
    assert sig.props.get("inputs_example"), "inputs_example should exist"
    assert sig.props.get("journey_touchpoints"), "journey touchpoints should be derived"
