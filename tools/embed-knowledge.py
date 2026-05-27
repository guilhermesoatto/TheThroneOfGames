"""
embed-knowledge.py
==================
Lê todos os arquivos Markdown das subpastas de docs/ai/knowledge/ e os embeda
em 4 colecoes ChromaDB independentes, particionadas por dominio de consulta:

  architecture/   -> colecao "architecture"   (patterns, database, decisoes estruturais)
  business-rules/ -> colecao "business-rules" (ubiquitous language, regras de dominio)
  bdd/            -> colecao "bdd"            (cenarios Gherkin .feature / .md)
  decisions/      -> colecao "decision-log"   (ADRs gerados por reflect.py)

Uso:
    python tools/embed-knowledge.py                          # todas as colecoes
    python tools/embed-knowledge.py --collection architecture  # so uma colecao

Dependencias:
    pip install -r tools/requirements.txt
"""

from __future__ import annotations

import argparse
import hashlib
import re
import sys
from pathlib import Path

import chromadb
from chromadb.config import Settings
from sentence_transformers import SentenceTransformer

REPO_ROOT = Path(__file__).resolve().parent.parent
KNOWLEDGE_DIR = REPO_ROOT / "docs" / "ai" / "knowledge"
CHROMA_DIR = KNOWLEDGE_DIR / "chroma"
EMBEDDING_MODEL = "all-MiniLM-L6-v2"

COLLECTION_MAP: dict[str, Path] = {
    "architecture":   KNOWLEDGE_DIR / "architecture",
    "business-rules": KNOWLEDGE_DIR / "business-rules",
    "bdd":            KNOWLEDGE_DIR / "bdd",
    "decision-log":   KNOWLEDGE_DIR / "decisions",
}


def split_by_headings(content: str, source_path: str) -> list[dict]:
    chunks: list[dict] = []
    current_heading = "Introduction"
    current_lines: list[str] = []

    for line in content.splitlines():
        if re.match(r"^#{1,3} ", line):
            if current_lines:
                chunk_text = "\n".join(current_lines).strip()
                if len(chunk_text) > 50:
                    chunks.append({"text": chunk_text, "heading": current_heading, "source": source_path})
            current_heading = line.lstrip("#").strip()
            current_lines = [line]
        else:
            current_lines.append(line)

    if current_lines:
        chunk_text = "\n".join(current_lines).strip()
        if len(chunk_text) > 50:
            chunks.append({"text": chunk_text, "heading": current_heading, "source": source_path})

    return chunks


def stable_id(text: str, source: str, heading: str) -> str:
    raw = f"{source}::{heading}::{text[:100]}"
    return hashlib.sha256(raw.encode()).hexdigest()[:16]


def collect_files(directory: Path) -> list[Path]:
    return [
        p for p in directory.rglob("*")
        if p.suffix in {".md", ".feature"} and "chroma" not in p.parts
    ]


def embed_collection(
    collection_name: str,
    source_dir: Path,
    client: chromadb.PersistentClient,
    model: SentenceTransformer,
    reset: bool = False,
) -> int:
    if not source_dir.exists():
        print(f"  [WARN] Diretorio nao encontrado, pulando: {source_dir}")
        return 0

    files = collect_files(source_dir)
    if not files:
        print(f"  [INFO] Sem arquivos em {source_dir.name}/ -- colecao '{collection_name}' vazia.")
        return 0

    if reset:
        try:
            client.delete_collection(collection_name)
            print(f"  [RESET] Colecao '{collection_name}' deletada.")
        except Exception:
            pass  # colecao ainda nao existia

    collection = client.get_or_create_collection(
        name=collection_name,
        metadata={"hnsw:space": "cosine"},
    )

    total_chunks = 0
    for file_path in files:
        relative_path = file_path.relative_to(REPO_ROOT).as_posix()
        print(f"  Processing: {relative_path}")

        content = file_path.read_text(encoding="utf-8")
        chunks = split_by_headings(content, relative_path)

        if not chunks:
            print(f"    -> Sem chunks validos, pulando.")
            continue

        ids = [stable_id(c["text"], c["source"], c["heading"]) for c in chunks]
        texts = [c["text"] for c in chunks]
        metadatas = [
            {"source": c["source"], "heading": c["heading"], "collection": collection_name}
            for c in chunks
        ]
        embeddings = model.encode(texts, show_progress_bar=False).tolist()

        collection.upsert(ids=ids, documents=texts, embeddings=embeddings, metadatas=metadatas)
        print(f"    -> {len(chunks)} chunks upsertados.")
        total_chunks += len(chunks)

    print(f"  [OK] Colecao '{collection_name}': {collection.count()} documentos total.")
    return total_chunks


def main() -> None:
    parser = argparse.ArgumentParser(description="Embeda arquivos Markdown em colecoes ChromaDB particionadas.")
    parser.add_argument(
        "--collection", type=str, default=None, choices=list(COLLECTION_MAP.keys()),
        help="Re-embeda apenas uma colecao especifica. Padrao: todas.",
    )
    parser.add_argument(
        "--reset", action="store_true",
        help="Deleta e recria a(s) colecao(oes) antes de indexar. Use ao renomear ou remover .md.",
    )
    args = parser.parse_args()

    print(f"[embed-knowledge] Modelo: {EMBEDDING_MODEL}")
    print(f"[embed-knowledge] ChromaDB: {CHROMA_DIR}")
    CHROMA_DIR.mkdir(parents=True, exist_ok=True)

    client = chromadb.PersistentClient(path=str(CHROMA_DIR), settings=Settings(anonymized_telemetry=False))

    print(f"[embed-knowledge] Carregando modelo {EMBEDDING_MODEL}...")
    model = SentenceTransformer(EMBEDDING_MODEL)
    print()

    target = {args.collection: COLLECTION_MAP[args.collection]} if args.collection else COLLECTION_MAP

    if args.reset:
        print("[embed-knowledge] Modo --reset: colecoes serao deletadas e recriadas.\n")

    grand_total = 0
    for col_name, col_dir in target.items():
        print(f"=== Colecao: {col_name} ({col_dir.name}/) ===")
        grand_total += embed_collection(col_name, col_dir, client, model, reset=args.reset)
        print()

    print(f"[embed-knowledge] Concluido. Total geral: {grand_total} chunks.")
    print(f"[embed-knowledge] Commit o diretorio {CHROMA_DIR.relative_to(REPO_ROOT)} junto com seu codigo.")


if __name__ == "__main__":
    main()
