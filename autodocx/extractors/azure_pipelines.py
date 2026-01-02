from __future__ import annotations
from pathlib import Path
from typing import Iterable, List, Set, Dict, Any, Optional, Tuple
import hashlib
import re, yaml
from autodocx.types import Signal

ENV_HINTS = ["dev","develop","test","qa","stage","staging","uat","preprod","prod","production","preview"]

def _infer_envs_from_path(p: Path) -> Set[str]:
    s = str(p).lower()
    envs = set()
    for h in ENV_HINTS:
        if f"-{h}" in s or f"_{h}" in s or f"/{h}/" in s or s.endswith(f"{h}.yml") or s.endswith(f"{h}.yaml"):
            envs.add(_norm_env(h))
    return envs

def _infer_envs_from_yaml(doc: dict) -> Set[str]:
    envs = set()
    # Common places: variables: { name: environment, value: prod }, stages names, templates with - dev/prod suffix
    def walk(d):
        if isinstance(d, dict):
            for k,v in d.items():
                if k in ["environment","env","name","displayName","stage","stages"]:
                    if isinstance(v, str):
                        ev = _pick_env(v)
                        if ev: envs.add(ev)
                walk(v)
        elif isinstance(d, list):
            for x in d: walk(x)
    walk(doc)
    return envs

def _pick_env(s: str) -> str | None:
    low = s.lower()
    for h in ENV_HINTS:
        if re.search(rf"\b{re.escape(h)}\b", low):
            return _norm_env(h)
    return None

def _norm_env(h: str) -> str:
    if h in ["develop","preview","production"]: return {"develop":"dev","preview":"preview","production":"prod"}[h]
    if h in ["stage"]: return "staging"
    return h

