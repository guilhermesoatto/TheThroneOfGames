"""
query-knowledge.py
==================
Consulta as colecoes ChromaDB particionadas com uma query em linguagem natural.

Uso:
    python tools/query-knowledge.py "outbox pattern para eventos de pagamento"
    python tools/query-knowledge.py "quando usar Strategy" --n 5
    python tools/query-knowledge.py "Customer aggregate" --collection business-rules
    python tools/query-knowledge.py "pontos de fidelidade" --all

Roteamento automatico por intencao (quando --collection e omitido e --all nao informado):
    "qual pattern / como implementar / decisao estrutural" -> architecture
    "como se chama / termo / glossario / dominio"         -> business-rules
    "o que foi decidido / historico / ADR"                -> decision-log
    "cenario / dado que / quando / entao / gherkin / bdd" -> bdd

Dependencias:
    pip install -r tools/requirements.txt
"""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

import chromadb
from chromadb.config import Settings

REPO_ROOT = Path(__file__).resolve().parent.parent
KNOWLEDGE_DIR = REPO_ROOT / "docs" / "ai" / "knowledge"
CHROMA_DIR = KNOWLEDGE_DIR / "chroma"
EMBEDDING_MODEL = "all-MiniLM-L6-v2"

VALID_COLLECTIONS = ["architecture", "business-rules", "bdd", "decision-log"]

ROUTING_RULES: list[tuple[str, str]] = [
    (r"\b(pattern|padrão|padrao|implementar|arquitetura|design|estrutura|escalar|cache|fila|outbox|saga|cqrs|event.sourcing|repository|strategy|factory|decorator|adapter|facade|observer|command|specification)\b", "architecture"),
    (r"\b(como se chama|termo|glossario|glossário|dominio|domínio|entidade|aggregate|value.?object|bounded.?context|linguagem|ubiquitous|sinonimo|sinônimo|proibido)\b", "business-rules"),
    (r"\b(decidido|decisão|decisao|historico|histórico|adr|porque|porquê|motivo|escolhemos|optamos|alternativa)\b", "decision-log"),
    (r"\b(cenário|cenario|dado.?que|quando|entao|então|bdd|gherkin|feature|scenario|given|when|then|comportamento|acceptance)\b", "bdd"),
]


def route_query(query: str) -> str:
    """Inferir a colecao mais adequada a partir da query."""
    q = query.lower()
    for pattern, collection in ROUTING_RULES:
        if re.search(pattern, q, re.IGNORECASE):
            return collection
    return "architecture"  # fallback


def load_collection(client: chromadb.PersistentClient, name: str):
    try:
        return client.get_collection(name)
    except Exception:
        return None


def format_results(documents, metadatas, distances, collection_name: str) -> None:
    for i, (doc, meta, dist) in enumerate(zip(documents, metadatas, distances), 1):
        relevance = round((1 - dist) * 100, 1)
        print(f"--- Resultado {i} (relevancia: {relevance}%) [colecao: {collection_name}] ---")
        print(f"Fonte:    {meta.get('source', 'N/A')}")
        print(f"Secao:    {meta.get('heading', 'N/A')}")
        print(f"Conteudo:\n{doc[:600]}{'...' if len(doc) > 600 else ''}")
        print()


def query_single(client, model, collection_name: str, query: str, n: int) -> bool:
    """Query uma unica colecao. Retorna True se obteve resultados."""
    col = load_collection(client, collection_name)
    if col is None:
        print(f"[WARN] Colecao '{collection_name}' nao encontrada. Execute embed-knowledge.py primeiro.")
        return False

    if model is None:
        from sentence_transformers import SentenceTransformer as _ST
        model = _ST(EMBEDDING_MODEL)

    query_embedding = model.encode([query]).tolist()
    results = col.query(
        query_embeddings=query_embedding,
        n_results=min(n, col.count()),
        include=["documents", "metadatas", "distances"],
    )

    documents = results.get("documents", [[]])[0]
    metadatas = results.get("metadatas", [[]])[0]
    distances = results.get("distances", [[]])[0]

    if not documents:
        return False

    format_results(documents, metadatas, distances, collection_name)
    return True


