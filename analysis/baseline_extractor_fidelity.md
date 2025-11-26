# Baseline: Extractor Fidelity (2025-11-13)

Running
```bash
python -m autodocx_cli scan repos/Towne-Park-Billing-Source-Code/Towne-Park-Billing-PA-Solution --out out
```
before improving the Logic Apps extractor produced SIRs such as:

```json
{
  "id": "doc:CapacityAlertFlow-CC0D1317-DC4C-F011-877A-002248029144.json",
  "kind": "doc",
  "props": {
    "note": "LogicApps/Flow parse error: 'str' object has no attribute 'get'"
  }
}
```

The workflow was downgraded to a plain doc signal and downstream renderers had no trigger or step metadata.
