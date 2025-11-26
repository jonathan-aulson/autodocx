#!/usr/bin/env python3
# autodocx.py
# A minimal, ready-to-run universal auto-documentation engine:
# - Plugin registry extracts normalized Signals (Repo-SIR v2) + Evidence Graph
# - Emits Option 1 universal JSON artifacts per file
# - Generates docs (no-LLM) and optionally LLM-driven docs via Option 1 prompts
#
# Dependencies:
#   pip install pyyaml python-hcl2 prance openapi-spec-validator rich jinja2
# Optional:
#   pip install mkdocs-material
#
# Usage:
#   python autodocx.py scan /path/to/repo --out out --llm openai --model gpt-4o-mini
#   python autodocx.py render --in out --mkdocs-build
#   python autodocx.py all /path/to/repo --out out --llm openai --model gpt-4o-mini --mkdocs-build
#
# Env:
#   OPENAI_API_KEY=<your key>

from __future__ import annotations

import argparse
import fnmatch
import glob
import hashlib
import json
import os
import re
import sys
import time
from dataclasses import dataclass, asdict
from pathlib import Path
from typing import Iterable, Protocol, List, Dict, Any, Optional, Tuple

import yaml
from rich import print as rprint
from rich.progress import track

from dotenv import load_dotenv

try:
    import hcl2
except Exception:
    hcl2 = None

try:
    from prance import ResolvingParser
    from openapi_spec_validator import validate_spec as validate_openapi_spec
except Exception:
    ResolvingParser = None
    validate_openapi_spec = None

from jinja2 import Template


DEBUG = False

# ---------- Core data model (Repo-SIR v2 simplified) ----------

@dataclass
class Signal:
    kind: str                    # 'api', 'op', 'event', 'db', 'job', 'infra', 'doc', 'route', ...
    props: Dict[str, Any]        # e.g., path, method, topic, queue, cron, table, region, env, service, name
    evidence: List[str]          # file:line anchors or schema pointers
    subscores: Dict[str, float]  # parsed, coverage, schema_evidence, link_integrity, runtime_alignment, test_alignment, doc_alignment, inferred_fraction

# Node/Edge for Evidence Graph
@dataclass
class Node:
    id: str
    type: str                  # Service, API, Operation, MessageTopic, Job, Datastore, Schema, InfraResource, Doc
    name: str
    props: Dict[str, Any]
    evidence: List[str]
    subscores: Dict[str, float]

@dataclass
class Edge:
    source: str
    target: str
    type: str                  # calls, publishes, consumes, reads, writes, deploys_to, uses_secret, exposes_port, depends_on
    props: Dict[str, Any]
    evidence: List[str]
    subscores: Dict[str, float]


# ---------- Plugin interface ----------

class Extractor(Protocol):
    name: str
    patterns: List[str]  # glob patterns

    def detect(self, repo: Path) -> bool: ...
    def discover(self, repo: Path) -> Iterable[Path]: ...
    def extract(self, path: Path) -> Iterable[Signal]: ...


def hash_file(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(8192), b""):
            h.update(chunk)
    return h.hexdigest()


# ---------- Utilities ----------

SECRET_PATTERNS = [
    re.compile(r"(?i)(api[_-]?key|secret|token|password|passwd)\s*[:=]\s*([^\s\"']{8,})"),
    re.compile(r"AKIA[0-9A-Z]{16}"),  # AWS access key id (basic)
    re.compile(r"([?&]sig=)[^&\s]+")  # Mask SAS signatures in Logic App/Flow callback URLs (e.g., ...&sig=abc123)
]

def redact(text: str) -> str:
    red = text
    for pat in SECRET_PATTERNS:
        # Special-case substitution for URL sig masking
        if pat.pattern.startswith("([?&]sig="):
            red = pat.sub(r"\1***REDACTED***", red)
        else:
            red = pat.sub(r"\1: ***REDACTED***", red)
    return red

def make_evidence_snippet(path: Path, start_line: int, end_line: int) -> str:
    lines = []
    try:
        with path.open("r", encoding="utf-8", errors="ignore") as f:
            content = f.readlines()
        s = max(0, start_line - 1)
        e = min(len(content), end_line)
        body = "".join(content[s:e])
        body = redact(body)
        snippet = f"{path}:{start_line}-{end_line}\n{body}"
        lines.append(snippet.strip())
    except Exception as e:
        lines.append(f"{path}:{start_line}-{end_line} (unreadable: {e})")
    return "\n".join(lines)

def service_name_from_path(path: Path, repo_root: Path) -> str:
    # Heuristic: use top-level folder name under repo (fallback to repo name)
    try:
        rel = path.relative_to(repo_root)
        parts = rel.parts
        if len(parts) > 1:
            return parts[0]
    except Exception:
        pass
    return repo_root.name

def clamp01(x: float) -> float:
    return max(0.0, min(1.0, x))


# ---------- Starter plugins ----------

class OpenAPIExtractor:
    name = "openapi"
    patterns = ["**/*.yaml", "**/*.yml", "**/*.json"]

    def detect(self, repo: Path) -> bool:
        for pat in self.patterns:
            for p in repo.glob(pat):
                if p.is_file():
                    try:
                        with p.open("r", encoding="utf-8", errors="ignore") as f:
                            head = f.read(4096)
                        if "openapi:" in head or '"openapi"' in head or "swagger:" in head:
                            return True
                    except Exception:
                        continue
        return False

    def discover(self, repo: Path) -> Iterable[Path]:
        for pat in self.patterns:
            for p in repo.glob(pat):
                if not p.is_file():
                    continue
                try:
                    with p.open("r", encoding="utf-8", errors="ignore") as f:
                        head = f.read(4096)
                    if any(k in head for k in ["openapi:", '"openapi"', "swagger:"]):
                        yield p
                except Exception:
                    continue

    def extract(self, path: Path) -> Iterable[Signal]:
        signals: List[Signal] = []
        try:
            text = path.read_text(encoding="utf-8", errors="ignore")
            # Attempt robust parse if prance available, else yaml/json dump
            if ResolvingParser:
                parser = ResolvingParser(str(path))
                spec = parser.specification
            else:
                spec = yaml.safe_load(text)

            if not spec:
                return signals

            version = spec.get("openapi") or spec.get("swagger")
            if not version:
                return signals

            servers = [s.get("url") for s in spec.get("servers", []) if isinstance(s, dict)]
            info = spec.get("info", {})
            title = info.get("title") or path.stem
            service = title

            # Root API signal
            signals.append(Signal(
                kind="api",
                props={"name": title, "version": version, "servers": servers, "service": service, "file": str(path)},
                evidence=[f"{path}:1-50"],
                subscores={"parsed": 1.0, "schema_evidence": 1.0}
            ))

            paths = spec.get("paths", {}) or {}
            for pth, methods in paths.items():
                if not isinstance(methods, dict):
                    continue
                for m, op in methods.items():
                    if m.lower() not in ["get", "post", "put", "delete", "patch", "head", "options", "trace"]:
                        continue
                    summ = (op or {}).get("summary") or ""
                    security = (op or {}).get("security") or spec.get("security") or []
                    auth = "none"
                    if security:
                        # Simplify: presence means some auth
                        auth = "auth"
                    signals.append(Signal(
                        kind="op",
                        props={"service": service, "method": m.upper(), "path": pth, "summary": summ, "auth": auth, "api": title, "file": str(path)},
                        evidence=[f"{path}:openapi(paths)"],
                        subscores={"parsed": 1.0, "endpoint_or_op_coverage": 1.0, "schema_evidence": 1.0}
                    ))
        except Exception as e:
            # Best-effort skip with evidence
            signals.append(Signal(
                kind="doc",
                props={"name": path.name, "file": str(path), "note": f"OpenAPI parse error: {e}"},
                evidence=[f"{path}:1-1"],
                subscores={"parsed": 0.1}
            ))
        return signals


class K8sManifestsExtractor:
    name = "k8s"
    patterns = ["**/*.yaml", "**/*.yml"]

    def detect(self, repo: Path) -> bool:
        for p in repo.glob("**/*.yaml"):
            if self._looks_k8s(p): return True
        for p in repo.glob("**/*.yml"):
            if self._looks_k8s(p): return True
        return False

    def _looks_k8s(self, path: Path) -> bool:
        try:
            text = path.read_text(encoding="utf-8", errors="ignore")
            if "apiVersion:" in text and "kind:" in text:
                return True
        except Exception:
            pass
        return False

    def discover(self, repo: Path) -> Iterable[Path]:
        for pat in self.patterns:
            for p in repo.glob(pat):
                if p.is_file():
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

                # Infra resource signal
                signals.append(Signal(
                    kind="infra",
                    props={"resource_kind": kind, "name": name, "namespace": ns, "file": str(path)},
                    evidence=[f"{path}:1-80"],
                    subscores={"parsed": 1.0, "runtime_alignment": 0.5}
                ))

                if kind == "Service":
                    spec = doc.get("spec", {}) or {}
                    ports = spec.get("ports", []) or []
                    for prt in ports:
                        port = prt.get("port")
                        target = prt.get("targetPort")
                        signals.append(Signal(
                            kind="op",
                            props={"service": name, "method": "PORT", "path": f":{port}", "summary": f"Exposes port {port}->{target}", "auth": "none", "file": str(path)},
                            evidence=[f"{path}:Service:{name}"],
                            subscores={"parsed": 1.0, "runtime_alignment": 0.6}
                        ))
                if kind == "Ingress":
                    spec = doc.get("spec", {}) or {}
                    rules = spec.get("rules", []) or []
                    for rule in rules:
                        host = rule.get("host")
                        http = rule.get("http", {}) or {}
                        paths = http.get("paths", []) or []
                        for pth in paths:
                            pathv = pth.get("path")
                            signals.append(Signal(
                                kind="op",
                                props={"service": name, "method": "HTTP", "path": f"{host}{pathv}", "summary": "Ingress route", "auth": "none", "file": str(path)},
                                evidence=[f"{path}:Ingress:{name}"],
                                subscores={"parsed": 1.0, "runtime_alignment": 0.7}
                            ))
        except Exception as e:
            signals.append(Signal(
                kind="doc",
                props={"name": path.name, "file": str(path), "note": f"K8s parse error: {e}"},
                evidence=[f"{path}:1-1"],
                subscores={"parsed": 0.1}
            ))
        return signals


