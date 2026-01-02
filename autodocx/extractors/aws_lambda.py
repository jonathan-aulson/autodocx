from __future__ import annotations

import json
from pathlib import Path
from typing import Any, Dict, Iterable, List, Optional, Set, Tuple

import yaml

from autodocx.types import Signal


class AWSLambdaExtractor:
    """
    Parse AWS SAM/CloudFormation templates or Serverless Framework manifests
    to emit workflow signals for AWS Lambda functions.
    """

    name = "aws_lambda"
    patterns = [
        "**/template.yaml",
        "**/template.yml",
        "**/template.json",
        "**/sam.*.yaml",
        "**/serverless.yaml",
        "**/serverless.yml",
        "**/serverless.json",
    ]

    def detect(self, repo: Path) -> bool:
        return any(repo.glob(pattern) for pattern in self.patterns)

    def discover(self, repo: Path) -> Iterable[Path]:
        for pattern in self.patterns:
            for match in repo.glob(pattern):
                if match.is_file():
                    yield match

    def extract(self, path: Path) -> Iterable[Signal]:
        data = self._load_manifest(path)
        if not data:
            return []
        signals: List[Signal] = []
        # CloudFormation / SAM resources
        resources = data.get("Resources") or {}
        for name, resource in resources.items():
            if not isinstance(resource, dict):
                continue
            if (resource.get("Type") or "").lower() == "aws::lambda::function":
                signal = self._signal_from_cf_lambda(name, resource, path)
                if signal:
                    signals.append(signal)
        # Serverless functions shorthand
        functions = data.get("functions") or {}
        if isinstance(functions, dict):
            for name, fn in functions.items():
                if not isinstance(fn, dict):
                    continue
                signal = self._signal_from_serverless_lambda(name, fn, path)
                if signal:
                    signals.append(signal)
        return signals

    # ---------------- internal helpers ----------------

    def _load_manifest(self, path: Path) -> Dict[str, Any]:
        try:
            text = path.read_text(encoding="utf-8")
            if path.suffix.lower() == ".json":
                return json.loads(text)
            return yaml.safe_load(text) or {}
        except Exception:
            return {}

    def _signal_from_cf_lambda(self, name: str, resource: Dict[str, Any], path: Path) -> Signal | None:
        props = resource.get("Properties") or {}
        runtime = props.get("Runtime")
        handler = props.get("Handler")
        events = self._parse_sam_events(props.get("Events") or {})
        relationships = self._relationships_from_events(name, events, path)
        steps_meta, datastores, service_deps, process_calls = self._event_dependency_hints(events)
        meta = {
            "name": name,
            "engine": "aws_lambda",
            "file": str(path),
            "runtime": runtime,
            "handler": handler,
            "memory": props.get("MemorySize"),
            "timeout": props.get("Timeout"),
            "description": props.get("Description"),
            "environment": (props.get("Environment") or {}).get("Variables"),
            "triggers": events,
            "steps": steps_meta,
            "relationships": relationships,
            "datasource_tables": datastores,
            "service_dependencies": service_deps,
            "process_calls": process_calls,
        }
        return Signal(
            kind="workflow",
            props=meta,
            evidence=[f"{path}:{name}"],
            subscores={"parsed": 1.0, "schema_evidence": 0.4},
        )

    def _signal_from_serverless_lambda(self, name: str, fn: Dict[str, Any], path: Path) -> Signal | None:
        events = self._parse_serverless_events(fn.get("events") or [])
        relationships = self._relationships_from_events(name, events, path)
        steps_meta, datastores, service_deps, process_calls = self._event_dependency_hints(events)
        meta = {
            "name": name,
            "engine": "aws_lambda",
            "file": str(path),
            "runtime": fn.get("runtime"),
            "handler": fn.get("handler"),
            "memory": fn.get("memorySize"),
            "timeout": fn.get("timeout"),
            "description": fn.get("description"),
            "environment": fn.get("environment"),
            "triggers": events,
            "steps": steps_meta,
            "relationships": relationships,
            "datasource_tables": datastores,
            "service_dependencies": service_deps,
            "process_calls": process_calls,
        }
        return Signal(
            kind="workflow",
            props=meta,
            evidence=[f"{path}:{name}"],
            subscores={"parsed": 0.9},
        )

    def _parse_sam_events(self, events: Dict[str, Any]) -> List[Dict[str, Any]]:
        parsed: List[Dict[str, Any]] = []
        for name, event in events.items():
            if not isinstance(event, dict):
                continue
            etype = event.get("Type")
            props = event.get("Properties") or {}
            parsed.append({"name": name, "type": etype, "properties": props})
        return parsed

    def _parse_serverless_events(self, events: List[Any]) -> List[Dict[str, Any]]:
        parsed: List[Dict[str, Any]] = []
        for event in events:
            if isinstance(event, dict):
                for etype, config in event.items():
                    parsed.append(
                        {
                            "name": etype,
                            "type": etype,
                            "properties": config if isinstance(config, dict) else {"value": config},
                        }
                    )
            else:
                parsed.append({"name": "event", "type": str(event), "properties": {"value": event}})
        return parsed

    def _event_dependency_hints(self, events: List[Dict[str, Any]]) -> Tuple[List[Dict[str, Any]], List[str], List[str], List[str]]:
        steps: List[Dict[str, Any]] = []
        datastores: Set[str] = set()
        service_deps: Set[str] = set()
        process_refs: Set[str] = set()
        for idx, event in enumerate(events or []):
            etype = (event.get("type") or "event").lower()
            name = event.get("name") or f"event_{idx}"
            props = event.get("properties") or {}
            datastore = self._event_datastore(props, etype)
            service = self._event_service(props, etype)
            if datastore:
                datastores.add(str(datastore))
                if etype == "s3":
                    process_refs.add(f"s3://{datastore}")
            if service:
                service_deps.add(str(service))
                process_refs.add(str(service))
            step: Dict[str, Any] = {
                "name": name,
                "connector": etype,
                "type": etype,
                "operation": "triggers" if etype in {"api", "http", "httpapi", "schedule"} else "consumes",
            }
            if datastore:
                step["datasource_table"] = datastore
            if service:
                step["destination"] = service
            if props:
                step["context"] = props
            steps.append(step)
        return steps, sorted(datastores), sorted(service_deps), sorted(process_refs)

    def _event_datastore(self, props: Dict[str, Any], etype: str) -> Optional[str]:
        if etype in {"s3"}:
            return props.get("Bucket") or props.get("bucket")
        if etype in {"dynamodb", "table"}:
            return props.get("Table") or props.get("table")
        if etype in {"stream", "kinesis"}:
            return props.get("Stream") or props.get("stream")
        if etype in {"sqs"}:
            return props.get("Queue") or props.get("queue")
        return None

    def _event_service(self, props: Dict[str, Any], etype: str) -> Optional[str]:
        if etype in {"api", "http", "httpapi"}:
            method = props.get("Method") or props.get("method") or props.get("http", {}).get("method")
            route = props.get("Path") or props.get("path") or props.get("http", {}).get("path")
            if method or route:
                return f"{(method or 'ANY').upper()} {route or '/'}"
        if etype == "schedule":
            return props.get("Schedule") or props.get("schedule") or props.get("rate")
        if etype in {"sns"}:
            return props.get("Topic") or props.get("topic")
        return None

    def _relationships_from_events(self, func_name: str, events: List[Dict[str, Any]], path: Path) -> List[Dict[str, Any]]:
        relationships: List[Dict[str, Any]] = []
        for event in events:
            etype = (event.get("type") or "").lower()
            props = event.get("properties") or {}
            target_display = None
            if etype in {"api", "http", "httpapi"}:
                method = props.get("Method") or props.get("method") or props.get("http", {}).get("method") or "ANY"
                route = props.get("Path") or props.get("path") or props.get("http", {}).get("path") or "/"
                target_display = f"{method.upper()} {route}"
            elif etype in {"schedule"}:
                target_display = props.get("Schedule") or props.get("rate") or "cron"
            elif etype in {"s3"}:
                bucket = props.get("Bucket") or props.get("bucket")
                target_display = f"S3:{bucket}"
            elif etype in {"stream", "kinesis"}:
                stream = props.get("Stream") or props.get("stream")
                target_display = f"Stream:{stream}"
            operation = {"type": "triggered_by", "detail": etype or "event"}
            relationships.append(
                {
                    "source": {"type": "lambda", "name": func_name},
                    "target": {"kind": etype or "event", "display": target_display or etype or "event"},
                    "operation": operation,
                    "connector": etype or "event",
                    "evidence": [f"{path}:{func_name}"],
                }
            )
        return relationships
