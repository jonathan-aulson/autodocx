from __future__ import annotations

from pathlib import Path

from autodocx.extractors.tibco_bw import TibcoBWExtractor


def test_bw_invoke_emits_relationship(tmp_path: Path) -> None:
    sample = """<?xml version="1.0" encoding="UTF-8"?>
<bpws:process xmlns:bpws="http://docs.oasis-open.org/wsbpel/2.0/process/executable"
              xmlns:tibex="http://www.tibco.com/bpel/2007/extensions"
              xmlns:scaext="http://xsd.tns.tibco.com/amf/models/sca/extensions"
              xmlns:rest="http://xsd.tns.tibco.com/bw/models/binding/rest"
              xmlns:tns="http://example.com/bw"
              name="Sample.Process">
  <bpws:partnerLinks>
    <bpws:partnerLink name="creditservice" partnerLinkType="tns:credit">
      <tibex:ReferenceBinding>
        <tibex:binding>
          <scaext:binding connector="CreditConnector"
                          docBasePath="https://api.example.com"
                          docResourcePath="credit"
                          path="/score"
                          name="Credit Service">
            <operation httpMethod="POST" operationName="postScore"/>
          </scaext:binding>
        </tibex:binding>
      </tibex:ReferenceBinding>
    </bpws:partnerLink>
  </bpws:partnerLinks>
  <bpws:flow>
    <bpws:invoke name="CallEquifax"
                 partnerLink="creditservice"
                 operation="postScore"/>
  </bpws:flow>
</bpws:process>
"""
    sample_path = tmp_path / "Sample.process"
    sample_path.write_text(sample, encoding="utf-8")

    extractor = TibcoBWExtractor()
    signals = list(extractor.extract(sample_path))
    workflow = next(sig for sig in signals if sig.kind == "workflow")
    rels = workflow.props.get("relationships") or []
    assert rels, "Expected HTTP relationship from partner link invoke"
    http_rels = [r for r in rels if (r.get("target") or {}).get("kind") == "http"]
    assert http_rels, "Expected at least one HTTP relationship"
    assert any(r.get("operation", {}).get("type") in {"writes", "calls"} for r in http_rels)
    steps = workflow.props.get("steps") or []
    step = next(step for step in steps if step["name"] == "CallEquifax")
    assert step["friendly_display"].startswith("POST")
    assert "api.example.com" in (step.get("url_or_path") or "")
    assert workflow.props.get("debug_counts")
    assert isinstance(workflow.props.get("control_edges"), list)
    assert workflow.props.get("start_activity")
    subs = workflow.subscores or {}
    for key in (
        "known_types_coverage",
        "role_coverage",
        "evidence_strength",
        "transition_integrity",
        "inferred_fraction",
    ):
        assert key in subs
