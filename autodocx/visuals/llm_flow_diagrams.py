from __future__ import annotations

import json
import subprocess
import textwrap
from pathlib import Path
from typing import Any, Dict, List, Optional, Sequence

from autodocx.llm.provider import call_openai_meta
try:
    from rich import print as rprint
except Exception:  # pragma: no cover
    def rprint(msg):
        print(msg)

DIAGRAM_PROMPT = textwrap.dedent(
    """
    You are an expert workflow illustrator. Using the provided component metadata and workflow facts,
    produce a single Graphviz DOT diagram that merges the related processes into one readable flow.

    Requirements:
    - Create nodes for every meaningful activity/step (`activities.name`) and important data entities.
    - Use directed edges for transitions. When a transition references `from` / `to` activities, connect them.
    - When relationships/calls mention cross-workflow dependencies, draw an edge between the corresponding nodes.
    - Cluster related steps from the same workflow using subgraphs named after the workflow.
    - Include swimlanes or grouping labels when multiple components/families appear.
    - Keep the diagram under 40 nodes; consolidate obvious sequences into single nodes if needed.
    - Output **only** Graphviz DOT syntax. Do not wrap it in Markdown fences or commentary.
    """
).strip()


def _load_json(path: Path) -> Optional[Dict[str, Any]]:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except Exception:
        return None


def _collect_workflows(out_base: Path, component_entry: Dict[str, Any]) -> List[Dict[str, Any]]:
    workflows: List[Dict[str, Any]] = []
    for rel in component_entry.get("sir_files", []):
        data = _load_json(out_base / rel)
        if not data:
            continue
        workflows.append(
            {
                "name": data.get("process_name") or data.get("name") or rel,
                "activities": data.get("activities") or [],
                "transitions": data.get("transitions") or [],
                "relationships": data.get("relationships") or [],
            }
        )
    return workflows


def _chunk(items: Sequence[Any], size: int) -> List[List[Any]]:
    return [list(items[i : i + size]) for i in range(0, len(items), size)]


def _extract_dot(text: str) -> str:
    stripped = text.strip().strip("`")
    # If the LLM returned a full digraph, keep it; otherwise wrap as a body.
    lower = stripped.lower()
    if lower.startswith("digraph"):
        return stripped
    # Strip common fencing artifacts
    for fence in ("```dot", "```", "graphviz"):
        if lower.startswith(fence):
            stripped = stripped[len(fence) :].strip()
            break
    body = stripped or "label=\"Produced from textual summary\";"
    return f"digraph G {{\n{body}\n}}\n"


def _render_svg(dot_text: str, out_path: Path) -> None:
    out_path.parent.mkdir(parents=True, exist_ok=True)
    proc = subprocess.run(
        ["dot", "-Tsvg", "-o", str(out_path)],
        input=dot_text,
        text=True,
        capture_output=True,
    )
    if proc.returncode != 0:
        warn = proc.stderr.strip()
        # Second attempt: wrap the body as a label to avoid syntax errors
        safe_label = dot_text.replace("\"", "'").replace("\n", " ")[:500]
        fallback_dot = f"digraph G {{ label=\"LLM diagram unavailable; sanitized: {safe_label}\"; }}"
        proc2 = subprocess.run(
            ["dot", "-Tsvg", "-o", str(out_path)],
            input=fallback_dot,
            text=True,
            capture_output=True,
        )
        if proc2.returncode != 0:
            # Final minimal stub; suppress further warnings
            final_stub = "digraph G { label=\"LLM diagram unavailable\"; }"
            subprocess.run(
                ["dot", "-Tsvg", "-o", str(out_path)],
                input=final_stub,
                text=True,
                capture_output=True,
            )


def generate_llm_workflow_diagrams(
    out_base: Path,
    context: Dict[str, Any],
    *,
    max_flows_per_batch: int = 5,
    llm_callable=None,
) -> Dict[str, List[str]]:
    """
    Generate LLM-authored workflow diagrams per component.
    Returns component_name -> list of diagram relative paths.
    """
    out_base = Path(out_base)
    diagrams_root = out_base / "diagrams" / "llm_svg"
    produced: Dict[str, List[str]] = {}

    for component_name, entry in (context.get("components") or {}).items():
        workflows = _collect_workflows(out_base, entry)
        if not workflows:
            continue
        chunks = _chunk(workflows, max_flows_per_batch)
        for idx, chunk in enumerate(chunks, start=1):
            payload = {
                "component": component_name,
                "families": entry.get("families", []),
                "workflows": chunk,
            }
            result = (
                llm_callable(DIAGRAM_PROMPT, payload)
                if llm_callable
                else call_openai_meta(prompt=DIAGRAM_PROMPT, input_json=payload)
            )
            dot_text = _extract_dot(result.get("text", ""))
            svg_path = diagrams_root / entry["slug"] / f"{entry['slug']}-flow-{idx}.svg"
            _render_svg(dot_text, svg_path)
            rel = svg_path.relative_to(out_base).as_posix()
            produced.setdefault(component_name, []).append(rel)

    return produced