class AzurePipelinesExtractor:
    name = "azure_pipelines"
    patterns = [
        "**/azure-pipelines.yml",
        "**/azure-pipelines.yaml",
        "**/azure-pipelines*.yml",
        "**/azure-pipelines*.yaml",
        ".azure/pipelines/*.yml",
        ".azure/pipelines/*.yaml",
    ]

    def detect(self, repo: Path) -> bool:
        for pat in self.patterns:
            if any(repo.glob(pat)):
                return True
        return False

    def discover(self, repo: Path) -> Iterable[Path]:
        for pat in self.patterns:
            yield from repo.glob(pat)

    def extract(self, path: Path) -> Iterable[Signal]:
        signals: List[Signal] = []
        try:
            doc = yaml.safe_load(path.read_text(encoding="utf-8", errors="ignore")) or {}
            provided_name = doc.get("name")
            if provided_name:
                name = provided_name
            else:
                digest = hashlib.sha1(str(path).encode("utf-8")).hexdigest()[:8]
                name = f"{path.stem}-{digest}"
            schedules = []
            for s in (doc.get("schedules") or []):
                if isinstance(s, dict) and s.get("cron"):
                    schedules.append(s["cron"])
            envs = set()
            envs |= _infer_envs_from_path(path)
            envs |= _infer_envs_from_yaml(doc)
            (
                relationships,
                steps_meta,
                datastores,
                service_deps,
                process_refs,
            ) = self._collect_relationships(doc, pipeline_name=name, file_path=str(path))
            signals.append(Signal(
                kind="job",
                props={
                    "name": name,
                    "schedules": schedules,
                    "file": str(path),
                    "ci_system": "azure_pipelines",
                    "environments": sorted(envs),
                    "relationships": relationships,
                    "steps": steps_meta,
                    "datasource_tables": sorted(datastores),
                    "service_dependencies": sorted(service_deps),
                    "process_calls": sorted(process_refs),
                },
                evidence=[f"{path}:1-60"],
                subscores={"parsed": 1.0}
            ))
        except Exception as e:
            signals.append(Signal(kind="doc", props={"name": path.name, "file": str(path), "note": f"Azure Pipelines parse error: {e}"}, evidence=[f"{path}:1-1"], subscores={"parsed": 0.1}))
        return signals

    # -------------- relationship helpers --------------

    def _collect_relationships(
        self, doc: Dict[str, Any], pipeline_name: str, file_path: str
    ) -> Tuple[List[Dict[str, Any]], List[Dict[str, Any]], Set[str], Set[str], Set[str]]:
        relationships: List[Dict[str, Any]] = []
        steps_meta: List[Dict[str, Any]] = []
        datastores: Set[str] = set()
        service_deps: Set[str] = set()
        process_refs: Set[str] = set()
        stages = doc.get("stages")
        if isinstance(stages, list):
            for stage in stages:
                self._relationships_from_stage(stage, file_path, relationships, steps_meta, datastores, service_deps, process_refs)
        jobs = doc.get("jobs")
        if isinstance(jobs, list):
            for job in jobs:
                self._relationships_from_job(job, file_path, relationships, steps_meta, datastores, service_deps, process_refs)
        steps = doc.get("steps")
        if isinstance(steps, list):
            self._relationships_from_steps(
                steps,
                source={"type": "pipeline", "name": pipeline_name},
                file_path=file_path,
                relationships=relationships,
                steps_meta=steps_meta,
                datastores=datastores,
                service_deps=service_deps,
            )
        return relationships, steps_meta, datastores, service_deps, process_refs

    def _relationships_from_stage(
        self,
        stage: Dict[str, Any],
        file_path: str,
        relationships: List[Dict[str, Any]],
        steps_meta: List[Dict[str, Any]],
        datastores: Set[str],
        service_deps: Set[str],
        process_refs: Set[str],
    ) -> None:
        stage_name = stage.get("stage") or stage.get("name")
        if not stage_name:
            return
        source = {"type": "stage", "name": stage_name}
        for dep in _ensure_list(stage.get("dependsOn")):
            process_refs.add(f"{stage_name}->{dep}")
            relationships.append(self._relationship(
                source=source,
                target={"kind": "stage", "ref": dep, "display": dep},
                operation="depends_on",
                connector="azure_pipelines",
                direction="outbound",
                context={"relationship": "dependsOn"},
                evidence=[f"{file_path}:stages"],
            ))
        env = stage.get("environment")
        if isinstance(env, dict):
            env_name = env.get("name")
        else:
            env_name = env
        if env_name:
            service_deps.add(str(env_name))
            relationships.append(self._relationship(
                source=source,
                target={"kind": "environment", "ref": env_name, "display": env_name},
                operation="deploys_to",
                connector="azure_pipelines",
                direction="outbound",
                context={},
                evidence=[f"{file_path}:stages"],
            ))
        for job in stage.get("jobs") or []:
            self._relationships_from_job(job, file_path, relationships, steps_meta, datastores, service_deps, process_refs, parent_stage=stage_name)

    def _relationships_from_job(
        self,
        job: Dict[str, Any],
        file_path: str,
        relationships: List[Dict[str, Any]],
        steps_meta: List[Dict[str, Any]],
        datastores: Set[str],
        service_deps: Set[str],
        process_refs: Set[str],
        parent_stage: Optional[str] = None,
    ) -> None:
        job_name = job.get("job") or job.get("deployment") or job.get("name")
        if not job_name:
            return
        source = {"type": "job", "name": job_name}
        for dep in _ensure_list(job.get("dependsOn")):
            process_refs.add(f"{job_name}->{dep}")
        env = job.get("environment")
        if isinstance(env, dict):
            env_name = env.get("name")
        else:
            env_name = env
        if env_name:
            service_deps.add(str(env_name))
            relationships.append(self._relationship(
                source=source,
                target={"kind": "environment", "ref": env_name, "display": env_name},
                operation="deploys_to",
                connector="azure_pipelines",
                direction="outbound",
                context={"stage": parent_stage} if parent_stage else {},
                evidence=[f"{file_path}:jobs"],
            ))
        steps = job.get("steps")
        if isinstance(steps, list):
            self._relationships_from_steps(
                steps,
                source=source,
                file_path=file_path,
                relationships=relationships,
                steps_meta=steps_meta,
                datastores=datastores,
                service_deps=service_deps,
            )
        strategy = job.get("strategy") or {}
        run_once = (strategy.get("runOnce") or {}).get("deploy") or {}
        strategy_steps = run_once.get("steps")
        if isinstance(strategy_steps, list):
            self._relationships_from_steps(
                strategy_steps,
                source=source,
                file_path=file_path,
                relationships=relationships,
                steps_meta=steps_meta,
                datastores=datastores,
                service_deps=service_deps,
            )

    def _relationships_from_steps(
        self,
        steps: List[Any],
        source: Dict[str, Any],
        file_path: str,
        relationships: List[Dict[str, Any]],
        steps_meta: List[Dict[str, Any]],
        datastores: Set[str],
        service_deps: Set[str],
    ) -> None:
        for step in steps:
            if not isinstance(step, dict):
                continue
            task = (step.get("task") or "").lower()
            inputs = step.get("inputs") or {}
            entry: Dict[str, Any] = {
                "name": step.get("displayName") or task or "step",
                "connector": task or ("script" if step.get("script") else "step"),
                "job": source.get("name"),
                "inputs_keys": sorted(inputs.keys())[:5] if isinstance(inputs, dict) else [],
            }
            if step.get("script"):
                entry["script"] = step["script"].splitlines()[0][:80]
            env_name = inputs.get("PowerPlatformSPN") or inputs.get("environment")
            if env_name:
                service_deps.add(str(env_name))
                relationships.append(self._relationship(
                    source=source,
                    target={"kind": "environment", "ref": env_name, "display": env_name},
                    operation="deploys_to",
                    connector=task or "task",
                    direction="outbound",
                    context={"task": step.get("displayName")},
                    evidence=[f"{file_path}:steps"],
                ))
            if task.startswith("publishbuildartifacts") or "publish" in step:
                artifact_name = inputs.get("ArtifactName") or step.get("publish")
                if artifact_name:
                    datastores.add(str(artifact_name))
                    entry["datasource_table"] = artifact_name
                    relationships.append(self._relationship(
                        source=source,
                        target={"kind": "artifact", "ref": artifact_name, "display": artifact_name},
                        operation="publishes",
                        connector=task or "publish",
                        direction="outbound",
                        context={"task": step.get("displayName")},
                        evidence=[f"{file_path}:steps"],
                    ))
            steps_meta.append({k: v for k, v in entry.items() if v})

    def _relationship(
        self,
        *,
        source: Dict[str, Any],
        target: Dict[str, Any],
        operation: str,
        connector: str,
        direction: str,
        context: Optional[Dict[str, Any]] = None,
        evidence: Optional[List[str]] = None,
    ) -> Dict[str, Any]:
        hash_input = f"{source.get('name')}-{target.get('ref')}-{operation}-{connector}"
        digest = hashlib.sha1(hash_input.encode("utf-8")).hexdigest()[:8]
        rel_id = f"{(source.get('name') or 'pipeline').lower()}_{digest}"
        return {
            "id": rel_id.lower(),
            "source": source,
            "target": target,
            "operation": {"type": operation, "verb": "", "crud": "", "protocol": "cicd"},
            "connector": connector,
            "direction": direction,
            "context": context or {},
            "roles": ["cicd.deploys"] if operation == "deploys_to" else ["cicd.depends"] if operation == "depends_on" else ["cicd.publishes"],
            "evidence": evidence or [],
            "confidence": 0.8,
        }


def _ensure_list(value: Any) -> List[Any]:
    if value is None:
        return []
    if isinstance(value, list):
        return value
    return [value]
