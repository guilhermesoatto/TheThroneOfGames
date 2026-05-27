# [KNOWLEDGE: DESIGN PATTERNS CATALOG — Business-Triggered]

Este documento cataloga os padrões de design organizados por **pelo problema de negócio que os dispara**, não pela categoria técnica. Como um agente de IA, consulte aqui ao identificar uma necessidade de design. Cada padrão inclui: gatilho de negócio → decisão → stub canônico em TypeScript, C# e Angular (quando aplicável).

---

## GUIA DE DECISÃO RÁPIDA

```
A regra de negócio MUDA conforme o tipo de cliente/contexto? → Strategy
Preciso criar objetos complexos passo a passo?               → Builder
A criação do objeto varia por subtipo?                       → Factory Method
Quero adicionar comportamento sem alterar a classe?          → Decorator
Preciso simplificar uma API complexa de subsistema?          → Facade
Preciso adaptar uma interface externa ao meu domínio?        → Adapter
Múltiplos componentes/serviços precisam reagir a um evento?  → Observer / Domain Event
Quero encapsular uma ação como objeto (desfazível/enfileirável)? → Command
Preciso consultar objetos por critério de negócio reutilizável? → Specification
Preciso garantir apenas 1 instância global?                  → Singleton (⚠️ ver aviso)
Preciso coordenar uma operação longa entre múltiplos domínios? → Saga
Preciso separar leitura e escrita para escalar leitura?      → CQRS
Preciso garantir entrega de evento mesmo se o broker cair?   → Outbox Pattern
O estado de negócio é relevante ao longo do tempo?           → Event Sourcing
```

---

## 1. STRATEGY
**Gatilho de negócio:** "A regra de cálculo muda conforme o tipo de cliente, região, ou configuração — e novos tipos serão adicionados no futuro."

**Quando NÃO usar:** Se a variação é apenas 2 casos que nunca crescerão → use `if/else` simples.

**TypeScript:**
```typescript
// domain/pricing/discount.strategy.ts
export interface IDiscountStrategy {
  calculate(orderAmount: number): number;
}

export class GoldCustomerDiscount implements IDiscountStrategy {
  calculate(amount: number): number { return amount * 0.15; }
}
export class BronzeCustomerDiscount implements IDiscountStrategy {
  calculate(amount: number): number { return amount * 0.05; }
}

// Use Case — não sabe qual estratégia é usada
export class CalculateOrderTotalUseCase {
  constructor(private readonly discount: IDiscountStrategy) {}
  execute(amount: number): number { return amount - this.discount.calculate(amount); }
}
```

**C#:**
```csharp
// Domain/Pricing/IDiscountStrategy.cs
public interface IDiscountStrategy { decimal Calculate(decimal amount); }
public sealed class GoldCustomerDiscount : IDiscountStrategy {
    public decimal Calculate(decimal amount) => amount * 0.15m;
}

// Application/UseCases/CalculateOrderTotalUseCase.cs
public sealed class CalculateOrderTotalUseCase(IDiscountStrategy discount) {
    public decimal Execute(decimal amount) => amount - discount.Calculate(amount);
}
```

**Angular:** O Application Service recebe `IDiscountStrategy` via DI. O `appConfig` faz o binding baseado no tier do cliente logado.

---

## 2. FACTORY METHOD
**Gatilho de negócio:** "A criação de uma entidade ou objeto de valor varia conforme o subtipo — e novos subtipos podem ser adicionados sem mudar o código existente."

**TypeScript:**
```typescript
// domain/notification/notification.factory.ts
export interface INotification { send(message: string): void; }
export class EmailNotification implements INotification { send(msg: string) { /* email */ } }
export class SmsNotification  implements INotification  { send(msg: string) { /* sms */ } }

export class NotificationFactory {
  static create(channel: 'email' | 'sms'): INotification {
    if (channel === 'email') return new EmailNotification();
    if (channel === 'sms')   return new SmsNotification();
    throw new Error(`Unsupported channel: ${channel}`);
  }
}
```

**C#:**
```csharp
public interface INotification { void Send(string message); }
public sealed class NotificationFactory {
    public static INotification Create(string channel) => channel switch {
        "email" => new EmailNotification(),
        "sms"   => new SmsNotification(),
        _       => throw new NotSupportedException($"Channel {channel} not supported.")
    };
}
```

---

## 3. BUILDER
**Gatilho de negócio:** "A criação de um objeto envolve muitos campos opcionais, validações progressivas, ou uma sequência de etapas — e o construtor ficaria com 8+ parâmetros."

