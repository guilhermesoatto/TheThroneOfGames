# Issue #2: InMemory → PostgreSQL Migration - Resolution Report

**Date**: 16 de Janeiro de 2026  
**Issue**: CRITICAL - Integration tests using InMemoryDatabase instead of PostgreSQL  
**Status**: ✅ **PARCIALMENTE RESOLVIDO** - Código corrigido, 23 testes permanecem com problemas de esquema

---

## Trabalho Realizado

### 1. Substituição InMemory → PostgreSQL

**Arquivos Modificados (3)**:
- `GameStore.Usuarios.API.Tests/UsuariosWebApplicationFactory.cs`
- `GameStore.Catalogo.API.Tests/CatalogoWebApplicationFactory.cs`
- `GameStore.Vendas.API.Tests/VendasWebApplicationFactory.cs`

**Mudança Aplicada**:
```csharp
// BEFORE (InMemory):
services.AddDbContext<UsuariosDbContext>(options => 
    options.UseInMemoryDatabase("TestDb_Usuarios"));

// AFTER (PostgreSQL):
var connectionString = "Host=localhost;Port=5432;Database=GameStore_Test;Username=sa;Password=YourSecurePassword123!";
services.AddDbContext<UsuariosDbContext>(options => 
    options.UseNpgsql(connectionString));
```

### 2. Criação de DesignTimeDbContextFactory

**Arquivos Criados (3)**:
- `GameStore.Usuarios/Infrastructure/Persistence/UsuariosDbContextFactory.cs`
- `GameStore.Catalogo/Infrastructure/Persistence/CatalogoDbContextFactory.cs`
- `GameStore.Vendas/Infrastructure/Persistence/VendasDbContextFactory.cs`

**Propósito**: Permitir `dotnet ef` commands sem depender de startup project

### 3. Adição de EF Core Design Package

**Comando Executado**:
```powershell
dotnet add GameStore.Usuarios package Microsoft.EntityFrameworkCore.Design --version 9.0.0
dotnet add GameStore.Catalogo package Microsoft.EntityFrameworkCore.Design --version 9.0.0
dotnet add GameStore.Vendas package Microsoft.EntityFrameworkCore.Design --version 9.0.0
```

### 4. Aplicação de Migrations

**Comandos Executados**:
```powershell
dotnet ef database update --project GameStore.Usuarios --context UsuariosDbContext
dotnet ef database update --project GameStore.Catalogo --context CatalogoDbContext
dotnet ef database update --project GameStore.Vendas --context VendasDbContext
```

**Resultados**:
- Usuarios: No migrations applied (already up to date)
- Catalogo: Migration '20260115223906_InitialPostgreSQL' applied ✅
- Vendas: Pending (not captured in output)

### 5. Configuração Docker Container

**Container PostgreSQL Criado**:
```powershell
docker run -d --name postgresql-test \
  -e POSTGRES_USER=sa \
  -e POSTGRES_PASSWORD=YourSecurePassword123! \
  -e POSTGRES_DB=GameStore_Test \
  -p 5432:5432 postgres:16-alpine
```

**Status**: Up and running (8+ seconds healthy)

---

## Problemas Encontrados

### Problema A: Conflito Migrate() vs EnsureCreated()

**Sintoma**:
```
Npgsql.PostgresException: 42P01: relation "Usuarios" does not exist
```

**Causa Raiz**:
Tentamos usar `Database.EnsureCreated()` mas já existem migrations aplicadas no banco. EnsureCreated() não aplica migrations, apenas cria schema diretamente do modelo.

**Tentativa 1**: Usar `Database.Migrate()`
- Resultado: Mesmo erro - table já existe de migrations anteriores

**Tentativa 2**: Usar `Database.EnsureDeleted()` + `Database.EnsureCreated()`
- Resultado: `EnsureCreated()` não cria tabelas quando migrations existem

**Problema Fundamental**:
- `EnsureCreated()` ignora migrations se elas existem
- `Migrate()` não funciona no contexto de testes (precisa aplicar migrations ANTES dos testes)
- Conflito entre abordagem de migrations (Production) e testes isolados

### Problema B: Schema Isolation

**Issue**: Testes de integração estão competindo pela mesma tabela "Usuarios" no mesmo database `GameStore_Test`

**Evidência**:
- Line 57 em todos os WebApplicationFactory tentam query `dbUsuarios.Usuarios.Any()`
- Tabela não existe porque `EnsureCreated()` não criou (migrations conflict)
- Mesmo se criar, testes paralelos irão interferir uns com os outros

---

## Soluções Possíveis

### Solução 1: Database-Per-Test-Run (RECOMENDADO)

Criar database único por execução de teste:

```csharp
protected override void ConfigureClient(HttpClient client)
{
    base.ConfigureClient(client);
    
    var testDbName = $"GameStore_Test_{Guid.NewGuid():N}";
    var connectionString = $"Host=localhost;Port=5432;Database={testDbName};Username=sa;Password=YourSecurePassword123!";
    
    using var scope = Services.CreateScope();
    var scopedServices = scope.ServiceProvider;
    
    // Apply migrations to new database
    var dbUsuarios = scopedServices.GetRequiredService<UsuariosDbContext>();
    dbUsuarios.Database.Migrate();
    
    // Seed data...
    
    // Register cleanup
    RegisterTestDatabaseForCleanup(testDbName);
}
```

**Vantagens**:
- ✅ Isolamento completo entre test runs
- ✅ Usa migrations reais (valida schema)
- ✅ Pode rodar testes em paralelo

**Desvantagens**:
- ❌ Lento (criação de DB + migrations)
- ❌ Requer cleanup manual

### Solução 2: Truncate Tables Between Tests

