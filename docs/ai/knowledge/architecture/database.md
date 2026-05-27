# [KNOWLEDGE: DATABASE PATTERNS — PostgreSQL + EF Core + DDD]

> Consulte este arquivo via `docs/ai/skills/chroma-search.md` antes de implementar qualquer camada de persistência.
> Banco de dados escolhido: **PostgreSQL 16** — free, K8s-native (CloudNativePG), EF Core Npgsql.

---

## Repository Pattern

**Gatilho de negócio:** _"Precisamos salvar e buscar [Entidade] sem que o domínio saiba onde ela está guardada."_

**Regras invioláveis:**
- O Repository retorna o **Aggregate completo** (nunca parcial). Lazy loading é proibido.
- O Repository aceita e retorna somente o **Aggregate Root** — nunca entidades filhas diretamente.
- A interface (`IXxxRepository`) mora em `Application/Ports/`. A implementação (`EfCoreXxxRepository`) mora em `Infrastructure/Repositories/`.
- Operações de leitura em repositórios usam `AsNoTracking()` quando não haverá persistência posterior.

**C# stub:**
```csharp
// Application/Ports/ILoyaltyAccountRepository.cs
public interface ILoyaltyAccountRepository
{
    Task<LoyaltyAccount?> FindByIdAsync(string id, CancellationToken ct = default);
    Task SaveAsync(LoyaltyAccount account, CancellationToken ct = default);
}

// Infrastructure/Repositories/EfCoreLoyaltyAccountRepository.cs
public sealed class EfCoreLoyaltyAccountRepository : ILoyaltyAccountRepository
{
    private readonly LoyaltyDbContext _db;
    public EfCoreLoyaltyAccountRepository(LoyaltyDbContext db) => _db = db;

    public async Task<LoyaltyAccount?> FindByIdAsync(string id, CancellationToken ct)
        => await _db.LoyaltyAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task SaveAsync(LoyaltyAccount account, CancellationToken ct)
    {
        _db.LoyaltyAccounts.Update(account);
        await _db.SaveChangesAsync(ct);
    }
}
```

**Quando NÃO usar:** Se você só precisa de leitura projetada (relatórios, dashboards) — use uma Query direta com Dapper ou EF Core raw SQL bypass; não force um Aggregate completo para isso (CQRS Read Side).

---

## Unit of Work Pattern

**Gatilho de negócio:** _"Creditar os pontos E registrar a transação precisam ser atômicos — ou os dois acontecem, ou nenhum."_

**Regras:**
- `DbContext` já é uma UoW nativa no EF Core. Não crie uma UoW wrapper manual desnecessária.
- Um Use Case chama `SaveChangesAsync()` **uma única vez** ao final — nunca dentro do Repository.
- Use transações explícitas somente para operações que envolvem múltiplos DbContexts ou Aggregates (caso raro — questione o design se isso acontecer com frequência).

**C# stub:**
```csharp
// Use Case chama repository.SaveAsync() que internamente chama db.SaveChangesAsync()
// O DbContext gerencia a UoW automaticamente dentro de um request HTTP (scoped lifetime).
services.AddDbContext<LoyaltyDbContext>(options =>
    options.UseNpgsql(connectionString), ServiceLifetime.Scoped);
```

---

## Outbox Pattern

**Gatilho de negócio:** _"O evento `PointsCredited` DEVE ser publicado no broker, mas se o sistema cair depois de salvar no banco e antes de publicar, o evento se perde."_

**Solução:** Salvar evento + Aggregate na **mesma transação** de banco. Um worker lê a tabela Outbox e publica assincronamente.

**Garantia:** At-least-once delivery. Consumidores devem ser idempotentes (verificar `EventId` duplicado).

**Estrutura da tabela Outbox:**
```sql
-- Schema: loyalty (um schema por Bounded Context)
CREATE TABLE loyalty.outbox_messages (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_type      TEXT NOT NULL,
    event_version   INT  NOT NULL DEFAULT 1,
    aggregate_id    TEXT NOT NULL,
    aggregate_name  TEXT NOT NULL,
    payload         JSONB NOT NULL,
    correlation_id  TEXT NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    processed_at    TIMESTAMPTZ,
    error           TEXT
);
CREATE INDEX idx_outbox_unprocessed ON loyalty.outbox_messages (created_at) WHERE processed_at IS NULL;
```

