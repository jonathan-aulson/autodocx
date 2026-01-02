from __future__ import annotations

import json
import re
import zipfile
import xml.etree.ElementTree as ET
from pathlib import Path
from typing import Any, Dict, Iterable, List, Optional, Sequence

from autodocx.types import Signal


def _parse_xml(path: Path) -> Optional[ET.Element]:
    try:
        text = path.read_text(encoding="utf-8", errors="ignore")
    except Exception:
        return None
    try:
        return ET.fromstring(text)
    except ET.ParseError:
        return None


def _xml_attr(element: Optional[ET.Element], name: str, default: str = "") -> str:
    if element is None:
        return default
    return element.attrib.get(name, default)


def _collect_patterns(repo: Path, patterns: Sequence[str]) -> Iterable[Path]:
    seen: set[Path] = set()
    for pattern in patterns:
        for path in repo.glob(pattern):
            if path.is_file() and path not in seen:
                seen.add(path)
                yield path


def _default_signal_props(path: Path, name: Optional[str] = None) -> Dict[str, Any]:
    return {
        "name": name or path.stem,
        "file": str(path),
        "triggers": [],
        "steps": [],
        "relationships": [],
        "datasource_tables": [],
        "service_dependencies": [],
        "process_calls": [],
        "identifiers": [],
    }


def _connector_from_text(text: str) -> str:
    lowered = text.lower()
    if "http" in lowered or "rest" in lowered:
        return "http"
    if "soap" in lowered:
        return "soap"
    if "jdbc" in lowered or "sql" in lowered or "db" in lowered:
        return "jdbc"
    if "jms" in lowered or "queue" in lowered or "topic" in lowered:
        return "jms"
    if "process" in lowered:
        return "process"
    return "custom"


class BwModuleManifestExtractor:
    """
    Parses BW module descriptors (*.jsv, *.msv, *.bwm) and emits workflow metadata
    before .bwp parsing occurs. Includes triggers, component steps, and dependency hints.
    """

    name = "bw_module_manifest"
    patterns = ["**/*.jsv", "**/*.msv", "**/*.bwm"]

    def detect(self, repo: Path) -> bool:
        return any(_collect_patterns(repo, self.patterns))

    def discover(self, repo: Path) -> Iterable[Path]:
        return _collect_patterns(repo, self.patterns)

    def extract(self, path: Path) -> Iterable[Signal]:
        root = _parse_xml(path)
        if root is None:
            return [
                Signal(
                    kind="workflow_manifest",
                    props={"name": path.stem, "file": str(path), "triggers": [], "steps": [], "relationships": []},
                    evidence=[f"{path}:1-1"],
                    subscores={"parsed": 0.0},
                )
            ]

        suffix = path.suffix.lower()
        if suffix == ".bwm":
            props = self._parse_composite(root, path)
        else:
            props = self._parse_shared_vars(root, path)
        return [
            Signal(
                kind="workflow_manifest",
                props=props,
                evidence=[f"{path}:1-1"],
                subscores={"parsed": 1.0 if props.get("steps") else 0.6},
            )
        ]

    def _parse_composite(self, root: ET.Element, path: Path) -> Dict[str, Any]:
        props = _default_signal_props(path, root.attrib.get("name") or path.stem)
        props["module_namespace"] = root.attrib.get("targetNamespace", "")

        for svc in root.findall(".//{*}service"):
            trig = {
                "type": "sca.service",
                "name": svc.attrib.get("name"),
                "binding": svc.attrib.get("promote") or svc.attrib.get("interface"),
                "evidence": {"file": str(path)},
            }
            props["triggers"].append(trig)

        for comp in root.findall(".//{*}component"):
            comp_name = comp.attrib.get("name")
            impl = comp.attrib.get("implementation") or comp.attrib.get("{http://www.omg.org/XMI}type", "component")
            props["steps"].append(
                {
                    "name": comp_name,
                    "type": impl,
                    "connector": _connector_from_text(impl),
                    "evidence": {"file": str(path)},
                }
            )

        for reference in root.findall(".//{*}reference"):
            source = reference.attrib.get("name")
            target = reference.attrib.get("target") or reference.attrib.get("promote")
            if not (source and target):
                continue
            props["relationships"].append(
                {
                    "type": "calls",
                    "source": source,
                    "target": target,
                    "evidence": {"file": str(path)},
                }
            )
            props["process_calls"].append(target)

        identifiers: List[str] = []
        for prop in root.findall(".//{*}property"):
            name = prop.attrib.get("name")
            if not name:
                continue
            identifiers.append(name)
        props["identifiers"] = identifiers
        return props

    def _parse_shared_vars(self, root: ET.Element, path: Path) -> Dict[str, Any]:
        props = _default_signal_props(path, path.stem)
        vars_found: List[Dict[str, Any]] = []
        for var in root.findall(".//{*}Variable") + root.findall(".//{*}SharedVariable"):
            name = var.attrib.get("name")
            if not name:
                continue
            value = var.attrib.get("value") or ""
            vars_found.append({"name": name, "value": value})
        props["steps"] = [{"name": v["name"], "connector": "variable", "outputs_example": v["value"]} for v in vars_found]
        props["identifiers"] = [v["name"] for v in vars_found]
        return props


