from __future__ import annotations
from pathlib import Path
from typing import Iterable, List
import yaml
from autodocx.types import Signal

class K8sManifestsExtractor:
    name = "k8s"
    patterns = ["**/*.yml", "**/*.yaml"]

    def detect(self, repo: Path) -> bool:
        for p in repo.glob("**/*.yml"):
            try:
                t = p.read_text(encoding="utf-8", errors="ignore")
                if "apiVersion:" in t and "kind:" in t:
                    return True
            except Exception:
                continue
        for p in repo.glob("**/*.yaml"):
            try:
                t = p.read_text(encoding="utf-8", errors="ignore")
                if "apiVersion:" in t and "kind:" in t:
                    return True
            except Exception:
                continue
        return False

    def discover(self, repo: Path) -> Iterable[Path]:
        for pat in self.patterns:
            for p in repo.glob(pat):
                try:
                    t = p.read_text(encoding="utf-8", errors="ignore")
                    if "apiVersion:" in t and "kind:" in t:
                        yield p
                except Exception:
                    continue

    def extract(self, path: Path) -> Iterable[Signal]:
        signals: List[Signal] = []
        try:
            raw = path.read_text(encoding="utf-8", errors="ignore")
            docs = list(yaml.safe_load_all(raw))
            for i, doc in enumerate(docs):
                if not isinstance(doc, dict): continue
                kind = doc.get("kind")
                meta = doc.get("metadata", {}) or {}
                name = meta.get("name") or f"{path.stem}-{i}"
                ns = meta.get("namespace") or "default"
                signals.append(Signal(kind="infra", props={"resource_kind": kind, "name": name, "namespace": ns, "file": str(path)}, evidence=[f"{path}:1-80"], subscores={"parsed": 1.0}))
        except Exception as e:
            signals.append(Signal(kind="doc", props={"name": path.name, "file": str(path), "note": f"K8s parse error: {e}"}, evidence=[f"{path}:1-1"], subscores={"parsed": 0.1}))
        return signals