**TypeScript:**
```typescript
// domain/report/report.builder.ts
export class ReportBuilder {
  private _title = '';
  private _dateRange?: { from: Date; to: Date };
  private _filters: string[] = [];

  setTitle(title: string): this { this._title = title; return this; }
  setDateRange(from: Date, to: Date): this { this._dateRange = { from, to }; return this; }
  addFilter(filter: string): this { this._filters.push(filter); return this; }
  build(): Report {
    if (!this._title) throw new Error('Report title is required.');
    return new Report(this._title, this._dateRange, this._filters);
  }
}

const report = new ReportBuilder()
  .setTitle('Monthly Sales')
  .setDateRange(new Date('2026-01-01'), new Date('2026-01-31'))
  .addFilter('region=SOUTH')
  .build();
```

**C#:**
```csharp
public sealed class ReportBuilder {
    private string _title = string.Empty;
    private DateTimeOffset? _from, _to;

    public ReportBuilder WithTitle(string title) { _title = title; return this; }
    public ReportBuilder WithDateRange(DateTimeOffset from, DateTimeOffset to) {
        _from = from; _to = to; return this;
    }
    public Report Build() {
        if (string.IsNullOrEmpty(_title)) throw new InvalidOperationException("Title required.");
        return new Report(_title, _from, _to);
    }
}
```

---

## 4. ADAPTER
**Gatilho de negócio:** "Uma API externa, SDK de terceiro, ou sistema legado retorna dados em um formato diferente do modelo de domínio."

**TypeScript:**
```typescript
// infrastructure/gateways/stripe-payment.adapter.ts
import Stripe from 'stripe';
import type { IPaymentGateway, PaymentResult } from '../../application/ports/IPaymentGateway';

export class StripePaymentAdapter implements IPaymentGateway {
  constructor(private readonly stripe: Stripe) {}

  async charge(amount: number, currency: string, token: string): Promise<PaymentResult> {
    const charge = await this.stripe.charges.create({ amount: amount * 100, currency, source: token });
    return { id: charge.id, status: charge.status === 'succeeded' ? 'SUCCESS' : 'FAILED' };
  }
}
```

**C#:**
```csharp
// Infrastructure/Gateways/StripePaymentAdapter.cs
public sealed class StripePaymentAdapter(StripeClient stripe) : IPaymentGateway {
    public async Task<PaymentResult> ChargeAsync(decimal amount, string currency, string token, CancellationToken ct) {
        var options = new ChargeCreateOptions { Amount = (long)(amount * 100), Currency = currency, Source = token };
        var charge = await stripe.V1.Charges.CreateAsync(options, cancellationToken: ct);
        return new PaymentResult(charge.Id, charge.Status == "succeeded" ? PaymentStatus.Success : PaymentStatus.Failed);
    }
}
```

---

## 5. DECORATOR
**Gatilho de negócio:** "Preciso adicionar comportamento transversal (logging, cache, retry, auditoria) a uma operação sem alterar sua implementação."

**TypeScript:**
```typescript
// infrastructure/repositories/cached-user.repository.ts
export class CachedUserRepository implements IUserRepository {
  constructor(
    private readonly inner: IUserRepository,
    private readonly cache: ICacheService,
  ) {}

  async findById(id: string): Promise<User | null> {
    const cached = await this.cache.get<User>(`user:${id}`);
    if (cached) return cached;
    const user = await this.inner.findById(id);
    if (user) await this.cache.set(`user:${id}`, user, 300);
    return user;
  }
}
```

**C#:**
```csharp
public sealed class CachedUserRepository(IUserRepository inner, IDistributedCache cache) : IUserRepository {
    public async Task<User?> FindByIdAsync(Guid id, CancellationToken ct) {
        var key = $"user:{id}";
        var cached = await cache.GetStringAsync(key, ct);
        if (cached is not null) return JsonSerializer.Deserialize<User>(cached);
        var user = await inner.FindByIdAsync(id, ct);
        if (user is not null) await cache.SetStringAsync(key, JsonSerializer.Serialize(user), ct);
        return user;
    }
}
```

---

## 6. FACADE
**Gatilho de negócio:** "Uma operação de negócio envolve coordenar 3+ subsistemas — e o chamador não deve conhecer a complexidade interna."

**TypeScript:**
```typescript
// application/facades/checkout.facade.ts
export class CheckoutFacade {
  constructor(
    private readonly inventory: IInventoryService,
    private readonly payment: IPaymentGateway,
    private readonly notification: INotificationService,
  ) {}

  async execute(command: CheckoutCommand): Promise<Either<DomainError, OrderId>> {
    const reserved = await this.inventory.reserve(command.items);
    if (isLeft(reserved)) return reserved;
    const charged = await this.payment.charge(command.amount, command.token);
    if (isLeft(charged)) { await this.inventory.release(command.items); return charged; }
    await this.notification.send(command.customerId, 'ORDER_CONFIRMED');
    return right(charged.value.orderId);
  }
}
```

---

