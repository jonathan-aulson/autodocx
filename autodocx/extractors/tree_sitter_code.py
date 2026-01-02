from __future__ import annotations

from pathlib import Path
from typing import Dict, Iterable, List, Optional
import re

try:
    from tree_sitter import Node  # type: ignore
except ImportError:  # pragma: no cover
    Node = None  # type: ignore

from autodocx.tree_sitter_support import (
    create_parser,
    language_for_path,
    slice_text,
    tree_sitter_available,
)
from autodocx.types import Signal


class TreeSitterCodeExtractor:
    name = "tree_sitter_code"
    patterns = ["**/*.py", "**/*.cs", "**/*.js", "**/*.ts", "**/*.tsx", "**/*.jsx"]

    ENTITY_NODE_MAP: Dict[str, Dict[str, str]] = {
        "python": {
            "function_definition": "function",
            "class_definition": "class",
        },
        "c_sharp": {
            "class_declaration": "class",
            "interface_declaration": "interface",
            "method_declaration": "method",
        },
        "javascript": {
            "function_declaration": "function",
            "method_definition": "method",
            "class_declaration": "class",
        },
        "typescript": {
            "function_declaration": "function",
            "method_definition": "method",
            "class_declaration": "class",
        },
        "tsx": {
            "function_declaration": "function",
            "method_definition": "method",
            "class_declaration": "class",
        },
    }
    BUSINESS_VERB_BANK = {
        "process",
        "calculate",
        "invoice",
        "bill",
        "charge",
        "refund",
        "assign",
        "approve",
        "authorize",
        "validate",
        "notify",
        "email",
        "dispatch",
        "schedule",
        "sync",
        "synchronize",
        "import",
        "export",
        "reconcile",
        "audit",
        "archive",
        "provision",
        "enrich",
        "integrate",
        "register",
        "escalate",
        "track",
        "report",
    }

    def detect(self, repo: Path) -> bool:
        if not tree_sitter_available():
            return False
        return any(repo.glob("**/*.py")) or any(repo.glob("**/*.cs")) or any(repo.glob("**/*.ts")) or any(repo.glob("**/*.js"))

    def discover(self, repo: Path) -> Iterable[Path]:
        if not tree_sitter_available():
            return []
        for pattern in self.patterns:
            for candidate in repo.glob(pattern):
                if candidate.is_file():
                    yield candidate

    def extract(self, path: Path) -> Iterable[Signal]:
        if not tree_sitter_available() or Node is None:
            return []
        lang_name = language_for_path(path)
        if not lang_name or lang_name not in self.ENTITY_NODE_MAP:
            return []

        try:
            code = path.read_text(encoding="utf-8", errors="ignore")
        except Exception:
            return []

        try:
            parser = create_parser(lang_name)
        except RuntimeError:
            return []
        tree = parser.parse(code.encode("utf-8"))

        entities = self._collect_entities(lang_name, tree.root_node, code.encode("utf-8"))
        signals: List[Signal] = []
        for entity in entities:
            start_line = entity["start_point"][0] + 1
            end_line = entity["end_point"][0] + 1
            datastore_hints = self._infer_datastore_hints(entity)
            service_hints = self._infer_service_hints(entity)
            props = {
                "name": entity["name"],
                "entity_type": entity["entity_type"],
                "language": lang_name,
                "file": str(path),
                "docstring": entity.get("docstring", ""),
                "start_line": start_line,
                "end_line": end_line,
                "business_verbs": entity.get("business_verbs", []),
            }
            if datastore_hints:
                props["datasource_tables"] = datastore_hints
            if service_hints:
                props["service_dependencies"] = service_hints
                props["process_calls"] = service_hints
            signals.append(
                Signal(
                    kind="code_entity",
                    props=props,
                    evidence=[f"{path}:{start_line}-{end_line}"],
                    subscores={"parsed": 0.8},
                )
            )
        return signals

    def _collect_entities(self, lang_name: str, root: Node, code_bytes: bytes) -> List[Dict[str, any]]:
        entities: List[Dict[str, any]] = []
        stack = [root]
        target_types = self.ENTITY_NODE_MAP.get(lang_name, {})

        while stack:
            node = stack.pop()
            node_type = node.type
            if node_type in target_types:
                entity = self._build_entity(lang_name, node, target_types[node_type], code_bytes)
                if entity:
                    entities.append(entity)
            stack.extend(list(node.children))
        return entities

    def _build_entity(self, lang_name: str, node: Node, entity_type: str, code_bytes: bytes) -> Optional[Dict[str, any]]:
        name_node = self._find_identifier(lang_name, node)
        if name_node is None:
            return None
        name = slice_text(code_bytes, name_node.start_byte, name_node.end_byte).strip()
        doc = ""
        if lang_name == "python" and entity_type in {"function", "class"}:
            doc = self._python_docstring(node, code_bytes)
        elif lang_name in {"javascript", "typescript", "tsx", "c_sharp"}:
            doc = self._leading_comment_text(node, code_bytes)
        verbs = self._infer_business_verbs(name, doc)
        return {
            "name": name,
            "entity_type": entity_type,
            "start_point": node.start_point,
            "end_point": node.end_point,
            "docstring": doc,
            "business_verbs": verbs,
        }

    def _find_identifier(self, lang_name: str, node: Node) -> Optional[Node]:
        if lang_name in {"python", "c_sharp", "javascript", "typescript", "tsx"}:
            for child in node.children:
                if child.type == "identifier":
                    return child
        return None

    def _python_docstring(self, node: Node, code_bytes: bytes) -> str:
        block = None
        for child in node.children:
            if child.type == "block":
                block = child
                break
        if block is None or not block.children:
            return ""
        first_stmt = block.children[0]
        if first_stmt.type in {"expression_statement", "string"}:
            text = slice_text(code_bytes, first_stmt.start_byte, first_stmt.end_byte).strip()
            if text.startswith(('"""', "'''", '"', "'")):
                return text.strip('"\'' + "\n ")
        return ""

    def _leading_comment_text(self, node: Node, code_bytes: bytes) -> str:
        sibling = node.prev_sibling
        comments: List[str] = []
        while sibling and sibling.type == "comment":
            comment_text = slice_text(code_bytes, sibling.start_byte, sibling.end_byte).strip()
            comments.insert(0, comment_text.lstrip("/ ").lstrip("* ").strip())
            sibling = sibling.prev_sibling
        return "\n".join(comments).strip()

    def _infer_business_verbs(self, name: str, docstring: str) -> List[str]:
        tokens = self._split_identifier(name)
        tokens.extend(self._split_identifier(docstring))
        verbs: List[str] = []
        for token in tokens:
            lower = token.lower()
            if lower in self.BUSINESS_VERB_BANK and lower not in verbs:
                verbs.append(lower)
        return verbs[:5]

    DATASTORE_STOPWORDS = {"and", "or", "to", "the", "a", "an", "of", "for", "from"}

    def _infer_datastore_hints(self, entity: Dict[str, any]) -> List[str]:
        hints: List[str] = []
        text = f"{entity.get('name') or ''} {entity.get('docstring') or ''}"
        for match in re.findall(r"(?:table|collection|dataset|queue)\s+([A-Za-z0-9_]+)", text, flags=re.IGNORECASE):
            token = match.strip("_")
            if token and token.lower() not in self.DATASTORE_STOPWORDS and token not in hints:
                hints.append(match)
        docstring_tokens = re.findall(r"[A-Za-z0-9_]+", entity.get("docstring") or "")
        for token in docstring_tokens:
            lower = token.lower()
            if lower.endswith(("table", "collection", "dataset", "queue")) and lower not in self.DATASTORE_STOPWORDS and token not in hints:
                hints.append(token)
        for token in re.findall(r"[A-Za-z0-9_]+", entity.get("name") or ""):
            lower = token.lower()
            if lower.endswith(("table", "repository", "repo", "store")) and token not in hints:
                hints.append(token)
        return hints[:5]

    def _infer_service_hints(self, entity: Dict[str, any]) -> List[str]:
        hints: List[str] = []
        doc = entity.get("docstring") or ""
        for url in re.findall(r"https?://[^\s)]+", doc):
            if url not in hints:
                hints.append(url)
        combined = f"{entity.get('name') or ''} {doc}"
        for token in re.findall(r"[A-Za-z0-9_]+", combined):
            lower = token.lower()
            if lower.endswith(("service", "client", "api")) and token not in hints:
                hints.append(token)
        return hints[:5]

    def _split_identifier(self, text: str) -> List[str]:
        if not text:
            return []
        camel_tokens = re.findall(r"[A-Z]?[a-z]+|[A-Z]+(?=[A-Z]|$)|\d+", text)
        snake_tokens = re.split(r"[^A-Za-z0-9]+", text)
        combined = camel_tokens + snake_tokens
        return [token for token in combined if token]
