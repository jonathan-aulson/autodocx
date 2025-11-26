from __future__ import annotations
import json
from pathlib import Path
from typing import Any, Dict

def save_graph(path: Path, graph: Dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(graph, indent=2), encoding="utf-8")
