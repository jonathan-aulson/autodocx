from __future__ import annotations

import json
from pathlib import Path
from typing import List, Dict

from autodocx.utils.environment import load_project_dotenv

load_project_dotenv()

REPORT_PATH = Path("out/manifests/scaffold_coverage.json")


def main() -> None:
    if not REPORT_PATH.exists():
        print("scaffold_coverage.json not found under out/manifests/. Run autodocx scan first.")
        return
    data = json.loads(REPORT_PATH.read_text(encoding="utf-8"))
    rows: List[Dict[str, object]] = data.get("rows") or []
    total = data.get("gaps_recorded", len(rows))
    print(f"Scaffold coverage gaps: {total}")
    if not rows:
        print("All signals supplied identifiers, datastores, and process dependencies. \o/")
        return
    by_extractor: Dict[str, List[Dict[str, object]]] = {}
    for row in rows:
        by_extractor.setdefault(row.get("extractor") or "unknown", []).append(row)
    for extractor, issues in sorted(by_extractor.items(), key=lambda kv: len(kv[1]), reverse=True):
        print(f"\n[{extractor}] {len(issues)} gap(s)")
        for row in issues[:10]:
            missing = ", ".join(row.get("missing") or [])
            name = row.get("name") or row.get("kind")
            print(f" - {name} ({row.get('kind')}): missing {missing}")
        if len(issues) > 10:
            print(f"   ... {len(issues) - 10} more gaps suppressed for brevity")


if __name__ == "__main__":
    main()
