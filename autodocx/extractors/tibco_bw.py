# autodocx/extractors/tibco_bw.py
from __future__ import annotations
from pathlib import Path
from typing import Iterable, List, Dict, Any
import json
import re
import os
from lxml import etree

from autodocx.types import Signal

# try to import redact but tolerate it missing
try:
    from autodocx.utils.redaction import redact
except Exception:
    # fallback no-op redact if redaction module fails for any reason
    def redact(s: str) -> str:
        try:
            return str(s)
        except Exception:
            return "*****REDACTED*****"


# import the universal mapper
try:
    from autodocx.utils.roles import map_connectors_to_roles
except Exception:
    # fallback: no-op mapper
    def map_connectors_to_roles(connectors):
        return []


# Helpers -----------------------------------------------------------------

def _strip_tag(t: str) -> str:
    return t.split("}")[-1] if "}" in t else t

def _make_snippet(path: Path, lineno: int, context: int = 3) -> Dict[str, Any]:
    """
    Read file and return snippet around lineno.
    Returns dict {path, lines, snippet} with redact applied.
    """
    try:
        lines = path.read_text(encoding="utf-8", errors="ignore").splitlines()
        idx = max(0, lineno - 1)
        start = max(0, idx - context)
        end = min(len(lines), idx + context + 1)
        snippet_text = "\n".join(lines[start:end])
        snippet_text = redact(snippet_text)
        return {"path": str(path), "lines": f"{start+1}-{end}", "snippet": snippet_text}
    except Exception as e:
        return {"path": str(path), "lines": "", "snippet": f"(unreadable snippet: {e})"}

def _find_repo_root(path: Path) -> Path:
    """
    Heuristic: climb parents until you find a .git or a 'Workflows' folder (common in your repos).
    Fallback: current working directory.
    """
    p = path.resolve()
    for parent in [p] + list(p.parents):
        if (parent / ".git").exists():
            return parent
        if any((parent / child).is_dir() and child.lower() == "workflows" for child in parent.iterdir() if child.is_dir()):
            return parent
    # fallback: cwd
    return Path.cwd()

def _safe_name_for_file(s: str) -> str:
    return re.sub(r"[^A-Za-z0-9_.-]+", "_", s).strip("_")[:200]

# Main extractor ----------------------------------------------------------

