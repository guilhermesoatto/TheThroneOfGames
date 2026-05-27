"""
rag-watch.py
============
File watcher para re-embedar automaticamente o ChromaDB quando arquivos
.md ou .feature forem salvos em docs/ai/knowledge/.

Uso:
    python tools/rag-watch.py
    python tools/rag-watch.py --debounce 1.0   # espera 1s apos o ultimo save

Comportamento:
  - Monitora os 4 subdiretorios de colecao em docs/ai/knowledge/:
      architecture/   -> reembeda colecao "architecture"
      business-rules/ -> reembeda colecao "business-rules"
      bdd/            -> reembeda colecao "bdd"
      decisions/      -> reembeda colecao "decision-log"
  - Ao detectar modificacao ou criacao de .md / .feature:
      1. Aguarda o debounce (500ms por padrao) para agrupar saves rapidos
      2. Chama embed-knowledge.py --collection <nome> via subprocess
  - Imprime status de cada colecao ao iniciar (doc count atual)
  - Encerra limpo com Ctrl+C

Dependencias:
    pip install -r tools/requirements.txt   (inclui watchdog>=4.0.0)
"""

from __future__ import annotations

import subprocess
import sys
import threading
import time
from pathlib import Path

try:
    from watchdog.events import FileSystemEvent, FileSystemEventHandler
    from watchdog.observers import Observer
except ImportError:
    print("[rag-watch] ERRO: watchdog nao instalado.")
    print("  Execute: pip install -r tools/requirements.txt")
    sys.exit(1)

REPO_ROOT = Path(__file__).resolve().parent.parent
KNOWLEDGE_DIR = REPO_ROOT / "docs" / "ai" / "knowledge"
CHROMA_DIR = KNOWLEDGE_DIR / "chroma"
EMBED_SCRIPT = REPO_ROOT / "tools" / "embed-knowledge.py"

# Mapeamento: subdiretorio -> nome da colecao
DIR_TO_COLLECTION: dict[str, str] = {
    "architecture":   "architecture",
    "business-rules": "business-rules",
    "bdd":            "bdd",
    "decisions":      "decision-log",
}

WATCHED_EXTENSIONS = {".md", ".feature"}


# ---------------------------------------------------------------------------
# Colecao status (exibida no startup)
# ---------------------------------------------------------------------------

def print_collection_status() -> None:
    """Imprime o numero de documentos em cada colecao ChromaDB."""
    if not CHROMA_DIR.exists():
        print("[rag-watch] ChromaDB ainda nao inicializado.")
        print("            Execute: python tools/embed-knowledge.py")
        return

    try:
        import chromadb
        from chromadb.config import Settings

        client = chromadb.PersistentClient(
            path=str(CHROMA_DIR),
            settings=Settings(anonymized_telemetry=False),
        )
        print("\n  Colecao                 Documentos")
        print("  ----------------------  ----------")
        for col_name in DIR_TO_COLLECTION.values():
            try:
                col = client.get_collection(col_name)
                count = col.count()
            except Exception:
                count = 0
            status = f"{count:>10}" if count > 0 else "  (vazia)"
            print(f"  {col_name:<22}  {status}")
        print()
    except ImportError:
        print("[rag-watch] [WARN] chromadb nao instalado — nao e possivel exibir status.")


# ---------------------------------------------------------------------------
# Debounce por colecao
# ---------------------------------------------------------------------------

class _CollectionDebouncer:
    """Agrupa saves rapidos e dispara embed apenas uma vez por colecao."""

    def __init__(self, debounce_seconds: float) -> None:
        self._debounce = debounce_seconds
        self._timers: dict[str, threading.Timer] = {}
        self._lock = threading.Lock()

    def schedule(self, collection_name: str) -> None:
        with self._lock:
            existing = self._timers.get(collection_name)
            if existing:
                existing.cancel()
            timer = threading.Timer(self._debounce, self._run_embed, args=[collection_name])
            timer.daemon = True
            timer.start()
            self._timers[collection_name] = timer

    def _run_embed(self, collection_name: str) -> None:
        with self._lock:
            self._timers.pop(collection_name, None)

        print(f"\n[rag-watch] Alteracao detectada -> re-embedando colecao '{collection_name}'...")
        result = subprocess.run(
            [sys.executable, str(EMBED_SCRIPT), "--collection", collection_name],
            cwd=str(REPO_ROOT),
            capture_output=False,
        )
        if result.returncode == 0:
            print(f"[rag-watch] '{collection_name}' atualizada com sucesso.")
        else:
            print(f"[rag-watch] [WARN] embed-knowledge.py retornou codigo {result.returncode} para '{collection_name}'.")