class TerraformExtractor:
    name = "terraform"
    patterns = ["**/*.tf"]

    def detect(self, repo: Path) -> bool:
        return any(repo.glob("**/*.tf"))

    def discover(self, repo: Path) -> Iterable[Path]:
        for p in repo.glob("**/*.tf"):
            yield p

    def extract(self, path: Path) -> Iterable[Signal]:
        signals: List[Signal] = []
        if hcl2 is None:
            signals.append(Signal(
                kind="doc",
                props={"name": path.name, "file": str(path), "note": "python-hcl2 not installed"},
                evidence=[f"{path}:1-1"],
                subscores={"parsed": 0.1}
            ))
            return signals
        try:
            with path.open("r", encoding="utf-8", errors="ignore") as f:
                data = hcl2.load(f)
            resources = (data.get("resource") or {})
            for rtype, instances in resources.items():
                for name, props in (instances or {}).items():
                    signals.append(Signal(
                        kind="infra",
                        props={"resource_type": rtype, "name": name, "file": str(path)},
                        evidence=[f"{path}:resource:{rtype}.{name}"],
                        subscores={"parsed": 1.0, "runtime_alignment": 0.6}
                    ))
                    # Special-case common messaging/db
                    if "kafka" in rtype or "sns" in rtype or "sqs" in rtype or "pubsub" in rtype or "rabbit" in rtype:
                        signals.append(Signal(
                            kind="event",
                            props={"topic_or_queue": name, "broker": rtype, "direction": "unknown", "file": str(path)},
                            evidence=[f"{path}:resource:{rtype}.{name}"],
                            subscores={"parsed": 1.0, "schema_evidence": 0.3}
                        ))
                    if "db" in rtype or "postgres" in rtype or "mysql" in rtype or "rds" in rtype:
                        signals.append(Signal(
                            kind="db",
                            props={"engine": rtype, "name": name, "file": str(path)},
                            evidence=[f"{path}:resource:{rtype}.{name}"],
                            subscores={"parsed": 1.0, "schema_evidence": 0.2}
                        ))
        except Exception as e:
            signals.append(Signal(
                kind="doc",
                props={"name": path.name, "file": str(path), "note": f"Terraform parse error: {e}"},
                evidence=[f"{path}:1-1"],
                subscores={"parsed": 0.1}
            ))
        return signals


class GitHubActionsExtractor:
    name = "github_actions"
    patterns = [".github/workflows/*.yml", ".github/workflows/*.yaml"]

    def detect(self, repo: Path) -> bool:
        return any(repo.glob(".github/workflows/*.yml")) or any(repo.glob(".github/workflows/*.yaml"))

    def discover(self, repo: Path) -> Iterable[Path]:
        for pat in self.patterns:
            for p in repo.glob(pat):
                yield p

    def extract(self, path: Path) -> Iterable[Signal]:
        signals: List[Signal] = []
        try:
            doc = yaml.safe_load(path.read_text(encoding="utf-8", errors="ignore")) or {}
            name = doc.get("name") or path.stem
            on = doc.get("on") or {}
            schedules = []
            if isinstance(on, dict):
                if "schedule" in on:
                    schedules = [s.get("cron") for s in on.get("schedule") or [] if isinstance(s, dict)]
            signals.append(Signal(
                kind="job",
                props={"name": name, "schedules": schedules, "file": str(path), "ci_system": "github_actions"},
                evidence=[f"{path}:1-40"],
                subscores={"parsed": 1.0}
            ))
        except Exception as e:
            signals.append(Signal(
                kind="doc",
                props={"name": path.name, "file": str(path), "note": f"GHA parse error: {e}"},
                evidence=[f"{path}:1-1"],
                subscores={"parsed": 0.1}
            ))
        return signals


class ExpressJSExtractor:
    name = "express"
    patterns = ["**/*.js", "**/*.ts"]

    ROUTE_RE = re.compile(r"""(?P<obj>\bapp\b|\brouter\b)\.(?P<method>get|post|put|delete|patch|options|head)\s*\(\s*['"`](?P<path>[^'"`]+)['"`]""")

    def detect(self, repo: Path) -> bool:
        # Cheap detection
        pkg = repo / "package.json"
        if pkg.exists():
            try:
                j = json.loads(pkg.read_text(encoding="utf-8", errors="ignore"))
                deps = {**(j.get("dependencies") or {}), **(j.get("devDependencies") or {})}
                if "express" in deps:
                    return True
            except Exception:
                pass
        # fallback: grep-ish head scan
        for p in repo.glob("**/*.js"):
            if self._has_express(p): return True
        for p in repo.glob("**/*.ts"):
            if self._has_express(p): return True
        return False

    def _has_express(self, path: Path) -> bool:
        try:
            head = path.read_text(encoding="utf-8", errors="ignore")[:4096]
            return "express" in head or "app.get(" in head or "router.get(" in head
        except Exception:
            return False

    def discover(self, repo: Path) -> Iterable[Path]:
        for pat in self.patterns:
            for p in repo.glob(pat):
                if p.suffix.lower() in [".js", ".ts"]:
                    yield p

    def extract(self, path: Path) -> Iterable[Signal]:
        signals: List[Signal] = []
        try:
            content = path.read_text(encoding="utf-8", errors="ignore")
            svc = service_name_from_path(path, Path.cwd())
            for m in self.ROUTE_RE.finditer(content):
                method = m.group("method").upper()
                route = m.group("path")
                ln = content[:m.start()].count("\n") + 1
                signals.append(Signal(
                    kind="route",
                    props={"service": svc, "method": method, "path": route, "file": str(path)},
                    evidence=[f"{path}:{ln}-{ln+2}"],
                    subscores={"parsed": 1.0, "endpoint_or_op_coverage": 0.6}
                ))
        except Exception as e:
            signals.append(Signal(
                kind="doc",
                props={"name": path.name, "file": str(path), "note": f"Express parse error: {e}"},
                evidence=[f"{path}:1-1"],
                subscores={"parsed": 0.1}
            ))
        return signals


class SQLMigrationsExtractor:
    name = "sql_migrations"
    patterns = ["**/*.sql"]

    CREATE_RE = re.compile(r"(?is)\bcreate\s+table\s+([a-zA-Z0-9_.\"]+)")

    def detect(self, repo: Path) -> bool:
        return any(repo.glob("**/*.sql"))

    def discover(self, repo: Path) -> Iterable[Path]:
        for p in repo.glob("**/*.sql"):
            yield p

    def extract(self, path: Path) -> Iterable[Signal]:
        signals: List[Signal] = []
        try:
            txt = path.read_text(encoding="utf-8", errors="ignore")
            for m in self.CREATE_RE.finditer(txt):
                table = m.group(1).strip('"')
                ln = txt[:m.start()].count("\n") + 1
                signals.append(Signal(
                    kind="db",
                    props={"table": table, "file": str(path)},
                    evidence=[f"{path}:{ln}-{ln+5}"],
                    subscores={"parsed": 1.0, "schema_evidence": 0.6}
                ))
        except Exception as e:
            signals.append(Signal(
                kind="doc",
                props={"name": path.name, "file": str(path), "note": f"SQL parse error: {e}"},
                evidence=[f"{path}:1-1"],
                subscores={"parsed": 0.1}
            ))
        return signals


class AzurePipelinesExtractor:
    name = "azure_pipelines"
    patterns = ["**/azure-pipelines.yml", "**/azure-pipelines.yaml", ".azure/pipelines/*.yml", ".azure/pipelines/*.yaml"]

    def detect(self, repo: Path) -> bool:
        for pat in self.patterns:
            if any(repo.glob(pat)):
                return True
        return False

    def discover(self, repo: Path) -> Iterable[Path]:
        for pat in self.patterns:
            for p in repo.glob(pat):
                if p.is_file():
                    yield p

    def extract(self, path: Path) -> Iterable[Signal]:
        signals: List[Signal] = []
        try:
            doc = yaml.safe_load(path.read_text(encoding="utf-8", errors="ignore")) or {}
            name = doc.get("name") or path.stem
            # Azure Pipelines schedules live at top-level "schedules": [{ cron: "..." }, ...]
            schedules = []
            for s in (doc.get("schedules") or []):
                if isinstance(s, dict) and s.get("cron"):
                    schedules.append(s.get("cron"))
            signals.append(Signal(
                kind="job",
                props={"name": name, "schedules": schedules, "file": str(path), "ci_system": "azure_pipelines"},
                evidence=[f"{path}:1-60"],
                subscores={"parsed": 1.0}
            ))
        except Exception as e:
            signals.append(Signal(
                kind="doc",
                props={"name": path.name, "file": str(path), "note": f"Azure Pipelines parse error: {e}"},
                evidence=[f"{path}:1-1"],
                subscores={"parsed": 0.1}
            ))
        return signals



