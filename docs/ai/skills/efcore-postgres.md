# [SKILL: EF CORE + POSTGRESQL — .NET 10]

> Layer 3 — Regras de implementação. Nunca contradizer Layer 0 (guardrails) ou Layer 1 (architecture.md).
> Provider: `Npgsql.EntityFrameworkCore.PostgreSQL` ≥ 9.0

---

## 1. NuGet Packages Obrigatórios

```xml
<!-- NomeDoProjeto.Infrastructure.csproj -->
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.*" PrivateAssets="all" />
<!-- Para o Outbox worker -->
<PackageReference Include="Microsoft.Extensions.Hosting" Version="9.*" />
```

> Antes de adicionar qualquer NuGet, execute `dotnet list package --vulnerable --include-transitive`.
> Se houver vulnerabilidade **HIGH/CRITICAL**, não instale — consulte GitHub Advisory Database.

---

## 2. Estrutura de Projeto Obrigatória

```
NomeDoProjeto.Infrastructure/
├── Persistence/
│   ├── NomeDbContext.cs                   ← Um DbContext por Bounded Context
│   ├── Configurations/                    ← IEntityTypeConfiguration<T> por Aggregate
│   │   └── NomeAggregateConfiguration.cs
│   ├── Outbox/
│   │   ├── OutboxMessage.cs
│   │   └── OutboxConfiguration.cs
│   └── Migrations/                        ← gerado pelo EF CLI — não editar manualmente
├── Repositories/
│   └── EfCoreNomeAggregateRepository.cs
└── DependencyInjection/
    └── InfrastructureServiceExtensions.cs ← AddInfrastructure(services, config)
```

---

## 3. DbContext — Regras Canônicas

```csharp
// Infrastructure/Persistence/LoyaltyDbContext.cs
public sealed class LoyaltyDbContext : DbContext
{
    public LoyaltyDbContext(DbContextOptions<LoyaltyDbContext> options) : base(options) { }

    public DbSet<LoyaltyAccount> LoyaltyAccounts => Set<LoyaltyAccount>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // [OBRIGATÓRIO] Schema isolation — um schema por Bounded Context
        modelBuilder.HasDefaultSchema("loyalty");

        // [OBRIGATÓRIO] Aplicar todas as IEntityTypeConfiguration do assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // [OBRIGATÓRIO] snake_case para todas as colunas/tabelas (convenção PostgreSQL)
        configurationBuilder.Conventions.Add(_ => new SnakeCaseNamingConvention());
    }
}
```

**Regras para DbContext:**
- `NEVER` retornar `IQueryable` fora do DbContext — queries devem ser encapsuladas no Repository.
- `NEVER` usar `DbContext` com lifetime Singleton — sempre `Scoped` (padrão do `AddDbContext`).
- `NEVER` chamar `SaveChangesAsync()` dentro de um `IEntityTypeConfiguration`.
- Lazy loading é **proibido** — configure `UseLazyLoadingProxies()` NUNCA.

---

## 4. IEntityTypeConfiguration — Mapeamento Canônico

```csharp
// Infrastructure/Persistence/Configurations/LoyaltyAccountConfiguration.cs
public sealed class LoyaltyAccountConfiguration : IEntityTypeConfiguration<LoyaltyAccount>
{
    public void Configure(EntityTypeBuilder<LoyaltyAccount> builder)
    {
        builder.ToTable("loyalty_accounts"); // snake_case explícito

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasMaxLength(36).IsRequired();
        builder.Property(a => a.CustomerId).HasMaxLength(36).IsRequired();

        // [Value Object Conversion] PointsBalance → int
        builder.Property(a => a.Balance)
            .HasConversion(
                v => v.Value,
                v => PointsBalance.FromRaw(v))
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(a => a.Status)
            .HasConversion<string>()    // Enum → varchar (legível no DB)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        // Ignore DomainEvents — não persistir a lista de eventos no Aggregate
        builder.Ignore(a => a.DomainEvents);

        // Index para lookup por CustomerId (frequente em Use Cases)
        builder.HasIndex(a => a.CustomerId).IsUnique().HasDatabaseName("idx_loyalty_accounts_customer_id");
    }
}
```

---

## 5. Outbox — Configuração e Worker

