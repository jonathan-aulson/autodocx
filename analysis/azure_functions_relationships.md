# Azure Functions Relationship Extraction (2025-11-15)

Prior to this update the Azure Functions extractor only emitted `route` signals with no dependency data. Commands used
to verify the new relationships:

```bash
python - <<'PY'
import json, pathlib
sir = pathlib.Path('out/sir/GetDataFromEDW.json')
data = json.loads(sir.read_text())
print(json.dumps(data["props"]["relationships"], indent=2))
PY
```

Sample output (`GetDataFromEDW.cs`):

```json
[
  {
    "id": "http_589d1c74",
    "source": {"type": "trigger", "name": "http", "step_id": "httptrigger"},
    "target": {"kind": "function", "ref": "GetDataFromEDW", "display": "GetDataFromEDW"},
    "operation": {"type": "receives", "verb": "", "crud": "", "protocol": "service"},
    "connector": "httptrigger",
    "direction": "inbound",
    "context": {"route": "/get-data", "auth_level": "Function", "route_params": []},
    "roles": ["interface.receive"],
    "evidence": ["...\\GetDataFromEDW.cs:2-4"],
    "confidence": 0.85
  }
]
```

Queue/service bus/storage bindings are emitted in the same schema (connector-driven `target.kind`, roles, evidence). The route context includes `route_params` so downstream consumers can mention DTO-like hints directly in UX narratives.
