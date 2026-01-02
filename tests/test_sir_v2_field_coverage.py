from __future__ import annotations

import json
from pathlib import Path
from typing import Any, Dict

from autodocx.extractors.express import ExpressJSExtractor
from autodocx.extractors.logicapps import LogicAppsWDLExtractor
from autodocx.extractors.powerbuilder import PowerBuilderExtractor
from autodocx.extractors.tibco_bw import TibcoBWExtractor
from autodocx.scaffold.signal_scaffold import build_scaffold
from autodocx.sir.v2 import build_sir_v2
from autodocx.types import Signal


def test_sir_v2_contains_required_fields_for_key_stacks(tmp_path: Path) -> None:
    repo_root = tmp_path
    bw = _emit_bw_signal(repo_root)
    pb = _emit_powerbuilder_signal(repo_root)
    logic = _emit_logicapps_signal(repo_root)
    express = _emit_express_signal(repo_root)

    for sir in (bw, pb, logic, express):
        _assert_common_fields(sir)

    assert bw["relationships"], "BW extractor should surface partner link relationships"
    assert pb["business_scaffold"]["dependencies"]["datastores"], "PowerBuilder scaffold should list datastores"
    assert logic["resources"]["triggers"], "Logic Apps workflows must expose triggers"
    assert express["business_scaffold"]["dependencies"]["processes"], "Code extractor scaffold holds invoked processes"


def _assert_common_fields(sir: Dict[str, Any]) -> None:
    assert sir["provenance"], "SIR v2 records must retain provenance entries"
    assert "business_scaffold" in sir and sir["business_scaffold"]["io_summary"] is not None
    assert "doc_slug" in sir and sir["_doc_slug"] == sir["doc_slug"]
    assert "graph_features" in sir
    assert "roles" in sir
    assert "resources" in sir and "logging" in sir["resources"]
    assert "props" in sir and "logging" in sir["props"]
    assert "interdependencies_slice" in sir
    assert "extrapolations" in sir
    assert "deterministic_explanation" in sir


def _prep_signal(signal: Signal, repo_root: Path, slug_prefix: str) -> Dict[str, Any]:
    props = signal.props
    props.setdefault("component_or_service", slug_prefix)
    if not signal.evidence:
        file_hint = props.get("file") or f"{slug_prefix}.txt"
        signal.evidence.append(f"{file_hint}:1-5")
    scaffold = build_scaffold(signal)
    props["business_scaffold"] = scaffold
    slug = f"{slug_prefix}-{(props.get('name') or signal.kind).lower()}".replace(" ", "-")
    sir = build_sir_v2(
        signal,
        repo_root,
        business_scaffold=scaffold,
        component=props.get("component_or_service"),
        graph_features={"degree": 1},
        roles=["workflow"],
        roles_evidence={"workflow": [{"connector": "unit-test"}]},
        doc_slug=slug[:80],
    )
    assert sir is not None
    return sir


def _emit_bw_signal(repo_root: Path) -> Dict[str, Any]:
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
    sample_path = repo_root / "Sample.process"
    sample_path.write_text(sample, encoding="utf-8")
    extractor = TibcoBWExtractor()
    signals = list(extractor.extract(sample_path))
    workflow = next(sig for sig in signals if sig.kind == "workflow")
    return _prep_signal(workflow, repo_root, "bw")


def _emit_powerbuilder_signal(repo_root: Path) -> Dict[str, Any]:
    src = repo_root / "u_demo.sru"
    src.write_text(
        """
        SELECT account_id, account_name FROM dbo.Accounts;
        CALL PostProcessAccounts;
        """,
        encoding="utf-8",
    )
    extractor = PowerBuilderExtractor()
    signals = list(extractor.extract(src))
    workflow = signals[0]
    return _prep_signal(workflow, repo_root, "powerbuilder")


def _emit_logicapps_signal(repo_root: Path) -> Dict[str, Any]:
    content = {
        "properties": {
            "connectionReferences": {
                "shared_powerplatformadminv2-1": {
                    "api": {"name": "shared_powerplatformadminv2"},
                    "connection": {"connectionReferenceLogicalName": "ref_powerplatform"},
                }
            },
            "definition": {
                "triggers": {
                    "Recurrence": {
                        "type": "Recurrence",
                        "inputs": {"recurrence": {"frequency": "Day", "interval": 1}},
                    }
                },
                "actions": {
                    "GetTenantCapacity": {
                        "type": "OpenApiConnection",
                        "inputs": {
                            "host": {
                                "connectionName": "shared_powerplatformadminv2-1",
                                "apiId": "/providers/Microsoft.PowerApps/apis/shared_powerplatformadminv2",
                            },
                            "method": "get",
                            "path": "/tenantCapacity",
                        },
                    }
                },
            },
        }
    }
    file_path = repo_root / "flow.json"
    file_path.write_text(json.dumps(content), encoding="utf-8")
    extractor = LogicAppsWDLExtractor()
    signals = list(extractor.extract(file_path))
    workflow = signals[0]
    return _prep_signal(workflow, repo_root, "logicapps")


def _emit_express_signal(repo_root: Path) -> Dict[str, Any]:
    source = repo_root / "app.js"
    source.write_text(
        """
const express = require('express');
const axios = require('axios');
const app = express();
const db = require('./db');

app.get('/users/:userId', (req, res) => {
  db.collection('users').find({ id: req.params.userId });
  axios.get('https://billing.internal/api');
  res.send('ok');
});
""",
        encoding="utf-8",
    )
    extractor = ExpressJSExtractor()
    signals = list(extractor.extract(source))
    workflow = signals[0]
    return _prep_signal(workflow, repo_root, "express")
