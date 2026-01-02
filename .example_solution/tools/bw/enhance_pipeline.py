#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
enhance_pipeline.py — Adds post-processing steps to the pipeline:
1. related_docs.py: Append "Related Documents" sections to Markdown files in out/docs.
2. enhance_visuals.py: Enhance Markdown files with MkDocs Material–compatible visuals/icons,
   while skipping sections that contain embedded .svg image links.
"""

import os
import sys
import logging
import json
import re
from pathlib import Path
import yaml

# Configure logging
log_path = Path("out/logs/enhance_pipeline.log")
log_path.parent.mkdir(parents=True, exist_ok=True)
logging.basicConfig(
    filename=log_path,
    filemode="a",
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
)
console = logging.StreamHandler(sys.stdout)
console.setLevel(logging.INFO)
logging.getLogger().addHandler(console)

try:
    import openai
except ImportError:
    openai = None

def _resolve_repo_root() -> Path:
    return Path(__file__).resolve().parents[3]

def _load_llm_config(repo_root: Path, cli_model: str = "") -> dict:
    cfg = {}
    for p in [repo_root / ".roo" / "config.yaml", repo_root / "config.yaml"]:
        if p.exists():
            try:
                cfg = yaml.safe_load(p.read_text(encoding="utf-8-sig")) or {}
                break
            except Exception:
                pass
    provider = (cfg.get("provider") or "openai").lower()
    model = cli_model or cfg.get("model") or os.environ.get("OPENAI_MODEL") or "gpt-5-chat-latest"
    return {"provider": provider, "model": model}

def _call_llm_openai(messages: list, model: str, max_tokens=1600, temperature=0.2) -> str:
    """Match the working pattern from dox_follow_plan.py"""
    try:
        import openai  # type: ignore
        client = openai.OpenAI()  # relies on OPENAI_API_KEY env var
        resp = client.chat.completions.create(
            model=model, temperature=temperature, max_tokens=max_tokens, messages=messages
        )
        return (resp.choices[0].message.content or "").strip()
    except Exception as e:
        logging.error(f"LLM call failed: {e}")
        return f"**[LLM ERROR]** {e}"

def call_llm(prompt: str, model: str = "gpt-5-chat-latest", max_tokens: int = 1200) -> str:
    if not openai:
        logging.error("OpenAI not installed. Cannot call LLM.")
        return ""
    messages = [{"role": "user", "content": prompt}]
    return _call_llm_openai(messages, model=model, max_tokens=max_tokens, temperature=0.2)

def _clean_json_output(raw: str) -> str:
    cleaned = re.sub(r"^```(?:json)?", "", raw.strip(), flags=re.IGNORECASE | re.MULTILINE)
    cleaned = re.sub(r"```$", "", cleaned, flags=re.MULTILINE)
    m = re.search(r"\{.*\}", cleaned, flags=re.DOTALL)
    if m:
        return m.group(0)
    return cleaned

def related_docs_step(docs_dir: Path, model: str):
    """Append Related Documents section if missing."""
    for md_file in docs_dir.rglob("*.md"):
        text = md_file.read_text(encoding="utf-8")
        if "## Related Documents" in text:
            continue
        prompt = f"Analyze the following Markdown document and suggest a list of related documents (filenames only) from the same directory. Output JSON: {{'related': ['file1.md','file2.md']}}.\n\n{text[:4000]}"
        resp = call_llm(prompt, model=model)
        try:
            parsed = json.loads(_clean_json_output(resp))
            related = parsed.get("related", [])
        except Exception as e:
            logging.error(f"Failed to parse related docs JSON: {e}\nRaw output:\n{resp}")
            related = []
        if related:
            with open(md_file, "a", encoding="utf-8") as f:
                f.write("\n\n## Related Documents\n")
                for r in related:
                    f.write(f"- [{r}]({r})\n")
            logging.info(f"Appended Related Documents to {md_file}")

def enhance_visuals_step(docs_dir: Path, model: str):
    """Enhance Markdown with MkDocs Material visuals/icons, skipping .svg image sections."""
    for md_file in docs_dir.rglob("*.md"):
        text = md_file.read_text(encoding="utf-8")

        # Split into blocks separated by image lines with .svg
        blocks = re.split(r"(\!\[.*?\]\(.*?\.svg\))", text, flags=re.IGNORECASE)
        enhanced_blocks = []

        for block in blocks:
            if re.match(r"\!\[.*?\]\(.*?\.svg\)", block, flags=re.IGNORECASE):
                # Preserve image block exactly
                enhanced_blocks.append(block)
                continue
            if not block.strip():
                enhanced_blocks.append(block)
                continue

            # Enhance non-image text blocks
            sections = block.split("\n## ")
            enhanced_sections = []
            for i, sec in enumerate(sections):
                prefix = "## " if i > 0 else ""
                prompt = f"Enhance the following Markdown section with MkDocs Material–compatible visuals/icons. Preserve all text exactly, only add icons/tables/emojis where appropriate.\n\n{sec[:4000]}"
                resp = call_llm(prompt, model=model, max_tokens=1500)
                enhanced_sections.append(prefix + (resp if resp else sec))
            enhanced_blocks.append("\n".join(enhanced_sections))

        new_text = "".join(enhanced_blocks)
        md_file.write_text(new_text, encoding="utf-8")
        logging.info(f"Enhanced visuals in {md_file}")

def main():
    repo_root = _resolve_repo_root()
    cfg = _load_llm_config(repo_root)
    model = cfg["model"]

    docs_dir = repo_root / "out" / "docs"
    if not docs_dir.exists():
        logging.error("out/docs directory not found.")
        return 1
    logging.info("Starting related_docs step...")
    related_docs_step(docs_dir, model)
    logging.info("Starting enhance_visuals step...")
    enhance_visuals_step(docs_dir, model)
    logging.info("Enhancement pipeline complete.")
    return 0

if __name__ == "__main__":
    sys.exit(main())