```csharp
protected override void ConfigureClient(HttpClient client)
{
    base.ConfigureClient(client);
    
    using var scope = Services.CreateScope();
    var dbUsuarios = scope.ServiceProvider.GetRequiredService<UsuariosDbContext>();
    
    // Truncate all tables
    dbUsuarios.Database.ExecuteSqlRaw("TRUNCATE TABLE \"Usuarios\" CASCADE");
    dbUsuarios.Database.ExecuteSqlRaw("TRUNCATE TABLE \"Jogos\" CASCADE");
    
    // Seed data...
}
```

**Vantagens**:
- ✅ Rápido
- ✅ Usa schema real com migrations

**Desvantagens**:
- ❌ Não funciona com testes paralelos
- ❌ Precisa listar todas as tabelas manualmente

### Solução 3: Respawn Library (ENTERPRISE)

Usar `Respawn` NuGet package:

```csharp
private static Checkpoint _checkpoint = new Checkpoint
{
    TablesToIgnore = new[] { "__EFMigrationsHistory" }
};

protected override void ConfigureClient(HttpClient client)
{
    base.ConfigureClient(client);
    
    await _checkpoint.Reset("Host=localhost;...");
    
    // Seed data...
}
```

**Vantagens**:
- ✅ Automatiza truncate de todas as tabelas
- ✅ Rápido
- ✅ Production-ready

**Desvantagens**:
- ❌ Dependency externa

---

## Decisão Tomada

**Status**: ⚠️ **PARALISADO AGUARDANDO DECISÃO**

### Recomendação do AI Agent:

**SOLUÇÃO 1 (Database-Per-Test-Run)** com melhorias:

1. Criar database com GUID único por test class (não por test method)
2. Usar `IClassFixture` para compartilhar WebApplicationFactory entre testes da mesma classe
3. Implementar `IAsyncLifetime` para cleanup automático:

```csharp
public class IntegrationTestFixture : IAsyncLifetime
{
    private string _testDatabaseName;
    public WebApplicationFactory<Program> Factory { get; private set; }
    
    public async Task InitializeAsync()
    {
        _testDatabaseName = $"GameStore_Test_{Guid.NewGuid():N}";
        
        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace DbContexts with test database
                    var connStr = $"Host=localhost;Port=5432;Database={_testDatabaseName};...";
                    // ... configure services
                });
            });
        
        // Apply migrations
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UsuariosDbContext>();
        await db.Database.MigrateAsync();
    }
    
    public async Task DisposeAsync()
    {
        // Drop test database
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UsuariosDbContext>();
        await db.Database.EnsureDeletedAsync();
    }
}
```

**Benefícios**:
- ✅ Isolamento total
- ✅ Cleanup automático
- ✅ Migrations validadas
- ✅ Pode rodar classes de teste em paralelo

**Trade-off**:
- Testes ~3-5s mais lentos por classe (aceitável)

---

## Próximos Passos

1. **[ ] Implementar IntegrationTestFixture** conforme Solução 1
2. **[ ] Refatorar 3 WebApplicationFactory** para usar fixture
3. **[ ] Adicionar NuGet**: `Microsoft.Extensions.Hosting.Abstractions` (se necessário)
4. **[ ] Testar isolamento** rodando testes em paralelo
5. **[ ] Re-executar validation suite**
6. **[ ] Documentar resultados finais**

---

## Métricas de Impacto

### Antes (InMemory):
- ❌ 23 integration tests SKIPPED/FAIL
- ❌ Não valida schema real
- ❌ Discrepância local vs pipeline
- ⏱️ Testes rápidos (~0.5s)

### Depois (PostgreSQL Parcial):
- ⚠️ 23 integration tests ainda FAIL (schema issue)
- ✅ Connection strings corretas
- ✅ Container PostgreSQL rodando
- ⏱️ Build + migrations: +5s

### Meta (PostgreSQL Completo):
- ✅ 23 integration tests PASS
- ✅ Schema validation completa
- ✅ Mesmo comportamento local e pipeline
- ⏱️ Estimativa: ~15s total integration suite

---

## Arquivos Afetados - Summary

**Modified (3)**:
- `GameStore.Usuarios.API.Tests/UsuariosWebApplicationFactory.cs`
- `GameStore.Catalogo.API.Tests/CatalogoWebApplicationFactory.cs`
- `GameStore.Vendas.API.Tests/VendasWebApplicationFactory.cs`

**Created (3)**:
- `GameStore.Usuarios/Infrastructure/Persistence/UsuariosDbContextFactory.cs`
- `GameStore.Catalogo/Infrastructure/Persistence/CatalogoDbContextFactory.cs`
- `GameStore.Vendas/Infrastructure/Persistence/VendasDbContextFactory.cs`

**Package Added (3 projects)**:
- `Microsoft.EntityFrameworkCore.Design` 9.0.0

**Container Created**:
- `postgresql-test` (postgres:16-alpine)

---

## Lições Aprendidas

1. **EnsureCreated() vs Migrate()**:
   - `EnsureCreated()` não funciona quando migrations existem
   - Sempre usar `Migrate()` em integration tests para validar schema real

2. **Test Isolation**:
   - Shared database causa race conditions
   - Database-per-test-run é solução enterprise-grade

3. **DesignTimeDbContextFactory**:
   - Essencial para rodar `dotnet ef` commands sem startup project
   - Separar connection strings: Dev, Test, Production

4. **Docker Container Startup**:
   - PostgreSQL leva ~8-10s para ficar ready
   - Sempre validar status antes de rodar testes

---

## Referências

- [EF Core Testing Guidance](https://learn.microsoft.com/en-us/ef/core/testing/)
- [WebApplicationFactory<TEntryPoint>](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [Respawn GitHub](https://github.com/jbogard/Respawn)
- [xUnit IClassFixture](https://xunit.net/docs/shared-context)

