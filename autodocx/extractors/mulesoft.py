from __future__ import annotations

from pathlib import Path
from typing import Iterable, List, Dict, Any, Optional, Set

from lxml import etree

from autodocx.types import Signal

MULE_INDICATORS = ("<mule", "xmlns:mule", "xmlns:http", "xmlns:db")
HTTP_METHODS = {"GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS", "HEAD"}


def _read_head(path: Path, limit: int = 4096) -> str:
    try:
        return path.read_text(encoding="utf-8", errors="ignore")[:limit].lower()
    except Exception:
        return ""


def _snippet(path: Path, lineno: int, context: int = 2) -> Dict[str, Any]:
    try:
        lines = path.read_text(encoding="utf-8", errors="ignore").splitlines()
    except Exception:
        return {"path": str(path), "lines": f"{lineno}-{lineno}", "snippet": ""}
    start = max(lineno - context - 1, 0)
    end = min(len(lines), lineno + context)
    snippet = "\n".join(lines[start:end])
    return {"path": str(path), "lines": f"{start+1}-{end}", "snippet": snippet}


class MuleSoftExtractor:
    """Parses MuleSoft XML flows for triggers, steps, and dependencies."""

    name = "mulesoft_flows"
    patterns = ["**/*.xml", "**/*.mule"]

    def detect(self, repo: Path) -> bool:
        for idx, candidate in enumerate(repo.rglob("*.xml")):
            if idx > 100:
                break
            if self._looks_like_mule(candidate):
                return True
        return False

    def discover(self, repo: Path) -> Iterable[Path]:
        seen = set()
        for pattern in self.patterns:
            for candidate in repo.glob(pattern):
                if candidate.is_file() and candidate not in seen and self._looks_like_mule(candidate):
                    seen.add(candidate)
                    yield candidate

    def extract(self, path: Path) -> Iterable[Signal]:
        try:
            parser = etree.XMLParser(remove_comments=True, recover=True)
            tree = etree.parse(str(path), parser)
            root = tree.getroot()
        except Exception as exc:
            return [
                Signal(
                    kind="doc",
                    props={"name": path.stem, "file": str(path), "note": f"Mule XML parse error: {exc}"},
                    evidence=[{"path": str(path), "lines": "1-1", "snippet": ""}],
                    subscores={"parsed": 0.1},
                )
            ]

        if "mule" not in (etree.QName(root.tag).localname.lower()):
            return []

        signals: List[Signal] = []
        flows = root.xpath(".//*[local-name()='flow' or local-name()='sub-flow']")
        nsmap = {k: v for k, v in (root.nsmap or {}).items() if k}
        for flow in flows:
            name = flow.get("name") or path.stem
            triggers: List[Dict[str, Any]] = []
            steps: List[Dict[str, Any]] = []
            relationships: List[Dict[str, Any]] = []
            datastores: Set[str] = set()
            services: Set[str] = set()
            process_calls: Set[str] = set()
            identifier_hints: Set[str] = set()
            evidence: List[Dict[str, Any]] = []

            for elem in flow.iterchildren():
                tag = etree.QName(elem.tag)
                local = tag.localname.lower()
                prefix = elem.prefix or tag.namespace or ""
                connector = f"{elem.prefix}:{local}" if elem.prefix else local
                lineno = elem.sourceline or 1
                evidence.append(_snippet(path, lineno))
                step: Dict[str, Any] = {"name": elem.get("doc:name") or elem.get("name") or local, "connector": connector, "evidence": evidence[-1]}

                if "listener" in local and "http" in (tag.namespace or ""):
                    method_attr = elem.get("allowedMethods") or elem.get("methods") or "GET"
                    method = method_attr.split(",")[0].strip().upper()
                    path_attr = elem.get("path") or elem.get("config-ref")
                    triggers.append({"name": step["name"], "type": "http", "method": method if method in HTTP_METHODS else "GET", "path": path_attr, "evidence": evidence[-1]})
                elif local in {"scheduler", "schedule"}:
                    triggers.append({"name": step["name"], "type": "timer", "method": "SCHEDULE", "path": elem.get("cron-expression") or elem.get("frequency"), "evidence": evidence[-1]})
                elif "request" in local and "http" in (tag.namespace or ""):
                    url = elem.get("url") or elem.get("path") or elem.get("config-ref")
                    services.add(url or step["name"])
                    relationships.append(
                        {
                            "id": f"mulesoft_http_{len(relationships)}",
                            "source": {"type": "activity", "name": step["name"]},
                            "target": {"kind": "http", "ref": url, "display": url},
                            "operation": {"type": "calls", "crud": "execute", "protocol": "http"},
                            "connector": connector,
                            "direction": "outbound",
                            "context": {"url": url},
                            "roles": ["interface.calls"],
                            "evidence": [evidence[-1]],
                        }
                    )
                elif prefix and prefix.lower().startswith("db"):
                    table = elem.get("table") or self._extract_child_text(elem, ".//db:sql", namespaces=nsmap)
                    if table:
                        table = self._infer_table_name(table)
                    if table:
                        datastores.add(table)
                        step["datasource_table"] = table
                    relationships.append(
                        {
                            "id": f"mulesoft_db_{len(relationships)}",
                            "source": {"type": "activity", "name": step["name"]},
                            "target": {"kind": "sql", "ref": table or "database", "display": table or "database"},
                            "operation": {"type": "writes" if "insert" in local else "reads", "crud": "update" if "insert" in local else "read", "protocol": "sql"},
                            "connector": connector,
                            "direction": "outbound",
                            "context": {"table": table},
                            "roles": ["data.jdbc"],
                            "evidence": [evidence[-1]],
                        }
                    )
                elif local == "flow-ref":
                    target_flow = elem.get("name")
                    if target_flow:
                        process_calls.add(target_flow)
                        relationships.append(
                            {
                                "id": f"mulesoft_flowref_{len(relationships)}",
                                "source": {"type": "activity", "name": step["name"]},
                                "target": {"kind": "workflow", "ref": target_flow, "display": target_flow},
                                "operation": {"type": "calls", "crud": "execute", "protocol": "flow"},
                                "connector": connector,
                                "direction": "outbound",
                                "context": {},
                                "roles": ["invoke.process"],
                                "evidence": [evidence[-1]],
                            }
                        )
                if elem.text:
                    identifier_hints.update(self._identifier_tokens(elem.text))
                for attr_val in elem.attrib.values():
                    identifier_hints.update(self._identifier_tokens(attr_val))
                steps.append(step)

            if not steps and not triggers:
                continue

            props = {
                "name": name,
                "file": str(path),
                "engine": "mulesoft",
                "wf_kind": "mulesoft_flow",
                "triggers": triggers,
                "steps": steps,
                "relationships": relationships,
                "datasource_tables": sorted(datastores),
                "service_dependencies": sorted(filter(None, services)),
                "process_calls": sorted(process_calls),
                "identifier_hints": sorted(identifier_hints),
            }

            signals.append(
                Signal(
                    kind="workflow",
                    props=props,
                    evidence=evidence[:10],
                    subscores={"parsed": 0.9 if steps else 0.4},
                )
            )
        return signals

    def _looks_like_mule(self, path: Path) -> bool:
        head = _read_head(path)
        return any(indicator in head for indicator in MULE_INDICATORS)

    def _identifier_tokens(self, value: Optional[str]) -> Set[str]:
        tokens: Set[str] = set()
        if not value:
            return tokens
        for token in value.replace("#", " ").replace("/", " ").split():
            cleaned = token.strip("{}[]()\"'")
            if cleaned and cleaned.lower().endswith("id") and len(cleaned) <= 64:
                tokens.add(cleaned)
        return tokens

    def _extract_child_text(self, elem: etree._Element, xpath: str, namespaces: Dict[str, Any]) -> Optional[str]:
        try:
            match = elem.xpath(xpath, namespaces=namespaces)
        except Exception:
            return None
        if not match:
            return None
        node = match[0]
        if isinstance(node, etree._Element):
            text = (node.text or "").strip()
        else:
            text = str(node).strip()
        return text or None

    def _infer_table_name(self, sql_text: str) -> Optional[str]:
        if not sql_text:
            return None
        sql_clean = " ".join(sql_text.split())
        lowered = sql_clean.lower()
        for keyword in ("from", "into", "update"):
            if keyword in lowered:
                idx = lowered.find(keyword)
                start = idx + len(keyword)
                remainder_raw = sql_clean[start:].lstrip()
                remainder = lowered[start:].lstrip()
                if remainder:
                    token = remainder_raw.split()[0].strip(",;")
                    if token and token[0].isalnum():
                        return token
        return sql_clean if " " not in sql_clean else None
