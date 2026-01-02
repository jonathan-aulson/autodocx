from __future__ import annotations

from pathlib import Path
from typing import Iterable, List, Dict, Any, Optional, Tuple, Set
import re

from autodocx.types import Signal

PB_EVENT_RE = re.compile(r"^\s*event\s+([a-zA-Z0-9_]+)", re.IGNORECASE | re.MULTILINE)
PB_FUNC_RE = re.compile(
    r"^\s*(public|protected)?\s*(function|subroutine)\s+[a-zA-Z0-9_]+\s+([a-zA-Z0-9_.]+)\s*\(",
    re.IGNORECASE | re.MULTILINE,
)
PB_CALL_RE = re.compile(r"([a-zA-Z_][a-zA-Z0-9_\.]+)\s*\.\s*([a-zA-Z_][a-zA-Z0-9_]*)\s*\(", re.IGNORECASE)
PB_HTTP_RE = re.compile(r'createobject\(\s*"(httpclient|restclient)"\s*\)|sendrequest\s*\(|setrequestheader', re.IGNORECASE)
PB_SOAP_RE = re.compile(r"(soapconnection|createsoapconnection)\s*|\.soap\w+\(", re.IGNORECASE)
PB_EXT_RE = re.compile(r"\bfunction\b.*\bexternal\b", re.IGNORECASE)
PB_SQL_HINT = re.compile(r"\b(select|insert|update|delete|merge|call)\b", re.IGNORECASE)
PB_DW_OP_RE = re.compile(
    r"\b(dw_[a-zA-Z0-9_]+)\.(retrieve|update|insertrow|deleterow|settrans|settransobject)",
    re.IGNORECASE,
)
IMG_REF_RE = re.compile(r'["\']([^"\']+\.(?:png|ico))["\']', re.IGNORECASE)
SQL_PATTERN = re.compile(r"(?is)\b(select|insert|update|delete)\b.+?;")
IDENTIFIER_SUFFIXES = ("id", "key", "code", "number", "guid", "token")

MAX_EVIDENCE = 8


