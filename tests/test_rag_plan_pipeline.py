from pathlib import Path

from autodocx.rag.plan import build_rag_docs, generate_xml_doc_plan


def test_generate_xml_plan_and_rag_docs(tmp_path: Path) -> None:
    repo_root = tmp_path / "repo"
    repo_root.mkdir(parents=True)
    (repo_root / "README.md").write_text("# Demo\n\nSample README", encoding="utf-8")

    out_dir = tmp_path / "out"
    out_dir.mkdir(parents=True)

    doc_context = {
        "components": {"Demo": {}},
        "constellations": {
            "constellation_1": {
                "slug": "demo-constellation",
                "components": ["Demo"],
                "evidence_packet": "evidence/constellations/demo.json",
            }
        },
    }

    def fake_plan_llm(prompt, payload):
        return {
            "text": "<docPlan repo='demo'>"
            "<page slug='demo-overview' title='Demo Overview'>"
            "<section title='Summary'/>"
            "<section title='Risks'/>"
            "</page></docPlan>"
        }

    plan_path = generate_xml_doc_plan(repo_root, out_dir, doc_context, llm_callable=fake_plan_llm)
    assert plan_path.exists()

    class DummyEmbed:
        def query(self, text, top_k=5):
            return [
                {
                    "text": "Sample snippet",
                    "path": "src/app.py",
                    "start_line": 1,
                    "end_line": 3,
                    "component": "Demo",
                    "score": 0.9,
                }
            ]

    def fake_rag_llm(prompt, payload):
        return {"text": "# Demo Overview\n\n## Summary\ncontent\n\n## Risks\nmore content"}

    generated = build_rag_docs(plan_path, DummyEmbed(), out_dir, doc_context, llm_callable=fake_rag_llm)
    assert generated
    assert (out_dir / "docs" / "curated" / "rag" / "demo-overview.md").exists()
