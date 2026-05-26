"""
reflect.py
==========
Reflexion Loop (Shinn et al., 2023 pattern adapted for DDD suitcases).

Executado pelo agente apos cada fase aprovada pelo Domain Expert no fluxo
do Ralph Pattern (new-aggregate.md). Realiza 4 acoes:

  1. Gera um Architecture Decision Record (ADR) em docs/ai/knowledge/decisions/
  2. Re-embeda a colecao "decision-log" no ChromaDB
  3. Atualiza docs/ai/knowledge/CONTEXT.md (fast context primer)
  4. Registra arquivos Gherkin novos (.feature) na colecao "bdd" se existirem

Uso:
    python tools/reflect.py --prd docs/ai/tasks/prd-loyalty.json --phase 1-domain
    python tools/reflect.py --prd docs/ai/tasks/prd-order.json --phase 2-use-cases

Fases validas: 1-domain | 2-use-cases | 3-infrastructure | 4-presentation

Dependencias:
    pip install -r tools/requirements.txt
    (embed-knowledge.py deve ter sido executado ao menos uma vez antes)
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from datetime import datetime, timezone
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
KNOWLEDGE_DIR = REPO_ROOT / "docs" / "ai" / "knowledge"
DECISIONS_DIR = KNOWLEDGE_DIR / "decisions"
BDD_DIR = KNOWLEDGE_DIR / "bdd"
CONTEXT_FILE = KNOWLEDGE_DIR / "CONTEXT.md"
CHROMA_DIR = KNOWLEDGE_DIR / "chroma"
TASKS_DIR = REPO_ROOT / "docs" / "ai" / "tasks"

VALID_PHASES = ["1-domain", "2-use-cases", "3-infrastructure", "4-presentation"]

PHASE_LABELS = {
    "1-domain": "Domain Layer",
    "2-use-cases": "Application Layer (Use Cases)",
    "3-infrastructure": "Infrastructure Layer (Adapters)",
    "4-presentation": "Presentation Layer (Controllers)",
}


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def load_prd(prd_path: Path) -> dict:
    if not prd_path.exists():
        print(f"[reflect] ERRO: prd.json nao encontrado: {prd_path}")
        sys.exit(1)
    with open(prd_path, encoding="utf-8") as f:
        return json.load(f)


def next_adr_number(decisions_dir: Path) -> int:
    decisions_dir.mkdir(parents=True, exist_ok=True)
    existing = list(decisions_dir.glob("ADR-*.md"))
    if not existing:
        return 1
    numbers = []
    for f in existing:
        m = re.match(r"ADR-(\d+)", f.stem)
        if m:
            numbers.append(int(m.group(1)))
    return max(numbers) + 1 if numbers else 1


def tasks_for_phase(prd: dict, phase: str) -> list[dict]:
    return [t for t in prd.get("tasks", []) if t.get("phase") == phase]


def completed_tasks(tasks: list[dict]) -> list[dict]:
    return [t for t in tasks if t.get("passes") is True]


def rejected_tasks(tasks: list[dict]) -> list[dict]:
    return [t for t in tasks if t.get("rejectionReason")]


# ---------------------------------------------------------------------------
# ADR Generation
# ---------------------------------------------------------------------------

def generate_adr(prd: dict, phase: str, adr_number: int) -> str:
    now = datetime.now(timezone.utc).strftime("%Y-%m-%d")
    aggregate = prd.get("aggregate", "Unknown")
    context = prd.get("boundedContext", "unknown")
    phase_label = PHASE_LABELS.get(phase, phase)
    adr_id = f"ADR-{adr_number:04d}"

    phase_tasks = tasks_for_phase(prd, phase)
    done = completed_tasks(phase_tasks)
    rejected = rejected_tasks(phase_tasks)

    artifacts = []
    for t in done:
        artifacts.extend(t.get("artifacts", []))

    business_rules = prd.get("acceptanceCriteria", {})

    lines = [
        f"# {adr_id}: {aggregate} — {phase_label}",
        "",
        f"**Status:** Accepted",
        f"**Date:** {now}",
        f"**Aggregate:** {aggregate}",
        f"**Bounded Context:** {context}",
        f"**Phase:** {phase}",
        "",
        "---",
        "",
        "## Context",
        "",
        f"Phase `{phase}` of aggregate `{aggregate}` was completed and approved by the Domain Expert.",
        f"This ADR captures the decisions made during the {phase_label} implementation.",
        "",
        "## Decisions Made",
        "",
    ]

    for t in done:
        lines.append(f"- **{t.get('description', t.get('id', '?'))}**")
        if t.get("testCommand"):
            lines.append(f"  - Test: `{t['testCommand']}`")

    lines += ["", "## Artifacts Produced", ""]
    for a in artifacts:
        lines.append(f"- `{a}`")

    if rejected:
        lines += ["", "## Alternatives Considered (Rejected)", ""]
        for t in rejected:
            reason = t.get("rejectionReason", "No reason recorded")
            lines.append(f"- **{t.get('description', t.get('id', '?'))}**: {reason}")

    lines += ["", "## Acceptance Criteria (from prd.json)", ""]
    phase_key = phase.split("-")[0] if "-" in phase else phase
    phase_criteria = business_rules.get(
        "domain" if phase == "1-domain" else
        "application" if phase == "2-use-cases" else
        "infrastructure" if phase == "3-infrastructure" else
        "deployment",
        {}
    )
    if phase_criteria:
        for k, v in phase_criteria.items():
            status = "OK" if v is True else ("PENDING" if v is None else "FAIL")
            lines.append(f"- `{k}`: {status}")
    else:
        lines.append("- *(criteria not defined for this phase)*")

    lines += [
        "",
        "## Consequences",
        "",
        f"- The `{aggregate}` aggregate now has a complete {phase_label} implementation.",
        "- Subsequent phases must not violate the layer boundaries established here.",
        "- This ADR is embedded in the `decision-log` ChromaDB collection for semantic retrieval.",
    ]

    return "\n".join(lines)


# ---------------------------------------------------------------------------
# CONTEXT.md Update
# ---------------------------------------------------------------------------

def parse_context(content: str) -> dict:
    """Parse CONTEXT.md into mutable sections."""
    return {"raw": content}


def update_context(prd: dict, phase: str, adr_number: int, adr_path: Path) -> None:
    if not CONTEXT_FILE.exists():
        print(f"[reflect] [WARN] CONTEXT.md nao encontrado, pulando atualizacao.")
        return

    aggregate = prd.get("aggregate", "Unknown")
    context_bc = prd.get("boundedContext", "unknown")
    prd_file = f"docs/ai/tasks/prd-{aggregate.lower()}.json"
    phase_label = PHASE_LABELS.get(phase, phase)
    now = datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M UTC")
    adr_rel = adr_path.relative_to(REPO_ROOT).as_posix()
    adr_id = f"ADR-{adr_number:04d}"

    content = CONTEXT_FILE.read_text(encoding="utf-8")

    # Update last updated timestamp
    content = re.sub(
        r"Última atualização:.*",
        f"Última atualização: {now}",
        content,
    )

    # Update active aggregate count
    agg_count_match = re.search(r"Total de Aggregates ativos: (\d+)", content)
    if agg_count_match:
        current_count = int(agg_count_match.group(1))
        # Only increment if aggregate not already listed
        if aggregate not in content:
            content = content.replace(
                f"Total de Aggregates ativos: {current_count}",
                f"Total de Aggregates ativos: {current_count + 1}",
            )

    # Update ADR count
    adr_count_match = re.search(r"Total de ADRs registrados: (\d+)", content)
    if adr_count_match:
        current_adr = int(adr_count_match.group(1))
        content = content.replace(
            f"Total de ADRs registrados: {current_adr}",
            f"Total de ADRs registrados: {current_adr + 1}",
        )

    # Replace placeholder row in Aggregates table or add new row
    placeholder = "| *(nenhum ainda)* | — | — | — | — |"
    new_row = f"| {aggregate} | {context_bc} | {phase_label} | active | `{prd_file}` |"
    if placeholder in content:
        content = content.replace(placeholder, new_row)
    elif aggregate not in content:
        # Append after last | row in the aggregate table
        content = re.sub(
            r"(## Aggregates Ativos\n.*?\n\|.*?\|\n)((\|.*?\|\n)*)",
            lambda m: m.group(0) + new_row + "\n",
            content,
            flags=re.DOTALL,
        )

    # Replace placeholder row in Decisions table or prepend new row
    adr_placeholder = "| *(nenhuma ainda)* | — | — | — | — |"
    short_phase = phase_label.split(" ")[0]
    new_decision = f"| {adr_number} | [{adr_id}]({adr_rel}) | {aggregate} | {now[:10]} | {short_phase} layer approved |"
    if adr_placeholder in content:
        content = content.replace(adr_placeholder, new_decision)
    else:
        # Insert before last decision row block to keep "last 5" ordered
        content = re.sub(
            r"(## Últimas 5 Decisões Arquiteturais\n.*?\n\|.*?\|\n\|.*?\|\n)(\|.*?\|\n)(\|.*?\|\n)(\|.*?\|\n)(\|.*?\|\n)(\|.*?\|\n)",
            lambda m: m.group(1) + new_decision + "\n" + m.group(2) + m.group(3) + m.group(4) + m.group(5),
            content,
            flags=re.DOTALL,
        )
        # Fallback: simple append after header rows
        if new_decision not in content:
            content = content.replace(
                "| *(nenhuma ainda)* |",
                new_decision,
            )
            if new_decision not in content:
                # Just append before the Constraints section
                content = content.replace(
                    "## Constraints Ativas",
                    new_decision + "\n\n## Constraints Ativas",
                )

    CONTEXT_FILE.write_text(content, encoding="utf-8")
    print(f"[reflect] CONTEXT.md atualizado.")


# ---------------------------------------------------------------------------
# ChromaDB embedding of new ADR
# ---------------------------------------------------------------------------

def embed_adr(adr_path: Path, adr_content: str, model=None) -> None:
    """Embeda o novo ADR na colecao decision-log do ChromaDB."""
    if not CHROMA_DIR.exists():
        print("[reflect] [WARN] ChromaDB nao encontrado — pulando embedding do ADR.")
        print("          Execute embed-knowledge.py para inicializar o banco.")
        return

    try:
        import chromadb
        from chromadb.config import Settings

        client = chromadb.PersistentClient(
            path=str(CHROMA_DIR),
            settings=Settings(anonymized_telemetry=False),
        )
        collection = client.get_or_create_collection(
            name="decision-log",
            metadata={"hnsw:space": "cosine"},
        )

        if model is None:
            from sentence_transformers import SentenceTransformer
            model = SentenceTransformer("all-MiniLM-L6-v2")
        relative_path = adr_path.relative_to(REPO_ROOT).as_posix()

        raw = f"{relative_path}::ADR::{adr_content[:100]}"
        doc_id = hashlib.sha256(raw.encode()).hexdigest()[:16]

        embedding = model.encode([adr_content]).tolist()
        collection.upsert(
            ids=[doc_id],
            documents=[adr_content],
            embeddings=embedding,
            metadatas=[{"source": relative_path, "heading": "ADR", "collection": "decision-log"}],
        )
        print(f"[reflect] ADR embedado na colecao 'decision-log' ({collection.count()} docs total).")

    except ImportError:
        print("[reflect] [WARN] chromadb/sentence-transformers nao instalados — pulando embedding.")


def embed_bdd_if_present(aggregate: str, model=None) -> None:
    """Re-embeda arquivos .feature do aggregate na colecao bdd, se existirem."""
    feature_files = list(BDD_DIR.glob(f"{aggregate.lower()}*.feature")) + \
                    list(BDD_DIR.glob(f"{aggregate}*.feature"))
    if not feature_files:
        return

    if not CHROMA_DIR.exists():
        return

    try:
        import chromadb
        from chromadb.config import Settings

        client = chromadb.PersistentClient(
            path=str(CHROMA_DIR),
            settings=Settings(anonymized_telemetry=False),
        )
        collection = client.get_or_create_collection(
            name="bdd",
            metadata={"hnsw:space": "cosine"},
        )
        if model is None:
            from sentence_transformers import SentenceTransformer
            model = SentenceTransformer("all-MiniLM-L6-v2")

        for feature_path in feature_files:
            content = feature_path.read_text(encoding="utf-8")
            relative_path = feature_path.relative_to(REPO_ROOT).as_posix()
            raw = f"{relative_path}::BDD::{content[:100]}"
            doc_id = hashlib.sha256(raw.encode()).hexdigest()[:16]
            embedding = model.encode([content]).tolist()
            collection.upsert(
                ids=[doc_id],
                documents=[content],
                embeddings=embedding,
                metadatas=[{"source": relative_path, "heading": "Feature", "collection": "bdd"}],
            )
            print(f"[reflect] BDD feature embedada: {relative_path}")

    except ImportError:
        pass


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main() -> None:
    parser = argparse.ArgumentParser(description="Reflexion Loop: gera ADR e atualiza CONTEXT.md apos fase aprovada.")
    parser.add_argument("--prd", required=True, help="Caminho para o prd-[aggregate].json")
    parser.add_argument("--phase", required=True, choices=VALID_PHASES, help="Fase que foi aprovada.")
    parser.add_argument(
        "--force", action="store_true",
        help="Sobrepoe ADR existente para a mesma fase (idempotencia desativada).",
    )
    args = parser.parse_args()

    prd_path = REPO_ROOT / args.prd if not Path(args.prd).is_absolute() else Path(args.prd)
    prd = load_prd(prd_path)
    aggregate = prd.get("aggregate", "Unknown")

    print(f"[reflect] Aggregate: {aggregate}")
    print(f"[reflect] Phase aprovada: {args.phase} ({PHASE_LABELS.get(args.phase, '')})")

    # Idempotency guard: checar se ADR para este aggregate+phase ja existe
    DECISIONS_DIR.mkdir(parents=True, exist_ok=True)
    existing_adrs = list(DECISIONS_DIR.glob(f"ADR-*-{aggregate}-{args.phase}.md"))
    if existing_adrs and not args.force:
        print(f"[reflect] [WARN] ADR para '{aggregate}' fase '{args.phase}' ja existe:")
        for f in existing_adrs:
            print(f"          {f.relative_to(REPO_ROOT)}")
        print("          Use --force para sobrepor.")
        sys.exit(0)

    phase_tasks = tasks_for_phase(prd, args.phase)
    done = completed_tasks(phase_tasks)
    if not done:
        print(f"[reflect] [WARN] Nenhuma tarefa com passes=true na fase '{args.phase}'.")
        print("          Verifique se o prd.json esta atualizado antes de refletir.")
        sys.exit(1)

    print(f"[reflect] Tarefas concluidas nesta fase: {len(done)}")

    # Carregar modelo UMA VEZ e passar para as funcoes de embedding
    try:
        from sentence_transformers import SentenceTransformer
        print("[reflect] Carregando modelo de embedding...")
        model = SentenceTransformer("all-MiniLM-L6-v2")
    except ImportError:
        model = None
        print("[reflect] [WARN] sentence-transformers nao instalado — ADR nao sera embedado.")

    # 1. Gerar ADR
    adr_number = next_adr_number(DECISIONS_DIR)
    adr_content = generate_adr(prd, args.phase, adr_number)
    adr_filename = f"ADR-{adr_number:04d}-{aggregate}-{args.phase}.md"
    adr_path = DECISIONS_DIR / adr_filename

    DECISIONS_DIR.mkdir(parents=True, exist_ok=True)
    adr_path.write_text(adr_content, encoding="utf-8")
    print(f"[reflect] ADR gerado: {adr_path.relative_to(REPO_ROOT)}")

    # 2. Embeda ADR na colecao decision-log
    embed_adr(adr_path, adr_content, model=model)

    # 3. Atualiza CONTEXT.md
    update_context(prd, args.phase, adr_number, adr_path)

    # 4. Re-embeda .feature files se existirem
    embed_bdd_if_present(aggregate, model=model)

    print(f"\n[reflect] Reflexion completa. Commit os seguintes arquivos:")
    print(f"  git add {adr_path.relative_to(REPO_ROOT)}")
    print(f"  git add {CONTEXT_FILE.relative_to(REPO_ROOT)}")
    print(f"  git add {CHROMA_DIR.relative_to(REPO_ROOT)}")
    print(f'  git commit -m "reflect({aggregate}): phase {args.phase} approved — {adr_filename}"')


if __name__ == "__main__":
    main()
