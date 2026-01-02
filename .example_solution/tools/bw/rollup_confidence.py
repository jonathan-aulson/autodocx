#!/usr/bin/env python3
# .roo/tools/bw/rollup_confidence.py
# Aggregate & surface confidence scoring for high-level docs (Family_* and plan-generated docs).
#
# Usage:
#   python .roo/tools/bw/rollup_confidence.py \
#       --docs out/docs \
#       --sir out/sir/_interdeps.json \
#       --plan out/dox_draft_plan.md
#
# Behavior:
#   - Parses YAML front matter of all .md in out/docs recursively and indexes confidence scores.
#   - For each Family_* doc: finds its member process docs via _interdeps.json, aggregates their scores.
#   - For each plan-based doc (from dox_draft_plan.md): uses its `inputs` to aggregate scores.
#   - Inserts an inline single-line confidence summary under the title, and appends a rollup table at bottom.
#
# Idempotent: re-runs update-in-place safely using HTML comment markers.

import sys, argparse, json, re, statistics
from pathlib import Path
from typing import Dict, List, Tuple, Optional
import yaml

HERE = Path(__file__).resolve()
REPO_ROOT = HERE.parents[3]
DEFAULT_DOCS = REPO_ROOT / "out" / "docs"
DEFAULT_INTERDEPS = REPO_ROOT / "out" / "sir" / "_interdeps.json"
DEFAULT_PLAN = REPO_ROOT / "out" / "dox_draft_plan.md"

FRONT_RE = re.compile(r"^\ufeff?\s*---\s*\r?\n(?P<yml>.*?\r?\n)---\s*(?:\r?\n|$)", re.DOTALL)
INLINE_MARK = "<!-- CONFIDENCE_INLINE -->"
BLOCK_START = "<!-- CONFIDENCE_ROLLUP_START -->"
BLOCK_END = "<!-- CONFIDENCE_ROLLUP_END -->"

def read_text(p: Path) -> str:
    return p.read_text(encoding="utf-8-sig")

def write_text(p: Path, text: str) -> None:
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(text, encoding="utf-8")

def parse_front_matter(md_text: str) -> Tuple[Dict, str]:
    m = FRONT_RE.match(md_text)
    if not m:
        return {}, md_text
    yml = m.group("yml")
    body = md_text[m.end():]
    try:
        header = yaml.safe_load(yml) or {}
    except Exception:
        header = {}
    return header, body

def render_front_matter(header: Dict, body: str) -> str:
    yml = yaml.safe_dump(header, sort_keys=False).strip()
    return f"---\n{yml}\n---\n\n{body}"

def h1_span(body: str) -> Tuple[int, int]:
    """
    Return (start_index, end_index) of the first H1 line '# ...\n' inside body (no front matter).
    If not found, returns (-1, -1).
    """
    lines = body.splitlines(True)
    pos = 0
    for i, ln in enumerate(lines):
        if ln.lstrip().startswith("# "):
            return pos, pos + len(ln)
        pos += len(ln)
    return -1, -1

def insert_or_replace_inline_confidence(body: str, score: Optional[float]) -> str:
    """
    Insert (or replace) the inline one-liner directly under the first H1.
    """
    if score is None:
        return body

    # Remove existing inline marker block (if any)
    body = re.sub(rf"{re.escape(INLINE_MARK)}.*?\n", "", body, flags=re.DOTALL)

    start, end = h1_span(body)
    if start == -1:
        # No H1; inject at top
        line = f'{INLINE_MARK}\n> **Confidence Score:** {score:.2f} — *(see scoring table at bottom for details)*\n\n'
        return line + body

    # Place immediately after the H1 line
    prefix = body[:end]
    suffix = body[end:]
    inline = f'{INLINE_MARK}\n> **Confidence Score:** {score:.2f} — *(see scoring table at bottom for details)*\n\n'
    return prefix + inline + suffix

def remove_existing_rollup(body: str) -> str:
    return re.sub(
        rf"{re.escape(BLOCK_START)}.*?{re.escape(BLOCK_END)}\s*",
        "",
        body,
        flags=re.DOTALL
    )

def compute_overall_score(scores: List[float]) -> Optional[float]:
    """
    Aggregate into a single score:
      overall = mean - 0.10 * (fraction of components with score < 0.60)
    Rationale: average confidence, penalized by low-scoring components.
    """
    usable = [s for s in scores if isinstance(s, (int, float))]
    if not usable:
        return None
    mean = sum(usable) / len(usable)
    low_frac = sum(1 for s in usable if s < 0.60) / len(usable)
    overall = max(0.0, min(1.0, mean - 0.10 * low_frac))
    return round(overall, 4)

