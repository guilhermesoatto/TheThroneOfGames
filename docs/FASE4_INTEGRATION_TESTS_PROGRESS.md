# FASE 4 - Integration Tests Progress Report
**Data**: 15 de Janeiro de 2026  
**Branch**: `refactor/clean-architecture`  
**Commit**: `442cd0f` - "feat(tests): implementar padrão SQL Server para testes integração Catalogo"

## ✅ Objetivos Alcançados

### 1. Criação do Projeto GameStore.Catalogo.API.Tests
- ✅ Projeto NUnit criado com estrutura correta
- ✅ Referências configuradas: TheThroneOfGames.API, GameStore.Catalogo, GameStore.Usuarios
- ✅ Microsoft.AspNetCore.Mvc.Testing 9.0.0 instalado

### 2. Resolução do Problema de DI Container
**Problema Identificado**: `WebApplicationFactory<Program>` estava resolvendo para `GameStore.Catalogo.API.Program` em vez de `TheThroneOfGames.API.Program`

**Solução Implementada**:
```csharp
// ANTES (ERRADO)
public class CatalogoWebApplicationFactory : WebApplicationFactory<Program>

// DEPOIS (CORRETO)
public class CatalogoWebApplicationFactory : WebApplicationFactory<global::Program>
```

**Resultado**: UsuariosDbContext e CatalogoDbContext agora são corretamente injetados no DI container.

### 3. Configuração do SQL Server
**Container Docker**:
- Nome: `sqlserver2019`
- Imagem: `mcr.microsoft.com/mssql/server:2019-latest`
- Porta: `1433`
- Credenciais: `sa / YourSecurePassword123!`
- Database: `GameStore`

**Connection String**:
```json
"DefaultConnection": "Server=localhost,1433;Database=GameStore;User Id=sa;Password=YourSecurePassword123!;Encrypt=false;TrustServerCertificate=true;"
```

### 4. Pattern de Test Factory Implementado
```csharp
public class CatalogoWebApplicationFactory : WebApplicationFactory<global::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        
        using var scope = Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        
        // 1. Executar migrations
        var dbUsuarios = scopedServices.GetRequiredService<UsuariosDbContext>();
        dbUsuarios.Database.Migrate();
        
        var dbCatalogo = scopedServices.GetRequiredService<CatalogoDbContext>();
        dbCatalogo.Database.Migrate();
        
        // 2. Limpar dados de testes anteriores
        dbUsuarios.Usuarios.RemoveRange(dbUsuarios.Usuarios);
        dbCatalogo.Jogos.RemoveRange(dbCatalogo.Jogos);
        dbUsuarios.SaveChanges();
        dbCatalogo.SaveChanges();
        
        // 3. Seed admin user
        var adminUser = new Usuario(
            name: "Admin User",
            email: "admin@test.com",
            passwordHash: UsuarioService.HashPassword("Admin@123!"),
            role: "Admin",
            activeToken: Guid.NewGuid().ToString()
        );
        adminUser.Activate();
        dbUsuarios.Usuarios.Add(adminUser);
        dbUsuarios.SaveChanges();
    }
}
```

## 📊 Resultados dos Testes

### Status Atual: **3/4 Passando (75%)**

| Teste | Status | Tempo | Observações |
|-------|--------|-------|-------------|
| `ServerIsRunning` | ✅ PASS | - | Server responde corretamente |
| `CanReachSwagger` | ✅ PASS | - | Swagger UI acessível |
| `NonAdminCannotAccessGameManagement` | ✅ PASS | - | Autorização funcionando |
| `AdminCanCreateAndUpdateGame` | ❌ FAIL | 413ms | InternalServerError no POST |

### Erro Pendente
```
AdminCanCreateAndUpdateGame: Expected: Created, But was: InternalServerError
Location: AdminGameManagementTests.cs:59
```

**Próximo Passo**: Investigar por que o endpoint `/api/Catalogo/games` está retornando 500 Internal Server Error.

## 🔍 Decisões Técnicas

### Por que SQL Server em vez de InMemory?
1. **Compatibilidade**: Test/ usa SQL Server com sucesso (44/48 testes)
2. **Complexidade**: Monolito com múltiplos DbContexts dificulta InMemory
3. **Realismo**: Testes mais próximos do ambiente de produção
4. **Isolamento**: Migrations + cleanup garantem ambiente limpo

### Por que global::Program?
- TheThroneOfGames.API registra **todos** os DbContexts (Usuarios, Catalogo, Vendas)
- GameStore.Catalogo.API registra **apenas** CatalogoDbContext
- Testes de integração precisam de acesso a **todos** os contexts
- `global::` garante resolução para o namespace raiz

## 📁 Arquivos Modificados

### Criados
- `GameStore.Catalogo.API.Tests/CatalogoWebApplicationFactory.cs`
- `GameStore.Catalogo.API.Tests/AdminGameManagementTests.cs`
- `GameStore.Catalogo.API.Tests/HealthCheckTests.cs`
- `GameStore.Catalogo.API.Tests/appsettings.json`
- `GameStore.Catalogo.API.Tests/appsettings.Test.json`

### Modificados
- `GameStore.Catalogo.API.Tests/GameStore.Catalogo.API.Tests.csproj` (+ referências)

## 🎯 Próximos Passos

### Imediatos (FASE 4 - Continuação)
1. **Fix InternalServerError** no teste `AdminCanCreateAndUpdateGame`
   - Verificar logs da aplicação
   - Validar endpoint `/api/Catalogo/games`
   - Confirmar que handler está registrado corretamente
   
2. **Replicar Pattern para Usuarios**
   - Criar `GameStore.Usuarios.API.Tests`
   - Copiar `UsuariosWebApplicationFactory` com mesmo pattern
   - Migrar testes relevantes de `Test/Integration/`

3. **Replicar Pattern para Vendas**
   - Criar `GameStore.Vendas.API.Tests`
   - Copiar `VendasWebApplicationFactory` com mesmo pattern
   - Migrar testes relevantes

### Médio Prazo (FASE 5)
- Remover projeto `Test/` legado após validação completa
- Consolidar documentação de testes
- Setup CI/CD para executar testes de integração

## 💡 Lições Aprendidas

### 1. Namespace Resolution em WebApplicationFactory
Quando há múltiplos `Program` classes no projeto:
- Sempre use `global::Program` para referenciar o programa principal
- Verifique qual Program está sendo usado com breakpoints

### 2. SQL Server Password Management
- Sempre documente a senha usada no container
- Use mesma senha em appsettings.Test.json
- Prefira recrear container com senha conhecida

### 3. Migration Execution em Testes
- Executar migrations no `ConfigureClient` (após app construída)
- Não no `ConfigureWebHost` (app ainda não está pronta)
- Sempre executar para **todos** os DbContexts usados

### 4. Data Isolation Strategy
```csharp
// Limpar ANTES dos testes, não DEPOIS
dbContext.Entidades.RemoveRange(dbContext.Entidades);
dbContext.SaveChanges();
```

## 📈 Estatísticas

- **Tempo Total Investido**: ~2 horas
- **Tentativas de Solução**: 4 (ConfigureTestServices x2, InMemory x1, SQL Server ✅)
- **Commits**: 3
- **Testes Migrados**: 4
- **Taxa de Sucesso**: 75% (3/4 passando)

## 🔗 Referências

- [Bounded Contexts Migration Instructions](.github/instructions/bounded-contexts-migration.instructions.md)
- [Test/ CustomWebApplicationFactory](Test/Integration/CustomWebApplicationFactory.cs) - Pattern de referência
- [Microsoft Docs: Integration tests in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
