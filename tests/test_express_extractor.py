from pathlib import Path

from autodocx.extractors.express import ExpressJSExtractor
from autodocx.scaffold.signal_scaffold import build_scaffold

JS_SAMPLE = """
const express = require('express');
const axios = require('axios');
const app = express();
const db = require('./db');

app.get('/users/:userId', (req, res) => {
  db.collection('users').find({ id: req.params.userId });
  axios.get('https://billing.internal/api');
  res.send('ok');
});
"""


def test_express_extractor_emits_hints(tmp_path: Path) -> None:
    source = tmp_path / "app.js"
    source.write_text(JS_SAMPLE, encoding="utf-8")

    extractor = ExpressJSExtractor()
    signals = list(extractor.extract(source))
    assert signals
    sig = signals[0]
    assert "users" in (sig.props.get("datasource_tables") or [])
    assert "https://billing.internal/api" in (sig.props.get("service_dependencies") or [])
    assert "userId" in (sig.props.get("identifier_hints") or [])
    scaffold = build_scaffold(sig)
    assert scaffold["io_summary"]["identifiers"]
    assert scaffold["dependencies"]["datastores"]
    assert scaffold["dependencies"]["processes"]