# ---------------------------------------------------------------------------
# Event handler
# ---------------------------------------------------------------------------

class _KnowledgeEventHandler(FileSystemEventHandler):
    def __init__(self, debouncer: _CollectionDebouncer) -> None:
        super().__init__()
        self._debouncer = debouncer

    def _resolve_collection(self, event_path: str) -> str | None:
        """Dado o path do evento, retorna o nome da colecao ou None se irrelevante."""
        path = Path(event_path)
        if path.suffix not in WATCHED_EXTENSIONS:
            return None
        # Encontrar qual subdiretorio monitorado contem este arquivo
        try:
            rel = path.relative_to(KNOWLEDGE_DIR)
        except ValueError:
            return None
        if not rel.parts:
            return None
        subdir = rel.parts[0]
        return DIR_TO_COLLECTION.get(subdir)

    def on_modified(self, event: FileSystemEvent) -> None:
        if event.is_directory:
            return
        col = self._resolve_collection(event.src_path)
        if col:
            self._debouncer.schedule(col)

    def on_created(self, event: FileSystemEvent) -> None:
        if event.is_directory:
            return
        col = self._resolve_collection(event.src_path)
        if col:
            self._debouncer.schedule(col)


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main() -> None:
    import argparse

    parser = argparse.ArgumentParser(description="File watcher: re-embeda ChromaDB ao salvar .md / .feature.")
    parser.add_argument(
        "--debounce", type=float, default=0.5,
        help="Segundos de espera apos o ultimo save antes de disparar o embed (padrao: 0.5).",
    )
    args = parser.parse_args()

    if not KNOWLEDGE_DIR.exists():
        print(f"[rag-watch] ERRO: Diretorio de conhecimento nao encontrado: {KNOWLEDGE_DIR}")
        sys.exit(1)

    if not EMBED_SCRIPT.exists():
        print(f"[rag-watch] ERRO: embed-knowledge.py nao encontrado: {EMBED_SCRIPT}")
        sys.exit(1)

    print("=" * 60)
    print("  RAG Watch  —  ia-arquiteto-hexagon-pattern")
    print("=" * 60)
    print(f"\n  Monitorando: {KNOWLEDGE_DIR.relative_to(REPO_ROOT)}")
    print(f"  Debounce:    {args.debounce}s")
    print(f"  Extensoes:   {', '.join(sorted(WATCHED_EXTENSIONS))}")
    print()

    print_collection_status()

    watched_dirs = [
        str(KNOWLEDGE_DIR / subdir)
        for subdir in DIR_TO_COLLECTION
        if (KNOWLEDGE_DIR / subdir).exists()
    ]
    if not watched_dirs:
        print("[rag-watch] [WARN] Nenhum subdiretorio de colecao encontrado em knowledge/.")
        print("            Estrutura esperada: architecture/, business-rules/, bdd/, decisions/")

    debouncer = _CollectionDebouncer(args.debounce)
    event_handler = _KnowledgeEventHandler(debouncer)
    observer = Observer()

    for d in watched_dirs:
        observer.schedule(event_handler, d, recursive=True)
        print(f"  Watching: {Path(d).relative_to(REPO_ROOT)}")

    observer.start()
    print("\n[rag-watch] Pronto. Salve um .md para disparar o re-embed. Ctrl+C para encerrar.\n")

    try:
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        print("\n[rag-watch] Encerrando...")
        observer.stop()

    observer.join()
    print("[rag-watch] Encerrado.")


if __name__ == "__main__":
    main()
