# ADR-0000 — [AggregateName] — [Phase]

> **Gerado por:** `python tools/reflect.py --prd docs/ai/tasks/prd-[nome].json --phase [fase]`  
> **Data:** YYYY-MM-DD  
> **Status:** Accepted

---

## Contexto

Descreva o contexto e o problema que levou a esta decisão. Inclua:
- Qual fase do aggregate foi completada
- Quais alternativas foram consideradas
- Quais constraints existiam (performance, segurança, coerência com domínio)

## Decisão

**O que foi decidido e por quê.**

Exemplo:
> `PointsAmount` é um Value Object imutável representando quantidade de pontos (inteiro não-negativo).
> Validação ocorre no construtor; valores inválidos lançam `DomainError`.
> Escolhido em vez de `number` primitivo para garantir invariantes no nível de tipo.

## Consequências

### Positivas
- ...

### Negativas / Trade-offs
- ...

## Artefatos gerados

| Tipo | Caminho |
|---|---|
| Entidade / VO | `src/domain/models/[Name].ts` |
| Teste | `src/domain/models/[Name].test.ts` |
| Evento | `src/domain/events/[Event].ts` |
| Use Case | `src/application/use-cases/[Name]UseCase.ts` |
| Port | `src/domain/ports/I[Name].ts` |

## Supersede / Relacionados

- Supersede: *(nenhum)*
- Relacionado a: *(ADR-000X se houver)*