## 7. OBSERVER / DOMAIN EVENT
**Gatilho de negócio:** "Quando algo acontece no domínio (ex: pedido confirmado), outros processos precisam reagir — mas o domínio não deve conhecer quem reage."

> **Regra:** Domain Events são o mecanismo padrão de comunicação inter-Aggregate neste suitcase. Consulte `docs/ai/architecture.md §2` para o contrato obrigatório.

**TypeScript:**
```typescript
// domain/order/OrderConfirmed.event.ts
export class OrderConfirmedEvent implements IDomainEvent {
  readonly eventType = 'OrderConfirmed';
  readonly eventVersion = 1;
  constructor(
    readonly eventId: string,
    readonly aggregateId: string,
    readonly aggregateName: string,
    readonly occurredAt: Date,
    readonly correlationId: string,
    readonly payload: Readonly<{ customerId: string; totalAmount: number }>,
  ) {}
}
```

**C#:**
```csharp
// Domain/Order/OrderConfirmedEvent.cs
public sealed record OrderConfirmedEvent : DomainEvent {
    public required string CustomerId { get; init; }
    public required decimal TotalAmount { get; init; }
}
```

**Angular (NgRx Action como Observer):**
```typescript
// Emissão
store.dispatch(OrderActions.orderConfirmed({ orderId, customerId }));

// Reação (Effect)
confirmOrder$ = createEffect(() => this.actions$.pipe(
  ofType(OrderActions.orderConfirmed),
  switchMap(({ orderId }) => this.notificationService.send(orderId))
));
```

---

## 8. COMMAND
**Gatilho de negócio:** "Preciso encapsular uma ação como objeto para: enfileirar, registrar em auditoria, ou suportar desfazer (undo)."

**TypeScript:**
```typescript
// application/commands/place-order.command.ts
export interface PlaceOrderCommand {
  readonly customerId: string;
  readonly items: ReadonlyArray<{ productId: string; quantity: number }>;
  readonly paymentToken: string;
}

// Use Case como Command Handler
export class PlaceOrderUseCase {
  async execute(command: PlaceOrderCommand): Promise<Either<DomainError, Order>> { ... }
}
```

---

## 9. REPOSITORY
**Gatilho de negócio:** "Preciso persistir e recuperar Aggregates sem que o domínio conheça o banco de dados."

> **Regra:** Todo Repository é um Port (interface) no `/application`. A implementação concreta fica no `/infrastructure`.

**TypeScript:**
```typescript
// application/ports/IOrderRepository.ts
export interface IOrderRepository {
  save(order: Order): Promise<void>;
  findById(id: string): Promise<Order | null>;
  findByCustomer(customerId: string): Promise<Order[]>;
}
```

**C#:**
```csharp
// Application/Ports/IOrderRepository.cs
public interface IOrderRepository {
    Task SaveAsync(Order order, CancellationToken ct);
    Task<Order?> FindByIdAsync(Guid id, CancellationToken ct);
}
```

---

## 10. SPECIFICATION
**Gatilho de negócio:** "Regras de filtragem ou elegibilidade são complexas, combinadas de formas diferentes (AND, OR, NOT), e precisam ser reutilizadas."

**TypeScript:**
```typescript
// domain/customer/specs/eligible-for-upgrade.spec.ts
export interface ISpecification<T> { isSatisfiedBy(candidate: T): boolean; }

export class EligibleForGoldUpgrade implements ISpecification<Customer> {
  isSatisfiedBy(customer: Customer): boolean {
    return customer.totalSpent > 5000 && customer.accountAgeInDays > 180;
  }
}

export class AndSpecification<T> implements ISpecification<T> {
  constructor(private a: ISpecification<T>, private b: ISpecification<T>) {}
  isSatisfiedBy(candidate: T): boolean { return this.a.isSatisfiedBy(candidate) && this.b.isSatisfiedBy(candidate); }
}
```

---

## 11. SINGLETON ⚠️
**Gatilho de negócio:** "Preciso de uma única instância de um recurso caro (ex: pool de conexões, configuração global)."

> **AVISO:** Singleton é frequentemente um anti-pattern em DDD porque cria acoplamento global e dificulta testes. **Prefira a injeção de dependência do container** (`providedIn: 'root'` em Angular, DI nativo em .NET). Use Singleton explícito apenas para recursos de infraestrutura que realmente precisam de instância única fora do contêiner de DI.

---

## 12. OUTBOX PATTERN
**Gatilho de negócio:** "Preciso garantir que um evento de domínio seja publicado no broker (Kafka, RabbitMQ) MESMO SE o serviço cair após persistir no banco — garantindo consistência eventual."

**Fluxo:**
1. Use Case persiste o Aggregate no DB + insere o evento na tabela `outbox` — na MESMA transação.
2. Um worker (background job) lê a tabela `outbox` e publica no broker.
3. Após publicação confirmada, marca o registro como `published`.

