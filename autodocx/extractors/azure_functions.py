from __future__ import annotations
from pathlib import Path
from typing import Iterable, List, Dict, Any, Optional, Tuple
import hashlib
import json, re
from autodocx.types import Signal

HTTP_METHODS = {"get", "post", "put", "delete", "patch", "head", "options"}
_FUNC_BLOCK_RE = re.compile(r'\[Function\("(?P<func>[^"]+)"\)\](?P<body>.*?)(?=\[Function\("|$)', re.IGNORECASE | re.DOTALL)
_HTTP_TRIGGER_RE = re.compile(r"\[HttpTrigger\((?P<args>.*?)\)\]", re.IGNORECASE | re.DOTALL)
_BINDING_ATTR_RE = re.compile(r"\[(?P<attr>(Http|Timer|Queue|ServiceBus|Blob|CosmosDB)(Trigger|Output))\((?P<args>.*?)\)\]", re.IGNORECASE | re.DOTALL)


class AzureFunctionsExtractor:
    name = "azure_functions"
    patterns = ["**/function.json", "**/*.cs"]

    def detect(self, repo: Path) -> bool:
        return any(repo.glob("**/function.json")) or any(repo.glob("**/*.cs"))

    def discover(self, repo: Path) -> Iterable[Path]:
        fns = list(repo.glob("**/function.json"))
        if fns:
            for p in fns:
                yield p
            return
        for p in repo.glob("**/*.cs"):
            yield p

    def extract(self, path: Path) -> Iterable[Signal]:
        if path.name == "function.json":
            return self._extract_from_function_json(path)
        if path.suffix.lower() == ".cs":
            return self._extract_from_cs(path)
        return []

    # -------------------- function.json --------------------

    def _extract_from_function_json(self, path: Path) -> Iterable[Signal]:
        signals: List[Signal] = []
        try:
            doc = json.loads(path.read_text(encoding="utf-8", errors="ignore"))
        except Exception as exc:
            return [
                Signal(
                    kind="doc",
                    props={"name": path.name, "file": str(path), "note": f"function.json parse error: {exc}"},
                    evidence=[f"{path}:1-1"],
                    subscores={"parsed": 0.1},
                )
            ]

        bindings = doc.get("bindings") or []
        parent_name = path.parent.name if path.parent.name else ""
        func_name = doc.get("entryPoint") or parent_name or path.stem
        relationships: List[Dict[str, Any]] = []
        http_routes: List[Tuple[str, str]] = []

        for binding in bindings:
            rels, route_info = self._relationships_from_binding(binding, func_name, source=str(path))
            relationships.extend(rels)
            http_routes.extend(route_info)

        if http_routes:
            for method, route in http_routes:
                signals.append(
                    Signal(
                        kind="route",
                        props={
                            "name": func_name,
                            "method": method,
                            "path": route,
                            "file": str(path),
                            "relationships": relationships,
                        },
                        evidence=[f"{path}:1-80"],
                        subscores={"parsed": 1.0, "endpoint_or_op_coverage": 0.7},
                    )
                )
        elif relationships:
            # Fallback workflow-style signal if no explicit HTTP route
            signals.append(
                Signal(
                    kind="workflow",
                    props={
                        "name": func_name,
                        "engine": "azure_functions",
                        "wf_kind": "azure_function",
                        "file": str(path),
                        "triggers": [],
                        "steps": [],
                        "relationships": relationships,
                    },
                    evidence=[f"{path}:1-80"],
                    subscores={"parsed": 0.9},
                )
            )
        return signals

    # -------------------- C# parser --------------------

    def _extract_from_cs(self, path: Path) -> Iterable[Signal]:
        try:
            text = path.read_text(encoding="utf-8", errors="ignore")
        except Exception as exc:
            return [
                Signal(
                    kind="doc",
                    props={"name": path.name, "file": str(path), "note": f"C# parse error: {exc}"},
                    evidence=[f"{path}:1-1"],
                    subscores={"parsed": 0.1},
                )
            ]

        signals: List[Signal] = []
        for block in _FUNC_BLOCK_RE.finditer(text):
            func_name = block.group("func")
            body = block.group("body")
            relationships: List[Dict[str, Any]] = []
            routes: List[Tuple[str, str]] = []

            for match in _HTTP_TRIGGER_RE.finditer(body):
                args = match.group("args")
                methods = self._parse_http_methods(args)
                route = self._extract_named_arg(args, "Route") or self._extract_named_arg(args, "RouteTemplate") or func_name
                auth = self._extract_auth_level(args)
                cleaned_route = self._clean_route(route)
                context = {"route": cleaned_route, "auth_level": auth}
                route_params = self._route_parameters(cleaned_route)
                if route_params:
                    context["route_params"] = route_params
                relationships.append(
                    self._build_relationship(
                        source={"type": "trigger", "name": "http", "step_id": "httptrigger"},
                        target={"kind": "function", "ref": func_name, "display": func_name},
                        operation="receives",
                        connector="httptrigger",
                        direction="inbound",
                        context=context,
                        evidence=[self._line_span(path, text, match.start(), match.end())],
                    )
                )
                for method in methods or ["GET"]:
                    routes.append((method, cleaned_route))

            for binding in _BINDING_ATTR_RE.finditer(body):
                attr = binding.group("attr")
                if attr.lower().startswith("httptrigger"):
                    continue
                rels, additional_routes = self._relationships_from_attribute(attr, binding.group("args"), func_name, text, path, binding.start(), binding.end())
                relationships.extend(rels)
                routes.extend(additional_routes)

            if routes:
                for method, route in routes:
                    signals.append(
                        Signal(
                            kind="route",
                            props={
                                "name": func_name,
                                "method": method,
                                "path": route,
                                "file": str(path),
                                "relationships": relationships,
                            },
                            evidence=[self._line_span(path, text, block.start(), block.end())],
                            subscores={"parsed": 1.0, "endpoint_or_op_coverage": 0.7},
                        )
                    )
            elif relationships:
                signals.append(
                    Signal(
                        kind="workflow",
                        props={
                            "name": func_name,
                            "engine": "azure_functions",
                            "wf_kind": "azure_function",
                            "file": str(path),
                            "triggers": [],
                            "steps": [],
                            "relationships": relationships,
                        },
                        evidence=[self._line_span(path, text, block.start(), block.end())],
                        subscores={"parsed": 0.9},
                    )
                )
        return signals

    # -------------------- helpers --------------------

    def _relationships_from_binding(
        self, binding: Dict[str, Any], func_name: str, source: str
    ) -> Tuple[List[Dict[str, Any]], List[Tuple[str, str]]]:
        btype = (binding.get("type") or "").lower()
        direction = (binding.get("direction") or "").lower()
        relationships: List[Dict[str, Any]] = []
        routes: List[Tuple[str, str]] = []

        if btype == "httptrigger":
            route = self._clean_route(binding.get("route") or binding.get("path") or func_name)
            methods = [m.upper() for m in binding.get("methods") or [] if m.lower() in HTTP_METHODS] or ["GET"]
            auth = binding.get("authLevel")
            context = {"route": route, "auth_level": auth}
            route_params = self._route_parameters(route)
            if route_params:
                context["route_params"] = route_params
            relationships.append(
                self._build_relationship(
                    source={"type": "trigger", "name": "http", "step_id": "httptrigger"},
                    target={"kind": "function", "ref": func_name, "display": func_name},
                    operation="receives",
                    connector="httptrigger",
                    direction="inbound",
                    context=context,
                    evidence=[f"{source}:httptrigger"],
                )
            )
            for method in methods:
                routes.append((method, route))

        elif "trigger" in btype:
            kind, ref, display, context = self._binding_target(btype, binding)
            if kind and ref:
                relationships.append(
                    self._build_relationship(
                        source={"type": "trigger", "name": btype, "step_id": btype},
                        target={"kind": kind, "ref": ref, "display": display or ref},
                        operation="receives" if kind == "queue" else "reads",
                        connector=btype,
                        direction="inbound",
                        context=context,
                        evidence=[f"{source}:{btype}"],
                    )
                )

        if "output" in btype or direction == "out":
            kind, ref, display, context = self._binding_target(btype, binding)
            if kind and ref:
                relationships.append(
                    self._build_relationship(
                        source={"type": "function", "name": func_name, "step_id": func_name},
                        target={"kind": kind, "ref": ref, "display": display or ref},
                        operation="publishes" if kind in {"queue", "servicebus", "event"} else "writes",
                        connector=btype,
                        direction="outbound",
                        context=context,
                        evidence=[f"{source}:{btype}"],
                    )
                )

        return relationships, routes

    def _relationships_from_attribute(
        self,
        attr: str,
        args: str,
        func_name: str,
        text: str,
        path: Path,
        start: int,
        end: int,
    ) -> Tuple[List[Dict[str, Any]], List[Tuple[str, str]]]:
        attr_lower = attr.lower()
        rels: List[Dict[str, Any]] = []
        routes: List[Tuple[str, str]] = []

        if attr_lower.startswith("httptrigger"):
            methods = self._parse_http_methods(args)
            route = self._extract_named_arg(args, "Route") or self._extract_named_arg(args, "RouteTemplate") or func_name
            auth = self._extract_auth_level(args)
            cleaned_route = self._clean_route(route)
            context = {"route": cleaned_route, "auth_level": auth}
            route_params = self._route_parameters(cleaned_route)
            if route_params:
                context["route_params"] = route_params
            rels.append(
                self._build_relationship(
                    source={"type": "trigger", "name": "http", "step_id": "httptrigger"},
                    target={"kind": "function", "ref": func_name, "display": func_name},
                    operation="receives",
                    connector="httptrigger",
                    direction="inbound",
                    context=context,
                    evidence=[self._line_span(path, text, start, end)],
                )
            )
            for method in methods or ["GET"]:
                routes.append((method, cleaned_route))

        else:
            kind, ref, display, context = self._binding_target_from_args(attr_lower, args)
            if not kind or not ref:
                return rels, route_info
            direction = "inbound" if attr_lower.endswith("trigger") else "outbound"
            op = "receives" if direction == "inbound" else ("publishes" if kind in {"queue", "servicebus"} else "writes")
            rels.append(
                self._build_relationship(
                    source={"type": "trigger" if direction == "inbound" else "function", "name": func_name, "step_id": attr_lower},
                    target={"kind": kind, "ref": ref, "display": display or ref},
                    operation=op,
                    connector=attr_lower,
                    direction=direction,
                    context=context,
                    evidence=[self._line_span(path, text, start, end)],
                )
            )
        return rels, routes

    def _binding_target(self, btype: str, binding: Dict[str, Any]) -> Tuple[Optional[str], Optional[str], Optional[str], Dict[str, Any]]:
        btype_lower = btype.lower()
        context: Dict[str, Any] = {}
        if "queue" in btype_lower:
            ref = binding.get("queueName") or binding.get("queue_name")
            return "queue", ref, ref, context
        if "servicebus" in btype_lower:
            ref = binding.get("queueName") or binding.get("topicName")
            return "servicebus", ref, ref, context
        if "blob" in btype_lower:
            ref = binding.get("path") or binding.get("containerName")
            context["path"] = ref
            return "storage", ref, ref, context
        if "cosmosdb" in btype_lower:
            ref = binding.get("collectionName") or binding.get("containerName")
            context["database"] = binding.get("databaseName")
            return "cosmosdb", ref, ref, context
        if "timer" in btype_lower:
            schedule = binding.get("schedule") or binding.get("scheduleExpression") or binding.get("expression")
            context["schedule"] = schedule
            return "timer", "schedule", schedule, context
        return None, None, None, context

    def _binding_target_from_args(self, attr: str, args: str) -> Tuple[Optional[str], Optional[str], Optional[str], Dict[str, Any]]:
        context: Dict[str, Any] = {}
        literals = re.findall(r'"([^"]+)"', args)
        first_literal = literals[0] if literals else None
        if "queue" in attr:
            return "queue", first_literal, first_literal, context
        if "servicebus" in attr:
            return "servicebus", first_literal, first_literal, context
        if "blob" in attr:
            context["path"] = first_literal
            return "storage", first_literal, first_literal, context
        if "cosmosdb" in attr:
            collection = self._extract_named_arg(args, "CollectionName") or first_literal
            context["database"] = self._extract_named_arg(args, "DatabaseName")
            return "cosmosdb", collection, collection, context
        if "timer" in attr:
            schedule = first_literal or self._extract_named_arg(args, "Schedule") or self._extract_named_arg(args, "ScheduleExpression")
            context["schedule"] = schedule
            return "timer", "schedule", schedule, context
        return None, None, None, context

    def _build_relationship(
        self,
        *,
        source: Dict[str, Any],
        target: Dict[str, Any],
        operation: str,
        connector: str,
        direction: str,
        context: Optional[Dict[str, Any]] = None,
        evidence: Optional[List[str]] = None,
    ) -> Dict[str, Any]:
        hash_input = f"{source.get('name')}-{target.get('ref')}-{connector}-{operation}"
        digest = hashlib.sha1(hash_input.encode("utf-8")).hexdigest()[:8]
        rel_id = f"{(source.get('name') or source.get('type') or 'func').lower()}_{digest}"
        return {
            "id": rel_id.lower(),
            "source": source,
            "target": target,
            "operation": {"type": operation, "verb": "", "crud": "", "protocol": "https" if target.get("kind") == "http" else "service"},
            "connector": connector,
            "direction": direction,
            "context": context or {},
            "roles": self._roles_for_kind(target.get("kind"), operation),
            "evidence": evidence or [],
            "confidence": 0.85,
        }

    def _roles_for_kind(self, kind: Optional[str], operation: str) -> List[str]:
        if kind in {"queue", "servicebus"}:
            return ["messaging.publish" if operation == "publishes" else "messaging.consume"]
        if kind in {"storage", "cosmosdb"}:
            return ["data.mutates" if operation == "writes" else "data.reads"]
        if kind == "timer":
            return ["schedule.trigger"]
        if kind == "function":
            return ["interface.receive"]
        return []

    def _parse_http_methods(self, args: str) -> List[str]:
        return [m.upper() for m in re.findall(r'"(get|post|put|delete|patch|head|options)"', args, flags=re.IGNORECASE)]

    def _extract_named_arg(self, args: str, key: str) -> Optional[str]:
        pattern = re.compile(rf"{key}\s*=\s*\"(?P<val>[^\"]+)\"", re.IGNORECASE)
        match = pattern.search(args)
        return match.group("val") if match else None

    def _extract_auth_level(self, args: str) -> Optional[str]:
        match = re.search(r"AuthorizationLevel\.(\w+)", args)
        return match.group(1) if match else None

    def _clean_route(self, route: str) -> str:
        if not route:
            return "/"
        route = route.strip()
        if not route.startswith("/"):
            route = "/" + route
        return route.replace("//", "/")

    def _line_span(self, path: Path, text: str, start: int, end: int) -> str:
        start_line = text[:start].count("\n") + 1
        end_line = text[:end].count("\n") + 1
        return f"{path}:{start_line}-{end_line}"

    def _route_parameters(self, route: str) -> List[str]:
        return [param.strip() for param in re.findall(r"\{([^}]+)\}", route or "")]
