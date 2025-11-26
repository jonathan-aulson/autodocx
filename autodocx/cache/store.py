from __future__ import annotations
import hashlib, json
from pathlib import Path
from typing import Any, Optional

class Cache:
    def __init__(self, root: Path):
        self.root = root

    def key(self, plugin: str, version: str, content: bytes) -> Path:
        h = hashlib.sha256(content).hexdigest()
        return self.root / plugin / version / f"{h}.json"

    def get(self, path: Path) -> Optional[dict]:
        try:
            return json.loads(path.read_text(encoding="utf-8"))
        except Exception:
            return None

    def put(self, path: Path, obj: dict) -> None:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(obj, indent=2), encoding="utf-8")
