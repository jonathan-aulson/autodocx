# autodocx/render/markdown_style.py
from __future__ import annotations

import re
from typing import Iterable, List

HEADING_ICON_MAP = {
    "executive summary": ":material-clipboard-text:",
    "workflow narrative": ":material-sitemap:",
    "interfaces and dependencies": ":material-api:",
    "key data handled": ":material-database:",
    "risks and follow ups": ":material-alert-circle:",
    "related documents": ":material-link-variant:",
    "overview": ":material-eye:",
    "summary": ":material-clipboard-check:",
    "components": ":material-cube-outline:",
    "process flows": ":material-sitemap:",
    "integration summary": ":material-link-variant:",
    "integration catalog": ":material-cloud-sync:",
    "ui entry points": ":material-monitor:",
    "glossary and roles": ":material-book-open-variant:",
    "process diagrams": ":material-vector-polyline:",
    "workflow diagrams": ":material-vector-polyline:",
    "technical appendix": ":material-clipboard-list:",
    "journey blueprints": ":material-map:",
    "ui snapshots": ":material-image:",
    "relationship highlights": ":material-chart-line:",
    "graph insights": ":material-chart-line:",
    "sequence snapshot": ":material-timeline:",
    "generated journey maps": ":material-map:",
    "component overview": ":material-view-dashboard-outline:",
    "interfaces exposed": ":material-api:",
    "invokes dependencies": ":material-call-merge:",
    "interdependency map": ":material-sitemap:",
    "key inputs and outputs": ":material-database:",
    "errors and logging": ":material-alert:",
    "error handling": ":material-bug:",
    "logging and telemetry": ":material-chart-line:",
    "extrapolations": ":material-lightbulb-on:",
    "packaging and artifacts": ":material-package-variant:",
    "traceability": ":material-link:",
    "evidence appendix": ":material-file-document:",
    "screens and apis they see": ":material-monitor:",
    "how users interact": ":material-account-group:",
    "analyst insights": ":material-magnify:",
    "coverage audit": ":material-shield-check:",
    "change log": ":material-history:",
    "runbooks and playbooks": ":material-book-open-variant:",
}

DEFAULT_HEADING_ICONS = {
    1: ":material-file-document-outline:",
    2: ":material-star:",
    3: ":material-chevron-right:",
    4: ":material-circle-small:",
    5: ":material-circle-small:",
    6: ":material-circle-small:",
}

ICON_REPLACEMENTS = {
    ":material-flow-branch:": ":material-source-branch:",
    ":material-bug-report:": ":material-bug:",
    ":material-flash-on:": ":material-flash:",
    ":material-info-outline:": ":material-information-outline:",
    ":material-library-books:": ":material-library:",
    ":material-input:": ":material-import:",
}

OUTRO_PATTERNS = [
    r"^[-*•]\\s*if you want\\b",
    r"^[-*•]\\s*if you'd like\\b",
    r"^[-*•]\\s*if you would like\\b",
    r"^[-*•]\\s*if you want me\\b",
    r"^[-*•]\\s*if you'd like me\\b",
    r"^[-*•]\\s*if you would like me\\b",
    r"^[-*•]\\s*i can\\b",
    r"^[-*•]\\s*let me know\\b",
    r"^[-*•]\\s*happy to\\b",
    r"^[-*•]\\s*feel free to\\b",
    r"^[-*•]\\s*would you like\\b",
    r"^[-*•]\\s*need me to\\b",
    r"^[-*•]\\s*next recommended actions\\b",
    r"^[-*•]\\s*next recommended action\\b",
    r"^if you want\\b",
    r"^if you'd like\\b",
    r"^if you would like\\b",
    r"^if you want me\\b",
    r"^if you'd like me\\b",
    r"^if you would like me\\b",
    r"^i can\\b",
    r"^let me know\\b",
    r"^happy to\\b",
    r"^feel free to\\b",
    r"^would you like\\b",
    r"^need me to\\b",
    r"^next recommended actions\\b",
    r"^next recommended action\\b",
]


def _normalize_heading(text: str) -> str:
    stripped = re.sub(r"\([^)]*\)", "", text)
    stripped = stripped.replace("&", " and ")
    stripped = re.sub(r"[^a-zA-Z0-9\\s/]+", " ", stripped)
    stripped = stripped.replace("/", " ")
    return re.sub(r"\s+", " ", stripped.strip().lower())


def _strip_heading_icon(text: str) -> str:
    cleaned = text.strip()
    cleaned = re.sub(r"^:[^:]+:\\s*", "", cleaned)
    parts = cleaned.split()
    if parts and any(ord(ch) > 127 for ch in parts[0]):
        cleaned = " ".join(parts[1:]) if len(parts) > 1 else ""
    return cleaned.strip() or text.strip()


def _heading_has_icon(text: str) -> bool:
    stripped = text.strip()
    if not stripped:
        return False
    if stripped.startswith(":") and ":" in stripped[1:]:
        return True
    first_token = stripped.split()[0]
    return any(ord(ch) > 127 for ch in first_token)


def decorate_headings(lines: Iterable[str]) -> List[str]:
    """
    Ensure every Markdown heading line includes a Material icon prefix.
    """
    out: List[str] = []
    in_front_matter = False
    in_code_fence = False
    for idx, line in enumerate(lines):
        if line.strip() == "---":
            if idx == 0 or in_front_matter:
                in_front_matter = not in_front_matter
            out.append(line)
            continue
        if line.strip().startswith("```"):
            in_code_fence = not in_code_fence
            out.append(line)
            continue
        if in_front_matter:
            out.append(line)
            continue
        if in_code_fence:
            out.append(line)
            continue
        for old, new in ICON_REPLACEMENTS.items():
            if old in line:
                line = line.replace(old, new)
        match = re.match(r"^(#{1,6})\\s+(.*)$", line)
        if not match:
            out.append(line)
            continue
        hashes, title = match.groups()
        cleaned = _strip_heading_icon(title)
        normalized = _normalize_heading(cleaned)
        icon = HEADING_ICON_MAP.get(normalized)
        if icon:
            out.append(f"{hashes} {icon} {cleaned}")
            continue
        if _heading_has_icon(title):
            out.append(line)
            continue
        icon = DEFAULT_HEADING_ICONS.get(len(hashes), ":material-star-outline:")
        out.append(f"{hashes} {icon} {cleaned}")
    return out


def decorate_markdown(text: str) -> str:
    lines = text.splitlines()
    return "\n".join(decorate_headings(lines))


def strip_llm_outro(text: str, *, max_lines_from_end: int = 20) -> str:
    lines = text.splitlines()
    if not lines:
        return text
    in_code_fence = False
    last_nonempty = -1
    for idx, line in enumerate(lines):
        if line.strip():
            last_nonempty = idx
    if last_nonempty == -1:
        return text
    cutoff = max(0, last_nonempty - max_lines_from_end)
    for idx, line in enumerate(lines):
        if line.strip().startswith("```"):
            in_code_fence = not in_code_fence
            continue
        if in_code_fence or idx < cutoff:
            continue
        stripped = line.strip()
        lower = stripped.lower()
        for pattern in OUTRO_PATTERNS:
            if re.search(pattern, lower):
                return "\n".join(lines[:idx]).rstrip()
    return text.rstrip()
