from __future__ import annotations
from pathlib import Path
from typing import Iterable, List, Dict, Any, Set, Tuple
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
                datastores, services = self._resource_hints(kind, doc)
                props = {
                    "resource_kind": kind,
                    "name": name,
                    "namespace": ns,
                    "file": str(path),
                }
                if datastores:
                    props["datasource_tables"] = sorted(datastores)
                if services:
                    sorted_services = sorted(services)
                    props["service_dependencies"] = sorted_services
                    props["process_calls"] = sorted_services
                signals.append(Signal(kind="infra", props=props, evidence=[f"{path}:1-80"], subscores={"parsed": 1.0}))
        except Exception as e:
            signals.append(Signal(kind="doc", props={"name": path.name, "file": str(path), "note": f"K8s parse error: {e}"}, evidence=[f"{path}:1-1"], subscores={"parsed": 0.1}))
        return signals

    def _resource_hints(self, kind: str | None, doc: Dict[str, Any]) -> Tuple[Set[str], Set[str]]:
        datastores: Set[str] = set()
        services: Set[str] = set()
        if not isinstance(doc, dict):
            return datastores, services
        kind_lower = (kind or "").lower()
        metadata = doc.get("metadata") or {}
        spec = doc.get("spec") or {}
        if kind_lower in {"configmap", "secret", "persistentvolumeclaim", "persistentvolume"}:
            name = metadata.get("name")
            if name:
                datastores.add(str(name))
        if kind_lower in {"deployment", "statefulset", "daemonset", "job"}:
            tmpl = (spec.get("template") or {}).get("spec") or {}
            for container in tmpl.get("containers") or []:
                image = container.get("image")
                if image:
                    services.add(str(image))
                for env in container.get("env") or []:
                    value_from = env.get("valueFrom") or {}
                    if "secretKeyRef" in value_from:
                        datastores.add(str((value_from["secretKeyRef"] or {}).get("name")))
                    if "configMapKeyRef" in value_from:
                        datastores.add(str((value_from["configMapKeyRef"] or {}).get("name")))
            for volume in tmpl.get("volumes") or []:
                if "configMap" in volume:
                    datastores.add(str((volume["configMap"] or {}).get("name")))
                if "secret" in volume:
                    datastores.add(str((volume["secret"] or {}).get("secretName")))
                if "persistentVolumeClaim" in volume:
                    datastores.add(str((volume["persistentVolumeClaim"] or {}).get("claimName")))
        if kind_lower == "service":
            svc_name = metadata.get("name")
            if svc_name:
                services.add(str(svc_name))
        datastores.discard("None")
        services.discard("None")
        return {d for d in datastores if d}, {s for s in services if s}
