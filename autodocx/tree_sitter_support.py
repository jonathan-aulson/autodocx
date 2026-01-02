from __future__ import annotations

from functools import lru_cache
from pathlib import Path
from typing import Dict, Optional

try:
    from tree_sitter import Parser  # type: ignore
    from tree_sitter_languages import get_language  # type: ignore

    _HAS_SET_LANGUAGE = callable(getattr(Parser, "set_language", None))
    _HAS_LANGUAGE_PROP = hasattr(Parser, "language")
    TREE_SITTER_AVAILABLE = _HAS_SET_LANGUAGE or _HAS_LANGUAGE_PROP
except ImportError:
    Parser = None  # type: ignore
    get_language = None  # type: ignore
    TREE_SITTER_AVAILABLE = False


LANGUAGE_BY_EXTENSION: Dict[str, str] = {
    ".py": "python",
    ".cs": "c_sharp",
    ".js": "javascript",
    ".ts": "typescript",
    ".tsx": "tsx",
    ".jsx": "javascript",
    ".java": "java",
    ".go": "go",
}


def language_for_path(path: Path) -> Optional[str]:
    return LANGUAGE_BY_EXTENSION.get(path.suffix.lower())


@lru_cache(maxsize=None)
def _language_handle(lang_name: str):
    if not TREE_SITTER_AVAILABLE or get_language is None:
        raise RuntimeError("tree_sitter_languages is not installed")
    return get_language(lang_name)


def create_parser(lang_name: str) -> Parser:
    if not TREE_SITTER_AVAILABLE or Parser is None:
        raise RuntimeError("tree_sitter is not installed")
    parser = Parser()
    language = _language_handle(lang_name)
    if callable(getattr(parser, "set_language", None)):
        parser.set_language(language)
    else:
        setattr(parser, "language", language)
    return parser


def slice_text(source: bytes, start_byte: int, end_byte: int) -> str:
    return source[start_byte:end_byte].decode("utf-8", errors="ignore")


def tree_sitter_available() -> bool:
    return TREE_SITTER_AVAILABLE