```csharp
// Infrastructure/Persistence/Outbox/OutboxMessage.cs
public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string EventType { get; init; }
    public int EventVersion { get; init; } = 1;
    public required string AggregateId { get; init; }
    public required string AggregateName { get; init; }
    public required string Payload { get; init; }        // JSON serializado
    public required string CorrelationId { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? Error { get; set; }
}

// Infrastructure/Persistence/Outbox/OutboxConfiguration.cs
public sealed class OutboxConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(o => o.CreatedAt).HasColumnType("timestamptz");
        builder.Property(o => o.ProcessedAt).HasColumnType("timestamptz");

        // Index parcial — só mensagens não processadas (alta performance)
        builder.HasIndex(o => o.CreatedAt)
            .HasFilter("processed_at IS NULL")
            .HasDatabaseName("idx_outbox_unprocessed");
    }
}
```

**Outbox Worker Pattern (polling a cada 5s):**
```csharp
// Infrastructure/Outbox/OutboxWorker.cs
public sealed class OutboxWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessPendingMessagesAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
```

---

## 6. Repository — Implementação Canônica

```csharp
// Infrastructure/Repositories/EfCoreLoyaltyAccountRepository.cs
public sealed class EfCoreLoyaltyAccountRepository : ILoyaltyAccountRepository
{
    private readonly LoyaltyDbContext _db;

    public EfCoreLoyaltyAccountRepository(LoyaltyDbContext db) => _db = db;

    public async Task<LoyaltyAccount?> FindByIdAsync(string id, CancellationToken ct = default)
        => await _db.LoyaltyAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<LoyaltyAccount?> FindByCustomerIdAsync(string customerId, CancellationToken ct = default)
        => await _db.LoyaltyAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.CustomerId == customerId, ct);

    public async Task SaveAsync(LoyaltyAccount account, CancellationToken ct = default)
    {
        // Enqueue domain events as outbox messages BEFORE saving
        EnqueueOutboxMessages(account);

        _db.LoyaltyAccounts.Update(account);
        await _db.SaveChangesAsync(ct); // Atomic: account state + outbox messages
    }

    private void EnqueueOutboxMessages(LoyaltyAccount account)
    {
        foreach (var evt in account.DomainEvents)
        {
            _db.OutboxMessages.Add(new OutboxMessage
            {
                EventType    = evt.GetType().Name,
                EventVersion = 1,
                AggregateId  = evt.AggregateId,
                AggregateName = nameof(LoyaltyAccount),
                Payload      = JsonSerializer.Serialize(evt, evt.GetType()),
                CorrelationId = evt.CorrelationId,
            });
        }
        account.ClearDomainEvents();
    }
}
```

---

## 7. Dependency Injection — Registro Canônico

```csharp
// Infrastructure/DependencyInjection/InfrastructureServiceExtensions.cs
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' não configurada.");

        services.AddDbContext<LoyaltyDbContext>(options =>
            options
                .UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.EnableRetryOnFailure(maxRetryCount: 3);
                    npgsql.CommandTimeout(30);
                })
                .UseSnakeCaseNamingConvention() // Npgsql convention
        );

        services.AddScoped<ILoyaltyAccountRepository, EfCoreLoyaltyAccountRepository>();
        services.AddHostedService<OutboxWorker>();

        return services;
    }
}
```

---

## 8. Connection String — Configuração por Ambiente

**appsettings.json** (valores placeholder — nunca reais):
```json
{
  "ConnectionStrings": {
    "Postgres": "REPLACE_VIA_ENV_OR_SECRET"
  }
}
```

**K8s → env var (em deployment.yaml):**
```yaml
env:
  - name: ConnectionStrings__Postgres
    valueFrom:
      secretKeyRef:
        name: loyalty-postgres-secret
        key: connection-string
```

**docker-compose local (variável de ambiente no shell):**
```
ConnectionStrings__Postgres=Host=localhost;Port=5432;Database=loyalty_db;Username=loyalty_user;Password=local_only_dev
```

---

## 9. Proibições Absolutas

| Code smell | Motivo | Alternativa |
|---|---|---|
| `EnsureCreated()` em não-teste | Destrói migrations | `MigrateAsync()` |
| `Include()` ilimitado | N+1 velado, carrega grafo completo | Projections com `Select()` para leitura |
| `SaveChangesAsync()` dentro de Repository query | Efeito colateral escondido | Separar query de comando |
| Hardcode de connection string | Segurança/12-Factor | Env var via K8s Secret |
| `.UseLazyLoadingProxies()` | Queries invisíveis, N+1 | Eager load explícito com `Include()` controlado |
| `DbContext` Singleton | Corrupção de estado entre requests | Sempre `Scoped` |
