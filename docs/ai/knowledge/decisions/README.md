# Architecture Decision Records (ADRs)

Esta pasta contém os **Architecture Decision Records** gerados automaticamente pelo Reflexion Loop após cada fase aprovada pelo Domain Expert.

## Como funciona

```
Domain Expert aprova fase no prd.json
          ↓
python tools/reflect.py --prd docs/ai/tasks/prd-[nome].json --phase [fase]
          ↓
┌─────────────────────────────┐
│ Gera ADR-NNNN-[agg]-[fase].md │  ← nesta pasta
│ Embeda no decision-log      │  ← ChromaDB atualizado
│ Atualiza CONTEXT.md         │  ← contador de ADRs + tabelas
│ Re-embeda cenários BDD      │  ← se .feature existir
└─────────────────────────────┘
```

## Convenção de nomes

```
ADR-NNNN-[AggregateName]-[phase].md

Exemplos:
  ADR-0001-LoyaltyAccount-1-domain.md
  ADR-0002-LoyaltyAccount-2-use-cases.md
  ADR-0003-RedemptionOrder-1-domain.md
```

Fases válidas: `1-domain` | `2-use-cases` | `3-infrastructure` | `4-presentation`

## Consultando ADRs via ChromaDB

```bash
# O que foi decidido sobre um aggregate específico?
python tools/query-knowledge.py "decisões sobre LoyaltyAccount" --collection decision-log

# Qual padrão foi escolhido para persistência?
python tools/query-knowledge.py "repositório outbox schema" --collection decision-log

# Busca cruzada (decisão + arquitetura)
python tools/query-knowledge.py "Either pattern uso" --all
```

## Regras

- **Nunca edite um ADR gerado.** Ele é registro histórico imutável.
- **Para reverter uma decisão:** crie um novo ADR que supersede o anterior.
- **Conflito com ADR existente:** pare e consulte o Domain Expert — não assuma.
- `reflect.py` é idempotente: recusa sobrescrever um ADR existente a menos que `--force` seja passado.

---

## Template — ADR-0000 (referência)

Salvo em: `docs/ai/knowledge/decisions/ADR-0000-Template.md`