def query_all(client, model, query: str, n: int, min_per_collection: int = 1) -> None:
    """Query todas as colecoes e consolida resultados rankeados por relevancia."""
    if model is None:
        from sentence_transformers import SentenceTransformer as _ST
        model = _ST(EMBEDDING_MODEL)

    query_embedding = model.encode([query]).tolist()
    all_results: list[tuple[float, str, str, str]] = []  # (relevance, doc, source, heading, collection)
    per_collection: dict[str, list] = {}

    for col_name in VALID_COLLECTIONS:
        col = load_collection(client, col_name)
        if col is None or col.count() == 0:
            continue
        results = col.query(
            query_embeddings=query_embedding,
            n_results=min(max(n, min_per_collection), col.count()),
            include=["documents", "metadatas", "distances"],
        )
        docs = results.get("documents", [[]])[0]
        metas = results.get("metadatas", [[]])[0]
        dists = results.get("distances", [[]])[0]
        col_entries = []
        for doc, meta, dist in zip(docs, metas, dists):
            relevance = round((1 - dist) * 100, 1)
            entry = (relevance, doc, meta.get("source", ""), meta.get("heading", ""), col_name)
            all_results.append(entry)
            col_entries.append(entry)
        per_collection[col_name] = col_entries

    if not all_results:
        print("[query-knowledge] Nenhum resultado encontrado em nenhuma colecao.")
        return

    # Garantir min_per_collection por colecao populada
    guaranteed: list = []
    if min_per_collection > 0:
        for col_name, entries in per_collection.items():
            guaranteed.extend(sorted(entries, key=lambda x: x[0], reverse=True)[:min_per_collection])

    # Completar ate n com os melhores globais nao ja incluidos
    guaranteed_ids = {id(e) for e in guaranteed}
    remaining = [e for e in all_results if id(e) not in guaranteed_ids]
    remaining.sort(key=lambda x: x[0], reverse=True)
    combined = guaranteed + [e for e in remaining if e not in guaranteed]
    combined.sort(key=lambda x: x[0], reverse=True)
    top = combined[:max(n, len(guaranteed))]

    for i, (relevance, doc, source, heading, col_name) in enumerate(top, 1):
        print(f"--- Resultado {i} (relevancia: {relevance}%) [colecao: {col_name}] ---")
        print(f"Fonte:    {source}")
        print(f"Secao:    {heading}")
        print(f"Conteudo:\n{doc[:600]}{'...' if len(doc) > 600 else ''}")
        print()


def main() -> None:
    if not CHROMA_DIR.exists():
        print("[query-knowledge] ERRO: Banco ChromaDB nao encontrado.")
        print(f"  Esperado em: {CHROMA_DIR}")
        print("  Execute primeiro: python tools/embed-knowledge.py")
        sys.exit(1)

    parser = argparse.ArgumentParser(description="Consulta a base de conhecimento ChromaDB particionada.")
    parser.add_argument("query", help="Query em linguagem natural ou termos de negocio.")
    parser.add_argument("--n", type=int, default=3, help="Numero de resultados (padrao: 3).")
    parser.add_argument(
        "--collection", type=str, default=None, choices=VALID_COLLECTIONS,
        help="Colecao especifica. Se omitido, roteamento automatico por intencao.",
    )
    parser.add_argument(
        "--all", action="store_true",
        help="Buscar em todas as colecoes e consolidar resultados rankeados.",
    )
    parser.add_argument(
        "--min-per-collection", type=int, default=1, dest="min_per_collection",
        help="Minimo de resultados por colecao populada no modo --all (padrao: 1).",
    )
    args = parser.parse_args()

    client = chromadb.PersistentClient(path=str(CHROMA_DIR), settings=Settings(anonymized_telemetry=False))

    # Lazy: modelo so e carregado se houver colecoes a consultar
    model = None

    print(f"\n{'='*70}")
    print(f'Query: "{args.query}"')
    print(f"{'='*70}\n")

    if args.all:
        query_all(client, model, args.query, args.n, args.min_per_collection)
    else:
        if args.collection:
            target = args.collection
        else:
            target = route_query(args.query)
            print(f"[auto-route] Colecao selecionada: {target}\n")
        query_single(client, model, target, args.query, args.n)


if __name__ == "__main__":
    main()
