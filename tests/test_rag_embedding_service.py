from pathlib import Path

from autodocx.rag.service import EmbeddingService


def _fake_embed(texts):
    return [[float(len(t))] for t in texts]


def test_embedding_service_chunks_and_queries(tmp_path: Path) -> None:
    repo_root = tmp_path / "repo"
    repo_root.mkdir(parents=True)
    sample = repo_root / "src" / "example.py"
    sample.parent.mkdir(parents=True)
    sample.write_text("line1\nline2\nline3\nline4\n", encoding="utf-8")

    out_dir = tmp_path / "out"
    out_dir.mkdir(parents=True)

    service = EmbeddingService(
        repo_root,
        out_dir,
        embed_model="test",
        embedding_callable=_fake_embed,
        qdrant_url=None,
    )

    artifacts = [{"repo_path": "src/example.py", "component_or_service": "demo"}]
    service.index_artifacts(artifacts, chunk_size=2)

    hits = service.query("example lines", top_k=2)
    assert hits, "expected at least one retrieval result"
    assert hits[0]["path"] == "src/example.py"
    assert hits[0]["start_line"] == 1
