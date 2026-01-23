# autodocx/render/markdown_style.py
from __future__ import annotations

import re
from typing import Iterable, List

HEADING_ICON_MAP = {
    "executive summary": ":material-clipboard-text:",
    "workflow narrative": ":material-sitemap:",
    "interfaces & dependencies": ":material-api:",
    "key data handled": ":material-database:",
    "risks & follow-ups": ":material-alert-circle:",
    "related documents": ":material-link-variant:",
    "overview": ":material-eye:",
    "summary": ":material-clipboard-check:",
    "components": ":material-cube-outline:",
    "process flows": ":material-sitemap:",
    "integration summary": ":material-link-variant:",
    "integration catalog": ":material-cloud-sync:",
    "ui entry points": ":material-monitor:",
    "glossary & roles": ":material-book-open-variant:",
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
    "invokes / dependencies": ":material-call-merge:",
    "interdependency map": ":material-sitemap:",
    "key inputs & outputs": ":material-database:",
    "errors & logging": ":material-alert:",
    "error handling": ":material-bug:",
    "logging & telemetry": ":material-chart-line:",
    "extrapolations": ":material-lightbulb-on:",
    "packaging & artifacts": ":material-package-variant:",
    "traceability": ":material-link:",
    "evidence appendix": ":material-file-document:",
    "screens and apis they see": ":material-monitor:",
    "how users interact": ":material-account-group:",
    "analyst insights": ":material-magnify:",
    "coverage audit": ":material-shield-check:",
    "change log": ":material-history:",
    "runbooks & playbooks": ":material-book-open-variant:",
}

DEFAULT_HEADING_ICONS = {
    1: ":material-file-document-outline:",
    2: ":material-star:",
    3: ":material-chevron-right:",
    4: ":material-circle-small:",
    5: ":material-circle-small:",
    6: ":material-circle-small:",
}


def _normalize_heading(text: str) -> str:
    return re.sub(r"\s+", " ", text.strip().lower())


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
        match = re.match(r"^(#{1,6})\\s+(.*)$", line)
        if not match:
            out.append(line)
            continue
        hashes, title = match.groups()
        if _heading_has_icon(title):
            out.append(line)
            continue
        normalized = _normalize_heading(title)
        icon = HEADING_ICON_MAP.get(normalized) or DEFAULT_HEADING_ICONS.get(len(hashes), ":material-star-outline:")
        out.append(f"{hashes} {icon} {title}")
    return out


def decorate_markdown(text: str) -> str:
    lines = text.splitlines()
    return "\n".join(decorate_headings(lines))
