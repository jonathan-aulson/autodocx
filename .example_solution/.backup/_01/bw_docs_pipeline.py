#!/usr/bin/env python3
import os, sys, json, hashlib, shutil, tempfile, datetime, subprocess
from pathlib import Path
from typing import Dict, List, Tuple
import click
from lxml import etree as ET
import yaml
from tqdm import tqdm

# Optional: install networkx/graphviz at runtime if missing? We assume installed.
import networkx as nx

TOOL_VERSION = "bw-docs-pipeline-0.1.0"

KNOWN_TYPES = {
    # expand over time
    "http:ReceiveHTTP", "http:SendHTTP", "rest:Invoke", "soap:Invoke",
    "mapper:Mapper", "jdbc:Insert", "jdbc:Update", "jdbc:Select", "jdbc:CallProcedure",
    "file:Read", "file:Write", "timer:TimerEvent", "jms:Send", "jms:Receive",
    "bw:Throw", "bw:Catch", "bw:Choice", "bw:ForEach", "bw:Group", "bw:Transition"
}

def sha256_file(p: Path) -> str:
    h = hashlib.sha256()
    with p.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()

def atomic_write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.NamedTemporaryFile("w", delete=False, encoding="utf-8") as tmp:
        tmp.write(text)
        tmp_path = Path(tmp.name)
    tmp_path.replace(path)

def find_start_activity(activities: List[Dict], transitions: List[Dict]) -> str:
    names = {a["name"] for a in activities}
    to_set = {t["to"] for t in transitions}
    candidates = list(names - to_set)
    return candidates[0] if candidates else (activities[0]["name"] if activities else None)

def parse_process_xml(xml_path: Path) -> Tuple[Dict, List[str], str]:
    """Returns (SIR, notes, detected_format)"""
    notes = []
    parser = ET.XMLParser(recover=True, remove_blank_text=True)
    root = ET.parse(str(xml_path), parser).getroot()
    tag = root.tag.lower()
    detected = "process_xml" if "process" in tag else "bwp_xml"

    # Try generic extraction
    nsmap = {k if k else "ns": v for k, v in root.nsmap.items()} if hasattr(root, "nsmap") else {}
    # Process name
    proc_name = root.get("name") or root.get("Name") or xml_path.stem

    # Activities
    activities = []
    # common patterns: //activities/activity or //Activity
    candidates = root.xpath(".//*[local-name()='activities']/*[local-name()='activity']") \
                 + root.xpath(".//*[local-name()='Activity']")
    seen = set()
    for el in candidates:
        name = el.get("name") or el.get("Name")
        typ = el.get("type") or el.get("Type")
        if name and typ and name not in seen:
            activities.append({"name": name, "type": typ})
            seen.add(name)

    # Transitions
    transitions = []
    t_nodes = root.xpath(".//*[local-name()='transitions']/*[local-name()='transition']") \
              + root.xpath(".//*[local-name()='Transition']")
    for t in t_nodes:
        frm = t.get("from") or t.get("From")
        to = t.get("to") or t.get("To")
        if frm and to:
            transitions.append({"from": frm, "to": to})

    if not activities:
        notes.append("No activities found via common patterns.")
    if not transitions:
        notes.append("No transitions found via common patterns.")

    sir = {
        "process_name": proc_name,
        "source_file": str(xml_path.as_posix()),
        "hash_sha256": sha256_file(xml_path),
        "start_activity": find_start_activity(activities, transitions),
        "activities": activities,
        "transitions": transitions,
        "metadata": {
            "extracted_at": datetime.datetime.utcnow().isoformat() + "Z",
            "tool_version": TOOL_VERSION,
            "detected_format": detected,
            "notes": notes
        }
    }
    return sir, notes, detected

def validate_against_schema(instance: Dict, schema_path: Path) -> Tuple[bool, str]:
    # Minimal validation without external lib; assumes structure; for strict, use jsonschema.
    try:
        required = ["process_name", "source_file", "hash_sha256", "activities", "transitions", "metadata"]
        for k in required:
            assert k in instance, f"Missing {k}"
        assert isinstance(instance["activities"], list)
        assert isinstance(instance["transitions"], list)
        assert len(instance["hash_sha256"]) == 64
        return True, "ok"
    except Exception as e:
        return False, str(e)

