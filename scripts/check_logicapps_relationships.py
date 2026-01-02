from __future__ import annotations

import json
from pathlib import Path

from autodocx.utils.environment import load_project_dotenv

load_project_dotenv()


TARGET_SIRS = [
    "BellServiceFeeChildFlow-42A88C06-CE84-EF11-AC20-0022480A57AC.json.json",
    "BillableAccountsChildFlow20241108-0E80E27B-0B9E-EF11-8A6A-0022480A57AC.json.json",
    "CapacityAlertFlow-CC0D1317-DC4C-F011-877A-002248029144.json.json",
]


def main() -> None:
    sir_dir = Path("out/sir_v2")
    rows = []
    for name in TARGET_SIRS:
        path = sir_dir / name
        if not path.exists():
            rows.append((name, "missing", "[]"))
            continue
        data = json.loads(path.read_text(encoding="utf-8"))
        props = data.get("props") or {}
        rels = props.get("relationships") or []
        kinds = sorted({(rel.get("target") or {}).get("kind") for rel in rels if rel})
        rows.append((name, len(rels), kinds))

    for name, count, kinds in rows:
        print(f"{name}: relationships={count} target_kinds={kinds}")


if __name__ == "__main__":
    main()