**C# stub:**
```csharp
// Infrastructure/Persistence/Outbox/OutboxMessage.cs
public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string EventType { get; init; }
    public int EventVersion { get; init; } = 1;
    public required string AggregateId { get; init; }
    public required string AggregateName { get; init; }
    public required string Payload { get; init; }      // JSON
    public required string CorrelationId { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? Error { get; set; }
}

// No Repository.SaveAsync() — converte DomainEvents em OutboxMessages antes de salvar:
private void EnqueueOutboxMessages(LoyaltyAccount account)
{
    foreach (var domainEvent in account.DomainEvents)
    {
        _db.OutboxMessages.Add(new OutboxMessage
        {
            EventType = domainEvent.GetType().Name,
            AggregateId = domainEvent.AggregateId,
            AggregateName = "LoyaltyAccount",
            Payload = JsonSerializer.Serialize(domainEvent),
            CorrelationId = domainEvent.CorrelationId,
        });
    }
    account.ClearDomainEvents();
}
```

---

## Schema Isolation per Bounded Context

**Gatilho de negócio:** _"O domínio de Loyalty NUNCA deve ver as tabelas do domínio de Orders diretamente."_

**Estratégia adotada:** Um schema PostgreSQL por Bounded Context dentro de um cluster compartilhado.

| Bounded Context | Schema PostgreSQL | DbContext .NET |
|---|---|---|
| Loyalty | `loyalty` | `LoyaltyDbContext` |
| Orders | `orders` | `OrdersDbContext` |
| Notifications | `notifications` | `NotificationsDbContext` |

**EF Core config:**
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.HasDefaultSchema("loyalty"); // Todos os Set<T> vão para o schema loyalty
    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
}
```

**Quando usar schema separado vs cluster separado:**
- Schema separado → desenvolvimento, staging, projetos pequenos/médios
- Cluster separado → produção crítica com requisitos de compliance (PCI-DSS, HIPAA) ou SLA diferenciado por domínio

---

## EF Core Migrations — Regras de Ouro

**Nomenclatura obrigatória:** `YYYYMMDD_HHmm_DescricaoBreve`
Exemplo: `20260408_1430_AddLoyaltyAccountTable`

**Proibições:**
- `NEVER` alterar uma migration já aplicada em staging/produção — crie uma nova.
- `NEVER` usar `EnsureCreated()` em produção — sempre `MigrationsAsync()`.
- `NEVER` remover uma coluna em produção sem uma migration de transição (adicionar coluna nova → migration → migrar dados → migration para remover antiga).

**Rollback:** EF Core não suporta rollback automático de DDL. Mantenha scripts SQL de rollback manual para cada migration aplicada em produção.

---

## Value Object Conversion (EF Core)

Value Objects não são entidades — EF Core precisa de um conversor explícito.

```csharp
// Infrastructure/Persistence/Configurations/LoyaltyAccountConfiguration.cs
builder.Property(a => a.Balance)
    .HasConversion(
        v => v.Value,          // PointsBalance → int (para o banco)
        v => new PointsBalance(v) // int → PointsBalance (do banco)
    )
    .HasColumnName("balance")
    .HasColumnType("integer");
```

---

## Connection String — Security

**Proibição absoluta:** Nunca hardcode connection string em código ou `appsettings.json` commitado.

**Padrão obrigatório:**
```csharp
// Program.cs — lê da variável de ambiente injetada pelo K8s Secret
var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Connection string 'Postgres' not found.");
```

**K8s Secret → env var:**
```yaml
env:
  - name: ConnectionStrings__Postgres
    valueFrom:
      secretKeyRef:
        name: loyalty-postgres-secret
        key: connection-string
```

---

## PostgreSQL + K8s (CloudNativePG)

**Operator:** CloudNativePG (CNCF Sandbox) — gerencia HA, backups, replicação por YAML declarativo.

**Install one-time (per cluster):**
```bash
kubectl apply --server-side -f \
  https://raw.githubusercontent.com/cloudnative-pg/cloudnative-pg/release-1.23/releases/cnpg-1.23.0.yaml
```

**Cluster CRD:** `infrastructure/postgres/k8s/cloudnativepg-cluster.yaml`

**Local dev:** `infrastructure/postgres/docker-compose.yml` — `postgres:16-alpine` ~80MB, starts in ~2s.