class BwResourceBindingExtractor:
    """
    Parses BW shared resource bindings (*.httpConnResource, *.jdbcResource, *.httpClientResource, *.jmsResource)
    to expose interface endpoints, datastore names, and process dependencies.
    """

    name = "bw_resource_binding"
    patterns = [
        "**/*.httpConnResource",
        "**/*.httpClientResource",
        "**/*.jdbcResource",
        "**/*.jmsResource",
    ]

    def detect(self, repo: Path) -> bool:
        return any(_collect_patterns(repo, self.patterns))

    def discover(self, repo: Path) -> Iterable[Path]:
        return _collect_patterns(repo, self.patterns)

    def extract(self, path: Path) -> Iterable[Signal]:
        root = _parse_xml(path)
        conn_type = path.suffix.lower().replace(".", "")
        props = _default_signal_props(path)
        props["resource_type"] = conn_type
        props["resource_name"] = root.attrib.get("name") if root is not None else path.stem

        if root is not None:
            substitution = []
            for binding in root.findall(".//{*}substitutionBindings"):
                template = binding.attrib.get("template")
                prop = binding.attrib.get("propName")
                substitution.append({"template": template, "prop": prop})
            props["substitution_bindings"] = substitution

            if conn_type.startswith("http"):
                props["triggers"].append(
                    {
                        "type": "http",
                        "path": root.attrib.get("name"),
                        "method": "dynamic",
                        "evidence": {"file": str(path)},
                    }
                )
            elif "jdbc" in conn_type:
                datastores = []
                for attr_name, attr_value in root.attrib.items():
                    if attr_name.lower() in {"schema", "catalog", "url", "datasource"} and attr_value:
                        datastores.append(attr_value)
                if datastores:
                    props["datasource_tables"] = datastores
            elif "jms" in conn_type:
                queues = []
                for elem in root.findall(".//{*}queue") + root.findall(".//{*}Topic"):
                    name = elem.attrib.get("name")
                    if name:
                        queues.append(name)
                props["service_dependencies"] = queues

            props["steps"].append(
                {
                    "name": props["resource_name"],
                    "connector": _connector_from_text(conn_type),
                    "inputs_keys": [b.get("prop") for b in substitution if b.get("prop")],
                    "outputs_example": substitution[:2],
                }
            )

        return [
            Signal(
                kind="connection_resource",
                props=props,
                evidence=[f"{path}:1-1"],
                subscores={"parsed": 1.0 if root is not None else 0.3},
            )
        ]


class BwSubstitutionVarExtractor:
    """
    Maps *.substvar files to environment-aware identifiers so downstream docs can reflect
    hostnames, schemas, and other deployment tokens.
    """

    name = "bw_substitution_vars"
    patterns = ["**/*.substvar"]

    def detect(self, repo: Path) -> bool:
        return any(_collect_patterns(repo, self.patterns))

    def discover(self, repo: Path) -> Iterable[Path]:
        return _collect_patterns(repo, self.patterns)

    def extract(self, path: Path) -> Iterable[Signal]:
        root = _parse_xml(path)
        props = _default_signal_props(path)
        props["environment_variables"] = []
        props["kind"] = "substitution_vars"
        identifiers: List[str] = []
        if root is not None:
            for gv in root.findall(".//globalVariable"):
                name = gv.findtext("name") or gv.attrib.get("name")
                value = gv.findtext("value") or gv.attrib.get("value") or ""
                if not name:
                    continue
                props["environment_variables"].append({"name": name, "value": value})
                if value:
                    identifiers.append(f"{name}={value}")
        props["identifiers"] = identifiers
        props["steps"] = [
            {"name": var.get("name"), "connector": "env.variable", "outputs_example": var.get("value")}
            for var in props["environment_variables"]
        ]
        return [
            Signal(
                kind="env_config",
                props=props,
                evidence=[f"{path}:1-1"],
                subscores={"parsed": 1.0 if root is not None else 0.2},
            )
        ]


