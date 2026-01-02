from __future__ import annotations

import textwrap
import xml.etree.ElementTree as ET
from pathlib import Path
from typing import Any, Dict, List, Optional

from autodocx.llm.provider import call_openai_meta

PLAN_PROMPT = textwrap.dedent(
    """
    You design a documentation plan for an engineering knowledge base.
    Using the supplied README excerpt, repo tree summary, and component list,
    produce concise XML describing 3-5 wiki pages that would help business
    stakeholders understand the system.

    Requirements:
    - Output ONLY XML using the structure:
      <docPlan repo="...">
        <page slug="orders-overview" title="Orders Overview">
          <section title="Current Workflows" />
          <section title="Risks" />
        </page>
      </docPlan>
    - Choose informative slugs (kebab-case) and 2-4 sections per page.
    - Prefer pages that map to constellations/components mentioned in the inputs.
    """
).strip()


RAG_PROMPT = textwrap.dedent(
    """
    You are authoring a Markdown knowledge article using retrieved repository snippets.
    Follow these rules:
    - Start with "# {page.title}".
    - Create sections matching `page.sections` in the provided order (use level-2 headings).
    - Use the `retrieved_context` entries as primary evidence. When referencing one, cite `(source: {cite})`.
    - When referencing an evidence packet, cite `(evidence: path/to/packet.json)`.
    - Keep the tone business-friendly and explain *why* facts matter.
    - Never hallucinate; if information is missing, explicitly state the limitation.
    """
).strip()


def generate_xml_doc_plan(
    repo_root: Path,
    out_dir: Path,
    doc_context: Dict[str, Any],
    *,
    llm_callable=None,
) -> Path:
    readme = _read_file(repo_root / "README.md", max_chars=4000)
    tree_summary = _summarize_repo_tree(repo_root)
    payload = {
        "readme": readme,
        "repo_tree": tree_summary,
        "components": sorted(doc_context.get("components", {}).keys()),
        "constellations": sorted((doc_context.get("constellations") or {}).keys()),
    }
    responder = llm_callable or call_openai_meta
    response = responder(PLAN_PROMPT, payload)
    xml_text = response.get("text", "").strip()
    if not xml_text.startswith("<"):
        xml_text = f"<docPlan>\n{xml_text}\n</docPlan>"
    plan_path = out_dir / "doc_draft_plan.xml"
    plan_path.write_text(xml_text, encoding="utf-8")
    # ensure it parses (raise early if malformed)
    ET.fromstring(xml_text)
    return plan_path


def build_rag_docs(
    plan_path: Path,
    embedding_service,
    out_dir: Path,
    doc_context: Dict[str, Any],
    *,
    llm_callable=None,
    top_k: int = 6,
) -> List[Path]:
    pages = _parse_plan(plan_path)
    if not pages:
        return []
    rag_dir = out_dir / "docs" / CURATED_DIR / "rag"
    rag_dir.mkdir(parents=True, exist_ok=True)
    generated_paths: List[Path] = []
    constellations = doc_context.get("constellations", {})
    responder = llm_callable or call_openai_meta
    for page in pages:
        query = f"{page['title']} {' '.join(page['sections'])}"
        retrieved = embedding_service.query(query, top_k=top_k)
        retrieved_context = [
            {
                "cite": f"{hit['path']}#L{hit['start_line']}-L{hit['end_line']}",
                "text": hit["text"],
                "component": hit.get("component"),
                "score": hit.get("score"),
            }
            for hit in retrieved
        ]
        packet_refs = _packets_for_components(constellations, retrieved_context)
        payload = {
            "page": {"title": page["title"], "sections": page["sections"]},
            "retrieved_context": retrieved_context,
            "evidence_packets": packet_refs,
        }
        response = responder(RAG_PROMPT, payload)
        body = response.get("text", "").strip()
        md_path = rag_dir / f"{page['slug']}.md"
        fm = [
            "---",
            f'title: "{page["title"]}"',
            "source: rag",
            "---",
            "",
        ]
        md_path.write_text("\n".join(fm + [body, ""]), encoding="utf-8")
        generated_paths.append(md_path)
    return generated_paths


# ---------------------------------------------------------------------- #
# Helpers
# ---------------------------------------------------------------------- #
CURATED_DIR = "curated"


def _read_file(path: Path, *, max_chars: int) -> str:
    if not path.exists():
        return ""
    return path.read_text(encoding="utf-8", errors="ignore")[:max_chars]


def _summarize_repo_tree(repo_root: Path, *, max_entries: int = 200) -> str:
    entries: List[str] = []
    for idx, path in enumerate(sorted(repo_root.rglob("*"))):
        if idx >= max_entries:
            break
        rel = path.relative_to(repo_root)
        if rel.parts and rel.parts[0].startswith(".git"):
            continue
        if path.is_dir():
            entries.append(f"[D] {rel}")
        else:
            entries.append(f"[F] {rel}")
    return "\n".join(entries)


def _parse_plan(plan_path: Path) -> List[Dict[str, Any]]:
    tree = ET.parse(plan_path)
    root = tree.getroot()
    pages: List[Dict[str, Any]] = []
    for page in root.findall(".//page"):
        title = page.attrib.get("title") or page.attrib.get("name")
        slug = page.attrib.get("slug") or _slug(title or "page")
        sections = [sec.attrib.get("title") for sec in page.findall("section") if sec.attrib.get("title")]
        sections = [s for s in sections if s]
        if not title or not sections:
            continue
        pages.append({"title": title, "slug": slug, "sections": sections})
    return pages


def _slug(value: str) -> str:
    cleaned = "".join(ch.lower() if ch.isalnum() else "-" for ch in value)
    cleaned = "-".join(filter(None, cleaned.split("-")))
    return cleaned or "page"


def _packets_for_components(constellations: Dict[str, Any], retrieved_context: List[Dict[str, Any]]) -> List[Dict[str, str]]:
    components = {ctx.get("component") for ctx in retrieved_context if ctx.get("component")}
    packets: List[Dict[str, str]] = []
    for cid, record in constellations.items():
        comps = set(record.get("components") or [])
        if not comps & components:
            continue
        packet_rel = record.get("evidence_packet")
        if not packet_rel:
            continue
        packets.append(
            {
                "slug": record.get("slug") or cid,
                "path": packet_rel,
            }
        )
    return packets