def scoring_help_block() -> str:
    return (
f"""{BLOCK_START}
## Confidence & Evidence Rollup

!!! info "How to read these scores"
    - **parsed** — base signal that the process was parsed at all (typically 0.5 when activities were found).
    - **known_types_coverage** — fraction of activities recognized as known BW types (higher is better; low values mean many unknown/opaque steps).
    - **transition_integrity** — 1.0 if all transitions link valid activities; lower means broken/missing links.
    - **role_coverage** — evidence of key roles detected (interface.receive / invoke.process / data.jdbc / messaging.jms, etc.).
    - **evidence_strength** — proportion of claims backed by concrete evidence (e.g., detected endpoints, JDBC targets).
    - **inferred_fraction** — portion of the explanation based on hypotheses (higher = more guesswork).

    Examples:
    - High **known_types_coverage** (≥ 0.7): process uses well-identified palette activities (HTTP/REST/JDBC/JMS/etc.).
    - Low **transition_integrity** (< 1.0): transitions reference non-existent steps (XML issues or partial parse).
    - Low **evidence_strength** (≈ 0.0): few/no concrete endpoints, datastore names, or invocation targets detected.
    - Higher **inferred_fraction** (≥ 0.5): explanation relies on educated guesses (scant evidence in source).
    - Overall score is the average of component scores, penalized by any low scores.
"""
    )

def render_rollup_table(rows: List[Dict], overall: Optional[float]) -> str:
    """
    rows: list of {
      'name': doc filename,
      'score': float|None,
      'sub': dict of subscores
    }
    """
    header = (
        "| Document | Score | parsed | known_types | transition_integrity | role_coverage | evidence_strength | inferred_fraction |\n"
        "|---|---:|---:|---:|---:|---:|---:|---:|\n"
    )
    lines = []
    for r in rows:
        sub = r.get("sub") or {}
        def fmt(v): 
            return "" if v is None else f"{v:.2f}"
        lines.append(
            f"| {r.get('name','')} | {fmt(r.get('score'))} | {fmt(sub.get('parsed'))} | {fmt(sub.get('known_types_coverage'))} | "
            f"{fmt(sub.get('transition_integrity'))} | {fmt(sub.get('role_coverage'))} | {fmt(sub.get('evidence_strength'))} | {fmt(sub.get('inferred_fraction'))} |"
        )
    overall_line = ""
    if overall is not None:
        overall_line = f"\n\n**Overall score (this document set):** {overall:.2f}\n"
    return header + "\n".join(lines) + overall_line + f"\n{BLOCK_END}\n"

def find_doc_recursive(root: Path, filename: str) -> Optional[Path]:
    # Exact filename match anywhere under root
    cand = list(root.rglob(filename))
    if cand:
        # prefer shortest path (root-level or nearest)
        cand.sort(key=lambda p: len(p.parts))
        return cand[0]
    return None

def load_plan_docs(plan_path: Path) -> List[Dict]:
    """Return the plan 'docs' list from YAML front matter, or empty list."""
    if not plan_path.exists():
        return []
    raw = read_text(plan_path)
    header, _ = parse_front_matter(raw)
    docs = header.get("docs") or []
    # If someone embedded plan yaml in a fenced block (fallback not needed here)
    return docs if isinstance(docs, list) else []

def load_interdeps_groups(interdeps_path: Path) -> Dict[str, List[str]]:
    """Return family -> [process names] from _interdeps.json."""
    if not interdeps_path.exists():
        return {}
    try:
        data = json.loads(read_text(interdeps_path))
        return data.get("groups", {}) or {}
    except Exception:
        return {}

def index_scores(docs_root: Path) -> Dict[str, Dict]:
    """
    Build an index: filename -> {
      'path': Path,
      'score': float|None,
      'sub': dict,
      'title': str|None
    }
    """
    out: Dict[str, Dict] = {}
    for p in docs_root.rglob("*.md"):
        try:
            txt = read_text(p)
            header, body = parse_front_matter(txt)
            score = header.get("confidence_score")
            subs = header.get("confidence_subscores") or {}
            # title: first H1
            _s, _e = h1_span(body)
            title = None
            if _s != -1:
                title = body[_s:_e].strip().lstrip("# ").strip()
            out[p.name] = {"path": p, "score": score, "sub": subs, "title": title}
        except Exception:
            continue
    return out

