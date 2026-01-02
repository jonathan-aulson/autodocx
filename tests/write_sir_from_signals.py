# tests/write_sir_from_signals.py
from pathlib import Path
import json
from autodocx.registry import load_extractors

repo = Path("repos/Towne-Park-Billing-Source-Code/Towne-Park-Billing-PA-Solution/BillingSystem").resolve()
out_sir = Path("out/sir_v2")
out_sir.mkdir(parents=True, exist_ok=True)

extractors = load_extractors()
all_signals = []
for ex in extractors:
    try:
        if ex.detect(repo):
            for p in ex.discover(repo):
                all_signals.extend(ex.extract(p))
    except Exception as e:
        print("Extractor failed:", ex.name, e)

# Print counts and write SIR for workflows
print("Total signals:", len(all_signals))
i = 0
for s in all_signals:
    if s.kind == "workflow":
        i += 1
        name = s.props.get("name") or f"workflow_{i}"
        safe = "".join(c if c.isalnum() or c in "._-" else "_" for c in name)[:180]
        obj = {
            "id": f"workflow:{safe}",
            "name": name,
            "props": s.props,
            "evidence": s.evidence,
            "subscores": s.subscores
        }
        (out_sir / f"{safe}.json").write_text(json.dumps(obj, indent=2), encoding="utf-8")
print("Workflows written:", i)
