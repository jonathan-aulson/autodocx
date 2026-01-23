from __future__ import annotations

import json
import time
from collections import defaultdict
from datetime import datetime, timezone
from pathlib import Path
import os
import subprocess
import shutil
from typing import Any, Dict, Iterable, List, Sequence, Tuple


def _now_iso() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _rel_path(path: Path, base: Path) -> str:
    try:
        return path.relative_to(base).as_posix()
    except ValueError:
        return path.as_posix()


def _rule_missing_logging(sir_obj: Dict[str, Any]) -> Iterable[Dict[str, Any]]:
    props = sir_obj.get("props") or {}
    steps = props.get("steps") or []
    logging = props.get("logging") or (sir_obj.get("business_scaffold") or {}).get("logging")
    if steps and not logging:
        yield {
            "rule_id": "missing_logging",
            "severity": "medium",
            "message": f"{sir_obj.get('name','signal')} has {len(steps)} steps but no logging metadata.",
        }


def _rule_insecure_http(sir_obj: Dict[str, Any]) -> Iterable[Dict[str, Any]]:
    props = sir_obj.get("props") or {}
    resources = list(props.get("steps") or []) + list(props.get("triggers") or [])
    findings: List[Dict[str, Any]] = []
    for res in resources:
        url = (
            res.get("url")
            or res.get("endpoint")
            or res.get("path")
            or (res.get("inputs") or {}).get("url")
        )
        if isinstance(url, str) and url.strip().lower().startswith("http://"):
            findings.append(
                {
                    "rule_id": "insecure_http",
                    "severity": "high",
                    "message": f"Insecure HTTP endpoint detected: {url}",
                }
            )
    return findings


def _run_rules(sir_obj: Dict[str, Any]) -> List[Dict[str, Any]]:
    findings: List[Dict[str, Any]] = []
    for rule in (_rule_missing_logging, _rule_insecure_http):
        findings.extend(rule(sir_obj))
    return findings


def _map_constellations(constellations: Sequence[Dict[str, Any]]) -> Dict[str, set[str]]:
    mapping: Dict[str, set[str]] = defaultdict(set)
    for record in constellations:
        cid = record.get("id")
        if not cid:
            continue
        for sir_file in record.get("sir_files", []):
            mapping[sir_file].add(cid)
    return mapping


def _run_semgrep_scan(repo_root: Path) -> List[Dict[str, Any]]:
    config = os.getenv("AUTODOCX_SEMGREP_CONFIG")
    if not config:
        return []
    semgrep_bin = shutil.which("semgrep")
    if not semgrep_bin:
        return []
    cmd = [semgrep_bin, "--config", config, "--json", "--quiet"]
    try:
        proc = subprocess.run(
            cmd,
            cwd=str(repo_root),
            capture_output=True,
            text=True,
            check=False,
        )
    except Exception:
        return []
    if proc.returncode not in (0, 1):
        return []
    try:
        data = json.loads(proc.stdout or "{}")
    except Exception:
        return []
    findings: List[Dict[str, Any]] = []
    for result in data.get("results") or []:
        path = result.get("path")
        start = ((result.get("start") or {}).get("line")) or 1
        check_id = result.get("check_id")
        severity = (result.get("extra") or {}).get("severity") or "info"
        message = (result.get("extra") or {}).get("message") or ""
        findings.append(
            {
                "rule_id": f"semgrep:{check_id}",
                "severity": severity.lower(),
                "message": message,
                "sir_id": None,
                "sir_name": None,
                "component": None,
                "sir_file": path,
                "evidence": [f"{path}:{start}"],
            }
        )
    return findings


def run_anti_pattern_scans(
    out_base: Path,
    repo_root: Path,
    constellations: Sequence[Dict[str, Any]],
    sir_records: Sequence[Tuple[Dict[str, Any], Path]],
) -> Tuple[Dict[str, List[Dict[str, Any]]], str | None]:
    out_base = Path(out_base)
    repo_root = Path(repo_root)
    quality_dir = out_base / "reports" / "quality"
    quality_dir.mkdir(parents=True, exist_ok=True)

    findings: List[Dict[str, Any]] = []
    for sir_obj, sir_path in sir_records:
        rel = _rel_path(sir_path, out_base)
        for finding in _run_rules(sir_obj):
            record = dict(finding)
            record["sir_id"] = sir_obj.get("id")
            record["sir_name"] = sir_obj.get("name")
            record["component"] = sir_obj.get("component_or_service")
            record["sir_file"] = rel
            record["evidence"] = sir_obj.get("evidence") or []
            findings.append(record)

    findings.extend(_run_semgrep_scan(repo_root))

    constellation_map = _map_constellations(constellations)
    by_constellation: Dict[str, List[Dict[str, Any]]] = defaultdict(list)
    for finding in findings:
        rel = finding["sir_file"]
        const_ids = sorted(constellation_map.get(rel, []))
        finding["constellations"] = const_ids
        for cid in const_ids:
            by_constellation[cid].append(finding)

    manifest = {
        "generated_at": _now_iso(),
        "total_findings": len(findings),
        "findings": findings,
        "by_constellation": {cid: entries for cid, entries in by_constellation.items()},
    }
    manifest_path = quality_dir / "anti_patterns.json"
    manifest_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    rel_manifest = manifest_path.relative_to(out_base).as_posix()
    return by_constellation, rel_manifest