def compute_confidence(sir: Dict) -> Tuple[float, Dict[str, float], List[str]]:
    # Deterministic scoring
    notes = []
    # 1) Parsed successfully
    base = 0.5 if sir.get("activities") else 0.0

    # 2) Known type coverage
    total = len(sir.get("activities", []))
    known = sum(1 for a in sir.get("activities", []) if a.get("type") in KNOWN_TYPES)
    coverage = (known / total) if total else 0.0
    extra2 = 0.3 * coverage
    if coverage < 1.0:
        notes.append(f"Unknown activity types present ({total-known}/{total}).")

    # 3) Transition integrity (endpoints exist)
    names = {a["name"] for a in sir.get("activities", [])}
    ok_trans = all(t["from"] in names and t["to"] in names for t in sir.get("transitions", []))
    extra3 = 0.2 if ok_trans else 0.0
    if not ok_trans:
        notes.append("One or more transitions reference missing activities.")

    # Optional cycle check (flag only)
    G = nx.DiGraph()
    G.add_nodes_from(names)
    G.add_edges_from([(t["from"], t["to"]) for t in sir.get("transitions", []) if t["from"] in names and t["to"] in names])
    cyclic = not nx.is_directed_acyclic_graph(G) if G.number_of_nodes() else False
    if cyclic:
        notes.append("Cycle detected (may be by design).")

    score = min(1.0, base + extra2 + extra3)
    subs = {
        "parsed": base,
        "known_types_coverage": coverage,
        "transition_integrity": 1.0 if ok_trans else 0.0,
        "acyclic_flag": 0.0 if cyclic else 1.0  # informational
    }
    return score, subs, notes

def graphviz_render(sir: Dict, out_svg: Path):
    names = {a["name"] for a in sir.get("activities", [])}
    G = nx.DiGraph()
    G.add_nodes_from(names)
    G.add_edges_from([(t["from"], t["to"]) for t in sir.get("transitions", [])])

    # Use pygraphviz or pydot if available; fallback to DOT via subprocess
    dot_lines = ["digraph G {", 'rankdir=LR;', 'node [shape=box, style="rounded"];']
    start = sir.get("start_activity")
    for a in sir.get("activities", []):
        attr = 'color="green", penwidth=2' if a["name"] == start else ''
        label = f'{a["name"]}\\n({a["type"]})'
        dot_lines.append(f'"{a["name"]}" [label="{label}" {attr}];')
    for t in sir.get("transitions", []):
        dot_lines.append(f'"{t["from"]}" -> "{t["to"]}";')
    dot_lines.append("}")

    dot = "\n".join(dot_lines)
    out_svg.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.NamedTemporaryFile("w", delete=False, encoding="utf-8", suffix=".dot") as f:
        f.write(dot)
        dot_path = f.name
    try:
        subprocess.run(["dot", "-Tsvg", dot_path, "-o", str(out_svg)], check=True)
    finally:
        try: os.remove(dot_path)
        except: pass

def load_prompt(prompt_path: Path) -> str:
    return prompt_path.read_text(encoding="utf-8")

def call_llm_explain(sir: Dict, prompt_template: str) -> Dict:
    # Uses environment-injected API keys via .roo/config.yaml -> supervisor
    # Model: gpt-5-chat, temperature=0, single-file SIR, strict budget
    import openai
    client = openai.OpenAI()
    system = prompt_template
    user = json.dumps({"SIR": sir}, ensure_ascii=False)

    resp = client.chat.completions.create(
        model="gpt-5-chat-latest",
        temperature=0,
        messages=[
            {"role": "system", "content": system},
            {"role": "user", "content": user}
        ],
        max_tokens=600  # token-cap enforcement
    )
    content = resp.choices[0].message.content.strip()
    # Expect JSON
    try:
        return json.loads(content)
    except Exception:
        return {
            "process_name": sir["process_name"],
            "overview": "LLM explanation unavailable or invalid JSON.",
            "steps": [{"name": a["name"], "type": a["type"], "description": "Unknown"} for a in sir["activities"]]
        }

def render_markdown(sir: Dict, llm: Dict, score: float, subs: Dict[str, float], score_notes: List[str]) -> str:
    fm = {
        "interface_name": sir["process_name"],
        "process_file": sir["source_file"],
        "source_hash": sir["hash_sha256"],
        "extracted_at": sir["metadata"]["extracted_at"],
        "tool_version": sir["metadata"]["tool_version"],
        "explanation_source": "symbolic_only",
        "documentation_status": "auto_generated",
        "review_status": "pending",
        "confidence_score": round(score, 3),
        "confidence_subscores": subs,
        "confidence_notes": score_notes,
        "detected_format": sir["metadata"]["detected_format"],
        "start_activity": sir.get("start_activity")
    }
    yaml_block = yaml.safe_dump(fm, sort_keys=False)
    lines = ["---", yaml_block.strip(), "---", ""]
    lines += [f"## {sir['process_name']} Process Overview", llm.get("overview", "").strip(), ""]
    lines += ["### Flow Steps"]
    for step in llm.get("steps", []):
        lines.append(f"**{step['name']}**")
        lines.append(f"Type: {step['type']}")
        lines.append(step.get("description", ""))
        lines.append("")
    return "\n".join(lines)