class BwTestSuiteExtractor:
    """
    Converts BW unit-test artifacts (*.bwt + companion *.ml data sets) into Signals highlighting
    invoked processes, sample payloads, and expected outputs.
    """

    name = "bw_test_suite"
    patterns = ["**/*.bwt", "**/*.ml"]

    def detect(self, repo: Path) -> bool:
        return any(_collect_patterns(repo, self.patterns))

    def discover(self, repo: Path) -> Iterable[Path]:
        return _collect_patterns(repo, self.patterns)

    def extract(self, path: Path) -> Iterable[Signal]:
        suffix = path.suffix.lower()
        root = _parse_xml(path)
        if suffix == ".bwt":
            props = self._parse_bwt(root, path)
            kind = "bw_test_case"
        else:
            props = self._parse_ml_dataset(root, path)
            kind = "bw_test_dataset"
        return [
            Signal(
                kind=kind,
                props=props,
                evidence=[f"{path}:1-1"],
                subscores={"parsed": 1.0 if root is not None else 0.4},
            )
        ]

    def _parse_bwt(self, root: Optional[ET.Element], path: Path) -> Dict[str, Any]:
        props = _default_signal_props(path)
        if root is None:
            return props
        proc = root.find(".//ProcessNode")
        process_name = _xml_attr(proc, "Name", path.stem)
        props["name"] = f"{process_name}::test"
        props["process_under_test"] = process_name

        for op in root.findall(".//Operation"):
            operation_name = _xml_attr(op, "Name")
            service_name = _xml_attr(op, "serviceName")
            props["steps"].append(
                {
                    "name": operation_name or service_name or "operation",
                    "connector": "callprocess",
                    "destination": service_name,
                    "inputs_example": self._collect_parameters(op),
                }
            )
            props["process_calls"].append(service_name)

        assertions = []
        for assertion in root.findall(".//Assertion"):
            name = _xml_attr(assertion, "Name", "assertion")
            lang = assertion.findtext("Lang") or ""
            assertions.append({"name": name, "language": lang})
        props["relationships"] = [{"type": "validates", "target": process_name, "evidence": assertions[:1]}]
        props["identifiers"] = [process_name]
        props["triggers"] = [
            {
                "type": "test_runner",
                "name": process_name,
                "evidence": {"file": str(path)},
            }
        ]
        return props

    def _collect_parameters(self, op: ET.Element) -> List[Dict[str, Any]]:
        params = []
        for param in op.findall(".//Parameter"):
            params.append({"name": param.attrib.get("Name"), "value": param.attrib.get("Value")})
        return params

    def _parse_ml_dataset(self, root: Optional[ET.Element], path: Path) -> Dict[str, Any]:
        props = _default_signal_props(path)
        props["name"] = f"{path.stem}::dataset"
        rows = []
        if root is not None:
            for row in root.findall(".//Row") + root.findall(".//TestDataRow"):
                sample = {child.attrib.get("Name"): child.attrib.get("Value") for child in row.findall(".//parameters")}
                if sample:
                    rows.append(sample)
        props["steps"] = [
            {
                "name": "dataset",
                "connector": "data.sample",
                "outputs_example": rows[:3],
            }
        ]
        props["identifiers"] = [path.stem]
        return props


