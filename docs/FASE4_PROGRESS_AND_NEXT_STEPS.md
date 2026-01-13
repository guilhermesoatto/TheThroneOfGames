# FASE 4: Reorganização de Testes - Progresso e Próximos Passos

**Data**: 08 de Janeiro de 2026  
**Branch**: `refactor/clean-architecture`  
**Status**: 80% Completo - Infraestrutura pronta, ajuste de configuração pendente

---

## 📋 Índice

1. [Contexto Geral](#contexto-geral)
2. [O Que Foi Feito](#o-que-foi-feito)
3. [Problema Atual](#problema-atual)
4. [Solução Proposta](#solução-proposta)
5. [Próximos Passos Detalhados](#próximos-passos-detalhados)
6. [Estrutura de Arquivos](#estrutura-de-arquivos)
7. [Comandos Úteis](#comandos-úteis)
8. [Troubleshooting](#troubleshooting)

---

## 🎯 Contexto Geral

### Objetivo da FASE 4
Reorganizar os testes de integração do projeto monolítico (`Test/`) para projetos específicos de cada bounded context, preparando o terreno para arquitetura de microservices.

### Estado Atual dos Testes

| Projeto | Tipo | Status | Resultado |
|---------|------|--------|-----------|
| GameStore.Catalogo.Tests | Unitário | ✅ Completo | 40/40 passing |
| GameStore.Usuarios.Tests | Unitário | ✅ Completo | 61/61 passing |
| GameStore.Common.Tests | Unitário | ⚠️ Parcial | 2/12 passing (10 falhas RabbitMQ - ignorar) |
| Test/ | Integração (Legacy) | ⚠️ Esperado | 44/48 passing (4 falhas esperadas) |
| **GameStore.Catalogo.API.Tests** | **Integração (Novo)** | 🔄 **Em Progresso** | **0/2 - Configuração pendente** |

---

## ✅ O Que Foi Feito

### 1. Projeto GameStore.Catalogo.API.Tests Criado

**Comando executado:**
```bash
dotnet new nunit -n GameStore.Catalogo.API.Tests -o GameStore.Catalogo.API.Tests
dotnet sln add GameStore.Catalogo.API.Tests\GameStore.Catalogo.API.Tests.csproj
```

**Localização**: `c:\Users\Guilherme\source\repos\TheThroneOfGames\GameStore.Catalogo.API.Tests\`

### 2. Pacotes Instalados

```xml
<!-- GameStore.Catalogo.API.Tests.csproj -->
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.0" />
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.0" />
  <PackageReference Include="NUnit" Version="4.2.2" />
  <PackageReference Include="NUnit.Analyzers" Version="4.3.0" />
  <PackageReference Include="NUnit3TestAdapter" Version="4.6.0" />
  <PackageReference Include="coverlet.collector" Version="6.0.2" />
</ItemGroup>
```

**Comandos executados:**
```bash
cd GameStore.Catalogo.API.Tests
dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 9.0.0
dotnet add reference ..\GameStore.Catalogo.API\GameStore.Catalogo.API.csproj
dotnet add reference ..\TheThroneOfGames.API\TheThroneOfGames.API.csproj
dotnet add reference ..\GameStore.Usuarios\GameStore.Usuarios.csproj
```

### 3. Program.cs do Catalogo.API Modificado

**Arquivo**: `GameStore.Catalogo.API\Program.cs`

**Mudança feita** (final do arquivo):
```csharp
app.Run();

// Make Program accessible for integration tests
namespace GameStore.Catalogo.API
{
    public partial class Program { }
}
```

**Motivo**: Tornar a classe `Program` acessível para `WebApplicationFactory<T>` nos testes, e evitar conflito com `TheThroneOfGames.API.Program` usando um namespace específico.

### 4. CatalogoWebApplicationFactory Criada

**Arquivo**: `GameStore.Catalogo.API.Tests\CatalogoWebApplicationFactory.cs`

**Estado Atual**:
```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using GameStore.Catalogo.Infrastructure.Persistence;
using GameStore.Usuarios.Infrastructure.Persistence;

namespace GameStore.Catalogo.API.Tests;

public class CatalogoWebApplicationFactory : WebApplicationFactory<GameStore.Catalogo.API.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        
        builder.ConfigureServices(services =>
        {
            // Remove all DbContext registrations
            services.RemoveAll(typeof(DbContextOptions<CatalogoDbContext>));
            services.RemoveAll(typeof(CatalogoDbContext));
            
            services.RemoveAll(typeof(DbContextOptions<UsuariosDbContext>));
            services.RemoveAll(typeof(UsuariosDbContext));
            
            // Add InMemory DbContexts for tests
            services.AddDbContext<CatalogoDbContext>(options =>
            {
                options.UseInMemoryDatabase("CatalogoTestDb");
            });
            
            services.AddDbContext<UsuariosDbContext>(options =>
            {
                options.UseInMemoryDatabase("UsuariosTestDb");
            });
            
            // Build service provider and seed data
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            
            var catalogoDb = scopedServices.GetRequiredService<CatalogoDbContext>();
            var usuariosDb = scopedServices.GetRequiredService<UsuariosDbContext>();
            
            catalogoDb.Database.EnsureCreated();
            usuariosDb.Database.EnsureCreated();
            
            // Seed admin user for testing
            if (!usuariosDb.Usuarios.Any(u => u.Email == "admin@test.com" && u.Role == "Admin"))
            {
                var adminUser = new GameStore.Usuarios.Domain.Entities.Usuario(
                    name: "Admin User",
                    email: "admin@test.com",
                    passwordHash: GameStore.Usuarios.Application.Services.UsuarioService.HashPassword("Admin@123!"),
                    role: "Admin",
                    activeToken: Guid.NewGuid().ToString()
                );
                adminUser.Activate();
                usuariosDb.Usuarios.Add(adminUser);
                usuariosDb.SaveChanges();
            }
        });
    }
}
```

### 5. AdminGameManagementTests Migrado

**Arquivo**: `GameStore.Catalogo.API.Tests\AdminGameManagementTests.cs`

Testes copiados de `Test\Integration\AdminGameManagementTests.cs` e adaptados:
- Namespace alterado para `GameStore.Catalogo.API.Tests`
- Factory alterado para `CatalogoWebApplicationFactory`
- Sintaxe NUnit corrigida: `Assert.IsNotNull()` → `Assert.That(..., Is.Not.Null)`

---

## ❌ Problema Atual

### Erro ao Executar Testes

**Comando**:
```bash
dotnet test GameStore.Catalogo.API.Tests
```

**Erro**:
```
System.InvalidOperationException : Services for database providers 
'Microsoft.EntityFrameworkCore.SqlServer', 'Microsoft.EntityFrameworkCore.InMemory' 
have been registered in the service provider. Only a single database provider 
can be registered in a service provider.
```

**Localização do erro**: Linha 45 em `CatalogoWebApplicationFactory.cs` (`catalogoDb.Database.EnsureCreated()`)

### Diagnóstico

O problema ocorre porque:

1. **Program.cs do Catalogo.API registra SQL Server** via `builder.Services.AddCatalogoContext(connectionString)` (que usa SQL Server)

2. **WebApplicationFactory tenta remover e re-registrar InMemory**, mas:
   - O método `RemoveAll()` não está removendo completamente a configuração do SQL Server
   - Quando tenta criar o DbContext InMemory, o Entity Framework detecta ambos provedores (SQL Server do registro original + InMemory do teste) e falha

3. **Causa raiz**: A extensão `AddCatalogoContext()` registra não apenas o DbContext, mas também outros serviços internos do EF Core que ainda apontam para SQL Server

---

## 💡 Solução Proposta

### Opção 1: Substituir ConfigureServices por ConfigureTestServices (RECOMENDADA)

Esta é a abordagem mais limpa e recomendada pela Microsoft.

**Mudança em CatalogoWebApplicationFactory.cs**:

```csharp
protected override void ConfigureWebHost(IWebHostBuilder builder)
{
    builder.UseEnvironment("Test");
    
    builder.ConfigureTestServices(services =>
    {
        // Remove SQL Server DbContexts
        var catalogoDescriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(DbContextOptions<CatalogoDbContext>));
        if (catalogoDescriptor != null)
            services.Remove(catalogoDescriptor);
        
        var usuariosDescriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(DbContextOptions<UsuariosDbContext>));
        if (usuariosDescriptor != null)
            services.Remove(usuariosDescriptor);
        
        // Add InMemory DbContexts for tests
        services.AddDbContext<CatalogoDbContext>(options =>
        {
            options.UseInMemoryDatabase("CatalogoTestDb");
        });
        
        services.AddDbContext<UsuariosDbContext>(options =>
        {
            options.UseInMemoryDatabase("UsuariosTestDb");
        });
    });
}

protected override void ConfigureClient(HttpClient client)
{
    base.ConfigureClient(client);
    
    // Seed data after application is built
    using var scope = Services.CreateScope();
    var usuariosDb = scope.ServiceProvider.GetRequiredService<UsuariosDbContext>();
    var catalogoDb = scope.ServiceProvider.GetRequiredService<CatalogoDbContext>();
    
    usuariosDb.Database.EnsureCreated();
    catalogoDb.Database.EnsureCreated();
    
    // Seed admin user for testing
    if (!usuariosDb.Usuarios.Any(u => u.Email == "admin@test.com" && u.Role == "Admin"))
    {
        var adminUser = new GameStore.Usuarios.Domain.Entities.Usuario(
            name: "Admin User",
            email: "admin@test.com",
            passwordHash: GameStore.Usuarios.Application.Services.UsuarioService.HashPassword("Admin@123!"),
            role: "Admin",
            activeToken: Guid.NewGuid().ToString()
        );
        adminUser.Activate();
        usuariosDb.Usuarios.Add(adminUser);
        usuariosDb.SaveChanges();
    }
}
```

**Por que funciona**: 
- `ConfigureTestServices` é executado **depois** de `ConfigureServices`, garantindo que nossas configurações de teste sobrescrevam as de produção
- O seeding é movido para `ConfigureClient`, que é executado quando o servidor já está totalmente configurado

### Opção 2: Usar SQL Server Real (Container Docker)

Se a opção 1 não funcionar, podemos usar um SQL Server real em container Docker para testes.

**Adicionar ao docker-compose**:
```yaml
sqlserver-test:
  image: mcr.microsoft.com/mssql/server:2022-latest
  environment:
    - ACCEPT_EULA=Y
    - SA_PASSWORD=Test@123!
    - MSSQL_PID=Developer
  ports:
    - "1434:1433"
  volumes:
    - sqlserver-test-data:/var/opt/mssql
```

**Modificar CatalogoWebApplicationFactory.cs**:
```csharp
builder.ConfigureAppConfiguration((context, config) =>
{
    config.AddInMemoryCollection(new Dictionary<string, string>
    {
        ["ConnectionStrings:DefaultConnection"] = "Server=localhost,1434;Database=CatalogoTestDb;User Id=sa;Password=Test@123!;TrustServerCertificate=True"
    });
});
```

---

## 🚀 Próximos Passos Detalhados

### Passo 1: Implementar Solução (5 minutos)

**1.1. Abrir arquivo CatalogoWebApplicationFactory.cs**
```bash
code GameStore.Catalogo.API.Tests\CatalogoWebApplicationFactory.cs
```

**1.2. Substituir o conteúdo completo do método `ConfigureWebHost` e adicionar `ConfigureClient`**

Use o código da **Opção 1** na seção "Solução Proposta" acima.

**1.3. Salvar o arquivo**

### Passo 2: Compilar e Testar (2 minutos)

```bash
# Compilar
dotnet build GameStore.Catalogo.API.Tests

# Se compilou sem erros, executar testes
dotnet test GameStore.Catalogo.API.Tests --logger "console;verbosity=detailed"
```

**Resultado esperado**:
```
Total de testes: 2
Com falha: 0-2 (pode ter falhas de API endpoint ainda)
```

### Passo 3: Ajustar Testes se Necessário (10-30 minutos)

Se os testes ainda falharem, verifique:

**3.1. Endpoints da API**

Os endpoints podem ter mudado. Verificar:
```bash
# Verificar rotas no AdminGameController
code GameStore.Catalogo.API\Controllers\Admin\AdminGameController.cs
```

Ajustar em `AdminGameManagementTests.cs` se necessário:
- `/api/admin/game` → rota correta
- `/api/Usuario/login` → pode precisar ser `/api/usuario/login`

**3.2. Estrutura de DTOs**

Verificar se `GameDTO` tem todas as propriedades necessárias:
```bash
code TheThroneOfGames.API\Models\DTO\GameDTO.cs
```

**3.3. Autenticação**

Se falhar login, verificar:
```bash
# Ver como autenticação está configurada
code GameStore.Catalogo.API\Program.cs

# Procurar por AddAuthentication e AddJwtBearer
```

### Passo 4: Validar Sucesso (2 minutos)

Quando os 2 testes passarem:

```bash
# Executar todos os testes do bounded context
dotnet test GameStore.Catalogo.API.Tests

# Verificar resultado
# Expected: Total de testes: 2, Com falha: 0
```

### Passo 5: Commit do Progresso (3 minutos)

```bash
git add GameStore.Catalogo.API.Tests/
git add GameStore.Catalogo.API/Program.cs
git add TheThroneOfGames.sln
git commit -m "feat(tests): adicionar testes de integração do Catalogo bounded context

- Criar projeto GameStore.Catalogo.API.Tests
- Implementar CatalogoWebApplicationFactory com InMemory database
- Migrar AdminGameManagementTests do projeto legacy
- Expor Program.cs do Catalogo.API para testes
- Corrigir sintaxe NUnit nos testes

FASE 4: 2/2 testes Catalogo passando"
```

### Passo 6: Replicar para Usuarios e Vendas (1-2 horas)

Depois que Catalogo funcionar, replicar o padrão:

#### 6.1. GameStore.Usuarios.API.Tests

```bash
# Criar projeto
dotnet new nunit -n GameStore.Usuarios.API.Tests -o GameStore.Usuarios.API.Tests
dotnet sln add GameStore.Usuarios.API.Tests\GameStore.Usuarios.API.Tests.csproj

# Adicionar pacotes
cd GameStore.Usuarios.API.Tests
dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 9.0.0
dotnet add reference ..\GameStore.Usuarios.API\GameStore.Usuarios.API.csproj
dotnet add reference ..\TheThroneOfGames.API\TheThroneOfGames.API.csproj

# Criar UsuariosWebApplicationFactory.cs (copiar estrutura de CatalogoWebApplicationFactory)
# Criar testes: AuthenticationTests.cs, AuthorizationTests.cs, PasswordValidationTests.cs
```

#### 6.2. GameStore.Vendas.API.Tests

```bash
# Mesmo processo, adaptando para Vendas
dotnet new nunit -n GameStore.Vendas.API.Tests -o GameStore.Vendas.API.Tests
# ... seguir mesmo padrão
```

### Passo 7: Validar Testes Completos (5 minutos)

```bash
# Executar TODOS os testes
dotnet test

# Resultado esperado:
# - GameStore.Catalogo.Tests: 40/40 ✅
# - GameStore.Usuarios.Tests: 61/61 ✅
# - GameStore.Catalogo.API.Tests: 2/2 ✅
# - GameStore.Usuarios.API.Tests: X/X ✅
# - GameStore.Vendas.API.Tests: X/X ✅
# - Test/ (legacy): 44/48 ⚠️ (esperado)
```

### Passo 8: Atualizar TODO (1 minuto)

Marcar FASE 4 como completa no arquivo de progresso.

---

## 📁 Estrutura de Arquivos

```
TheThroneOfGames/
├── GameStore.Catalogo.API/
│   ├── Program.cs                          # ✅ MODIFICADO (namespace adicionado)
│   └── Controllers/
│       └── Admin/
│           └── AdminGameController.cs
│
├── GameStore.Catalogo.API.Tests/           # ✅ NOVO PROJETO
│   ├── GameStore.Catalogo.API.Tests.csproj
│   ├── CatalogoWebApplicationFactory.cs    # 🔧 PRECISA AJUSTE
│   ├── AdminGameManagementTests.cs         # ✅ MIGRADO
│   └── Usings.cs
│
├── GameStore.Usuarios.API.Tests/           # ⏳ PENDENTE
├── GameStore.Vendas.API.Tests/             # ⏳ PENDENTE
│
├── Test/                                    # 🗑️ SERÁ DELETADO NA FASE 5
│   └── Integration/
│       ├── AdminGameManagementTests.cs     # Original (não modificar)
│       └── CustomWebApplicationFactory.cs
│
└── docs/
    └── FASE4_PROGRESS_AND_NEXT_STEPS.md   # 📄 ESTE ARQUIVO
```

---

## 🔧 Comandos Úteis

### Compilação

```bash
# Compilar apenas o projeto de testes
dotnet build GameStore.Catalogo.API.Tests

# Compilar toda a solução
dotnet build

# Limpar e recompilar
dotnet clean
dotnet build
```

### Execução de Testes

```bash
# Executar testes de um projeto específico
dotnet test GameStore.Catalogo.API.Tests

# Executar com logs detalhados
dotnet test GameStore.Catalogo.API.Tests --logger "console;verbosity=detailed"

# Executar todos os testes da solução
dotnet test

# Executar apenas testes que passaram pela última vez
dotnet test --filter "TestCategory!=Flaky"
```

### Debug

```bash
# Executar testes com debugger anexado
dotnet test GameStore.Catalogo.API.Tests --logger "console;verbosity=detailed" --filter "FullyQualifiedName~AdminCanCreateAndUpdateGame"

# Ver informações sobre o projeto
dotnet list GameStore.Catalogo.API.Tests package
dotnet list GameStore.Catalogo.API.Tests reference
```

### Git

```bash
# Ver status
git status

# Ver diff do que foi modificado
git diff

# Adicionar arquivos específicos
git add GameStore.Catalogo.API.Tests/

# Commit
git commit -m "feat(tests): mensagem aqui"

# Ver log
git log --oneline -10
```

---

## 🔍 Troubleshooting

### Problema 1: "Program não existe no namespace GameStore.Catalogo.API"

**Sintoma**:
```
error CS0234: O nome de tipo ou namespace "Program" não existe no namespace "GameStore.Catalogo.API"
```

**Solução**:
1. Verificar que `Program.cs` do Catalogo.API tem o namespace:
   ```csharp
   namespace GameStore.Catalogo.API
   {
       public partial class Program { }
   }
   ```

2. Recompilar o Catalogo.API:
   ```bash
   dotnet clean GameStore.Catalogo.API
   dotnet build GameStore.Catalogo.API
   ```

3. Recompilar os testes:
   ```bash
   dotnet build GameStore.Catalogo.API.Tests
   ```

### Problema 2: "Conflito entre SQL Server e InMemory"

**Sintoma**: Erro `Services for database providers 'Microsoft.EntityFrameworkCore.SqlServer', 'Microsoft.EntityFrameworkCore.InMemory' have been registered`

**Solução**: Implementar **Opção 1** da seção "Solução Proposta" (usar `ConfigureTestServices`)

### Problema 3: Testes passam mas retornam 404

**Sintoma**: Teste executa mas API retorna `404 Not Found`

**Diagnóstico**:
```bash
# Verificar se rota existe no controller
code GameStore.Catalogo.API\Controllers\Admin\AdminGameController.cs
```

**Possíveis causas**:
- Rota mudou: `/api/admin/game` vs `/api/Admin/Game`
- Controller não registrado
- Middleware de roteamento não configurado

**Solução**:
1. Verificar attribute de rota no controller:
   ```csharp
   [Route("api/admin/[controller]")]
   [Route("api/admin/game")]  // ou rota específica
   ```

2. Ajustar teste para usar rota correta

### Problema 4: Falha na autenticação em testes

**Sintoma**: Login retorna 401 ou token inválido

**Diagnóstico**:
1. Verificar se admin user foi criado corretamente no seed
2. Verificar se senha está sendo hasheada corretamente
3. Verificar configuração JWT no Catalogo.API

**Solução**:
```bash
# Ver configuração JWT
code GameStore.Catalogo.API\Program.cs

# Procurar por:
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
```

Verificar se:
- Secret key está configurada
- Issuer/Audience estão corretos
- Token expiration é adequado

### Problema 5: InMemory Database vazio

**Sintoma**: Testes falham porque não encontram dados esperados

**Solução**:
1. Verificar se `EnsureCreated()` está sendo chamado
2. Adicionar logs para debug:
   ```csharp
   Console.WriteLine($"Usuarios count: {usuariosDb.Usuarios.Count()}");
   Console.WriteLine($"Admin exists: {usuariosDb.Usuarios.Any(u => u.Email == \"admin@test.com\")}");
   ```

3. Verificar se cada teste está usando banco limpo (configurar no `SetUp` se necessário)

---

## 📚 Referências

### Documentação Oficial

- [Integration tests in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [WebApplicationFactory](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.testing.webapplicationfactory-1)
- [NUnit Documentation](https://docs.nunit.org/)
- [Entity Framework Core InMemory Provider](https://learn.microsoft.com/en-us/ef/core/providers/in-memory/)

### Arquivos do Projeto

- `.github/instructions/bounded-contexts-migration.instructions.md` - Regras de migração
- `.github/instructions/objetivo estrutura pre-micro services arch.instructions.md` - Arquitetura alvo
- `docs/FINISHING_STEPS.md` - Próximas fases do projeto

### Commits Relevantes

- `013f56f` - fix(tests): corrigir testes unitários dos bounded contexts
- `88b1c38` - FASE 3: Admin controllers migrated to bounded contexts
- `d90c429` - FASE 2: Events centralized in GameStore.Common

---

## ✅ Checklist de Validação

Antes de considerar FASE 4 completa, verificar:

- [ ] GameStore.Catalogo.API.Tests compila sem erros
- [ ] 2 testes em AdminGameManagementTests passam
- [ ] Admin user é criado corretamente no seed
- [ ] Endpoints da API retornam 2xx para requests válidos
- [ ] Testes de autorização funcionam (Forbidden para não-admin)
- [ ] GameStore.Usuarios.API.Tests criado e funcionando
- [ ] GameStore.Vendas.API.Tests criado e funcionando
- [ ] Todos os testes unitários ainda passam (101/101)
- [ ] Documentação atualizada
- [ ] Commit realizado com mensagem descritiva

---

## 🎯 Meta Final da FASE 4

**Resultado Esperado**:
```
Execução de Testes Completa:
✅ GameStore.Catalogo.Tests: 40/40 passing
✅ GameStore.Usuarios.Tests: 61/61 passing
✅ GameStore.Catalogo.API.Tests: 2/2 passing
✅ GameStore.Usuarios.API.Tests: X/X passing
✅ GameStore.Vendas.API.Tests: X/X passing
⚠️ GameStore.Common.Tests: 2/12 passing (RabbitMQ - ignorar)
⚠️ Test/ (legacy): 44/48 passing (esperado - será deletado)

Total: ~115+ testes passing
```

**Próxima Fase**: FASE 5 - Remover código legado (`Test/`, controllers antigos, etc.)

---

**Última atualização**: 08/01/2026  
**Responsável pela documentação**: GitHub Copilot Agent  
**Próxima revisão**: Após conclusão da FASE 4
