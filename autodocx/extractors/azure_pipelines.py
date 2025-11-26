from __future__ import annotations
from pathlib import Path
from typing import Iterable, List, Set, Dict, Any, Optional
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
            relationships = self._collect_relationships(doc, pipeline_name=name, file_path=str(path))
            signals.append(Signal(
                kind="job",
                props={
                    "name": name,
                    "schedules": schedules,
                    "file": str(path),
                    "ci_system": "azure_pipelines",
                    "environments": sorted(envs),
                    "relationships": relationships,
                },
                evidence=[f"{path}:1-60"],
                subscores={"parsed": 1.0}
            ))
        except Exception as e:
            signals.append(Signal(kind="doc", props={"name": path.name, "file": str(path), "note": f"Azure Pipelines parse error: {e}"}, evidence=[f"{path}:1-1"], subscores={"parsed": 0.1}))
        return signals

    # -------------- relationship helpers --------------

    def _collect_relationships(self, doc: Dict[str, Any], pipeline_name: str, file_path: str) -> List[Dict[str, Any]]:
        relationships: List[Dict[str, Any]] = []
        stages = doc.get("stages")
        if isinstance(stages, list):
            for stage in stages:
                relationships.extend(self._relationships_from_stage(stage, file_path))
        jobs = doc.get("jobs")
        if isinstance(jobs, list):
            for job in jobs:
                relationships.extend(self._relationships_from_job(job, file_path))
        steps = doc.get("steps")
        if isinstance(steps, list):
            relationships.extend(self._relationships_from_steps(steps, source={"type": "pipeline", "name": pipeline_name}, file_path=file_path))
        return relationships

    def _relationships_from_stage(self, stage: Dict[str, Any], file_path: str) -> List[Dict[str, Any]]:
        relationships: List[Dict[str, Any]] = []
        stage_name = stage.get("stage") or stage.get("name")
        if not stage_name:
            return relationships
        source = {"type": "stage", "name": stage_name}
        for dep in _ensure_list(stage.get("dependsOn")):
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
            relationships.extend(self._relationships_from_job(job, file_path, parent_stage=stage_name))
        return relationships

    def _relationships_from_job(self, job: Dict[str, Any], file_path: str, parent_stage: Optional[str] = None) -> List[Dict[str, Any]]:
        relationships: List[Dict[str, Any]] = []
        job_name = job.get("job") or job.get("deployment") or job.get("name")
        if not job_name:
            return relationships
        source = {"type": "job", "name": job_name}
        env = job.get("environment")
        if isinstance(env, dict):
            env_name = env.get("name")
        else:
            env_name = env
        if env_name:
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
            relationships.extend(self._relationships_from_steps(steps, source=source, file_path=file_path))
        strategy = job.get("strategy") or {}
        run_once = (strategy.get("runOnce") or {}).get("deploy") or {}
        strategy_steps = run_once.get("steps")
        if isinstance(strategy_steps, list):
            relationships.extend(self._relationships_from_steps(strategy_steps, source=source, file_path=file_path))
        return relationships

    def _relationships_from_steps(self, steps: List[Any], source: Dict[str, Any], file_path: str) -> List[Dict[str, Any]]:
        relationships: List[Dict[str, Any]] = []
        for step in steps:
            if not isinstance(step, dict):
                continue
            task = (step.get("task") or "").lower()
            inputs = step.get("inputs") or {}
            env_name = inputs.get("PowerPlatformSPN") or inputs.get("environment")
            if env_name:
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
                    relationships.append(self._relationship(
                        source=source,
                        target={"kind": "artifact", "ref": artifact_name, "display": artifact_name},
                        operation="publishes",
                        connector=task or "publish",
                        direction="outbound",
                        context={"task": step.get("displayName")},
                        evidence=[f"{file_path}:steps"],
                    ))
        return relationships

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
