# [SKILL: EVENT VERSIONING — Schema Evolution Without Breaking Consumers]

**Camada:** Layer 3 — Implementation Skill  
**Depende de:** `docs/ai/architecture.md §2` (contrato `IDomainEvent`)  
**Quando usar:** Sempre que for necessário evoluir o schema de um Domain Event já publicado em produção.

---

## REGRA DE OURO

> **Nunca altere um evento já consumido em produção. Evolua para frente.**

Um event schema publicado é um contrato. Quebrá-lo significa quebrar todos os consumers que já estão em produção sem nenhum aviso. A estratégia padrão é **Payload Expansion** — campos novos são sempre opcionais e backward-compatible.

---

## ÁRVORE DE DECISÃO: QUAL ESTRATÉGIA USAR?

```
Preciso evoluir o evento X?
│
├─ Estou apenas ADICIONANDO campos ao payload?
│  └─ Sim → [ESTRATÉGIA 1] Payload Expansion (same eventVersion)
│
├─ Estou RENOMEANDO um campo existente?
│  └─ Sim → [ESTRATÉGIA 2] Dual-Write Transition Period
│
├─ Estou REMOVENDO um campo ou ALTERANDO seu tipo?
│  └─ Sim → [ESTRATÉGIA 3] Parallel Version (novo eventType com versão)
│
└─ Estou mudando a SEMÂNTICA do evento (o que ele significa)?
   └─ Sim → [ESTRATÉGIA 3] + novo nome de evento (não reutilize o mesmo eventType)
```

---

## ESTRATÉGIA 1: PAYLOAD EXPANSION (campos opcionais)

**Quando usar:** adição de novos campos sem remover nenhum existente.  
**Impacto nos consumers:** zero — campos novos são ignorados por consumers antigos.  
**Regra:** campo novo → SEMPRE `optional` no TypeScript (`campo?: Tipo`).

### Antes (v1 em produção)
```typescript
// OrderPaid.event.ts
export interface OrderPaidPayload {
  readonly orderId: string;
  readonly amount: number;
  readonly currency: string;
}
```

### Depois (ainda v1 — apenas expande)
```typescript
// OrderPaid.event.ts
export interface OrderPaidPayload {
  readonly orderId: string;
  readonly amount: number;
  readonly currency: string;
  readonly customerId?: string;          // NOVO — opcional para backward compat
  readonly paymentMethod?: 'card' | 'pix' | 'boleto'; // NOVO — opcional
}
```

**NÃO incrementar `eventVersion`.** Consumers antigos desconhecem os novos campos e continuam funcionando.

---

## ESTRATÉGIA 2: DUAL-WRITE (renomeação de campo)

**Quando usar:** renomear um campo que consumers dependem.  
**Duração da transição:** manter ambos os campos até todos os consumers migrarem (definir deadline).

### Passo 1 — Dual-Write (período de transição)
```typescript
export interface OrderPaidPayload {
  readonly orderId: string;
  readonly amount: number;
  readonly currency: string;
  /** @deprecated Use `buyerId` instead. Remove after 2026-07-01. */
  readonly customerId?: string;  // campo antigo — manter até deadline
  readonly buyerId: string;      // novo nome canônico
}
```

### Passo 2 — Publisher emite os dois campos
```typescript
// Na camada de infraestrutura (EventPublisher Adapter)
const payload: OrderPaidPayload = {
  orderId: order.id.value,
  amount: order.totalAmount.value,
  currency: order.currency.value,
  buyerId: order.buyerId.value,
  customerId: order.buyerId.value, // dual-write: preenche o campo legado também
};
```

### Passo 3 — Após deadline: remover campo legado → agora sim incrementar `eventVersion`
```typescript
// eventVersion: 1 → 2 (breaking change: remoção do campo legado)
export interface OrderPaidPayload {
  readonly orderId: string;
  readonly amount: number;
  readonly currency: string;
  readonly buyerId: string;
}
```

---

## ESTRATÉGIA 3: PARALLEL VERSION (breaking change)

**Quando usar:** remoção de campos obrigatórios, alteração de tipo, ou mudança semântica profunda.  
**Abordagem:** publicar evento com novo `eventType` versioned. Manter o v1 ativo até consumers migrarem.

### Convenção de nomenclatura
```
eventType: "OrderPaid"   → eventVersion: 1
eventType: "OrderPaidV2" → eventVersion: 1  (é um novo tipo! version no nome, não no campo)
```

> **Por que `eventType` versioned e não só `eventVersion`?**  
> Brokers (Kafka, RabbitMQ) roteiam por `eventType`. Se mudar apenas `eventVersion`, subs antigos  
> ainda recebem o evento e quebram ao tentar desserializar o novo schema.

### Implementação TypeScript
```typescript
// OrderPaid.event.ts — V1 (não alterar, consumers legados precisam)
export interface OrderPaidV1Payload {
  readonly orderId: string;
  readonly customerId: string;  // campo legado que será removido
  readonly amount: number;
}

// OrderPaidV2.event.ts — V2 (novo evento, topic separado no broker)
export interface OrderPaidV2Payload {
  readonly orderId: string;
  readonly buyerId: string;      // renomeado de customerId
  readonly amount: number;
  readonly currency: string;     // novo campo obrigatório
}

export class OrderPaidV2 implements IDomainEvent {
  readonly eventType = 'OrderPaidV2';
  readonly eventVersion = 1; // versão do schema DESTE evento (começa em 1)
  // ...
}
```

### Routing no broker (Kafka example)
```
Topic: order.paid.v1   ← consumers legados continuam aqui
Topic: order.paid.v2   ← consumers novos migram para cá
```

### Ciclo de vida do evento legado (V1)
1. Anunciar deprecação (data de corte no payload como comentário `@deprecated`)
2. Monitorar: zero consumers lendo `order.paid.v1` → corte seguro
3. Parar de publicar no V1
4. Arquivar (não deletar) o schema V1 em `docs/ai/knowledge/event-schemas/`

---

## REGRAS ESTRITAS (não negociáveis)

| # | Regra |
|---|---|
| R1 | **Nunca** remover um campo de um evento sem passar pelo período de Dual-Write |
| R2 | **Nunca** alterar o tipo de um campo existente em produção (use Parallel Version) |
| R3 | **Nunca** reutilizar um `eventType` descontinuado com schema diferente |
| R4 | Campos novos são **sempre** `optional` (nunca `required`) em Payload Expansion |
| R5 | O campo `eventVersion` reflete a versão do **schema do tipo específico**, não a "geração histórica" |
| R6 | A skill `opentelemetry-logs.md` se aplica: logar `eventType`, `eventVersion` e `correlationId` ao publicar |

---

## ANTENA DE DETECÇÃO: quando o agente deve ativar esta skill

O agente DEVE consultar esta skill sempre que:
- Uma tarefa do `prd.json` incluir "alterar", "renomear", "remover", ou "adicionar campo" em um evento existente
- A phase for `1-domain` e os arquivos `*.event.ts` existentes já tiverem consumers listados no `prd.json`
- O Domain Expert mencionar "o evento mudou" ou "preciso de um novo campo no evento"

---

## REFERÊNCIAS
- `docs/ai/architecture.md §2` — Contrato `IDomainEvent` e regras básicas de versionamento
- `docs/ai/skills/opentelemetry-logs.md` — Propagação de `correlationId` em eventos
- `docs/ai/knowledge/design-patterns.md` — Padrão Outbox (garante at-least-once delivery)
