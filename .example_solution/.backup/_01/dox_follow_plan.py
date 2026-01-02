#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
dox_follow_plan.py — fulfill dox_draft_plan.md by generating business docs (+ diagrams).
"""

import os, sys, json, time, argparse, textwrap, re, subprocess, tempfile, datetime, logging
from pathlib import Path
from typing import Dict, List, Tuple
import yaml

# Configure logging
log_path = Path("out/logs/dox_runner.log")
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

_FRONT_RE = re.compile(r"^\ufeff?\s*---\s*\r?\n(?P<yaml>.*?\r?\n)---\s*(?:\r?\n|$)", re.DOTALL)

def _read_text(p: Path) -> str:
    return p.read_text(encoding="utf-8-sig")

def _write_text(p: Path, text: str) -> None:
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(text, encoding="utf-8")

def _parse_front_matter(md_text: str):
    m = _FRONT_RE.match(md_text)
    if not m:
        return {}, md_text
    header_txt = m.group("yaml")
    body = md_text[m.end():]
    try:
        header = yaml.safe_load(header_txt) or {}
    except Exception:
        header = {}
    return header, body

def _render_front_matter(header: dict, body: str) -> str:
    yml = yaml.safe_dump(header, sort_keys=False).strip()
    return f"---\n{yml}\n---\n\n{body}"

def _resolve_repo_root() -> Path:
    return Path(__file__).resolve().parents[3]

def _load_llm_config(repo_root: Path, cli_model: str = "") -> Dict:
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

def _call_llm_openai(messages: List[Dict], model: str, max_tokens=1600, temperature=0.2) -> str:
    try:
        import openai  # type: ignore
        client = openai.OpenAI()
        resp = client.chat.completions.create(
            model=model, temperature=temperature, max_tokens=max_tokens, messages=messages
        )
        return (resp.choices[0].message.content or "").strip()
    except Exception as e:
        logging.error(f"LLM call failed: {e}")
        return f"**[LLM ERROR]** {e}"

def _gather_inputs_text(base_dir: Path, names: List[str]) -> str:
    blobs = []
    for n in names or []:
        p = base_dir / n
        if p.exists():
            try:
                blobs.append(f"\n\n# SOURCE: {n}\n" + p.read_text(encoding='utf-8')[:50000])
            except Exception:
                blobs.append(f"\n\n# SOURCE: {n}\n[unreadable]")
        else:
            blobs.append(f"\n\n# SOURCE: {n}\n[missing]")
    return "\n".join(blobs)

def _make_doc_prompt(title: str, inputs_blob: str) -> List[Dict]:
    user = textwrap.dedent(f"""
    Create a business-facing document titled "{title}".
    - Audience: non-technical stakeholders; avoid jargon; be specific and plain-language.
    - Use concrete names (process names, REST methods/endpoints if visible, verbs like Search/Get/Sort).
    - Summarize end-to-end flows (inputs → actions → outputs).
    - Call out interdependencies and data touchpoints explicitly.
    - Include a short "Key Questions this answers" list.
    - If unknown, say "Unknown".

    Sources:
    {inputs_blob}
    """).strip()
    return [
        {"role": "system", "content": "You write clear, business-facing docs. Prefer active voice and plain English."},
        {"role": "user", "content": user},
    ]

def _slug(s: str) -> str:
    s = re.sub(r"[^a-zA-Z0-9\-_. ]", "", s).strip().replace(" ", "-")
    return s.lower() or "doc"

def _make_diagram_prompt(doc_title: str, doc_markdown: str) -> List[Dict]:
    user = textwrap.dedent(f"""
    Read the document below and propose one or more simple system/data flow diagrams that visualize the described flows.
    Output JSON (you may wrap it in a ```json code block if needed) with this schema:
    {{
      "diagrams": [
        {{
          "name": "<short-name>",
          "dot": "digraph G {{ rankdir=LR; node [shape=box, style=rounded]; ... }}"
        }}
      ]
    }}
    Rules:
    - Use Graphviz DOT syntax. Prefer rankdir=LR, rounded box nodes, labeled edges.
    - Keep diagrams concise: 5–12 nodes per diagram typical.
    - Base the diagrams SOLELY on this document (do not invent endpoints).
    - If no clear flow exists, return {{ "diagrams": [] }}.

    Document title: {doc_title}

    Document:
    {doc_markdown[:45000]}
    """).strip()
    return [
        {"role": "system", "content": "You convert prose into small, accurate Graphviz DOT diagrams."},
        {"role": "user", "content": user},
    ]

def _run_dot(dot_text: str, out_svg: Path) -> bool:
    exe = os.environ.get("DOT_PATH") or "dot"
    out_svg.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.NamedTemporaryFile("w", delete=False, suffix=".dot", encoding="utf-8") as tf:
        tf.write(dot_text)
        tmpdot = tf.name
    try:
        subprocess.run([exe, "-Tsvg", tmpdot, "-o", str(out_svg)], check=True)
        logging.info(f"SVG generated: {out_svg}")
        return True
    except Exception as e:
        logging.error(f"Graphviz render failed: {e}")
        return False
    finally:
        try:
            os.unlink(tmpdot)
        except Exception:
            pass

def _append_diagrams(doc_path: Path, diagrams: List[Dict]) -> None:
    if not diagrams:
        return
    md = doc_path.read_text(encoding="utf-8")
    lines = [md, "\n\n## Visual Flow Diagrams\n"]
    for d in diagrams:
        svg_name = f"{_slug(doc_path.stem)}-{_slug(d.get('name','flow'))}.svg"
        lines.append(f"**{d.get('name','Flow')}**\n\n![Flow](../graphs/{svg_name})\n")
    _write_text(doc_path, "\n".join(lines))

def _clean_json_output(raw: str) -> str:
    cleaned = re.sub(r"^```(?:json)?", "", raw.strip(), flags=re.IGNORECASE | re.MULTILINE)
    cleaned = re.sub(r"```$", "", cleaned, flags=re.MULTILINE)
    m = re.search(r"\{.*\}", cleaned, flags=re.DOTALL)
    if m:
        return m.group(0)
    return cleaned

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--plan", required=True, type=Path)
    ap.add_argument("--out", required=True, type=Path)
    ap.add_argument("--docs-per-round", type=int, default=3)
    ap.add_argument("--model", type=str, default="")
    args = ap.parse_args()

    repo_root = _resolve_repo_root()
    cfg = _load_llm_config(repo_root, args.model)
    provider, model = cfg["provider"], cfg["model"]

    raw = _read_text(args.plan)
    header, body = _parse_front_matter(raw)
    docs = header.get("docs") or []

    if not docs:
        logging.warning("No 'docs' YAML found in plan; nothing to do.")
        return 0

    args.out.mkdir(parents=True, exist_ok=True)
    pending = [d for d in docs if d and d.get("filename") and not (args.out / d["filename"]).exists() and d.get("status") != "done"]
    to_make = pending[: max(1, args.docs_per_round)] if pending else []

    base = args.out
    for spec in to_make:
        title = spec.get("title", "Untitled")
        filename = spec.get("filename") or f"{_slug(title)}.md"
        inputs = spec.get("inputs", [])
        inputs_blob = _gather_inputs_text(base, inputs)
        logging.info(f"Generating doc: {filename} model={model}")

        if provider != "openai":
            content = f"[Unsupported provider {provider}]"
        else:
            content = _call_llm_openai(_make_doc_prompt(title, inputs_blob), model=model, max_tokens=2200, temperature=0.2)

        out_path = base / filename
        clean_content = re.sub(r"(?i)would you like me to.*$", "", content.strip())
        _write_text(out_path, clean_content)
        logging.info(f"Doc written: {out_path}")

        diagrams: List[Dict] = []
        if provider == "openai":
            diag_json = _call_llm_openai(_make_diagram_prompt(title, clean_content), model=model, max_tokens=1200, temperature=0.0)
            try:
                parsed = json.loads(_clean_json_output(diag_json))
                diagrams = parsed.get("diagrams") or []
            except Exception as e:
                logging.error(f"Diagram JSON parse failed: {e}\nRaw output:\n{diag_json}")
                diagrams = []

        if not diagrams and provider == "openai":
            logging.info("Retrying diagram generation with fallback prompt")
            diag_json2 = _call_llm_openai(
                [
                    {"role": "system", "content": "You are a technical illustrator. Convert the following document into at least one accurate Graphviz DOT diagram that visualizes the workflows, data flows, or interactions described. Output JSON with diagrams[].dot (you may wrap in ```json)."},
                    {"role": "user", "content": f"Document title: {title}\n\nDocument:\n{clean_content}"}
                ],
                model=model,
                max_tokens=1200,
                temperature=0.0
            )
            try:
                parsed2 = json.loads(_clean_json_output(diag_json2))
                diagrams = parsed2.get("diagrams") or []
            except Exception as e:
                logging.error(f"Fallback diagram JSON parse failed: {e}\nRaw output:\n{diag_json2}")
                diagrams = []

        valid_svgs = 0
        for d in diagrams:
            dot = d.get("dot") or ""
            name = d.get("name") or "flow"
            if "digraph" not in dot:
                logging.warning(f"Skipping invalid diagram (no digraph): {name}")
                continue
            svg_path = Path("out/graphs") / f"{_slug(Path(filename).stem)}-{_slug(name)}.svg"
            if _run_dot(dot, svg_path):
                valid_svgs += 1

        if valid_svgs > 0:
            _append_diagrams(out_path, diagrams)
            logging.info(f"Appended {valid_svgs} diagrams to {filename}")
            spec["status"] = "done"
            spec["generated_at"] = time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())
        else:
            logging.error(f"No valid diagrams generated for {filename}. Doc will not be marked done.")

    meta = header.setdefault("meta", {})
    if isinstance(meta.get("remaining_execs"), int) and meta["remaining_execs"] > 0:
        meta["remaining_execs"] -= 1

    plan_text = _render_front_matter(header, body)
    _write_text(Path(args.plan), plan_text)

    left = [d for d in header.get("docs", []) if d and d.get("filename") and not (args.out / d["filename"]).exists()]
    logging.info(f"Round complete. Remaining planned docs: {len(left)}. remaining_execs={meta.get('remaining_execs')}.")
    return 0

if __name__ == "__main__":
    sys.exit(main())
