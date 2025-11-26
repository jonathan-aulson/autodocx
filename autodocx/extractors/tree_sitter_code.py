from __future__ import annotations

from pathlib import Path
from typing import Dict, Iterable, List, Optional

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
            signals.append(
                Signal(
                    kind="code_entity",
                    props={
                        "name": entity["name"],
                        "entity_type": entity["entity_type"],
                        "language": lang_name,
                        "file": str(path),
                        "docstring": entity.get("docstring", ""),
                        "start_line": start_line,
                        "end_line": end_line,
                    },
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
        return {
            "name": name,
            "entity_type": entity_type,
            "start_point": node.start_point,
            "end_point": node.end_point,
            "docstring": doc,
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
