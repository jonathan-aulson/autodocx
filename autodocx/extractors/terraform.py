from __future__ import annotations
from pathlib import Path
from typing import Iterable, List, Optional
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
                        attr_keys: List[str] = []
                        if isinstance(props, dict):
                            attr_keys = sorted([str(k) for k in props.keys()])[:10]
                        datastore_hint = self._resource_datastore_hint(rtype, props, name)
                        service_hint = self._resource_service_hint(rtype, props, name)
                        signal_props = {
                            "resource_type": rtype,
                            "name": str(name),
                            "file": str(path),
                            "attributes": attr_keys,
                            "friendly_display": f"{rtype}.{name}",
                        }
                        if datastore_hint:
                            signal_props["datasource_tables"] = [datastore_hint]
                        if service_hint:
                            signal_props["service_dependencies"] = [service_hint]
                            signal_props["process_calls"] = [service_hint]
                        summary = f"{rtype}.{name}"
                        signals.append(Signal(
                            kind="infra",
                            props=signal_props,
                            evidence=[f"{path}:resource:{rtype}.{name}"],
                            subscores={"parsed": 1.0}
                        ))
        except Exception as e:
            signals.append(Signal(kind="doc", props={"name": path.name, "file": str(path), "note": f"Terraform parse error: {e}"}, evidence=[f"{path}:1-1"], subscores={"parsed": 0.1}))
        return signals

    def _resource_datastore_hint(self, resource_type: str, props: Optional[dict], instance_name: str) -> Optional[str]:
        rtype = (resource_type or "").lower()
        if any(token in rtype for token in ("db", "database", "sql", "dynamodb", "cosmos", "storage", "bucket", "table")):
            return str((props or {}).get("name") or instance_name or resource_type)
        if "s3_bucket" in rtype and isinstance(props, dict):
            bucket = props.get("bucket") or props.get("bucket_prefix")
            if bucket:
                return str(bucket)
        return None

    def _resource_service_hint(self, resource_type: str, props: Optional[dict], instance_name: str) -> Optional[str]:
        rtype = (resource_type or "").lower()
        if any(token in rtype for token in ("lambda", "function", "api_gateway", "service", "app")):
            return str(instance_name or (props or {}).get("name") or resource_type)
        if "kinesis" in rtype or "sqs" in rtype or "sns" in rtype:
            return str(instance_name or (props or {}).get("name") or resource_type)
        return None

