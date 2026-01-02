from __future__ import annotations

import json
import math
import os
import uuid
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Any, Callable, Dict, Iterable, List, Optional, Sequence

try:  # pragma: no cover - optional dependency
    from qdrant_client import QdrantClient, models
except Exception:  # pragma: no cover - qdrant optional
    QdrantClient = None  # type: ignore
    models = None  # type: ignore

try:  # pragma: no cover - optional dependency
    from openai import OpenAI
except Exception:  # pragma: no cover - handled at runtime
    OpenAI = None  # type: ignore


DEFAULT_EMBED_MODEL = os.getenv("AUTODOCX_EMBED_MODEL", "text-embedding-3-large")
LOCAL_CHUNK_FILE = "rag/chunks.jsonl"


def _safe_slug(value: str) -> str:
    cleaned = "".join(ch.lower() if ch.isalnum() else "-" for ch in value.strip())
    cleaned = "-".join(filter(None, cleaned.split("-")))
    return cleaned or "repo"


def _default_overlap(chunk_size: int) -> int:
    return max(0, min(100, chunk_size // 5))


def _cosine_similarity(a: Sequence[float], b: Sequence[float]) -> float:
    if not a or not b or len(a) != len(b):
        return 0.0
    dot = sum(x * y for x, y in zip(a, b))
    norm_a = math.sqrt(sum(x * x for x in a))
    norm_b = math.sqrt(sum(x * x for x in b))
    if not norm_a or not norm_b:
        return 0.0
    return dot / (norm_a * norm_b)


@dataclass
class ChunkRecord:
    id: str
    path: str
    text: str
    start_line: int
    end_line: int
    component: str
    vector: Optional[List[float]] = None


EmbeddingCallable = Callable[[List[str]], List[List[float]]]


class EmbeddingService:
    """
    Lightweight embeddings/Qdrant orchestrator that can fall back to a local JSONL store.
    """

    def __init__(
        self,
        repo_root: Path,
        out_dir: Path,
        *,
        embed_model: str = DEFAULT_EMBED_MODEL,
        embedding_callable: Optional[EmbeddingCallable] = None,
        qdrant_url: Optional[str] = None,
        qdrant_api_key: Optional[str] = None,
        debug: bool = False,
    ) -> None:
        self.repo_root = Path(repo_root)
        self.out_dir = Path(out_dir)
        self.embed_model = embed_model
        self.debug = debug
        self.embedding_callable = embedding_callable or self._openai_embed
        self.collection_name = f"autodocx_{_safe_slug(self.repo_root.name)}"
        self._local_store_path = self.out_dir / LOCAL_CHUNK_FILE
        self._local_chunks: List[ChunkRecord] = []
        self._vector_size: Optional[int] = None

        qdrant_url = qdrant_url or os.getenv("AUTODOCX_QDRANT_URL")
        qdrant_api_key = qdrant_api_key or os.getenv("AUTODOCX_QDRANT_API_KEY")
        self.qdrant: Optional[QdrantClient] = None
        if qdrant_url and QdrantClient is not None:
            try:
                self.qdrant = QdrantClient(url=qdrant_url, api_key=qdrant_api_key)
            except Exception as exc:  # pragma: no cover - network failure
                self.qdrant = None
                if self.debug:
                    print(f"[rag] Failed to initialize Qdrant client: {exc}")

        self._load_local_chunks()

    # ------------------------------------------------------------------ #
    # Indexing
    # ------------------------------------------------------------------ #
    def index_artifacts(self, artifacts: Iterable[Dict[str, Any]], *, chunk_size: int = 400) -> None:
        files_indexed = 0
        new_chunks: List[ChunkRecord] = []
        for art in artifacts:
            rel_path = art.get("repo_path")
            if not rel_path:
                continue
            abs_path = self.repo_root / rel_path
            if not abs_path.exists() or not abs_path.is_file():
                continue
            try:
                text = abs_path.read_text(encoding="utf-8", errors="ignore")
            except Exception:
                continue
            if _looks_binary(text):
                continue
            component = art.get("component_or_service") or art.get("component") or "ungrouped"
            new_chunks.extend(self._chunk_file(abs_path, text, component, chunk_size=chunk_size))
            files_indexed += 1

        if not new_chunks:
            if self.debug:
                print("[rag] No new chunks generated for embeddings.")
            return

        vectors = self.embedding_callable([chunk.text for chunk in new_chunks])
        if not vectors:
            return
        self._vector_size = len(vectors[0])
        for chunk, vector in zip(new_chunks, vectors):
            chunk.vector = vector

        if self.qdrant:
            self._upsert_qdrant(new_chunks)
        self._local_chunks.extend(new_chunks)
        self._save_local_chunks(new_chunks)
        if self.debug:
            print(f"[rag] Indexed {len(new_chunks)} chunks from {files_indexed} file(s).")

    def _chunk_file(
        self,
        path: Path,
        text: str,
        component: str,
        *,
        chunk_size: int,
        overlap: Optional[int] = None,
    ) -> List[ChunkRecord]:
        lines = text.splitlines()
        if not lines:
            return []
        overlap = overlap if overlap is not None else _default_overlap(chunk_size)
        chunks: List[ChunkRecord] = []
        start = 0
        while start < len(lines):
            end = min(len(lines), start + chunk_size)
            snippet = "\n".join(lines[start:end]).strip()
            if snippet:
                chunk = ChunkRecord(
                    id=str(uuid.uuid4()),
                    path=str(path.relative_to(self.repo_root)),
                    text=snippet[:4000],
                    start_line=start + 1,
                    end_line=end,
                    component=component,
                )
                chunks.append(chunk)
            start = end - overlap
            if start < 0:
                start = 0
            if start >= len(lines):
                break
            if end == len(lines):
                break
        return chunks

    def _upsert_qdrant(self, chunks: List[ChunkRecord]) -> None:
        if not self.qdrant or models is None or not chunks:
            return
        vector_size = self._vector_size or len(chunks[0].vector or [])
        if not vector_size:
            return
        self._ensure_collection(vector_size)
        points = []
        for chunk in chunks:
            if not chunk.vector:
                continue
            payload = {
                "path": chunk.path,
                "start_line": chunk.start_line,
                "end_line": chunk.end_line,
                "component": chunk.component,
                "text": chunk.text,
            }
            points.append(models.PointStruct(id=chunk.id, vector=chunk.vector, payload=payload))
        if not points:
            return
        try:
            self.qdrant.upsert(collection_name=self.collection_name, points=points)
        except Exception as exc:  # pragma: no cover - network failure
            if self.debug:
                print(f"[rag] Failed to upsert points to Qdrant: {exc}")

    def _ensure_collection(self, vector_size: int) -> None:
        if not self.qdrant or models is None:
            return
        try:
            self.qdrant.recreate_collection(
                collection_name=self.collection_name,
                vectors_config=models.VectorParams(size=vector_size, distance=models.Distance.COSINE),
            )
        except Exception:  # pragma: no cover - existing collection
            pass

    # ------------------------------------------------------------------ #
    # Querying
    # ------------------------------------------------------------------ #
    def query(self, text: str, *, top_k: int = 5) -> List[Dict[str, Any]]:
        if not text.strip():
            return []
        vector = self.embedding_callable([text])[0]
        if self.qdrant:
            try:
                results = self.qdrant.search(
                    collection_name=self.collection_name,
                    query_vector=vector,
                    limit=top_k,
                    with_payload=True,
                )
                return [
                    {
                        "text": hit.payload.get("text", ""),
                        "path": hit.payload.get("path", ""),
                        "start_line": hit.payload.get("start_line", 0),
                        "end_line": hit.payload.get("end_line", 0),
                        "component": hit.payload.get("component", ""),
                        "score": hit.score,
                    }
                    for hit in results or []
                ]
            except Exception as exc:  # pragma: no cover - network failure
                if self.debug:
                    print(f"[rag] Qdrant search failed, falling back to local chunks: {exc}")
        return self._local_search(vector, top_k=top_k)

    def _local_search(self, vector: Sequence[float], *, top_k: int) -> List[Dict[str, Any]]:
        ranked: List[tuple[float, ChunkRecord]] = []
        for chunk in self._local_chunks:
            if not chunk.vector:
                continue
            score = _cosine_similarity(vector, chunk.vector)
            ranked.append((score, chunk))
        ranked.sort(key=lambda item: item[0], reverse=True)
        hits: List[Dict[str, Any]] = []
        for score, chunk in ranked[:top_k]:
            hits.append(
                {
                    "text": chunk.text,
                    "path": chunk.path,
                    "start_line": chunk.start_line,
                    "end_line": chunk.end_line,
                    "component": chunk.component,
                    "score": score,
                }
            )
        return hits

    # ------------------------------------------------------------------ #
    # Embedding provider + persistence helpers
    # ------------------------------------------------------------------ #
    def _openai_embed(self, texts: List[str]) -> List[List[float]]:  # pragma: no cover - network call
        if OpenAI is None:
            raise RuntimeError("openai is not installed; install the SDK or supply embedding_callable.")
        client = OpenAI(api_key=os.getenv("OPENAI_API_KEY"))
        response = client.embeddings.create(model=self.embed_model, input=texts)
        return [row.embedding for row in response.data]

    def _load_local_chunks(self) -> None:
        if not self._local_store_path.exists():
            return
        for line in self._local_store_path.read_text(encoding="utf-8").splitlines():
            try:
                payload = json.loads(line)
            except json.JSONDecodeError:
                continue
            self._local_chunks.append(ChunkRecord(**payload))

    def _save_local_chunks(self, chunks: List[ChunkRecord]) -> None:
        if not chunks:
            return
        self._local_store_path.parent.mkdir(parents=True, exist_ok=True)
        with self._local_store_path.open("a", encoding="utf-8") as handle:
            for chunk in chunks:
                handle.write(json.dumps(asdict(chunk)) + "\n")


def _looks_binary(text: str) -> bool:
    # Heuristic: treat as binary if many NULs
    return text.count("\x00") > 0