**C# (EF Core):**
```csharp
// Infrastructure/Outbox/OutboxMessage.cs
public sealed class OutboxMessage {
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string EventType { get; init; }
    public required string Payload { get; init; }  // JSON serializado
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }
}

// No Use Case — mesma transação
await dbContext.Orders.AddAsync(order, ct);
await dbContext.OutboxMessages.AddAsync(new OutboxMessage {
    EventType = "OrderPlaced",
    Payload = JsonSerializer.Serialize(new OrderPlacedPayload(order.Id, order.CustomerId))
}, ct);
await dbContext.SaveChangesAsync(ct);
```

---

## 13. SAGA (Process Manager)
**Gatilho de negócio:** "Uma operação de negócio envolve múltiplos Aggregates/serviços em sequência, e precisa de compensação (rollback parcial) se uma etapa falhar."

**Quando usar:** Checkout (reservar estoque → cobrar pagamento → confirmar pedido). Se pagamento falha → liberar estoque.

**Fluxo de Choreography (eventos):**
```
OrderPlaced → [Inventory] → StockReserved → [Payment] → PaymentCharged → [Order] → OrderConfirmed
                                                       ↘ PaymentFailed  → [Inventory] → StockReleased → [Order] → OrderCancelled
```

> **Regra:** Implemente Saga por Choreography (eventos) quando os serviços são poucos e independentes. Use Orchestration (Process Manager central) quando o fluxo é complexo e precisa de visibilidade central.

---

## 14. CQRS (Command Query Responsibility Segregation)
**Gatilho de negócio:** "As queries (leitura) têm requisitos de performance, formato ou escalabilidade muito diferentes das commands (escrita)."

**Quando usar:** Dashboards com agregações complexas, relatórios, listagens paginadas com filtros. Não use CQRS para CRUDs simples.

**TypeScript (separação de modelos):**
```typescript
// Command side (escreve em Aggregate — modelo rico)
class PlaceOrderUseCase { async execute(cmd: PlaceOrderCommand): Promise<void> { ... } }

// Query side (lê diretamente do banco com SQL otimizado — modelo anêmico para leitura)
class GetOrderSummaryQuery {
  async execute(customerId: string): Promise<OrderSummaryDto[]> {
    return this.db.query(`SELECT id, total, status FROM orders WHERE customer_id = $1`, [customerId]);
  }
}
```

---

## 15. EVENT SOURCING
**Gatilho de negócio:** "O histórico completo de mudanças de estado de uma entidade é tão importante quanto o estado atual (ex: conta bancária, histórico de preços, auditoria regulatória)."

**Quando NÃO usar:** Para a maioria dos CRUDs. Event Sourcing adiciona complexidade significativa. Use apenas quando a auditabilidade completa é um requisito não-funcional explícito.

**Conceito chave:** O estado atual do Aggregate é reconstruído aplicando todos os eventos passados em sequência:
```typescript
const account = events.reduce((state, event) => applyEvent(state, event), initialState);
```

---

## 16. SMART / DUMB COMPONENTS (Angular)
**Gatilho de negócio:** "Um componente de UI precisa tanto orquestrar dados quanto renderizá-los — causando mistura de responsabilidades e dificultando reutilização."

> **Regra Angular:** Separe sempre em Smart Component (injeta serviços, gerencia estado) e Dumb Component (recebe `@Input()`, emite `@Output()`).

Consulte `angular/docs/ai/architecture.md §3` para o padrão canônico.

---

## 17. NGRX STORE PATTERN (Angular)
**Gatilho de negócio:** "Múltiplos componentes em diferentes partes da aplicação precisam compartilhar e reagir ao mesmo estado."

**Quando NÃO usar:** Estado confinado a um único componente ou feature. Use `signal()` local.

**Estrutura obrigatória:**
```typescript
// Actions
export const OrderActions = createActionGroup({
  source: 'Order',
  events: {
    'Place Order': props<{ command: PlaceOrderCommand }>(),
    'Place Order Success': props<{ order: Order }>(),
    'Place Order Failure': props<{ error: string }>(),
  },
});

// Reducer (função pura)
export const orderReducer = createReducer(
  initialState,
  on(OrderActions.placeOrderSuccess, (state, { order }) => ({ ...state, orders: [...state.orders, order] })),
);

// Effect (I/O)
placeOrder$ = createEffect(() => this.actions$.pipe(
  ofType(OrderActions.placeOrder),
  switchMap(({ command }) => this.orderService.place(command).pipe(
    map(result => result.success ? OrderActions.placeOrderSuccess({ order: result.value }) : OrderActions.placeOrderFailure({ error: result.error }))
  ))
));
```
