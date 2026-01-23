from __future__ import annotations

import json
from pathlib import Path
from typing import Iterable, List, Dict, Any
import xml.etree.ElementTree as ET

from autodocx.types import Signal


class BwResourceBindingExtractor:
    name = "bw_resource_binding"
    patterns = [
        "**/*.httpConnResource",
        "**/*.httpClientResource",
        "**/*.jdbcResource",
        "**/*.jmsResource",
    ]

    def detect(self, repo: Path) -> bool:
        repo = Path(repo)
        return any(repo.glob(p) for p in self.patterns)

    def discover(self, repo: Path) -> Iterable[Path]:
        repo = Path(repo)
        for pattern in self.patterns:
            yield from repo.glob(pattern)

    def extract(self, path: Path) -> Iterable[Signal]:
        path = Path(path)
        text = path.read_text(encoding="utf-8", errors="ignore")
        kind = self._infer_kind(path)
        parsed = self._parse_resource(text)

        props: Dict[str, Any] = {
            "name": parsed.get("name") or path.stem,
            "file": str(path),
            "resource_type": kind,
            "resource_kind": kind,
        }
        if parsed.get("endpoint"):
            props.setdefault("endpoint", parsed["endpoint"])
        if parsed.get("method"):
            props.setdefault("method", parsed["method"])
        if parsed.get("datastore"):
            props.setdefault("datastore", parsed["datastore"])
            props.setdefault("datasource_tables", [parsed["datastore"]])
        if parsed.get("queue"):
            props.setdefault("queue", parsed["queue"])
        if parsed.get("host"):
            props.setdefault("host", parsed["host"])
        if parsed.get("port"):
            props.setdefault("port", parsed["port"])

        evidence = [f"{path}:1-1"]
        # Enrichment hints for scaffold/interdeps
        services = []
        if parsed.get("endpoint"):
            services.append(
                {
                    "kind": parsed.get("kind") or kind,
                    "connector": kind,
                    "endpoint": parsed.get("endpoint"),
                    "method": parsed.get("method"),
                    "operation": None,
                    "evidence": evidence[0],
                }
            )
        props["enrichment"] = {
            "bw_services": services,
            "datastores": [parsed["datastore"]] if parsed.get("datastore") else [],
        }
        yield Signal(kind="resource", props=props, evidence=evidence, subscores={"parsed": 1.0})

    def _infer_kind(self, path: Path) -> str:
        suffix = path.suffix.lower()
        if "jdbc" in suffix:
            return "jdbc"
        if "jms" in suffix:
            return "jms"
        if "client" in suffix:
            return "http_client"
        return "http"

    def _parse_resource(self, text: str) -> Dict[str, Any]:
        try:
            root = ET.fromstring(text)
            return self._parse_xml(root)
        except Exception:
            return self._parse_fallback(text)

    def _parse_xml(self, root: ET.Element) -> Dict[str, Any]:
        out: Dict[str, Any] = {}
        # Common HTTP structures
        url = root.attrib.get("url") or root.attrib.get("baseURI") or root.attrib.get("basePath")
        if url:
            out["endpoint"] = url
        method = root.attrib.get("method") or root.attrib.get("httpMethod")
        if method:
            out["method"] = method
        if not out.get("endpoint"):
            host = root.attrib.get("host")
            port = root.attrib.get("port")
            if host:
                out["host"] = host
                out["port"] = port or ""
                out["endpoint"] = f"{host}:{port}" if port else host

        # JDBC/JMS hints
        ds = root.attrib.get("datasource") or root.attrib.get("databaseName") or root.attrib.get("schema")
        if ds:
            out["datastore"] = ds
        queue = root.attrib.get("queue") or root.attrib.get("destination") or root.attrib.get("topic")
        if queue:
            out["queue"] = queue

        # Dive children for url/method/datastore
        for child in root.iter():
            tag = child.tag.lower()
            if "http" in tag and not out.get("endpoint"):
                if child.attrib.get("path"):
                    out["endpoint"] = child.attrib["path"]
            if "operation" in tag and not out.get("method"):
                m = child.attrib.get("httpmethod") or child.attrib.get("method")
                if m:
                    out["method"] = m
            if "datasource" in tag or "database" in tag:
                ds = child.attrib.get("name") or child.text
                if ds:
                    out["datastore"] = ds.strip()
            if "queue" in tag or "destination" in tag:
                q = child.attrib.get("name") or child.text
                if q:
                    out["queue"] = q.strip()
        return out

    def _parse_fallback(self, text: str) -> Dict[str, Any]:
        out: Dict[str, Any] = {}
        lines = [ln.strip() for ln in text.splitlines() if ln.strip()]
        for ln in lines:
            low = ln.lower()
            if "http" in low and "://" in ln and not out.get("endpoint"):
                out["endpoint"] = ln.strip()
            if "method" in low and ":" in ln and not out.get("method"):
                _, _, val = ln.partition(":")
                out["method"] = val.strip().upper()
            if "datasource" in low or "database" in low or "schema" in low:
                _, _, val = ln.partition("=")
                out["datastore"] = val.strip()
            if "queue" in low or "destination" in low:
                _, _, val = ln.partition("=")
                out["queue"] = val.strip()
        return out


class BwSubstitutionVarExtractor:
    name = "bw_substitution_vars"
    patterns = ["**/*.substvar"]

    def detect(self, repo: Path) -> bool:
        repo = Path(repo)
        return any(repo.glob("**/*.substvar"))

    def discover(self, repo: Path) -> Iterable[Path]:
        repo = Path(repo)
        yield from repo.glob("**/*.substvar")

    def extract(self, path: Path) -> Iterable[Signal]:
        path = Path(path)
        text = path.read_text(encoding="utf-8", errors="ignore")
        vars_map = self._parse_substvars(text)
        props = {
            "name": path.name,
            "file": str(path),
            "variables": vars_map,
            "kind": "substvar",
        }
        evidence = [f"{path}:1-1"]
        yield Signal(kind="config", props=props, evidence=evidence, subscores={"parsed": 1.0})

    def _parse_substvars(self, text: str) -> Dict[str, str]:
        vars_map: Dict[str, str] = {}
        for line in text.splitlines():
            line = line.strip()
            if not line or line.startswith("#") or "=" not in line:
                continue
            key, _, val = line.partition("=")
            vars_map[key.strip()] = val.strip()
        return vars_map
