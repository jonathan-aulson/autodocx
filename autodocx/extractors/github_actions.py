from __future__ import annotations
from pathlib import Path
from typing import Iterable, List, Set
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
            signals.append(Signal(
                kind="job",
                props={"name": name, "schedules": schedules, "file": str(path), "ci_system": "github_actions", "environments": sorted(envs)},
                evidence=[f"{path}:1-60"],
                subscores={"parsed": 1.0}
            ))
        except Exception as e:
            signals.append(Signal(kind="doc", props={"name": path.name, "file": str(path), "note": f"GHA parse error: {e}"}, evidence=[f"{path}:1-1"], subscores={"parsed": 0.1}))
        return signals