class TibcoBWExtractor:
    name = "tibco_bw_lxml"
    patterns = ["**/*.bwp", "**/*.process", "**/*.xml"]

    def detect(self, repo: Path) -> bool:
        # quick check for likely process files
        return any(repo.glob("**/*.bwp")) or any(repo.glob("**/*.process")) or any(repo.glob("**/*.xml"))

    def discover(self, repo: Path) -> Iterable[Path]:
        """
        Yield BW-relevant process files. Be robust and explicit:
        - Always include *.bwp and *.process
        - Include *.xml only if scanning the head shows BW markers
        """
        seen = set()
    
        # Always take .bwp and .process
        for p in repo.rglob("*.bwp"):
            if p.is_file() and p not in seen:
                seen.add(p)
                yield p
        for p in repo.rglob("*.process"):
            if p.is_file() and p not in seen:
                seen.add(p)
                yield p
    
        # Heuristic include .xml only if head suggests BW content
        for p in repo.rglob("*.xml"):
            if p.is_file() and p not in seen:
                try:
                    head = p.read_text(encoding="utf-8", errors="ignore")[:8192].lower()
                    if any(k in head for k in ("http://www.tibco.com/bpel", "xmlns:bpws", "xmlns:tibex", "bw.model", "<process")):
                        seen.add(p)
                        yield p
                except Exception:
                    # If unreadable, skip to reduce noise
                    continue


    def extract(self, path: Path) -> Iterable[Signal]:
        signals: List[Signal] = []
        try:
            parser = etree.XMLParser(remove_comments=True, recover=True)
            tree = etree.parse(str(path), parser)
            root = tree.getroot()
        except Exception as e:
            signals.append(Signal(
                kind="doc",
                props={"name": path.stem, "file": str(path), "note": f"XML parse error: {e}"},
                evidence=[{"path": str(path), "lines": "1-1", "snippet": "(parse error)"}],
                subscores={"parsed": 0.1}
            ))
            return signals

        # Determine process name
        proc_name = root.get("name") or root.get("id") or path.stem

        if os.getenv("AUTODOCX_DEBUG_BW", "0") == "1":
            print(f"[tibco_bw] extracting: {path} as process '{proc_name}'")
        

        triggers: List[Dict[str, Any]] = []
        steps: List[Dict[str, Any]] = []
        calls: List[str] = []
        sqls: List[Dict[str, Any]] = []
        evidence_list: List[Dict[str, Any]] = []

        # Iterate all elements
        for el in tree.iter():
            tag = _strip_tag(el.tag).lower()
            lineno = el.sourceline or 0
            xpath = tree.getpath(el) if hasattr(tree, "getpath") else f"/{tag}"
            anchor = {"path": str(path), "lines": f"{lineno}-{lineno}", "snippet": _make_snippet(path, lineno)["snippet"]}

            # Record generic evidence
            evidence_list.append(anchor)

            # Activity-like detection
            name = el.get("name") or el.get("id") or _strip_tag(el.tag)
            # Record steps for many BW activity tags (best-effort)
            if any(k in tag for k in ("activity", "invoke", "callprocess", "jdbc", "mapper", "httpreceiver", "http")):
                step = {"name": name, "type": tag, "evidence": anchor}
                # Try to find connector detail for JDBC/HTTP
                # JDBC: look for child elements or attributes containing 'sql' or 'statement'
                sql_text = None
                sql_node = el.find(".//sqlStatement")
                if sql_node is not None and (sql_node.text or "").strip():
                    sql_text = (sql_node.text or "").strip()
                else:
                    for attr in ("statement", "sql"):
                        if el.get(attr):
                            sql_text = el.get(attr)
                            break
                if sql_text:
                    step["connector"] = "jdbc"
                    sqls.append({"sql": sql_text.strip(), "evidence": anchor})
                # HTTP receiver hints
                # common attribute names: endpointUri, endpointURI, path, apipath
                for attr in ("endpointUri", "endpointURI", "path", "apipath", "url"):
                    v = el.get(attr)
                    if v and isinstance(v, str) and v.strip():
                        step["connector"] = "http"
                        step["url_or_path"] = v.strip()
                        triggers.append({"name": proc_name, "type": "http", "path": v.strip(), "method": el.get("method") or "", "evidence": anchor})
                        break

                steps.append(step)

            # CallProcess detection: target process may be attribute or child element text
            if "callprocess" in tag or "call-process" in tag or "callprocessactivity" in tag:
                tgt = el.get("target") or el.get("process") or ""
                if not tgt:
                    # look for child with targetProcess or similar
                    for c in el.iterchildren():
                        if "process" in _strip_tag(c.tag).lower():
                            tgt = (c.text or "").strip()
                            if tgt:
                                break
                if tgt:
                    calls.append(tgt)
                else:
                    # fallback: try to find word "process" in snippet
                    sn = (el.text or "") or ""
                    m = re.search(r"(?i)processName\s*[:=]\s*['\"]?([A-Za-z0-9_.:-]+)['\"]?", sn)
                    if m:
                        calls.append(m.group(1))

            # JDBC generic detection (SQL inside)
            if "jdbc" in tag or "sql" in tag or el.find(".//sqlStatement") is not None:
                sql_text = None
                if el.find(".//sqlStatement") is not None:
                    sql_text = (el.find(".//sqlStatement").text or "").strip()
                if not sql_text:
                    for attr in ("statement", "sql", "query"):
                        v = el.get(attr)
                        if v and isinstance(v, str) and re.search(r"\b(select|insert|update|delete)\b", v, re.I):
                            sql_text = v
                            break
                if sql_text:
                    sql_item = {"sql": sql_text.strip(), "evidence": anchor}
                    sqls.append(sql_item)

        # dedupe calls
        calls = sorted(set(calls))

        # Build role tags using the universal mapper
        # Collect candidate strings (connectors, types, names) and map them once
        connectors_to_map = []
        for s in steps:
            if s.get("connector"):
                connectors_to_map.append(s.get("connector"))
            if s.get("type"):
                connectors_to_map.append(s.get("type"))
            if s.get("name"):
                connectors_to_map.append(s.get("name"))

        for t in triggers:
            connectors_to_map.append(t.get("type") or "")
            connectors_to_map.append(t.get("path") or "")

        
        # map_connectors_to_roles returns a list of role strings (deduped/sorted)
        roles = set(map_connectors_to_roles(connectors_to_map))
        
        subs = {
            "parsed": 0.9 if steps else 0.2,
            "schema_evidence": 0.4 if sqls else 0.1,
            "endpoint_or_op_coverage": 0.8 if triggers else 0.15
        }

        # Compose workflow Signal
        wf_props = {
            "name": proc_name,
            "file": str(path),
            "engine": "tibco_bw",
            "wf_kind": "tibco_bw",
            "triggers": triggers,
            "steps": steps,
            "calls_flows": calls,
            "sql_statements": sqls,
            "roles": sorted(list(roles)),
        }
        wf_props.update(self._augment_narrative_props(proc_name, triggers, steps, sqls, path))

        # evidence rollup: unique anchors (limit to N)
        unique_evidence = []
        seen = set()
        for e in evidence_list:
            k = (e.get("path"), e.get("lines"))
            if k not in seen:
                seen.add(k)
                unique_evidence.append(e)
            if len(unique_evidence) >= 12:
                break

        signals.append(Signal(
            kind="workflow",
            props=wf_props,
            evidence=unique_evidence,
            subscores=subs
        ))

        # Additional derived Signals: db and route
        for s in sqls:
            signals.append(Signal(
                kind="db",
                props={"table": "", "sql_sample": s.get("sql")[:200], "file": str(path)},
                evidence=[s.get("evidence")],
                subscores={"parsed": 0.6, "schema_evidence": 0.4}
            ))

        for t in triggers:
            if t.get("type") == "http":
                method = (t.get("method") or "").upper() or "POST"
                signals.append(Signal(
                    kind="route",
                    props={"service": proc_name, "method": method, "path": t.get("path") or "", "file": str(path)},
                    evidence=[t.get("evidence")],
                    subscores={"parsed": 0.9, "endpoint_or_op_coverage": 0.8}
                ))

        return signals

    # ---- narrative helpers -------------------------------------------------

    def _augment_narrative_props(
        self,
        proc_name: str,
        triggers: List[Dict[str, Any]],
        steps: List[Dict[str, Any]],
        sqls: List[Dict[str, Any]],
        path: Path,
    ) -> Dict[str, Any]:
        additions: Dict[str, Any] = {}
        user_story = self._infer_user_story(proc_name, triggers, steps)
        if user_story:
            additions["user_story"] = user_story
        inputs_example = self._infer_inputs_example(triggers, steps)
        if inputs_example:
            additions["inputs_example"] = inputs_example
        outputs_example = self._infer_outputs_example(steps)
        if outputs_example:
            additions["outputs_example"] = outputs_example
        ui_views = self._collect_ui_views(steps)
        if ui_views:
            additions["ui_views"] = ui_views
        screenshots = self._find_local_screenshots(path, proc_name)
        if screenshots:
            additions["screenshots"] = screenshots
        data_samples = self._summarize_sql_samples(sqls)
        if data_samples:
            additions["data_samples"] = data_samples
        return additions

    def _infer_user_story(self, proc_name: str, triggers: List[Dict[str, Any]], steps: List[Dict[str, Any]]) -> str:
        action = ""
        if triggers:
            trig = triggers[0]
            if trig.get("type") == "http":
                action = f"receives HTTP {trig.get('method') or 'requests'} at {trig.get('path')}"
            else:
                action = f"is triggered by {trig.get('type')}"
        if not action:
            action = "is triggered by upstream processes"
        primary_steps = []
        for step in steps[:4]:
            connector = step.get("connector") or step.get("type")
            if connector:
                primary_steps.append(f"{step.get('name') or connector} via {connector}")
            elif step.get("name"):
                primary_steps.append(step["name"])
        if not primary_steps:
            primary_steps.append("performs orchestration and data processing")
        return f"{proc_name} {action}, then {' -> '.join(primary_steps)}."

    def _infer_inputs_example(self, triggers: List[Dict[str, Any]], steps: List[Dict[str, Any]]) -> Dict[str, Any]:
        if triggers:
            trig = triggers[0]
            return {
                "source": trig.get("type") or "trigger",
                "details": trig.get("path") or trig.get("name"),
            }
        for step in steps:
            if step.get("connector") == "jdbc" and step.get("url_or_path"):
                return {"source": "jdbc", "details": step["url_or_path"]}
        return {}

    def _infer_outputs_example(self, steps: List[Dict[str, Any]]) -> Dict[str, Any]:
        responders = []
        for step in steps:
            connector = (step.get("connector") or "").lower()
            if "http" in connector or "response" in (step.get("name") or "").lower():
                responders.append(step.get("name") or connector)
        if responders:
            return {"responders": responders[:5]}
        return {}

    def _collect_ui_views(self, steps: List[Dict[str, Any]]) -> List[str]:
        views = []
        for step in steps:
            name = (step.get("name") or "").lower()
            stype = (step.get("type") or "").lower()
            if any(keyword in name for keyword in ("ui", "screen", "page")) or any(keyword in stype for keyword in ("ui", "html")):
                views.append(step.get("name") or step.get("type"))
        return views[:5]

    def _find_local_screenshots(self, path: Path, proc_name: str) -> List[str]:
        screenshots: List[str] = []
        search_root = path.parent
        slug = _safe_name_for_file(proc_name).lower()
        for candidate in search_root.glob("*.png"):
            if slug in candidate.stem.lower():
                screenshots.append(str(candidate))
        return screenshots[:3]

    def _summarize_sql_samples(self, sqls: List[Dict[str, Any]]) -> List[Dict[str, str]]:
        samples: List[Dict[str, str]] = []
        for sql in sqls[:3]:
            stmt = sql.get("sql") or ""
            preview = redact(stmt[:400])
            if preview:
                samples.append({"statement_preview": preview})
        return samples
