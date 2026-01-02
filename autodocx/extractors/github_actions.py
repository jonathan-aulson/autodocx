from __future__ import annotations
from pathlib import Path
from typing import Any, Dict, Iterable, List, Set, Tuple
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
    on = doc.get("on") or {}
    # environments in GitHub can also appear in jobs.<job>.environment
    jobs = doc.get("jobs") or {}
    def walk(d):
        if isinstance(d, dict):
            for k,v in d.items():
                if k in ["environment","env","name"]:
                    if isinstance(v, str):
                        ev = _pick_env(v)
                        if ev: envs.add(ev)
                walk(v)
        elif isinstance(d, list):
            for x in d: walk(x)
    walk(on); walk(jobs)
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

class GitHubActionsExtractor:
    name = "github_actions"
    patterns = [".github/workflows/*.yml", ".github/workflows/*.yaml"]

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
            name = doc.get("name") or path.stem
            schedules = []
            on = doc.get("on")
            if isinstance(on, dict) and "schedule" in on:
                schedules = [s.get("cron") for s in on.get("schedule") or [] if isinstance(s, dict)]
            envs = set()
            envs |= _infer_envs_from_path(path)
            envs |= _infer_envs_from_yaml(doc)
            triggers = self._normalize_triggers(on)
            steps_meta, datastores, service_deps = self._collect_job_steps(doc.get("jobs") or {})
            service_deps.update(envs)
            signals.append(Signal(
                kind="job",
                props={
                    "name": name,
                    "schedules": schedules,
                    "file": str(path),
                    "ci_system": "github_actions",
                    "environments": sorted(envs),
                    "triggers": triggers,
                    "steps": steps_meta,
                    "datasource_tables": sorted(datastores),
                    "service_dependencies": sorted(service_deps),
                },
                evidence=[f"{path}:1-60"],
                subscores={"parsed": 1.0}
            ))
        except Exception as e:
            signals.append(Signal(kind="doc", props={"name": path.name, "file": str(path), "note": f"GHA parse error: {e}"}, evidence=[f"{path}:1-1"], subscores={"parsed": 0.1}))
        return signals

    def _normalize_triggers(self, on_field: Any) -> List[Dict[str, Any]]:
        triggers: List[Dict[str, Any]] = []
        if isinstance(on_field, str):
            triggers.append({"event": on_field})
        elif isinstance(on_field, list):
            for item in on_field:
                if isinstance(item, str):
                    triggers.append({"event": item})
        elif isinstance(on_field, dict):
            for event, config in on_field.items():
                if event == "schedule":
                    for sched in config or []:
                        if isinstance(sched, dict):
                            triggers.append({"event": "schedule", "cron": sched.get("cron")})
                    continue
                entry: Dict[str, Any] = {"event": event}
                if isinstance(config, dict):
                    if config.get("branches"):
                        entry["branches"] = config.get("branches")
                    if config.get("types"):
                        entry["types"] = config.get("types")
                triggers.append(entry)
        return triggers[:10]

    def _collect_job_steps(self, jobs: Dict[str, Any]) -> Tuple[List[Dict[str, Any]], Set[str], Set[str]]:
        steps_meta: List[Dict[str, Any]] = []
        datastores: Set[str] = set()
        service_deps: Set[str] = set()
        for job_name, job in (jobs or {}).items():
            env = job.get("environment")
            env_name = env.get("name") if isinstance(env, dict) else env
            if env_name:
                service_deps.add(str(env_name))
            for idx, step in enumerate(job.get("steps") or []):
                if not isinstance(step, dict):
                    continue
                entry: Dict[str, Any] = {
                    "job": job_name,
                    "name": step.get("name") or step.get("uses") or f"{job_name}:{idx+1}",
                    "connector": step.get("uses") or ("run" if step.get("run") else step.get("shell") or "step"),
                    "inputs_keys": sorted((step.get("with") or {}).keys())[:5],
                }
                if step.get("run"):
                    entry["script"] = step["run"].splitlines()[0][:80]
                uses = step.get("uses")
                if uses:
                    service_deps.add(str(uses))
                    uses_lower = uses.lower()
                    artifact_name = (step.get("with") or {}).get("name")
                    if "upload-artifact" in uses_lower and artifact_name:
                        datastores.add(str(artifact_name))
                    if "cache" in uses_lower:
                        cache_key = (step.get("with") or {}).get("key")
                        if cache_key:
                            datastores.add(str(cache_key))
                env_ref = (step.get("with") or {}).get("environment") or step.get("env", {}).get("AZURE_ENVIRONMENT")
                if env_ref:
                    service_deps.add(str(env_ref))
                steps_meta.append({k: v for k, v in entry.items() if v})
        return steps_meta, datastores, service_deps
