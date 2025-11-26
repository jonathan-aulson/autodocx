from __future__ import annotations
from pathlib import Path
from typing import Iterable, List
from autodocx.types import Signal
try:
    import hcl2
except Exception:
    hcl2 = None

class TerraformExtractor:
    name = "terraform"
    patterns = ["**/*.tf"]

    def detect(self, repo: Path) -> bool:
        return any(repo.glob("**/*.tf"))

    def discover(self, repo: Path) -> Iterable[Path]:
        yield from repo.glob("**/*.tf")

    def extract(self, path: Path) -> Iterable[Signal]:
        signals: List[Signal] = []
        if hcl2 is None:
            signals.append(Signal(kind="doc", props={"name": path.name, "file": str(path), "note": "python-hcl2 not installed"}, evidence=[f"{path}:1-1"], subscores={"parsed": 0.1}))
            return signals
        try:
            with path.open("r", encoding="utf-8", errors="ignore") as f:
                data = hcl2.load(f) or {}
            resources = data.get("resource") or {}
            # python-hcl2 often returns a list of dicts under "resource"
            items = []
            if isinstance(resources, dict):
                items.append(resources)
            elif isinstance(resources, list):
                items.extend([x for x in resources if isinstance(x, dict)])
            for group in items:
                for rtype, instances in group.items():
                    if isinstance(instances, dict):
                        it = instances.items()
                    elif isinstance(instances, list):
                        # list of {name: {…}}
                        merged = {}
                        for d in instances:
                            if isinstance(d, dict):
                                merged.update(d)
                        it = merged.items()
                    else:
                        continue
                    for name, props in it:
                        signals.append(Signal(
                            kind="infra",
                            props={"resource_type": rtype, "name": str(name), "file": str(path)},
                            evidence=[f"{path}:resource:{rtype}.{name}"],
                            subscores={"parsed": 1.0}
                        ))
        except Exception as e:
            signals.append(Signal(kind="doc", props={"name": path.name, "file": str(path), "note": f"Terraform parse error: {e}"}, evidence=[f"{path}:1-1"], subscores={"parsed": 0.1}))
        return signals

