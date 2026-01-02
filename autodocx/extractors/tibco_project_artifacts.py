from __future__ import annotations
from pathlib import Path
from typing import Iterable, List, Dict, Any
import json
import xml.etree.ElementTree as ET

from autodocx.types import Signal

class TibcoProjectArtifactsExtractor:
    """
    Scans for project-level WSDL, XSD, and service-descriptor-ish JSON files (best-effort).
    Emits Signals:
      - kind="api" for OpenAPI-like JSON (if 'openapi'/'swagger' present),
      - kind="doc" for any WSDL/XSD brief summary,
      - kind="data_contract" for XSD element stubs (as doc signals).
    This complements OpenAPIExtractor; it targets TIBCO-specific folders (Service Descriptors, Schemas).
    """
    name = "tibco_project_artifacts"
    patterns = ["**/*.wsdl", "**/*.xsd", "**/Service Descriptors/*.json", "**/Service Descriptors/*.xml", "**/Service Descriptors/*.wsdl", "**/*.json"]

    def detect(self, repo: Path) -> bool:
        # Honest best-effort: return True if any likely file exists.
        return any(repo.glob("**/*.wsdl")) or any(repo.glob("**/*.xsd")) or any(repo.glob("**/Service Descriptors/*.json"))

    def discover(self, repo: Path) -> Iterable[Path]:
        seen = set()
        for pat in self.patterns:
            for p in repo.glob(pat):
                if p.is_file() and p not in seen:
                    seen.add(p)
                    # content sniff for JSON: include if it has openapi/swagger or it's in Service Descriptors
                    yield p

    def extract(self, path: Path) -> Iterable[Signal]:
        signals: List[Signal] = []
        try:
            suffix = path.suffix.lower()
            text = path.read_text(encoding="utf-8", errors="ignore")
            if suffix == ".json":
                try:
                    obj = json.loads(text)
                except Exception:
                    obj = None
                if isinstance(obj, dict):
                    # OpenAPI / Swagger
                    if "openapi" in obj or "swagger" in obj or "paths" in obj:
                        title = (obj.get("info") or {}).get("title") or path.stem
                        version = (obj.get("info") or {}).get("version") or ""
                        signals.append(Signal(
                            kind="api",
                            props={"name": title, "version": version, "servers": [], "file": str(path)},
                            evidence=[f"{path}:1-40"],
                            subscores={"parsed": 1.0, "schema_evidence": 1.0}
                        ))
                    else:
                        # generic service descriptor JSON (TIBCO may store descriptors this way)
                        svc_sig = self._parse_service_descriptor(obj, path)
                        if svc_sig:
                            signals.append(svc_sig)
                        else:
                            signals.append(Signal(
                                kind="doc",
                                props={"name": path.stem, "file": str(path), "note": "Service descriptor JSON (not OpenAPI)"},
                                evidence=[f"{path}:1-40"],
                                subscores={"parsed": 0.5}
                            ))
                else:
                    signals.append(Signal(
                        kind="doc",
                        props={"name": path.stem, "file": str(path), "note": "JSON not parsed"},
                        evidence=[f"{path}:1-1"],
                        subscores={"parsed": 0.1}
                    ))

            elif suffix in {".wsdl", ".xml", ".xsd"}:
                try:
                    root = ET.fromstring(text)
                except Exception:
                    # fall back to doc signal
                    signals.append(Signal(
                        kind="doc",
                        props={"name": path.stem, "file": str(path), "note": "XML parse error or not a WSDL/XSD"},
                        evidence=[f"{path}:1-1"],
                        subscores={"parsed": 0.1}
                    ))
                    return signals

                tag = root.tag.lower()
                if tag.endswith("definitions") or "wsdl" in tag:
                    # WSDL: extract portType/operation names best-effort
                    ops = []
                    for op in root.findall(".//{*}operation"):
                        name = op.attrib.get("name")
                        if name:
                            ops.append(name)
                    signals.append(Signal(
                        kind="doc",
                        props={"name": path.stem, "file": str(path), "wsdl_operations_count": len(ops), "operations_sample": ops[:10]},
                        evidence=[f"{path}:1-1"],
                        subscores={"parsed": 0.8}
                    ))
                elif tag.endswith("schema") or tag.endswith("xsd"):
                    # XSD: list top-level element names
                    elems = [el.attrib.get("name") for el in root.findall(".//{*}element") if el.attrib.get("name")]
                    signals.append(Signal(
                        kind="doc",
                        props={"name": path.stem, "file": str(path), "xsd_elements_count": len(elems), "elements_sample": elems[:10]},
                        evidence=[f"{path}:1-1"],
                        subscores={"parsed": 0.7}
                    ))
                else:
                    signals.append(Signal(
                        kind="doc",
                        props={"name": path.stem, "file": str(path), "note": "XML parsed (unknown schema)"},
                        evidence=[f"{path}:1-1"],
                        subscores={"parsed": 0.5}
                    ))
            else:
                signals.append(Signal(
                    kind="doc",
                    props={"name": path.stem, "file": str(path), "note": "Unknown artifact file type"},
                    evidence=[f"{path}:1-1"],
                    subscores={"parsed": 0.2}
                ))
        except Exception as e:
            signals.append(Signal(
                kind="doc",
                props={"name": path.stem, "file": str(path), "note": f"artifact parse error: {e}"},
                evidence=[f"{path}:1-1"],
                subscores={"parsed": 0.1}
            ))
        return signals

    def _parse_service_descriptor(self, obj: Dict[str, Any], path: Path) -> Signal | None:
        """
        Best-effort parser for TIBCO service descriptor JSON (non-OpenAPI).
        Extracts endpoints/methods and surfaces them as triggers so downstream
        scaffold can treat them like HTTP interfaces.
        """
        endpoints: List[Dict[str, Any]] = []
        # common shapes: {"bindings":[{"http":{"path":...,"method":...}}]}
        bindings = obj.get("bindings") or obj.get("services") or []
        for binding in bindings:
            http = binding.get("http") if isinstance(binding, dict) else None
            if isinstance(http, dict):
                path_val = http.get("path") or http.get("uri") or binding.get("path")
                method = (http.get("method") or http.get("verb") or "").upper()
                if path_val:
                    endpoints.append({"type": "http", "path": path_val, "method": method or None, "evidence": {"file": str(path)}})
        # alternative: operations list
        for op in obj.get("operations") or []:
            if not isinstance(op, dict):
                continue
            path_val = op.get("path") or op.get("resourcePath")
            method = (op.get("method") or op.get("verb") or "").upper()
            if path_val:
                endpoints.append({"type": "http", "path": path_val, "method": method or None, "evidence": {"file": str(path)}})

        if not endpoints:
            return None
        props = {
            "name": obj.get("name") or path.stem,
            "file": str(path),
            "triggers": endpoints,
            "steps": [],
            "relationships": [],
            "identifiers": [obj.get("name") or path.stem],
        }
        return Signal(kind="service_descriptor", props=props, evidence=[f"{path}:1-40"], subscores={"parsed": 0.7})
