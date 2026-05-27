# Knowledge Base — ChromaDB Semantic Memory (Multi-Collection)

Esta pasta contém a **base de conhecimento vetorial** do suitcase, particionada em 4 coleções ChromaDB por domínio de consulta. O agente consulta a coleção certa baseado na intenção da query.

## Estrutura

```
docs/ai/knowledge/
├── README.md                        # Este arquivo
├── CONTEXT.md                       # Fast Context primer — lido pelo agente no início de cada sessão
├── architecture/                    → Coleção ChromaDB: "architecture"
│   ├── design-patterns.md           # Catálogo de patterns com gatilhos de negócio
│   └── database.md                  # Repository, Outbox, Migrations, Schema Isolation
├── business-rules/                  → Coleção ChromaDB: "business-rules"
│   └── ubiquitous-language.md       # Glossário de domínio (preenchido pelo Domain Expert)
├── bdd/                             → Coleção ChromaDB: "bdd"
│   └── [aggregate].feature          # Cenários Gherkin gerados pelo agente (Given/When/Then)
├── decisions/                       → Coleção ChromaDB: "decision-log"
│   └── ADR-NNNN.md                  # Architecture Decision Records (gerados por reflect.py)
└── chroma/                          # Banco vetorial ChromaDB (commitado!)
```

## Roteamento de consultas por intenção

| Intenção da query | Coleção alvo | Exemplo |
|---|---|---|
| "qual pattern para X" / decisão estrutural | `architecture` | `--collection architecture` |
| "como se chama X no domínio" / terminologia | `business-rules` | `--collection business-rules` |
| "o que foi decidido sobre X" / histórico | `decision-log` | `--collection decision-log` |
| "cenário de teste para X" / BDD | `bdd` | `--collection bdd` |
| Dúvida cruzada | todas | `--all` |

## Fluxo de uso

```
Domain Expert edita .md  →  python tools/embed-knowledge.py  →  4 coleções atualizadas
                                                                         ↓
                                              Agente consulta coleção específica:
                                              python tools/query-knowledge.py "..." --collection architecture

Fase aprovada no prd.json  →  python tools/reflect.py --prd ... --phase ...
                                        ↓                           ↓
                               decisions/ADR-NNNN.md       CONTEXT.md atualizado
                                        ↓
                               decision-log re-embedado
```

## Adicionando conhecimento

1. Edite (ou crie) um arquivo `.md` na subpasta correta (`architecture/`, `business-rules/`, etc.).
2. Use headings `##` e `###` para demarcar seções — cada seção vira um chunk no banco.
3. Rode o embedder (re-embeda todas as coleções):
   ```bash
   pip install -r tools/requirements.txt   # apenas uma vez
   python tools/embed-knowledge.py
   ```
4. Commit: `git add docs/ai/knowledge/ && git commit -m "knowledge: add [descrição]"`

## Testando queries

```bash
# Coleção específica
python tools/query-knowledge.py "quando usar o padrão Outbox" --collection architecture
python tools/query-knowledge.py "o que é LoyaltyAccount" --collection business-rules
python tools/query-knowledge.py "decisão sobre schema de eventos" --collection decision-log

# Todas as coleções
python tools/query-knowledge.py "pontos de fidelidade" --all
python tools/query-knowledge.py "nome correto da entidade de pedido" --source ubiquitous-language
python tools/query-knowledge.py "saga choreography vs orchestration" --n 5
```

## Por que commitar o chroma.db?

- Zero dependência de API externa — o banco roda completamente offline.
- Qualquer desenvolvedor que clonar o repo já tem o banco pronto.
- O modelo de embedding (`all-MiniLM-L6-v2`) é baixado uma vez pelo `sentence-transformers` e cacheado localmente.
- Se o `.md` não mudou, re-executar `embed-knowledge.py` é idempotente (upsert por hash).

## Notas sobre tamanho

O `chroma.db` para este suitcase começa em ~5-10 MB. Para projetos com documentação grande (centenas de páginas), considere usar o script de query apenas em CI e não commitar o banco — mas para uso típico de arquitetura o tamanho é insignificante.