def discover_process_files(root: Path) -> List[Path]:
    exts = [".process", ".bwp", ".xml"]
    paths = []
    for ext in exts:
        paths += list(root.rglob(f"*{ext}"))
    # naive filter: keep those that look like BW process XMLs
    return paths

@click.group()
def cli():
    pass

@cli.command()
@click.option("--root", type=click.Path(path_type=Path), required=True, help="Root folder with BW artifacts (e.g., bw-samples-master)")
@click.option("--sir-out", type=click.Path(path_type=Path), default=Path("out/sir"))
@click.option("--schema", type=click.Path(path_type=Path), default=Path(".roo/schemas/sir.schema.json"))
def extract(root: Path, sir_out: Path, schema: Path):
    files = discover_process_files(root)
    pbar = tqdm(files, desc="Extracting SIR")
    for f in pbar:
        try:
            sir, notes, detected = parse_process_xml(f)
            ok, msg = validate_against_schema(sir, schema)
            if not ok:
                pbar.write(f"[WARN] Schema validation failed for {f}: {msg}")
            out_path = sir_out / (sir["process_name"] + ".json")
            atomic_write_text(out_path, json.dumps(sir, indent=2))
        except Exception as e:
            pbar.write(f"[ERROR] {f}: {e}")

@cli.command()
@click.option("--sir", type=click.Path(path_type=Path), required=True)
@click.option("--graphs-out", type=click.Path(path_type=Path), default=Path("out/graphs"))
def visualize(sir: Path, graphs_out: Path):
    sir_obj = json.loads(Path(sir).read_text(encoding="utf-8"))
    svg_path = graphs_out / (sir_obj["process_name"] + ".svg")
    graphviz_render(sir_obj, svg_path)
    click.echo(f"OK: {svg_path}")

@cli.command()
@click.option("--sir", type=click.Path(path_type=Path), required=True)
@click.option("--prompt", type=click.Path(path_type=Path), default=Path(".roo/prompts/bw_explain.md"))
@click.option("--docs-out", type=click.Path(path_type=Path), default=Path("out/docs"))
def explain_render(sir: Path, prompt: Path, docs_out: Path):
    sir_obj = json.loads(Path(sir).read_text(encoding="utf-8"))
    score, subs, notes = compute_confidence(sir_obj)
    tmpl = load_prompt(prompt)
    llm = call_llm_explain(sir_obj, tmpl)
    md = render_markdown(sir_obj, llm, score, subs, notes)
    out_md = docs_out / (sir_obj["process_name"] + ".md")
    atomic_write_text(out_md, md)
    click.echo(f"OK: {out_md}")

@cli.command()
@click.option("--sir-root", type=click.Path(path_type=Path), default=Path("out/sir"))
@click.option("--graphs-out", type=click.Path(path_type=Path), default=Path("out/graphs"))
@click.option("--docs-out", type=click.Path(path_type=Path), default=Path("out/docs"))
@click.option("--prompt", type=click.Path(path_type=Path), default=Path(".roo/prompts/bw_explain.md"))
def all(sir_root: Path, graphs_out: Path, docs_out: Path, prompt: Path):
    sir_files = list(sir_root.glob("*.json"))
    pbar = tqdm(sir_files, desc="Explain & Render")
    tmpl = load_prompt(prompt)
    for sf in pbar:
        try:
            sir_obj = json.loads(sf.read_text(encoding="utf-8"))
            # visualize
            svg_path = graphs_out / (sir_obj["process_name"] + ".svg")
            graphviz_render(sir_obj, svg_path)
            # score + explain + render
            score, subs, notes = compute_confidence(sir_obj)
            llm = call_llm_explain(sir_obj, tmpl)
            md = render_markdown(sir_obj, llm, score, subs, notes)
            out_md = docs_out / (sir_obj["process_name"] + ".md")
            atomic_write_text(out_md, md)
        except Exception as e:
            pbar.write(f"[ERROR] {sf}: {e}")
    # write index
    idx = []
    for md in docs_out.glob("*.md"):
        idx.append({"doc": md.name})
    atomic_write_text(docs_out / "_index.json", json.dumps(idx, indent=2))
    click.echo("Pipeline complete.")
    
if __name__ == "__main__":
    cli()