class PowerBuilderExtractor:
    """Parses PowerBuilder artifacts (SRU/SRW/SRD/Ribbon XML/INI) to emit workflow signals with rich metadata."""

    name = "powerbuilder_code"
    patterns = ["**/*.sru", "**/*.srw", "**/*.srd", "**/*.xml", "**/*.ini", "**/*.json"]

    def detect(self, repo: Path) -> bool:
        return any(repo.glob("**/*.sru")) or any(repo.glob("**/*.srw")) or any(repo.glob("**/*.srd"))

    def discover(self, repo: Path) -> Iterable[Path]:
        seen = set()
        for pattern in self.patterns:
            for candidate in repo.glob(pattern):
                if candidate.is_file() and candidate not in seen:
                    seen.add(candidate)
                    yield candidate

    def extract(self, path: Path) -> Iterable[Signal]:
        suffix = path.suffix.lower()
        if suffix == ".srd":
            signal = self._extract_datawindow(path)
            return [signal] if signal else []
        if suffix in {".sru", ".srw"}:
            signal = self._extract_object(path)
            return [signal] if signal else []
        if suffix == ".xml":
            ribbon_signal = self._extract_ribbon(path)
            return [ribbon_signal] if ribbon_signal else []
        return []

    # ---------------- SRU/SRW -------------------------------------------------

    def _extract_object(self, path: Path) -> Optional[Signal]:
        text = self._read_text(path)
        if not text:
            return None
        object_type, object_name = self._pb_object_name(path)
        pbl_hint = path.parent.name.lower() if path.parent else "pb"
        process_name = f"{pbl_hint}.{object_name}"

        triggers: List[Dict[str, Any]] = []
        steps: List[Dict[str, Any]] = []
        relationships: List[Dict[str, Any]] = []
        datasource_tables: Set[str] = set()
        identifier_hints: Set[str] = set(self._identifier_tokens_from_text(text))
        process_calls: Set[str] = set()
        service_dependencies: Set[str] = set()

        events = self._find_matches(PB_EVENT_RE, text)
        for event_name, line, snippet in events:
            ev_name = f"{object_name}.{event_name}"
            evidence = self._make_evidence(path, line, snippet)
            triggers.append(
                {
                    "name": ev_name,
                    "type": "pb:event",
                    "method": "EVENT",
                    "path": event_name,
                    "evidence": evidence,
                }
            )
            steps.append(
                {
                    "name": ev_name,
                    "type": "pb:ui_event",
                    "connector": "pb:ui_event",
                    "friendly_display": f"PB Event {event_name}",
                    "role_hints": ["interface.receive"],
                    "evidence": evidence,
                }
            )

        functions = self._find_matches(PB_FUNC_RE, text)
        for func_name, line, snippet in functions:
            fname = f"{object_name}.{func_name}"
            evidence = self._make_evidence(path, line, snippet)
            steps.append(
                {
                    "name": fname,
                    "type": "pb:method_entry",
                    "connector": "pb:method_entry",
                    "friendly_display": f"PB Method {func_name}",
                    "role_hints": ["invoke.process"],
                    "evidence": evidence,
                }
            )

        sql_blocks = self._extract_sql_blocks(text, path)
        for idx, block in enumerate(sql_blocks, start=1):
            datasource_tables.add(block.get("table") or "")
            steps.append(
                {
                    "name": f"SQL_{idx}",
                    "type": "pb:db_exec",
                    "connector": "pb:db_exec",
                    "datasource_table": block.get("table"),
                    "datasource": block.get("datasource"),
                    "sql": block.get("sql"),
                    "friendly_display": block.get("friendly"),
                    "role_hints": ["data.jdbc"],
                    "evidence": block.get("evidence"),
                }
            )
            if block.get("table"):
                relationships.append(
                    self._relationship(
                        source=f"SQL_{idx}",
                        kind="sql",
                        target=block["table"],
                        operation="writes" if block.get("crud") == "write" else "reads",
                        evidence=block["evidence"],
                    )
                )

        for dw_name, op, line, snippet in self._find_dw_operations(text):
            evidence = self._make_evidence(path, line, snippet)
            step_name = f"{dw_name}.{op}"
            datasource_tables.add(dw_name)
            steps.append(
                {
                    "name": step_name,
                    "type": "pb:datawindow_op",
                    "connector": "pb:datawindow_op",
                    "datasource_table": dw_name,
                    "operation": op.lower(),
                    "friendly_display": f"{dw_name}.{op}",
                    "role_hints": ["data.jdbc"],
                    "evidence": evidence,
                }
            )

        if PB_HTTP_RE.search(text):
            evidence = self._make_evidence(path, self._first_line(text, PB_HTTP_RE), "HTTP client usage")
            url = self._first_url(text)
            service_dependencies.add(url or "httpclient")
            steps.append(
                {
                    "name": f"{object_name}.http_request",
                    "type": "pb:http_request",
                    "connector": "pb:http_request",
                    "url_or_path": url,
                    "friendly_display": f"HTTP call {url or ''}".strip(),
                    "role_hints": ["interface.receive", "interface.reply"],
                    "evidence": evidence,
                }
            )
            relationships.append(
                self._relationship(
                    source=f"{object_name}.http_request",
                    kind="http",
                    target=url or "httpclient",
                    operation="calls",
                    evidence=evidence,
                )
            )

        if PB_SOAP_RE.search(text):
            evidence = self._make_evidence(path, self._first_line(text, PB_SOAP_RE), "SOAP client usage")
            steps.append(
                {
                    "name": f"{object_name}.soap_call",
                    "type": "pb:soap_call",
                    "connector": "pb:soap_call",
                    "friendly_display": "SOAP call",
                    "role_hints": ["interface.receive", "interface.reply"],
                    "evidence": evidence,
                }
            )

        if PB_EXT_RE.search(text):
            evidence = self._make_evidence(path, self._first_line(text, PB_EXT_RE), "External function")
            steps.append(
                {
                    "name": f"{object_name}.external_function",
                    "type": "pb:external_function",
                    "connector": "pb:external_function",
                    "friendly_display": "External function",
                    "role_hints": ["invoke.process"],
                    "evidence": evidence,
                }
            )

        for call in self._extract_process_calls(text):
            process_calls.add(call)
            relationships.append(
                self._relationship(
                    source=f"{object_name}.method_call",
                    kind="workflow",
                    target=call,
                    operation="calls",
                    evidence=self._make_evidence(path, self._first_line(text, re.compile(re.escape(call))), call),
                )
            )

        image_refs = self._collect_image_refs(text)
        pb_meta = {
            "object_type": object_type,
            "pbl_hint": pbl_hint,
            "image_refs": image_refs,
        }

        evidence = self._gather_evidence(steps, sql_blocks)
        if not steps:
            return None

        props = {
            "name": process_name,
            "file": str(path),
            "engine": "powerbuilder",
            "wf_kind": "powerbuilder_object",
            "triggers": triggers,
            "steps": steps,
            "relationships": relationships,
            "datasource_tables": sorted(t for t in datasource_tables if t),
            "identifier_hints": sorted(identifier_hints),
            "process_calls": sorted(process_calls),
            "service_dependencies": sorted(service_dependencies),
            "pb_meta": pb_meta,
        }

        subscores = {
            "parsed": 0.85,
            "schema_evidence": 0.6 if datasource_tables else 0.2,
            "endpoint_or_op_coverage": 0.6 if service_dependencies else 0.2,
        }

        return Signal(
            kind="workflow",
            props=props,
            evidence=evidence,
            subscores=subscores,
        )

    # ---------------- DataWindow (.srd) --------------------------------------

    def _extract_datawindow(self, path: Path) -> Optional[Signal]:
        text = self._read_text(path)
        if not text:
            return None
        sql_blocks = self._extract_sql_blocks(text, path)
        if not sql_blocks:
            return None
        datasource_tables = {block.get("table") for block in sql_blocks if block.get("table")}
        steps = []
        relationships = []
        for idx, block in enumerate(sql_blocks, start=1):
            steps.append(
                {
                    "name": f"{path.stem}.sql_{idx}",
                    "type": "pb:datawindow_sql",
                    "connector": "pb:datawindow_sql",
                    "sql": block.get("sql"),
                    "datasource_table": block.get("table"),
                    "friendly_display": block.get("friendly"),
                    "role_hints": ["data.jdbc"],
                    "evidence": block.get("evidence"),
                }
            )
            if block.get("table"):
                relationships.append(
                    self._relationship(
                        source=f"{path.stem}.sql_{idx}",
                        kind="sql",
                        target=block["table"],
                        operation="reads" if block.get("crud") == "read" else "writes",
                        evidence=block["evidence"],
                    )
                )
        props = {
            "name": f"{path.parent.name.lower()}.{path.stem}",
            "file": str(path),
            "engine": "powerbuilder",
            "wf_kind": "powerbuilder_datawindow",
            "steps": steps,
            "relationships": relationships,
            "datasource_tables": sorted(t for t in datasource_tables if t),
            "identifier_hints": self._identifier_tokens_from_text(text),
            "pb_meta": {"datawindow": path.stem},
        }
        subscores = {
            "parsed": 0.8,
            "schema_evidence": 0.8,
            "endpoint_or_op_coverage": 0.2,
        }
        return Signal(
            kind="workflow",
            props=props,
            evidence=self._gather_evidence(steps, sql_blocks),
            subscores=subscores,
        )

    # ---------------- Ribbon XML ---------------------------------------------

    def _extract_ribbon(self, path: Path) -> Optional[Signal]:
        try:
            from lxml import etree
        except Exception:
            return None
        try:
            root = etree.parse(str(path)).getroot()
        except Exception:
            return None
        events = []
        icons = set()
        for el in root.xpath(".//*[@Clicked or @Selected]"):
            event = el.get("Clicked") or el.get("Selected")
            if not event:
                continue
            line = el.sourceline or 1
            snippet = etree.tostring(el, encoding="unicode")[:200]
            events.append((event, line, snippet))
            for attr in ("PictureName", "SmallIcon", "LargeIcon", "Icon"):
                val = el.get(attr)
                if val and (val.lower().endswith(".png") or val.lower().endswith(".ico")):
                    icons.add(val)
        if not events:
            return None
        pbl_hint = path.parent.name.lower() if path.parent else "ribbon"
        process_name = f"{pbl_hint}.{path.stem}"
        steps = []
        triggers = []
        for event_name, line, snippet in events:
            evidence = self._make_evidence(path, line, snippet)
            step_name = f"{path.stem}.{event_name}"
            triggers.append(
                {
                    "name": step_name,
                    "type": "pb:ribbon_event",
                    "method": "EVENT",
                    "path": event_name,
                    "evidence": evidence,
                }
            )
            steps.append(
                {
                    "name": step_name,
                    "type": "pb:ribbon_event",
                    "connector": "pb:ribbon_event",
                    "friendly_display": event_name,
                    "role_hints": ["interface.receive"],
                    "evidence": evidence,
                }
            )
        props = {
            "name": process_name,
            "file": str(path),
            "engine": "powerbuilder",
            "wf_kind": "powerbuilder_ribbon",
            "triggers": triggers,
            "steps": steps,
            "pb_meta": {"ribbon": True, "image_refs": sorted(icons)},
        }
        return Signal(
            kind="workflow",
            props=props,
            evidence=[step["evidence"] for step in steps[:MAX_EVIDENCE]],
            subscores={"parsed": 0.6},
        )

    # ---------------- Helpers -------------------------------------------------

    def _read_text(self, path: Path) -> str:
        try:
            return path.read_text(encoding="utf-8", errors="ignore")
        except Exception:
            return ""

    def _pb_object_name(self, path: Path) -> Tuple[str, str]:
        stem = path.stem.lower()
        if path.suffix.lower() == ".srw" or stem.startswith("w_"):
            return "Window", path.stem
        if stem.startswith("n_") or path.suffix.lower() == ".sru":
            return "NVO", path.stem
        if stem.startswith("m_"):
            return "Menu", path.stem
        return "Object", path.stem

    def _find_matches(self, pattern: re.Pattern[str], text: str) -> List[Tuple[str, int, str]]:
        matches: List[Tuple[str, int, str]] = []
        for match in pattern.finditer(text):
            name = match.group(match.lastindex or 1)
            line = text[: match.start()].count("\n") + 1
            snippet = match.group(0).strip()
            matches.append((name, line, snippet))
        return matches

    def _extract_sql_blocks(self, text: str, path: Path) -> List[Dict[str, Any]]:
        blocks: List[Dict[str, Any]] = []
        for match in SQL_PATTERN.finditer(text):
            sql = match.group(0).strip()
            verb = match.group(1).upper()
            table = self._primary_table(sql)
            line = text[: match.start()].count("\n") + 1
            evidence = self._make_evidence(path, line, sql[:200])
            blocks.append(
                {
                    "sql": sql,
                    "verb": verb,
                    "table": table,
                    "columns": self._identifier_tokens_from_text(sql),
                    "crud": "write" if verb in {"INSERT", "UPDATE", "DELETE"} else "read",
                    "friendly": f"{verb.title()} {table or 'SQL'}",
                    "evidence": evidence,
                }
            )
        return blocks

    def _primary_table(self, sql: str) -> Optional[str]:
        lowered = sql.lower()
        for keyword in ("from", "into", "update"):
            match = re.search(rf"\b{keyword}\s+([A-Za-z0-9_.\[\]\"`]+)", lowered)
            if match:
                token = match.group(1)
                return token.strip("[]\"`")
        return None

    def _extract_process_calls(self, text: str) -> List[str]:
        calls = []
        for match in PB_CALL_RE.finditer(text):
            qualifier, method = match.groups()
            calls.append(f"{qualifier}.{method}")
        return sorted(set(calls))

    def _find_dw_operations(self, text: str) -> List[Tuple[str, str, int, str]]:
        ops: List[Tuple[str, str, int, str]] = []
        for match in PB_DW_OP_RE.finditer(text):
            name, op = match.groups()
            line = text[: match.start()].count("\n") + 1
            snippet = match.group(0)
            ops.append((name, op, line, snippet))
        return ops

    def _identifier_tokens_from_text(self, text: str) -> List[str]:
        tokens = set()
        for token in re.findall(r"[A-Za-z0-9_]+", text or ""):
            lower = token.lower()
            if any(lower.endswith(suffix) for suffix in IDENTIFIER_SUFFIXES):
                tokens.add(token)
        return sorted(tokens)

    def _collect_image_refs(self, text: str) -> List[str]:
        refs = set()
        for match in IMG_REF_RE.findall(text or ""):
            refs.add(match.strip())
        for _, value in re.findall(r"(?i)\b(PictureName|Icon|SmallIcon|LargeIcon|Picture)\s*=\s*['\"]([^'\"]+)", text or ""):
            refs.add(value.strip())
        return sorted(refs)

    def _make_evidence(self, path: Path, line: int, snippet: str) -> Dict[str, Any]:
        return {"path": str(path), "lines": f"{line}-{line}", "snippet": snippet.strip()[:200]}

    def _relationship(self, source: str, kind: str, target: str, operation: str, evidence: Dict[str, Any]) -> Dict[str, Any]:
        return {
            "id": f"{kind}_{abs(hash((source, target))) % 10**8}",
            "source": {"type": "activity", "name": source},
            "target": {"kind": kind, "ref": target, "display": target},
            "operation": {"type": operation, "crud": "execute" if operation == "calls" else operation, "protocol": kind},
            "connector": kind,
            "direction": "outbound",
            "context": {},
            "roles": [],
            "evidence": [evidence],
        }

    def _gather_evidence(self, steps: List[Dict[str, Any]], sql_blocks: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
        evidence: List[Dict[str, Any]] = []
        for step in steps:
            ev = step.get("evidence")
            if ev and ev not in evidence:
                evidence.append(ev)
            if len(evidence) >= MAX_EVIDENCE:
                break
        if len(evidence) < MAX_EVIDENCE:
            for block in sql_blocks:
                ev = block.get("evidence")
                if ev and ev not in evidence:
                    evidence.append(ev)
                if len(evidence) >= MAX_EVIDENCE:
                    break
        return evidence

    def _first_line(self, text: str, pattern: re.Pattern[str]) -> int:
        match = pattern.search(text)
        if not match:
            return 1
        return text[: match.start()].count("\n") + 1

    def _first_url(self, text: str) -> Optional[str]:
        match = re.search(r"https?://[^\s\"']{6,120}", text)
        return match.group(0) if match else None