class BwDiagramExtractor:
    """
    Reads *.bwd diagrams and emits workflow steps + transitions so interdependency graphs
    have fallback coverage when .bwp parsing is incomplete.
    """

    name = "bw_diagram"
    patterns = ["**/*.bwd"]

    def detect(self, repo: Path) -> bool:
        return any(_collect_patterns(repo, self.patterns))

    def discover(self, repo: Path) -> Iterable[Path]:
        return _collect_patterns(repo, self.patterns)

    def extract(self, path: Path) -> Iterable[Signal]:
        root = _parse_xml(path)
        props = _default_signal_props(path)
        props["name"] = path.stem
        if root is not None:
            activities = []
            for act in root.findall(".//activity-dir"):
                step = {
                    "name": act.attrib.get("name"),
                    "type": act.attrib.get("type"),
                    "connector": _connector_from_text(act.attrib.get("type", "")),
                }
                activities.append(step)
            props["steps"] = activities

            relationships = []
            for link in root.findall(".//link-dir"):
                source = link.attrib.get("source")
                target = link.attrib.get("target")
                if not (source and target):
                    continue
                relationships.append(
                    {
                        "type": "transition",
                        "connector": "workflow",
                        "operation": {"type": link.attrib.get("linkType") or "flow"},
                        "source": {
                            "name": source,
                            "kind": "workflow.node",
                            "display": source,
                        },
                        "target": {
                            "name": target,
                            "kind": "workflow.node",
                            "display": target,
                        },
                        "evidence": {"file": str(path)},
                    }
                )
            props["relationships"] = relationships
            props["identifiers"] = [path.stem]
        return [
            Signal(
                kind="workflow_diagram",
                props=props,
                evidence=[f"{path}:1-1"],
                subscores={"parsed": 1.0 if root is not None else 0.2},
            )
        ]


class ArchiveExpansionExtractor:
    """
    Treats archives (*.ear, *.jar, *.zip, *.par, *.war) as metadata-bearing inputs by
    describing their contents so orchestration stages can reason about embedded modules.
    """

    name = "bw_archive_expansion"
    patterns = ["**/*.ear", "**/*.jar", "**/*.zip", "**/*.par", "**/*.war"]

    def detect(self, repo: Path) -> bool:
        return any(_collect_patterns(repo, self.patterns))

    def discover(self, repo: Path) -> Iterable[Path]:
        return _collect_patterns(repo, self.patterns)

    def extract(self, path: Path) -> Iterable[Signal]:
        entries: List[str] = []
        try:
            with zipfile.ZipFile(path, "r") as zf:
                entries = zf.namelist()
        except Exception:
            entries = []
        props = _default_signal_props(path)
        props["archive_entries"] = entries[:50]
        props["identifiers"] = [path.stem]

        def _entry_target(entry: str) -> Dict[str, Any]:
            stem = entry.rstrip("/").split("/")[-1] or entry
            suffix = stem.split(".")[-1].lower() if "." in stem else ""
            kind = "archive.entry"
            if suffix in {"bwp", "bwd", "bwt"}:
                kind = "workflow"
            elif suffix in {"xml", "xsd"}:
                kind = "schema"
            elif suffix in {"json"}:
                kind = "descriptor"
            return {
                "name": stem,
                "kind": kind,
                "ref": entry,
                "path": entry,
            }

        props["relationships"] = [
            {
                "type": "contains",
                "connector": "archive",
                "source": {
                    "name": path.name,
                    "kind": "archive",
                    "display": path.name,
                },
                "target": _entry_target(entry),
                "evidence": {"file": str(path)},
            }
            for entry in entries[:20]
        ]
        return [
            Signal(
                kind="archive_manifest",
                props=props,
                evidence=[f"{path}:1-1"],
                subscores={"parsed": 1.0 if entries else 0.2},
            )
        ]