class MarkdownDocsExtractor:
    name = "markdown_docs"
    patterns = ["**/*.md", "**/*.markdown"]

    def detect(self, repo: Path) -> bool:
        return any(repo.glob("**/*.md"))

    def discover(self, repo: Path) -> Iterable[Path]:
        for p in repo.glob("**/*.md"):
            yield p

    def extract(self, path: Path) -> Iterable[Signal]:
        title = path.stem.upper()
        kind = "doc"
        if path.name.lower() in ["readme.md", "readme"]:
            title = "README"
        if "adr" in path.parts or path.name.lower().startswith("adr"):
            kind = "doc"
            title = f"ADR: {path.stem}"
        return [Signal(
            kind=kind,
            props={"name": title, "file": str(path)},
            evidence=[f"{path}:1-10"],
            subscores={"parsed": 0.7, "doc_alignment": 0.5}
        )]


class LogicAppsWDLExtractor:
    name = "logicapps_wdl"
    patterns = [
        "**/workflow.json",
        "**/definition.json",
        "**/*.logicapp.json",
        "**/*logicapp*.json",
        "**/*flow*.json",
        # ARM template names commonly used
        "**/azuredeploy*.json",
        "**/template*.json",
        "**/arm*.json",
    ]

    def detect(self, repo: Path) -> bool:
        # Be generous: if we find any plausible workflow/ARM filenames and they contain WDL hints, enable the plugin
        for pat in self.patterns:
            for p in repo.glob(pat):
                if not p.is_file():
                    continue
                try:
                    t = p.read_text(encoding="utf-8", errors="ignore")
                except Exception:
                    continue
                if self._looks_wdl_text(t):
                    return True
        # Last resort: if the repo has any JSON at all, we still return False here to avoid heavy scans on random repos.
        return False

    def discover(self, repo: Path) -> Iterable[Path]:
        # Yield only files that look like LogicApp/Flow definitions by content, not just name
        seen: set[Path] = set()
        for pat in self.patterns + ["**/*.json"]:
            for p in repo.glob(pat):
                if p in seen or not p.is_file():
                    continue
                seen.add(p)
                try:
                    t = p.read_text(encoding="utf-8", errors="ignore")
                except Exception:
                    continue
                if self._looks_wdl_text(t):
                    yield p

    def extract(self, path: Path) -> Iterable[Signal]:
        signals: List[Signal] = []
        try:
            raw = path.read_text(encoding="utf-8", errors="ignore")
            try:
                doc = json.loads(raw)
            except Exception:
                doc = yaml.safe_load(raw)
            if not isinstance(doc, dict):
                return signals

            # Case A: direct top-level WDL
            if self._has_top_level_wdl(doc):
                name = doc.get("name") or doc.get("displayName") or Path(path).stem
                wf_kind = self._guess_kind(raw, path, is_arm=False)
                wf = self._parse_definition(doc, path, wf_kind=wf_kind)
                wf["name"] = name
                signals.append(self._signal_from_wf(wf, path, [f"{path}:triggers", f"{path}:actions"]))
                return signals

            # Case B: “definition” wrapper at top-level (Logic Apps Standard, Power Automate exports)
            if isinstance(doc.get("definition"), dict) and self._has_top_level_wdl(doc["definition"]):
                name = doc.get("name") or doc.get("displayName") or Path(path).stem
                # For Logic Apps Standard, folder path often is .../Workflows/<Name>/workflow.json → prefer folder name
                parts = [p for p in path.parts if p.lower() != "workflows"]
                if "workflows" in [p.lower() for p in path.parts]:
                    # try to pull next segment after 'workflows'
                    try:
                        idx = [p.lower() for p in path.parts].index("workflows")
                        name = path.parts[idx + 1]
                    except Exception:
                        pass
                wf_kind = self._guess_kind(raw, path, is_arm=False)
                wf = self._parse_definition(doc["definition"], path, wf_kind=wf_kind)
                wf["name"] = name
                signals.append(self._signal_from_wf(wf, path, [f"{path}:definition"]))
                return signals

            # Case B2: wrapper at properties.definition (common for Power Automate cloud flows)
            if isinstance(doc.get("properties"), dict) and isinstance(doc["properties"].get("definition"), dict):
                definition = doc["properties"]["definition"]
                if self._has_top_level_wdl(definition):
                    name = doc.get("name") or doc.get("displayName") or Path(path).stem
                    # Prefer folder name after 'Workflows' if present
                    parts_lower = [p.lower() for p in path.parts]
                    if "workflows" in parts_lower:
                        try:
                            idx = parts_lower.index("workflows")
                            name = path.parts[idx + 1]
                        except Exception:
                            pass
                    wf_kind = self._guess_kind(raw, path, is_arm=False)
                    wf = self._parse_definition(definition, path, wf_kind=wf_kind)
                    wf["name"] = name
                    signals.append(self._signal_from_wf(wf, path, [f"{path}:properties.definition"]))
                    return signals

            # Case C: ARM template containing Logic Apps workflows
            if isinstance(doc.get("resources"), list):
                for res in doc["resources"]:
                    if not isinstance(res, dict):
                        continue
                    if res.get("type") == "Microsoft.Logic/workflows":
                        name = res.get("name") or Path(path).stem
                        props = res.get("properties") or {}
                        definition = props.get("definition") or {}
                        if isinstance(definition, dict) and self._has_top_level_wdl(definition):
                            wf_kind = self._guess_kind(raw, path, is_arm=True)
                            wf = self._parse_definition(definition, path, wf_kind=wf_kind)
                            wf["name"] = name
                            signals.append(self._signal_from_wf(wf, path, [f"{path}:resources:Microsoft.Logic/workflows:{name}"]))
                return signals

            return signals

        except Exception as e:
            signals.append(Signal(
                kind="doc",
                props={"name": path.name, "file": str(path), "note": f"LogicApps/Flow parse error: {e}"},
                evidence=[f"{path}:1-1"],
                subscores={"parsed": 0.1}
            ))
            return signals

    # ------------- internals -------------

    def _guess_kind(self, raw: str, path: Path, is_arm: bool) -> str:
        # ARM template with Microsoft.Logic/workflows → Consumption
        if is_arm:
            return "logicapps_consumption"
        pstr = str(path).lower()
        text = (raw or "").lower()
        # Heuristics for Power Automate (cloud flows)
        # - Exports often contain "Microsoft.Flow" in manifest.json or definition wrapper
        # - solution/flows folder names are common
        if "microsoft.flow" in text or "connectionreferences" in text or "solutions" in pstr or "/flows/" in pstr:
            return "power_automate"
        # Logic Apps Standard projects typically have Workflows/<name>/workflow.json
        if "/workflows/" in pstr or pstr.endswith("/workflow.json"):
            return "logicapps_standard"
        # Fallback: Logic Apps (unknown edition)
        return "logicapps_standard"


    def _looks_wdl_text(self, text: str) -> bool:
        t = text[:200_000]  # cap
        if '"triggers"' in t and '"actions"' in t:
            return True
        if '"Microsoft.Logic/workflows"' in t:
            return True
        if '"$schema"' in t and "Microsoft.Logic" in t:
            return True
        return False

    def _has_top_level_wdl(self, d: dict) -> bool:
        return isinstance(d.get("triggers"), dict) and isinstance(d.get("actions"), dict)

    def _parse_definition(self, definition: dict, path: Path, wf_kind: str) -> Dict[str, Any]:
        engine = "logicapps" if "logicapps" in wf_kind else "power_automate"
        triggers = self._parse_triggers(definition.get("triggers", {}))
        steps, uses_connectors, calls_flows, data_refs = self._parse_actions(definition.get("actions", {}))
        data_schemas = self._schemas_from_triggers(triggers)
        return {
            "engine": engine,
            "wf_kind": wf_kind,  # <-- carry kind forward
            "triggers": triggers,
            "steps": steps,
            "uses_connectors": sorted({c for c in uses_connectors if c}),
            "calls_flows": sorted({c for c in calls_flows if c}),
            "data_schemas": data_schemas,
            "data_refs": sorted({r for r in data_refs if r}),
        }

    def _schemas_from_triggers(self, triggers: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
        out = []
        for t in triggers:
            if t.get("schema_props"):
                out.append({"name": f"{t.get('name')}_request_schema", "format": "json", "properties": t.get("schema_props")})
        return out

    def _parse_triggers(self, trg: dict) -> List[Dict[str, Any]]:
        out = []
        for name, body in (trg or {}).items():
            ttype = (body or {}).get("type")
            kind = "trigger"
            trig = {"name": name, "type": ttype, "kind": kind}
            inputs = (body or {}).get("inputs") or {}
            # Recurrence schedule
            if ttype and ttype.lower() == "recurrence":
                freq = inputs.get("recurrence", {}).get("frequency") or inputs.get("frequency")
                interval = inputs.get("recurrence", {}).get("interval") or inputs.get("interval")
                trig["schedule"] = {"frequency": freq, "interval": interval}
            # HTTP Request trigger schema (data elements)
            if ttype and ttype.lower() in ["request", "http", "httpwebhook", "apiconnectionwebhook"]:
                schema = inputs.get("schema") or inputs.get("body") or {}
                if isinstance(schema, dict):
                    props = schema.get("properties") or {}
                    trig["schema_props"] = sorted(list(props.keys()))
            # Connector-based triggers
            if ttype and "apiconnection" in ttype.lower():
                # Heuristic connector name from $connections param ref in inputs.host.connection.name
                conn_expr = (((inputs.get("host") or {}).get("connection") or {}).get("name") or "") if isinstance(inputs, dict) else ""
                connector = self._extract_connection_name(conn_expr)
                if connector:
                    trig["connector"] = connector
            out.append(trig)
        return out

    def _parse_actions(self, actions: dict) -> Tuple[List[Dict[str, Any]], List[str], List[str], List[str]]:
        steps = []
        connectors = []
        calls = []
        data_refs = []

        def collect_refs(val) -> List[str]:
            found = []
            def walk(v):
                if isinstance(v, str):
                    for m in re.finditer(r"@{([^}]+)}", v):
                        found.append(m.group(1))
                elif isinstance(v, dict):
                    for vv in v.values():
                        walk(vv)
                elif isinstance(v, list):
                    for vv in v:
                        walk(vv)
            walk(val)
            return found

        def walk_actions(name: str, node: dict, container: str = "actions"):
            if not isinstance(node, dict):
                return

            atype = node.get("type")
            info = {"name": name, "type": atype, "kind": "action", "connector": None, "operation": None, "method": None, "url_or_path": None, "inputs_keys": []}

            # Connector-backed actions
            if atype in ["OpenApiConnection", "ApiConnection", "ApiConnectionWebhook", "ServiceProvider"]:
                inputs = node.get("inputs") or {}
                conn_expr = (((inputs.get("host") or {}).get("connection") or {}).get("name") or "") if isinstance(inputs, dict) else ""
                connector = self._extract_connection_name(conn_expr)
                operation = inputs.get("operationId")
                method = inputs.get("method")
                pathv = inputs.get("path")
                if connector:
                    info["connector"] = connector
                    connectors.append(connector)
                if operation:
                    info["operation"] = operation
                if method:
                    info["method"] = str(method).upper()
                if pathv:
                    info["url_or_path"] = pathv
                info["inputs_keys"] = sorted(list((inputs.get("body") or {}).keys())) if isinstance(inputs.get("body"), dict) else []

            # Raw HTTP actions
            if atype in ["Http", "HttpWebhook"]:
                inputs = node.get("inputs") or {}
                method = (inputs.get("method") or "").upper()
                uri = inputs.get("uri")
                info.update({"connector": "http", "operation": None, "method": method, "url_or_path": uri})
                info["inputs_keys"] = sorted(list((inputs.get("body") or {}).keys())) if isinstance(inputs.get("body"), dict) else []

                # Detect flow-to-flow invocation via HTTP callback URL
                if uri and ("/workflows/" in uri and "/triggers/" in uri and "/run" in uri):
                    calls.append(uri)

            # Control flows with nested actions
            if atype in ["If", "Switch", "Foreach", "Until", "Scope"]:
                info["kind"] = atype.lower()
                # Nested actions live under different keys
                nested_keys = []
                if atype == "If":
                    nested_keys = ["actions", "else"]
                elif atype == "Switch":
                    # cases: { "Case": { "actions": { ... } }, "Default": { ... } }
                    for case_name, case_body in (node.get("cases") or {}).items():
                        inner = (case_body or {}).get("actions") or {}
                        for iname, ibody in (inner or {}).items():
                            walk_actions(f"{name}.{case_name}.{iname}", ibody, container="cases")
                elif atype in ["Foreach", "Until", "Scope"]:
                    nested_keys = ["actions"]
                for nk in nested_keys:
                    inner = node.get(nk) or {}
                    if isinstance(inner, dict):
                        for iname, ibody in inner.items():
                            walk_actions(f"{name}.{iname}", ibody, container=nk)

            # Collect dynamic data refs (e.g., outputs('Action')?['body/foo'])
            refs = collect_refs(node)
            if refs:
                data_refs.extend(refs)

            steps.append(info)

        for n, a in (actions or {}).items():
            walk_actions(n, a, "actions")

        return steps, connectors, calls, data_refs

    def _extract_connection_name(self, expr: str) -> Optional[str]:
        # Extract 'shared_servicebus' from "@parameters('$connections')['shared_servicebus']['connectionId']"
        m = re.search(r"\['([^']+)'\]", expr or "")
        if m:
            return m.group(1)
        return None

    def _signal_from_wf(self, wf: dict, path: Path, evidence: List[str]) -> Signal:
        subs = {"parsed": 1.0, "schema_evidence": 0.4 if wf.get("data_schemas") else 0.1, "endpoint_or_op_coverage": 0.0, "runtime_alignment": 0.4}
        return Signal(
            kind="workflow",
            props={
                "name": wf.get("name"),
                "file": str(path),
                "engine": wf.get("engine"),
                "wf_kind": wf.get("wf_kind"),  # <-- add this
                "triggers": wf.get("triggers"),
                "steps": wf.get("steps"),
                "uses_connectors": wf.get("uses_connectors"),
                "calls_flows": wf.get("calls_flows"),
                "data_schemas": wf.get("data_schemas"),
                "data_refs": wf.get("data_refs"),
            },
            evidence=evidence + [f"{path}:triggers", f"{path}:actions"],
            subscores=subs
        )


# ---------- Registry ----------

REGISTRY: List[Extractor] = [
    OpenAPIExtractor(),
    K8sManifestsExtractor(),
    TerraformExtractor(),
    GitHubActionsExtractor(),
    ExpressJSExtractor(),
    SQLMigrationsExtractor(),
    AzurePipelinesExtractor(),
    MarkdownDocsExtractor(),
    LogicAppsWDLExtractor(),  
]


# ---------- Runner, Graph, Joiners, Scorer ----------

def run_all(repo: Path) -> List[Signal]:
    signals: List[Signal] = []
    for plugin in REGISTRY:
        try:
            detected = plugin.detect(repo)
            if DEBUG:
                rprint(f"[cyan]{getattr(plugin, 'name','?')} detect -> {detected}[/cyan]")
            if detected:
                count = 0
                for p in plugin.discover(repo):
                    if DEBUG:
                        rprint(f"  -> candidate: {p}")
                    count += 1
                    signals.extend(plugin.extract(p))
                if DEBUG:
                    rprint(f"[green]{getattr(plugin,'name','?')} discovered {count} files[/green]")
        except Exception as e:
            rprint(f"[yellow]Plugin {getattr(plugin, 'name', 'unknown')} failed: {e}[/yellow]")
    return signals


def build_graph(signals: List[Signal], repo: Path) -> Tuple[List[Node], List[Edge]]:
    nodes: Dict[str, Node] = {}
    edges: List[Edge] = []

    def node_id(prefix: str, name: str) -> str:
        return f"{prefix}:{name}"

    for s in signals:
        kind = s.kind
        p = s.props
        ev = s.evidence
        subs = s.subscores

        # Minimal mapping to nodes
        if kind == "api":
            nid = node_id("API", p.get("name", "api"))
            nodes[nid] = Node(nid, "API", p.get("name", "api"), p, ev, subs)
        elif kind == "op":
            op_name = f"{p.get('method','OP')} {p.get('path','')}"
            nid = node_id("Operation", op_name)
            nodes[nid] = Node(nid, "Operation", op_name, p, ev, subs)
            # Link to API if present
            api = p.get("api")
            if api:
                edges.append(Edge(node_id("API", api), nid, "exposes", {}, ev, subs))
        elif kind == "route":
            op_name = f"{p.get('method','ROUTE')} {p.get('path','')}"
            nid = node_id("Operation", op_name)
            nodes[nid] = Node(nid, "Operation", op_name, p, ev, subs)
        elif kind == "event":
            name = p.get("topic_or_queue") or p.get("name") or "event"
            nid = node_id("MessageTopic", name)
            nodes[nid] = Node(nid, "MessageTopic", name, p, ev, subs)
        elif kind == "db":
            name = p.get("name") or p.get("table") or "db"
            nid = node_id("Datastore", name)
            nodes[nid] = Node(nid, "Datastore", name, p, ev, subs)
        elif kind == "infra":
            name = p.get("name") or p.get("resource_kind") or "infra"
            nid = node_id("InfraResource", f"{p.get('resource_kind') or p.get('resource_type')}:{name}")
            nodes[nid] = Node(nid, "InfraResource", name, p, ev, subs)
        elif kind == "job":
            name = p.get("name") or "job"
            nid = node_id("Job", name)
            nodes[nid] = Node(nid, "Job", name, p, ev, subs)
        elif kind == "doc":
            name = p.get("name") or "doc"
            nid = node_id("Doc", name)
            nodes[nid] = Node(nid, "Doc", name, p, ev, subs)
        elif kind == "workflow":
            name = p.get("name") or "workflow"
            nid = node_id("Workflow", name)
            nodes[nid] = Node(nid, "Workflow", name, p, ev, subs)
            # Link if this workflow calls other flows by URL
            for u in p.get("calls_flows", []) or []:
                # Try to extract the target workflow name from the URL: .../workflows/<name>/triggers/<t>/run
                m = re.search(r"/workflows/([^/]+)/triggers/([^/]+)/run", u)
                if m:
                    target_name = m.group(1)
                    target_id = node_id("Workflow", target_name)
                    edges.append(Edge(nid, target_id, "calls", {"via": "http"}, [f"{p.get('file')}:actions:http_call"], subs))


    # Simple joiners (placeholders for expansion)
    # - Nothing advanced here; hooks for future (HTTP clients → APIs, topics → consumers, etc.)

    return list(nodes.values()), edges

def compute_facets(nodes: List[Node], edges: List[Edge]) -> Dict[str, float]:
    # Aggregate naive facets across graph as a baseline
    # Real implementation should compute per-service/component facets; here, a single rollup
    counts = {
        "ops": sum(1 for n in nodes if n.type == "Operation"),
        "apis": sum(1 for n in nodes if n.type == "API"),
        "events": sum(1 for n in nodes if n.type == "MessageTopic"),
        "dbs": sum(1 for n in nodes if n.type == "Datastore"),
        "infra": sum(1 for n in nodes if n.type == "InfraResource"),
        "docs": sum(1 for n in nodes if n.type == "Doc"),
    }
    parsed = 1.0 if nodes else 0.0
    endpoint_or_op_coverage = clamp01(counts["ops"] / max(1, counts["ops"] + counts["apis"]))
    schema_evidence = clamp01((counts["apis"] + counts["dbs"]) / max(1, len(nodes)))
    link_integrity = clamp01(len(edges) / max(1, counts["ops"]))
    runtime_alignment = clamp01(counts["infra"] / max(1, len(nodes)))
    # Simplified; others default small
    test_alignment = 0.0
    doc_alignment = clamp01(counts["docs"] / max(1, len(nodes)))
    inferred_fraction = 0.1  # conservative penalty

    score = min(1.0,
        0.40*endpoint_or_op_coverage +
        0.20*schema_evidence +
        0.15*link_integrity +
        0.10*runtime_alignment +
        0.05*test_alignment +
        0.05*doc_alignment +
        0.05*parsed
        - 0.10*inferred_fraction
    )
    return {
        "score": round(score, 3),
        "parsed": parsed,
        "endpoint_or_op_coverage": round(endpoint_or_op_coverage, 3),
        "schema_evidence": round(schema_evidence, 3),
        "link_integrity": round(link_integrity, 3),
        "runtime_alignment": round(runtime_alignment, 3),
        "test_alignment": round(test_alignment, 3),
        "doc_alignment": round(doc_alignment, 3),
        "inferred_fraction": inferred_fraction,
        **counts
    }


# ---------- Option 1 universal JSON per artifact ----------

OPTION1_TYPES = {
    "api": "api_spec",
    "op": "route_code",       # operations from OpenAPI map primarily to API; pure routes also map to route_code
    "route": "route_code",
    "event": "event_definition",
    "db": "db_schema",
    "infra": "k8s_manifest",  # or terraform; we can infer from props
    "job": "ci_pipeline",
    "doc": "design_doc"
}

def to_option1_artifact(signal: Signal, repo: Path) -> Dict[str, Any]:
    # Map a Signal to the universal JSON schema (best-effort, evidence-first)
    artifact_type = OPTION1_TYPES.get(signal.kind, "other")
    props = signal.props
    repo_path = props.get("file", "")
    service = props.get("service") or service_name_from_path(Path(repo_path), repo)
    lang_or_fmt = "unknown"
    if repo_path.endswith((".yml", ".yaml")):
        lang_or_fmt = "yaml"
    elif repo_path.endswith(".json"):
        lang_or_fmt = "json"
    elif repo_path.endswith(".tf"):
        lang_or_fmt = "hcl2"
    elif repo_path.endswith(".sql"):
        lang_or_fmt = "sql"
    elif repo_path.endswith(".js"):
        lang_or_fmt = "javascript"
    elif repo_path.endswith(".ts"):
        lang_or_fmt = "typescript"
    elif repo_path.endswith(".md"):
        lang_or_fmt = "markdown"

    # Base skeleton per provided schema
    o = {
        "artifact_type": artifact_type,
        "name": props.get("name") or f"{signal.kind}:{props.get('path') or props.get('resource_kind') or ''}".strip(":"),
        "description": "",
        "repo_path": repo_path,
        "language_or_format": lang_or_fmt,
        "component_or_service": service,
        "capabilities": [],
        "entry_points": [],
        "interfaces": {
            "http_endpoints": [],
            "grpc_services": [],
            "graphql": {"queries": [], "mutations": [], "subscriptions": [], "evidence": ""},
            "events": []
        },
        "workflows": [],
        "data": {
            "schemas": [],
            "tables_or_collections": [],
            "pii_categories": []
        },
        "infrastructure": {
            "cloud": "",
            "regions": [],
            "k8s": {"namespaces": [], "deployments": []},
            "serverless": [],
            "terraform_modules": []
        },
        "build_and_deploy": {
            "ci": [],
            "artifacts": [],
            "environments": []
        },
        "security": {
            "auth": [],
            "secrets": [],
            "compliance": []
        },
        "observability": {
            "metrics": [],
            "logs": [],
            "alerts": [],
            "dashboards": []
        },
        "dependencies": {
            "internal_services": [],
            "external_services": [],
            "datastores": []
        },
        "operations": {
            "slo_sla": {"availability": "", "latency": ""},
            "runbooks": []
        },
        "risk_and_gaps": [],
        "assumptions": [],
        "confidence": 0.0,
        "evidence": []
    }

    # Populate based on kind with only evidenced data
    ev_snips = [{"path": e.split(":")[0], "lines": (e.split(":")[1] if ":" in e else ""), "snippet": ""} for e in signal.evidence[:3]]
    o["evidence"] = ev_snips

    # Confidence from subscores crude aggregation
    conf = 0.40*signal.subscores.get("endpoint_or_op_coverage", 0) + \
           0.20*signal.subscores.get("schema_evidence", 0) + \
           0.15*signal.subscores.get("link_integrity", 0) + \
           0.10*signal.subscores.get("runtime_alignment", 0) + \
           0.05*signal.subscores.get("doc_alignment", 0) + \
           0.05*signal.subscores.get("parsed", 0) - \
           0.10*signal.subscores.get("inferred_fraction", 0)
    o["confidence"] = round(clamp01(conf), 3)

    if signal.kind in ["op", "route"]:
        # HTTP endpoints if method matches HTTP verb
        method = props.get("method", "")
        pathv = props.get("path", "")
        if method in ["GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS"]:
            o["interfaces"]["http_endpoints"].append({
                "method": method,
                "path": pathv,
                "summary": props.get("summary", ""),
                "auth": props.get("auth", "none"),
                "request_schema_ref": "",
                "response_schema_ref": "",
                "status_codes": [],
                "evidence": "; ".join(signal.evidence[:1])
            })
            o["entry_points"].append({"type": "http", "value": f"{method} {pathv}", "evidence": signal.evidence[0] if signal.evidence else ""})
            o["capabilities"].append("exposes REST API")
    elif signal.kind == "api":
        o["name"] = props.get("name") or "API"
        o["capabilities"].append("exposes REST API")
        o["entry_points"].append({"type": "http", "value": ",".join(props.get("servers", [])), "evidence": signal.evidence[0] if signal.evidence else ""})
    elif signal.kind == "event":
        o["interfaces"]["events"].append({
            "topic_or_channel": props.get("topic_or_queue", ""),
            "direction": props.get("direction", "unknown"),
            "schema_ref": "",
            "broker": props.get("broker", "other"),
            "evidence": "; ".join(signal.evidence[:1])
        })
        o["entry_points"].append({"type": "message", "value": props.get("topic_or_queue",""), "evidence": signal.evidence[0] if signal.evidence else ""})
        o["capabilities"].append("publishes/subscribes events")
    elif signal.kind == "db":
        if props.get("table"):
            o["data"]["tables_or_collections"].append(props["table"])
        engine = props.get("engine", "")
        if engine:
            o["dependencies"]["datastores"].append(engine)
        o["capabilities"].append("stores structured data")
    elif signal.kind == "infra":
        rk = props.get("resource_kind")
        rt = props.get("resource_type")
        if rk:
            o["artifact_type"] = "k8s_manifest"
        elif rt:
            o["artifact_type"] = "terraform"
        o["capabilities"].append("defines infrastructure")
    elif signal.kind == "job":
        o["artifact_type"] = "ci_pipeline"
        schedules = props.get("schedules") or []
        if schedules:
            o["workflows"].append({
                "name": props.get("name"),
                "kind": "other",
                "trigger": "schedule",
                "steps_summary": "",
                "evidence": "; ".join(signal.evidence[:1])
            })
        o["capabilities"].append("runs CI/CD pipeline")

        # NEW: record CI system when evidenced
        ci_system = (props.get("ci_system") or "").strip()
        if ci_system in {"azure_pipelines", "github_actions", "gitlab_ci", "jenkins", "circleci"}:
            # normalize to the schema's allowed values
            o["build_and_deploy"]["ci"].append(ci_system)

    elif signal.kind == "doc":
        o["artifact_type"] = "design_doc"
        o["capabilities"].append("provides human documentation")
    elif signal.kind == "workflow":
        o["artifact_type"] = "workflow_dag"
        o["name"] = props.get("name") or "workflow"
        o["capabilities"].append("orchestrates workflow")
        if props.get("calls_flows"):
            o["capabilities"].append("triggers other flows")

        # If this is Logic Apps / Power Automate, mark cloud as Azure
        if (props.get("wf_kind") in {"logicapps_standard", "logicapps_consumption", "power_automate", "power_automate_desktop"}) or \
           ((props.get("engine") or "").lower() in {"logicapps", "logicapps/powerautomate"}):
            o["infrastructure"]["cloud"] = "azure"

        # Entry points from triggers
        for t in props.get("triggers") or []:
            ttype = (t.get("type") or "").lower()
            if ttype in ["request", "http", "httpwebhook", "apiconnectionwebhook"]:
                o["entry_points"].append({"type": "http", "value": f"{t.get('name')} ({ttype})", "evidence": f"{repo_path}:triggers"})
            elif ttype in ["recurrence"]:
                sch = t.get("schedule") or {}
                val = f"{sch.get('frequency')}/{sch.get('interval')}"
                o["entry_points"].append({"type": "schedule", "value": val, "evidence": f"{repo_path}:triggers"})
            elif "apiconnection" in ttype:
                o["entry_points"].append({"type": "message", "value": f"{t.get('connector','conn')} trigger", "evidence": f"{repo_path}:triggers"})

        # Workflows: single consolidated summary
        steps = props.get("steps") or []
        parts = []
        for s in steps[:40]:
            tag = s.get("connector") or s.get("type") or "step"
            parts.append(f"{s.get('name')}[{tag}]")
        wf_kind = props.get("wf_kind") or ("logicapps_standard" if "logicapps" in (props.get("engine") or "") else "power_automate")  # <-- choose kind
        o["workflows"].append({
            "name": props.get("name"),
            "kind": wf_kind,  # <-- emit specific kind
            "trigger": "http|schedule|event",
            "steps_summary": " -> ".join(parts) if parts else "",
            "evidence": f"{repo_path}:actions"
        })
        # Short textual path: Name[connector|type] -> ...
        parts = []
        for s in steps[:40]:
            tag = s.get("connector") or s.get("type") or "step"
            parts.append(f"{s.get('name')}[{tag}]")
        o["workflows"].append({
            "name": props.get("name"),
            "kind": "other",
            "trigger": "http|schedule|event",
            "steps_summary": " -> ".join(parts) if parts else "",
            "evidence": f"{repo_path}:actions"
        })

        # Data schemas from triggers (request JSON schema properties)
        for sch in props.get("data_schemas") or []:
            o["data"]["schemas"].append({"name": sch.get("name"), "format": "json", "evidence": f"{repo_path}:triggers"})

        # Dependencies from connectors (best-effort categories)
        connectors = props.get("uses_connectors") or []
        # Simple heuristic mapping to dependency types
        msg_keys = {"shared_servicebus", "shared_eventhubs", "shared_kafka"}
        db_keys = {"shared_sql", "shared_azuresql", "shared_commondataservice", "shared_commondataserviceforapps"}
        storage_keys = {"shared_azureblob", "shared_onedriveforbusiness", "shared_sharepointonline"}
        for c in connectors:
            if c in msg_keys:
                o["interfaces"]["events"].append({"topic_or_channel": "", "direction": "publishes|subscribes", "schema_ref": "", "broker": "other", "evidence": f"{repo_path}:actions"})
            if c in db_keys:
                o["dependencies"]["datastores"].append("sql")
            if c in storage_keys:
                o["dependencies"]["external_services"].append(c)
        # HTTP calls to other flows (note: URLs redacted if sig present)
        if props.get("calls_flows"):
            o["interfaces"]["http_endpoints"].extend([
                {"method": "POST", "path": u, "summary": "Calls another flow trigger", "auth": "custom", "request_schema_ref": "", "response_schema_ref": "", "status_codes": [], "evidence": f"{repo_path}:actions"}
                for u in props["calls_flows"]
            ])

    return o


# ---------- LLM prompts (Option 1) ----------

ARTIFACT_EXTRACTOR_PROMPT = """You are a software documentation extractor. Parse code/spec/config files and extract only information explicitly evidenced in the content.
Output must be strictly valid JSON matching the provided schema. Do not include comments or additional text.
If the file is not information-rich or not parseable, return a minimal JSON with artifact_type="other", description="skip: reason", confidence=0, and include evidence explaining the skip decision.
Never hallucinate. Populate fields only if supported by evidence, and include short evidence snippets with line numbers for key claims.
Keep descriptions concise and factual. Infer common defaults only if strongly implied by the file (and note as assumption).

Schema:
{
  "artifact_type": "api_spec | asyncapi | graphql_schema | grpc_proto | soap_wsdl | workflow_dag | step_functions | etl_job | integration_flow | k8s_manifest | terraform | cloudformation | serverless | ci_pipeline | docker | helm_chart | db_schema | migration | data_contract | message_schema | event_definition | postman_collection | route_code | readme | adr | design_doc | auth_config | security_policy | monitoring_dashboard | alert_policy | feature_flag_config | other",
  "name": "",
  "description": "",
  "repo_path": "",
  "language_or_format": "",
  "component_or_service": "",
  "capabilities": [
    "short verbs e.g., exposes REST API, processes payments, ingests telemetry"
  ],
  "entry_points": [
    {
      "type": "http | grpc | graphql | cli | schedule | message | job",
      "value": "",
      "evidence": "snippet with line numbers"
    }
  ],
  "interfaces": {
    "http_endpoints": [
      {
        "method": "GET",
        "path": "/v1/orders",
        "summary": "",
        "auth": "none | apiKey | oauth2 | oidc | custom",
        "request_schema_ref": "",
        "response_schema_ref": "",
        "status_codes": [
          "200",
          "400",
          "401",
          "500"
        ],
        "evidence": ""
      }
    ],
    "grpc_services": [
      {
        "service": "",
        "rpcs": [
          {
            "name": "",
            "request": "",
            "response": ""
          }
        ],
        "evidence": ""
      }
    ],
    "graphql": {
      "queries": [
        "",
        ""
      ],
      "mutations": [
        ""
      ],
      "subscriptions": [
        ""
      ],
      "evidence": ""
    },
    "events": [
      {
        "topic_or_channel": "",
        "direction": "publishes | subscribes",
        "schema_ref": "",
        "broker": "kafka | sns | sqs | pubsub | rabbitmq | nats | other",
        "evidence": ""
      }
    ]
  },
  "workflows": [
    {
      "name": "",
      "kind": "logicapps_standard | logicapps_consumption | power_automate | power_automate_desktop | bpmn | airflow | step_functions | nifi | mule | tibco_bw | other",
      "trigger": "schedule | event | http",
      "steps_summary": "",
      "evidence": ""
    }
  ],
  "data": {
    "schemas": [
      {
        "name": "",
        "format": "sql|json|avro|proto|graphql|xsd",
        "evidence": ""
      }
    ],
    "tables_or_collections": [
      "",
      ""
    ],
    "pii_categories": [
      "none | email | name | dob | payment | health | other"
    ]
  },
  "infrastructure": {
    "cloud": "aws | azure | gcp | onprem | hybrid",
    "regions": [
      "us-east-1"
    ],
    "k8s": {
      "namespaces": [],
      "deployments": []
    },
    "serverless": [
      {
        "function": "",
        "trigger": "http|event|schedule"
      }
    ],
    "terraform_modules": [
      "",
      ""
    ]
  },
  "build_and_deploy": {
    "ci": [
      "github_actions",
      "jenkins",
      "gitlab_ci",
      "azure_pipelines",
      "circleci"
    ],
    "artifacts": [
      "docker image",
      "lambda zip"
    ],
    "environments": [
      "dev",
      "staging",
      "prod"
    ]
  },
  "security": {
    "auth": [
      "oauth2",
      "oidc",
      "apiKey",
      "mTLS",
      "saml"
    ],
    "secrets": [
      "env var names…"
    ],
    "compliance": [
      "pci | hipaa | soc2 | gdpr | none"
    ]
  },
  "observability": {
    "metrics": [
      "",
      ""
    ],
    "logs": [
      "",
      ""
    ],
    "alerts": [
      "",
      ""
    ],
    "dashboards": [
      "",
      ""
    ]
  },
  "dependencies": {
    "internal_services": [
      "svc-a",
      "svc-b"
    ],
    "external_services": [
      "stripe",
      "auth0"
    ],
    "datastores": [
      "postgres",
      "s3"
    ]
  },
  "operations": {
    "slo_sla": {
      "availability": "",
      "latency": ""
    },
    "runbooks": [
      ""
    ]
  },
  "risk_and_gaps": [
    "unknown auth flow",
    "no schema for events"
  ],
  "assumptions": [
    "if any"
  ],
  "confidence": 0.0,
  "evidence": [
    {
      "path": "",
      "lines": "12-34",
      "snippet": "…"
    }
  ]
}

Inputs:
repo_path: {{repo_path}}
file_name: {{file_name}}
language_or_format_hint: {{hint_or_unknown}}
content: \"\"\" {{file_content}} \"\"\"

Tasks:
Classify artifact_type from this controlled list: [api_spec, asyncapi, graphql_schema, grpc_proto, soap_wsdl, workflow_dag, step_functions, etl_job, integration_flow, k8s_manifest, terraform, cloudformation, serverless, ci_pipeline, docker, helm_chart, db_schema, migration, data_contract, message_schema, event_definition, postman_collection, route_code, readme, adr, design_doc, auth_config, security_policy, monitoring_dashboard, alert_policy, feature_flag_config, other].
Extract fields into the universal schema. Only include facts directly supported by the content. Where possible, list endpoints, topics, workflows, schedules, resources, and environment names.
Provide 1–5 short evidence snippets with line numbers for the most important extracted facts.
Provide a confidence score 0.0–1.0 based on clarity, completeness, and corroborating evidence.
Output strictly valid JSON per the schema. No extra text.
"""

PER_ARTIFACT_MD_PROMPT = """You are generating a concise documentation snippet for one artifact, using only the supplied JSON fields.
Audience: mixed technical/business readers.
Do not invent details. If a field is missing, omit that subsection.

Input example:
{
  "artifact_json": {
    "artifact_type": "workflow_dag",
    "name": "OrderProcessing",
    "description": "Logic Apps workflow orchestrating order intake to fulfillment.",
    "repo_path": "workflows/OrderProcessing/workflow.json",
    "language_or_format": "json",
    "component_or_service": "order-service",
    "capabilities": ["orchestrates workflow", "triggers other flows"],
    "entry_points": [
      { "type": "http", "value": "orderReceived (request)", "evidence": "workflows/OrderProcessing/workflow.json:triggers" },
      { "type": "schedule", "value": "Minute/5", "evidence": "workflows/OrderProcessing/workflow.json:triggers" }
    ],
    "interfaces": {
      "http_endpoints": [
        {
          "method": "POST",
          "path": "https://example.logic.azure.com/workflows/Shipping/triggers/manual/run",
          "summary": "Calls another flow trigger",
          "auth": "custom",
          "request_schema_ref": "",
          "response_schema_ref": "",
          "status_codes": [],
          "evidence": "workflows/OrderProcessing/workflow.json:actions"
        }
      ],
      "grpc_services": [],
      "graphql": { "queries": [], "mutations": [], "subscriptions": [], "evidence": "" },
      "events": [
        { "topic_or_channel": "", "direction": "publishes|subscribes", "schema_ref": "", "broker": "other", "evidence": "workflows/OrderProcessing/workflow.json:actions" }
      ]
    },
    "workflows": [
      {
        "name": "OrderProcessing",
        "kind": "other",
        "trigger": "http|schedule|event",
        "steps_summary": "ValidateOrder[http] -> Enrich[shared_sql] -> QueueForShipping[shared_servicebus] -> CallShipping[http]",
        "evidence": "workflows/OrderProcessing/workflow.json:actions"
      }
    ],
    "data": {
      "schemas": [
        { "name": "orderReceived_request_schema", "format": "json", "evidence": "workflows/OrderProcessing/workflow.json:triggers" }
      ],
      "tables_or_collections": [],
      "pii_categories": ["name", "email", "payment"]
    },
    "infrastructure": {
      "cloud": "azure",
      "regions": ["eastus"],
      "k8s": { "namespaces": [], "deployments": [] },
      "serverless": [],
      "terraform_modules": []
    },
    "build_and_deploy": {
      "ci": ["github_actions"],
      "artifacts": [],
      "environments": ["dev", "prod"]
    },
    "security": {
      "auth": ["custom"],
      "secrets": ["LOGICAPP_SIG"],
      "compliance": ["none"]
    },
    "observability": {
      "metrics": [],
      "logs": [],
      "alerts": [],
      "dashboards": []
    },
    "dependencies": {
      "internal_services": [],
      "external_services": ["shared_azureblob"],
      "datastores": ["sql"]
    },
    "operations": {
      "slo_sla": { "availability": "", "latency": "" },
      "runbooks": []
    },
    "risk_and_gaps": ["no schema for events"],
    "assumptions": [],
    "confidence": 0.74,
    "evidence": [
      { "path": "workflows/OrderProcessing/workflow.json", "lines": "triggers", "snippet": "..." },
      { "path": "workflows/OrderProcessing/workflow.json", "lines": "actions", "snippet": "..." }
    ]
  }
}

Output (Markdown)
Title: <artifact_type>: <name> (<repo_path>)
Summary: 2–4 sentences describing what this artifact does and why it matters.
Interfaces: list endpoints/rpcs/events with 1-line descriptions.
Workflows: brief outline of steps/triggers.
Data: key schemas/tables, PII notes.
Infrastructure & Deploy: cloud, regions, K8s/Serverless, environments.
Security & Auth: methods, secrets, compliance.
Dependencies: internal/external services and datastores.
Operations: SLO/SLA if present, runbooks.
Risks/Gaps: bullets.
Evidence: inline code blocks with small snippets.
"""

SYSTEM_SYNTH_PROMPT = """You are a systems documentarian synthesizing many artifacts into coherent, business-readable documentation.
Inputs include a set of artifact JSON objects AND/OR their generated markdown snippets.

Goals:
- Deduplicate and group by component/service.
- Map capabilities to user/business value.
- Enumerate interfaces (HTTP/gRPC/GraphQL/events) with owners and purposes.
- Describe key workflows and data flows end-to-end.
- Call out environments, deployment topology, and major dependencies.
- Summarize security/auth/compliance posture.
- Highlight operational readiness (observability, SLOs, runbooks).
- Identify risks, unknowns, and next steps.
- Do not fabricate. If uncertain, state the gap explicitly.
- Clearly separate “Facts (evidenced)” vs “Open questions.”
- Keep it modular: clear section headings and bullet lists; no tables required.

Output (Markdown)
- Executive summary
- System overview and capability map
- Interfaces and integrations
- Key workflows
- Data model highlights
- Environments and deployment
- Security and compliance
- Observability and operations
- Risks and gaps
- Confidence scoring details (rollup data table with key facets, legend, & explanations)
- Appendix: Artifact index with repo paths and evidence pointers
"""


# ---------- LLM integration (optional) ----------

def call_openai(model: str, prompt: str, input_json: Optional[Dict[str, Any]] = None, max_tokens: int = 1500) -> str:
    try:
        import os
        import openai
        api_key = os.getenv("OPENAI_API_KEY")
        if not api_key:
            raise RuntimeError("OPENAI_API_KEY not set")
        openai.api_key = api_key

        # Use Responses API if available; fallback to ChatCompletions
        try:
            client = openai.OpenAI()
            messages = [
                {"role": "system", "content": "You are a precise, citation-driven documentation assistant."},
                {"role": "user", "content": prompt},
            ]
            if input_json is not None:
                messages.append({"role": "user", "content": json.dumps(input_json)})
            resp = client.chat.completions.create(
                model=model,
                messages=messages,
                temperature=0.1,
                max_tokens=max_tokens,
            )
            return resp.choices[0].message.content
        except Exception as e:
            # Legacy SDK path
            completion = openai.ChatCompletion.create(
                model=model,
                messages=[
                    {"role": "system", "content": "You are a precise, citation-driven documentation assistant."},
                    {"role": "user", "content": prompt},
                    {"role": "user", "content": json.dumps(input_json) if input_json else ""},
                ],
                temperature=0.1,
                max_tokens=max_tokens
            )
            return completion["choices"][0]["message"]["content"]
    except Exception as e:
        rprint(f"[yellow]LLM call failed: {e}[/yellow]")
        return ""


# ---------- Rendering (no-LLM + optional LLM) ----------

INDEX_TPL = Template("""# Repo Documentation

- Coverage summary:
  - APIs: {{ counts.apis }}
  - Operations: {{ counts.ops }}
  - Events: {{ counts.events }}
  - Datastores: {{ counts.dbs }}
  - Infra: {{ counts.infra }}
  - Docs: {{ counts.docs }}
- Confidence score (rollup): {{ facets.score }}

See:
- [System overview](system.md)
- [Artifacts](artifacts.md)
""")

SYSTEM_TPL_NO_LLM = Template("""# System overview

## Executive summary
This repository contains:
- {{ counts.apis }} APIs, {{ counts.ops }} operations
- {{ counts.events }} messaging topics/queues
- {{ counts.dbs }} datastore items
- {{ counts.infra }} infrastructure resources

Confidence score (rollup): {{ facets.score }}

## Interfaces and integrations
- HTTP operations:
{% for n in nodes if n.type == "Operation" and n.props.get("method") in ["GET","POST","PUT","DELETE","PATCH","HEAD","OPTIONS"] %}
  - {{ n.props.get("method") }} {{ n.props.get("path") }} (service: {{ n.props.get("service") or "unknown" }})
{% endfor %}
- Events/topics:
{% for n in nodes if n.type == "MessageTopic" %}
  - {{ n.name }} (broker: {{ n.props.get("broker","other") }})
{% endfor %}

## Workflows
{% for n in nodes if n.type == "Workflow" %}
- {{ n.name }} (engine: {{ n.props.get("engine") }}{% if n.props.get("wf_kind") %}, kind: {{ n.props.get("wf_kind") }}{% endif %})
  - Triggers:
  {% for t in n.props.get("triggers") or [] %}
    - {{ t.name }} ({{ t.type }}){% if t.schedule %} schedule: {{ t.schedule.frequency }}/{{ t.schedule.interval }}{% endif %}{% if t.schema_props %}; request fields: {{ ", ".join(t.schema_props) }}{% endif %}
  {% endfor %}
  - Steps (first ~10):
  {% for s in (n.props.get("steps") or [])[:10] %}
    - {{ s.name }} [{{ s.connector or s.type }}]{% if s.method %} {{ s.method }}{% endif %}{% if s.url_or_path %} {{ s.url_or_path }}{% endif %}{% if s.inputs_keys %} | inputs: {{ ", ".join(s.inputs_keys) }}{% endif %}
  {% endfor %}
  {% if n.props.get("calls_flows") %}
  - Calls other flows:
    {% for u in n.props.calls_flows %}
    - {{ u }}
    {% endfor %}
  {% endif %}
{% endfor %}

## Environments and deployment
- Clouds detected: {{ (clouds | join(", ")) if clouds else "n/a" }}
- CI systems detected: {{ (ci_systems | join(", ")) if ci_systems else "n/a" }}
- Environments referenced: {{ (environments | join(", ")) if environments else "n/a" }}
- Infra resources (sample):
{% for n in nodes if n.type == "InfraResource" %}
  - {{ n.props.get("resource_kind") or n.props.get("resource_type") }}: {{ n.name }}
{% endfor %}

## Data model highlights
- Tables:
{% for n in nodes if n.type == "Datastore" and n.props.get("table") %}
  - {{ n.props.get("table") }}
{% endfor %}

## Risks and gaps
- Links between clients and APIs not inferred (starter setup).
- Authentication/authorization may be incomplete.
- Schemas for events may be missing.
""")


ARTIFACTS_TPL = Template("""# Artifacts

{% for a in artifacts %}
## {{ a.artifact_type }}: {{ a.name }} ({{ a.repo_path }})

- Component/Service: {{ a.component_or_service }}
- Confidence: {{ a.confidence }}
- Capabilities: {{ ", ".join(a.capabilities) if a.capabilities else "n/a" }}

{% if a.artifact_type == "workflow_dag" %}
### Workflows
- Entry points:
{% if a.entry_points %}
{% for ep in a.entry_points %}
  - {{ ep.type }}: {{ ep.value }}
{% endfor %}
{% else %}
  - n/a
{% endif %}

- Steps overview:
{% if a.workflows %}
{% for wf in a.workflows %}
  - {{ wf.name or a.name }}: {{ wf.steps_summary or "(no steps parsed)" }}
{% endfor %}
{% else %}
  - n/a
{% endif %}

- Calls to other flows:
{% for c in a.interfaces.http_endpoints if c.summary and ("Calls another flow trigger" in c.summary) %}
  - {{ c.method }} {{ c.path }}
{% else %}
  - none detected
{% endfor %}

- Messaging connectors:
{% if a.interfaces.events %}
{% for ev in a.interfaces.events %}
  - {{ ev.broker }}{% if ev.direction %} ({{ ev.direction }}){% endif %}{% if ev.topic_or_channel %}: {{ ev.topic_or_channel }}{% endif %}
{% endfor %}
{% else %}
  - n/a
{% endif %}

- Data schemas:
{% if a.data.schemas %}
{% for s in a.data.schemas %}
  - {{ s.name }} ({{ s.format }})
{% endfor %}
{% else %}
  - n/a
{% endif %}

- Dependencies:
  - Datastores: {{ ", ".join(a.dependencies.datastores) if a.dependencies.datastores else "n/a" }}
  - External services: {{ ", ".join(a.dependencies.external_services) if a.dependencies.external_services else "n/a" }}
{% endif %}

### Interfaces
{% for ep in a.interfaces.http_endpoints %}
- HTTP {{ ep.method }} {{ ep.path }} (auth: {{ ep.auth }})
{% endfor %}
{% for ev in a.interfaces.events %}
- Event {{ ev.direction }} on {{ ev.topic_or_channel }} (broker: {{ ev.broker }})
{% endfor %}

### Data
- Tables: {{ ", ".join(a.data.tables_or_collections) if a.data.tables_or_collections else "n/a" }}

### Evidence
{{ (a.evidence[0].path if a.evidence else "") }}:{{ (a.evidence[0].lines if a.evidence else "") }}
                         
{% endfor %}
""")

def generate_docs(out_dir: Path, nodes: List[Node], edges: List[Edge], artifacts: List[Dict[str, Any]], facets: Dict[str, float], mkdocs_build: bool):
    docs_dir = out_dir / "docs"
    docs_dir.mkdir(parents=True, exist_ok=True)

    counts = {k: facets.get(k, 0) for k in ["apis", "ops", "events", "dbs", "infra", "docs"]}

    # Aggregate clouds, CI systems, and environments from artifacts
    clouds = sorted({
        (a.get("infrastructure", {}) or {}).get("cloud", "").strip()
        for a in artifacts
        if (a.get("infrastructure", {}) or {}).get("cloud")
    })
    ci_systems = sorted({
        ci.strip()
        for a in artifacts
        for ci in ((a.get("build_and_deploy", {}) or {}).get("ci") or [])
        if ci
    })
    environments = sorted({
        env.strip()
        for a in artifacts
        for env in ((a.get("build_and_deploy", {}) or {}).get("environments") or [])
        if env
    })


    (docs_dir / "index.md").write_text(
    INDEX_TPL.render(
        counts=counts, 
        facets=facets
    ), 
    encoding="utf-8"
)

    (docs_dir / "system.md").write_text(
    SYSTEM_TPL_NO_LLM.render(
        nodes=nodes,
        edges=edges,
        facets=facets,
        counts=counts,
        clouds=clouds,
        ci_systems=ci_systems,
        environments=environments
    ),
    encoding="utf-8"
)
    (docs_dir / "artifacts.md").write_text(
    ARTIFACTS_TPL.render(
        artifacts=artifacts
    ), 
    encoding="utf-8"
)

    mk = out_dir / "mkdocs.yml"
    mk.write_text("""site_name: AutoDocX
site_url: ""
theme:
  name: material
  features:
    - navigation.instant
    - content.code.copy
nav:
  - Home: docs/index.md
  - System: docs/system.md
  - Artifacts: docs/artifacts.md
""", encoding="utf-8")

    if mkdocs_build:
        try:
            import subprocess
            subprocess.run(["mkdocs", "build", "-f", str(mk), "-d", str(out_dir / "site")], check=True)
            rprint("[green]MkDocs site built at out/site[/green]")
        except Exception as e:
            rprint(f"[yellow]MkDocs build failed or mkdocs not installed: {e}[/yellow]")


# ---------- CLI ----------

def cmd_scan(args: argparse.Namespace):
    repo = Path(args.repo).resolve()
    out_dir = Path(args.out).resolve()
    out_dir.mkdir(parents=True, exist_ok=True)

    # Load .env from the repository root so OPENAI_API_KEY is available via os.getenv
    env_path = repo / ".env"
    if env_path.exists():
        load_dotenv(dotenv_path=str(env_path), override=False)

    rprint(f"[cyan]Scanning repo:[/cyan] {repo}")
    signals = run_all(repo)
    rprint(f"[green]Extracted signals:[/green] {len(signals)}")

    nodes, edges = build_graph(signals, repo)
    facets = compute_facets(nodes, edges)

    # Save graph
    (out_dir / "graph.json").write_text(json.dumps({
        "nodes": [asdict(n) for n in nodes],
        "edges": [asdict(e) for e in edges],
        "facets": facets,
        "generated_at": time.time()
    }, indent=2), encoding="utf-8")

    # Emit Option 1 artifacts
    artifacts_path = out_dir / "artifacts.jsonl"
    with artifacts_path.open("w", encoding="utf-8") as f:
        for s in signals:
            a = to_option1_artifact(s, repo)
            f.write(json.dumps(a) + "\n")
    rprint(f"[green]Wrote:[/green] {artifacts_path}")

    # Cache a compact artifacts.json for rendering convenience
    arts = [json.loads(line) for line in artifacts_path.read_text(encoding="utf-8").splitlines()]
    (out_dir / "artifacts.json").write_text(json.dumps(arts, indent=2), encoding="utf-8")

    if not args.no_render:
        generate_docs(out_dir, nodes, edges, arts, facets, mkdocs_build=args.mkdocs_build)

    # Optional LLM passes
    if args.llm and args.llm.lower() == "openai" and os.getenv("OPENAI_API_KEY") and not args.no_llm:
        # System synthesis
        systext = call_openai(args.model or "gpt-4o-mini", SYSTEM_SYNTH_PROMPT, {"artifacts_json": arts}, max_tokens=4000)
        if systext:
            (out_dir / "docs" / "system.md").write_text(systext, encoding="utf-8")
            rprint("[green]LLM system synthesis written to docs/system.md[/green]")
        else:
            rprint("[yellow]LLM system synthesis skipped (no output)[/yellow]")

def cmd_render(args: argparse.Namespace):
    out_dir = Path(args._in).resolve()
    data = json.loads((out_dir / "graph.json").read_text(encoding="utf-8"))
    nodes = [Node(**n) for n in data["nodes"]]
    edges = [Edge(**e) for e in data["edges"]]
    facets = data["facets"]
    arts = json.loads((out_dir / "artifacts.json").read_text(encoding="utf-8"))
    generate_docs(out_dir, nodes, edges, arts, facets, mkdocs_build=args.mkdocs_build)

def cmd_all(args: argparse.Namespace):
    args.no_render = False
    cmd_scan(args)

def main():
    p = argparse.ArgumentParser(description="AutoDocX - universal auto-documentation engine (hybrid Option 2 + Option 1).")
    sub = p.add_subparsers(dest="cmd")

    s1 = sub.add_parser("scan", help="Scan a repo; build graph; emit artifacts; generate docs.")
    s1.add_argument("repo", help="Path to repository root")
    s1.add_argument("--out", default="out", help="Output directory")
    s1.add_argument("--llm", default="", help="LLM provider: openai (optional)")
    s1.add_argument("--model", default="gpt-4o-mini", help="LLM model (OpenAI)")
    s1.add_argument("--no-llm", action="store_true", help="Disable LLM even if configured")
    s1.add_argument("--no-render", action="store_true", help="Skip docs render")
    s1.add_argument("--mkdocs-build", action="store_true", help="Run mkdocs build")
    s1.add_argument("--debug", action="store_true", help="Verbose plugin detection and discovery logs")
    s1.set_defaults(func=cmd_scan)

    s2 = sub.add_parser("render", help="Render docs from existing graph/artifacts.")
    s2.add_argument("--in", dest="_in", default="out", help="Input OUT directory (with graph.json, artifacts.json)")
    s2.add_argument("--mkdocs-build", action="store_true", help="Run mkdocs build")
    s2.add_argument("--debug", action="store_true", help="Verbose logs")
    s2.set_defaults(func=cmd_render)

    s3 = sub.add_parser("all", help="Scan + render + optional LLM + optional mkdocs build.")
    s3.add_argument("repo", help="Path to repository root")
    s3.add_argument("--out", default="out", help="Output directory")
    s3.add_argument("--llm", default="", help="LLM provider: openai (optional)")
    s3.add_argument("--model", default="gpt-4o-mini", help="LLM model (OpenAI)")
    s3.add_argument("--no-llm", action="store_true", help="Disable LLM even if configured")
    s3.add_argument("--mkdocs-build", action="store_true", help="Run mkdocs build")
    s3.add_argument("--debug", action="store_true", help="Verbose plugin detection and discovery logs")
    s3.set_defaults(func=cmd_all)

    args = p.parse_args()
    global DEBUG
    DEBUG
    if not args.cmd:
        p.print_help()
        sys.exit(1)
    args.func(args)


if __name__ == "__main__":
    main()