def family_name_from_file(name: str) -> Optional[str]:
    # Supports: "Family_<family>.md" or "Family <family>.md"
    if name.lower().startswith("family_") and name.lower().endswith(".md"):
        return name[7:-3]  # after 'Family_' and before '.md'
    if name.lower().startswith("family ") and name.lower().endswith(".md"):
        return name[7:-3]  # after 'Family ' and before '.md'
    return None

def aggregate_for_family(doc_name: str, groups: Dict[str, List[str]], index: Dict[str, Dict], docs_root: Path) -> Tuple[List[Dict], Optional[float]]:
    fam = family_name_from_file(doc_name)
    if not fam:
        return [], None
    members = groups.get(fam, []) or []
    rows = []
    scores = []
    for proc in members:
        source_name = f"{proc}.md"
        p = find_doc_recursive(docs_root, source_name)
        if not p:
            rows.append({"name": source_name, "score": None, "sub": {}})
            continue
        info = index.get(p.name)
        if not info or info.get("score") is None:
            rows.append({"name": p.name, "score": None, "sub": {}})
        else:
            rows.append({"name": p.name, "score": float(info["score"]), "sub": info.get("sub") or {}})
            scores.append(float(info["score"]))
    overall = compute_overall_score(scores)
    return rows, overall

def aggregate_for_plan_spec(spec: Dict, index: Dict, docs_root: Path) -> Tuple[List[Dict], Optional[float]]:
    inputs = spec.get("inputs") or []
    rows = []
    scores = []
    for inp in inputs:
        p = find_doc_recursive(docs_root, inp)
        if not p:
            rows.append({"name": inp, "score": None, "sub": {}})
            continue
        info = index.get(p.name)
        if not info or info.get("score") is None:
            rows.append({"name": p.name, "score": None, "sub": {}})
        else:
            rows.append({"name": p.name, "score": float(info["score"]), "sub": info.get("sub") or {}})
            scores.append(float(info["score"]))
    overall = compute_overall_score(scores)
    return rows, overall

def update_doc_with_rollup(target_path: Path, overall: Optional[float], rows: List[Dict]) -> None:
    try:
        raw = read_text(target_path)
        header, body = parse_front_matter(raw)

        # Inline single-line below title
        body = insert_or_replace_inline_confidence(body, overall)

        # Remove existing rollup block, then append fresh one
        body = remove_existing_rollup(body)
        block = scoring_help_block() + render_rollup_table(rows, overall)
        body = body.rstrip() + "\n\n" + block

        write_text(target_path, render_front_matter(header, body))
        print(f"[rollup] Updated: {target_path.relative_to(REPO_ROOT)} (overall={overall})")
    except Exception as e:
        print(f"[rollup][WARN] Failed to update {target_path}: {e}")

def main():
    ap = argparse.ArgumentParser(description="Append/insert confidence rollups into high-level docs.")
    ap.add_argument("--docs", type=Path, default=DEFAULT_DOCS, help="Docs root (out/docs).")
    ap.add_argument("--sir", type=Path, default=DEFAULT_INTERDEPS, help="Interdeps JSON (out/sir/_interdeps.json).")
    ap.add_argument("--plan", type=Path, default=DEFAULT_PLAN, help="Plan file (out/dox_draft_plan.md).")
    args = ap.parse_args()

    docs_root: Path = args.docs
    interdeps_path: Path = args.sir
    plan_path: Path = args.plan

    if not docs_root.exists():
        print(f"[rollup][WARN] Docs root not found: {docs_root}")
        return 0

    # Build score index across all docs
    index = index_scores(docs_root)

    # 1) Family_* targets
    groups = load_interdeps_groups(interdeps_path)
    for name, info in list(index.items()):
        if not name.lower().endswith(".md"):
            continue
        fam = family_name_from_file(name)
        if fam:
            rows, overall = aggregate_for_family(name, groups, index, docs_root)
            if rows:
                update_doc_with_rollup(info["path"], overall, rows)

    # 2) Plan-based targets
    plan_docs = load_plan_docs(plan_path)
    for spec in plan_docs:
        target_name = spec.get("filename")
        if not target_name:
            continue
        target_path = find_doc_recursive(docs_root, target_name)
        if not target_path:
            print(f"[rollup][INFO] Plan target missing (skipped): {target_name}")
            continue
        rows, overall = aggregate_for_plan_spec(spec, index, docs_root)
        if rows:
            update_doc_with_rollup(target_path, overall, rows)

    print("[rollup] Confidence rollup complete.")
    return 0

if __name__ == "__main__":
    sys.exit(main())