class JavaOsgiComponentExtractor:
    """
    Targets BW plug-in components (Java + MANIFEST + properties) to surface custom adapters,
    exported services, and datastore dependencies bundled with TIBCO solutions.
    """

    name = "bw_java_osgi_component"
    patterns = [
        "**/META-INF/MANIFEST.MF",
        "**/*.java",
        "**/*.properties",
    ]

    _CLASS_RE = re.compile(r"class\\s+(?P<name>[A-Za-z0-9_]+)")

    def detect(self, repo: Path) -> bool:
        return any(repo.glob("**/META-INF/MANIFEST.MF"))

    def discover(self, repo: Path) -> Iterable[Path]:
        return _collect_patterns(repo, self.patterns)

    def extract(self, path: Path) -> Iterable[Signal]:
        suffix = path.suffix.lower()
        if path.name == "MANIFEST.MF":
            props = self._parse_manifest(path)
            kind = "java_manifest"
        elif suffix == ".java":
            props = self._parse_java(path)
            kind = "java_plugin"
        else:
            props = self._parse_properties(path)
            kind = "java_properties"
        return [
            Signal(
                kind=kind,
                props=props,
                evidence=[f"{path}:1-1"],
                subscores={"parsed": 0.9},
            )
        ]

    def _parse_manifest(self, path: Path) -> Dict[str, Any]:
        props = _default_signal_props(path)
        entries = {}
        for line in path.read_text(encoding="utf-8", errors="ignore").splitlines():
            if ":" not in line:
                continue
            key, value = line.split(":", 1)
            entries[key.strip()] = value.strip()
        props["manifest"] = entries
        props["name"] = entries.get("Bundle-Name") or path.parent.name
        props["identifiers"] = [entries.get("Bundle-SymbolicName", path.stem)]
        props["steps"] = [{"name": "bundle", "connector": "osgi", "outputs_example": entries}]
        return props

    def _parse_java(self, path: Path) -> Dict[str, Any]:
        props = _default_signal_props(path)
        text = path.read_text(encoding="utf-8", errors="ignore")
        match = self._CLASS_RE.search(text)
        class_name = match.group("name") if match else path.stem
        props["name"] = class_name
        connectors = []
        for token in ["http", "jdbc", "jms", "kafka", "soap", "rest"]:
            if token in text.lower():
                connectors.append(token)
        props["steps"] = [{"name": class_name, "connector": connector} for connector in connectors or ["custom"]]
        props["service_dependencies"] = connectors
        props["identifiers"] = [class_name]
        return props

    def _parse_properties(self, path: Path) -> Dict[str, Any]:
        props = _default_signal_props(path)
        entries = {}
        for line in path.read_text(encoding="utf-8", errors="ignore").splitlines():
            if line.strip().startswith("#") or "=" not in line:
                continue
            key, value = line.split("=", 1)
            entries[key.strip()] = value.strip()
        props["name"] = path.stem
        props["steps"] = [{"name": k, "connector": "config", "outputs_example": v} for k, v in entries.items()]
        props["identifiers"] = list(entries.keys())
        return props


class BwServiceDescriptorExtractor:
    """
    Parses Service Descriptor payloads (*.Process-*.json/xml) to capture REST/SOAP surfaces
    with path/method metadata that the doc plan can prioritize.
    """

    name = "bw_service_descriptor"
    patterns = [
        "**/Service Descriptors/*.json",
        "**/Service Descriptors/*.xml",
        "**/*.Process-*.json",
        "**/*.Process-*.xml",
    ]

    def detect(self, repo: Path) -> bool:
        return any(_collect_patterns(repo, self.patterns))

    def discover(self, repo: Path) -> Iterable[Path]:
        return _collect_patterns(repo, self.patterns)

    def extract(self, path: Path) -> Iterable[Signal]:
        suffix = path.suffix.lower()
        if suffix == ".json":
            props = self._parse_json_descriptor(path)
        else:
            props = self._parse_xml_descriptor(path)
        return [
            Signal(
                kind="service_descriptor",
                props=props,
                evidence=[f"{path}:1-1"],
                subscores={"parsed": 1.0 if props.get("triggers") else 0.6},
            )
        ]

    def _parse_json_descriptor(self, path: Path) -> Dict[str, Any]:
        props = _default_signal_props(path)
        try:
            data = json.loads(path.read_text(encoding="utf-8", errors="ignore"))
        except Exception:
            data = {}
        props["descriptor_keys"] = list(data.keys())[:10]
        triggers = []
        paths = data.get("paths") or data.get("operations") or {}
        if isinstance(paths, dict):
            for key, value in list(paths.items())[:20]:
                if isinstance(value, dict):
                    for method in value.keys():
                        triggers.append(
                            {
                                "type": "http",
                                "method": method.upper(),
                                "path": key,
                            }
                        )
        props["triggers"] = triggers
        props["steps"] = [{"name": trig.get("path"), "connector": "http", "method": trig.get("method")} for trig in triggers]
        props["identifiers"] = [path.stem]
        return props

    def _parse_xml_descriptor(self, path: Path) -> Dict[str, Any]:
        root = _parse_xml(path)
        props = _default_signal_props(path)
        if root is None:
            return props
        operations = []
        for el in root.findall(".//{*}operation"):
            name = el.attrib.get("name")
            operations.append(name)
        props["triggers"] = [{"type": "soap", "name": name} for name in operations if name]
        props["steps"] = [{"name": name, "connector": "soap"} for name in operations if name]
        props["identifiers"] = operations[:5]
        return props
