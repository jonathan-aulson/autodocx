from __future__ import annotations

import zipfile
from pathlib import Path
from typing import Iterable, List, Dict

from autodocx.types import Signal


class BwArchiveExpansionExtractor:
    name = "bw_archive_expansion"
    patterns = ["**/*.ear", "**/*.jar", "**/*.zip", "**/*.par"]

    def detect(self, repo: Path) -> bool:
        repo = Path(repo)
        return any(repo.glob(p) for p in self.patterns)

    def discover(self, repo: Path) -> Iterable[Path]:
        repo = Path(repo)
        for pat in self.patterns:
            yield from repo.glob(pat)

    def extract(self, path: Path) -> Iterable[Signal]:
        path = Path(path)
        entries: List[Dict[str, str]] = []
        try:
            with zipfile.ZipFile(path, "r") as zf:
                for info in zf.infolist()[:200]:
                    entries.append({"name": info.filename, "size": info.file_size})
        except Exception:
            # Leave entries empty on failure, but still emit provenance so routing can continue.
            entries = []
        props = {
            "name": path.name,
            "file": str(path),
            "kind": "bw_archive",
            "entries": entries,
        }
        # Enrichment hint for downstream routing
        props["enrichment"] = {"archive_entries": entries}
        evidence = [f"{path}:1-1"]
        yield Signal(kind="archive", props=props, evidence=evidence, subscores={"parsed": 0.4})
