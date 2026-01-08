# 🔧 Troubleshooting Guide - GameStore Microservices

**Última Atualização:** 08/01/2026  
**Versão:** 2.0 - Post Bounded Contexts Migration

---

## 📋 Índice
1. [Problemas de Compilação](#problemas-de-compilação)
2. [Problemas de Testes](#problemas-de-testes)
3. [Problemas de Database](#problemas-de-database)
4. [Problemas de Dependency Injection](#problemas-de-dependency-injection)
5. [Problemas de Microservices](#problemas-de-microservices)
6. [Problemas de Eventos](#problemas-de-eventos)

---

## 🔴 Problemas de Compilação

### ❌ Erro: "Cannot resolve namespace GameStore.X"

**Sintoma:**
```
error CS0246: The type or namespace name 'GameStore' could not be found
```

**Causa:** Referências de projeto faltando ou namespaces incorretos após migração.

**Solução:**
```bash
# 1. Limpar solution
dotnet clean

# 2. Restaurar pacotes
dotnet restore

# 3. Rebuild em ordem
dotnet build GameStore.Common/GameStore.Common.csproj
dotnet build GameStore.CQRS.Abstractions/GameStore.CQRS.Abstractions.csproj
dotnet build GameStore.Usuarios/GameStore.Usuarios.csproj
dotnet build GameStore.Catalogo/GameStore.Catalogo.csproj
dotnet build GameStore.Vendas/GameStore.Vendas.csproj
```

### ❌ Erro: "Using directives for TheThroneOfGames.*"

**Sintoma:**
```csharp
using TheThroneOfGames.Domain.Events;  // Não existe mais!
```

**Causa:** Código ainda referenciando estrutura legada.

**Solução:**
Atualizar imports:
```csharp
// ❌ ANTIGO (Legado)
using TheThroneOfGames.Domain.Events;
using TheThroneOfGames.Application.Interface;

// ✅ NOVO (Bounded Context)
using GameStore.Common.Events;
using GameStore.Usuarios.Application.Interfaces;
```

**Script de busca:**
```bash
# Encontrar referências legadas
grep -r "using TheThroneOfGames" --include="*.cs"
```

---

## 🧪 Problemas de Testes

### ❌ Erro: "Unable to resolve service for type IUsuarioService"

**Sintoma:**
```
Failed for password: P@ssw0rd. Response: {"detail":"Unable to resolve service for type 'GameStore.Usuarios.Application.Interfaces.IUsuarioService' while attempting to activate 'TheThroneOfGames.API.Controllers.UsuarioController'."}
```

**Causa:** **DI Conflict** - Dois `IUsuarioService` com mesmo nome mas namespaces diferentes.

**Contexto:**
- `TheThroneOfGames.Application.Interface.IUsuarioService` (legado)
- `GameStore.Usuarios.Application.Interfaces.IUsuarioService` (bounded context)

**Solução:**
Registrar explicitamente com namespace completo:

```csharp
// Program.cs
services.AddScoped<GameStore.Usuarios.Application.Interfaces.IUsuarioService, UsuarioService>();
```

**✅ RESOLVIDO EM:** Commit 1cb8057 - "feat: migrate Admin UserManagement to bounded context"

### ❌ Erro: "Expected: OK But was: InternalServerError"

**Sintoma:**
Testes falhando com InternalServerError mas sem detalhes.

**Causa:** Exception não capturada no controller ou service.

**Debugging:**
```csharp
// 1. Habilitar logging detalhado
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information"
    }
  }
}

// 2. Adicionar try-catch com log
try {
    var result = await _service.DoSomething();
    return Ok(result);
} catch (Exception ex) {
    Console.WriteLine($"ERROR: {ex.Message}");
    Console.WriteLine($"STACK: {ex.StackTrace}");
    throw;
}
```

### ❌ Erro: Test Concurrency - Duplicate Key

**Sintoma:**
```
Violation of PRIMARY KEY constraint 'PK_Usuarios'. Cannot insert duplicate key in object 'dbo.Usuarios'. The duplicate key value is (testuser@example.com).
```

**Causa:** Múltiplos testes criando usuários com mesmo email simultaneamente.

**Solução:**
Usar emails únicos por teste:

```csharp
// ❌ PROBLEMA
var testEmail = "testuser@example.com"; // Sempre igual!

// ✅ SOLUÇÃO
var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
var testEmail = $"testuser{uniqueId}@example.com";
```

**✅ RESOLVIDO EM:** Commit 1cb8057 - "feat: migrate Admin UserManagement to bounded context"

### ❌ Erro: "Expected: Created But was: InternalServerError"

**Sintoma:**
AdminPromotionManagement test falhando ao criar promoção.

**Causa:** Campos obrigatórios (Title, Description, GameIds) não preenchidos.

**Solução:**
```csharp
// Controller
var p = new PromotionEntity
{
    Title = "Promotion", // ✅ Default value
    Description = "Discount promotion", // ✅ Default value
    GameIds = new List<Guid>(), // ✅ Empty list
    Discount = dto.Discount,
    ValidUntil = dto.ValidUntil
};
```

**✅ RESOLVIDO EM:** Commit 455685f - "feat: achieve 100% test success (48/48)"

---

## 💾 Problemas de Database

### ❌ Erro: "Table 'Usuarios' already exists"

**Sintoma:**
```
There is already an object named 'Usuarios' in the database.
```

**Causa:** Migrations aplicadas em ordem errada ou em DbContexts diferentes.

**Solução:**
```bash
# 1. Listar migrations aplicadas
dotnet ef migrations list --project GameStore.Usuarios

# 2. Remover migrations duplicadas
dotnet ef migrations remove --project GameStore.Usuarios

# 3. Recriar e aplicar corretamente
dotnet ef migrations add InitialUsuarios --project GameStore.Usuarios
dotnet ef database update --project GameStore.Usuarios
```

### ❌ Erro: "Invalid column name 'ActiveToken'"

**Sintoma:**
```
SqlException: Invalid column name 'ActiveToken'.
```

**Causa:** Migration não aplicada ou DbContext apontando para schema antigo.

**Solução:**
```bash
# Verificar última migration aplicada
dotnet ef migrations list --project GameStore.Usuarios

# Aplicar migration específica
dotnet ef database update AddLegacyEntities --project TheThroneOfGames.Infrastructure
```

### ❌ Erro: Cross-Context Data Flow

**Sintoma:**
Usuário criado no UsuariosDbContext não aparece no Admin (MainDbContext).

**Causa:** Dois DbContexts apontando para tabelas diferentes.

**Contexto:**
- `UsuariosDbContext.Usuarios` → Tabela "Usuarios" (bounded context)
- `MainDbContext.Users` → Tabela "Usuario" (legado)

**Solução:**
Migrar Admin controllers para usar bounded context:

```csharp
// ❌ ANTIGO
using TheThroneOfGames.Application.Interface;
private readonly IUsuarioService _userService; // Legado

// ✅ NOVO
using GameStore.Usuarios.Application.Interfaces;
private readonly IUsuarioService _userService; // Bounded context
```

**✅ RESOLVIDO EM:** Commit 1cb8057 - Migração do UserManagementController

---

## 🔌 Problemas de Dependency Injection

### ❌ Erro: "A service of type 'IEventBus' has not been registered"

**Sintoma:**
```
InvalidOperationException: Unable to resolve service for type 'GameStore.Common.Events.IEventBus'
```

**Causa:** EventBus não registrado no DI container.

**Solução:**
```csharp
// Program.cs
builder.Services.AddSingleton<IEventBus, SimpleEventBus>();
```

### ❌ Erro: Circular Dependency

**Sintoma:**
```
Some services are not able to be constructed (Error while validating the service descriptor 'ServiceType: X Lifetime: Scoped ImplementationType: Y': A circular dependency was detected)
```

**Causa:** Service A depende de B que depende de A.

**Solução:**
1. Usar eventos para quebrar dependência circular
2. Refatorar para extrair interface comum
3. Usar Factory Pattern

```csharp
// ❌ PROBLEMA
public class ServiceA {
    public ServiceA(ServiceB b) { }
}
public class ServiceB {
    public ServiceB(ServiceA a) { } // Circular!
}

// ✅ SOLUÇÃO 1: Eventos
public class ServiceA {
    private readonly IEventBus _eventBus;
    public void DoSomething() {
        _eventBus.PublishAsync(new SomethingHappenedEvent());
    }
}

// ✅ SOLUÇÃO 2: Interface
public interface ICommonService { }
public class ServiceA : ICommonService { }
public class ServiceB {
    public ServiceB(ICommonService common) { }
}
```

---

## 🚀 Problemas de Microservices

### ❌ Erro: "Address already in use"

**Sintoma:**
```
Unable to bind to https://localhost:5001 on the IPv4 loopback interface: 'Address already in use'.
```

**Causa:** Múltiplos microservices tentando usar mesma porta.

**Solução:**
Configurar portas únicas em `launchSettings.json`:

```json
{
  "GameStore.Usuarios.API": {
    "applicationUrl": "https://localhost:5001;http://localhost:5000"
  },
  "GameStore.Catalogo.API": {
    "applicationUrl": "https://localhost:6001;http://localhost:6000"
  },
  "GameStore.Vendas.API": {
    "applicationUrl": "https://localhost:7001;http://localhost:7000"
  }
}
```

### ❌ Erro: Microservice não encontra outro microservice

**Sintoma:**
```
HttpRequestException: Connection refused (localhost:5000)
```

**Causa:** Microservice tentando se comunicar com outro que não está rodando.

**Solução:**
```bash
# 1. Verificar ordem de inicialização
# GameStore.Usuarios.API (primeiro - autenticação)
# GameStore.Catalogo.API (segundo - catálogo)
# GameStore.Vendas.API (terceiro - vendas)

# 2. Usar Docker Compose para orquestração
docker-compose up -d

# 3. Ou usar dotnet-watch para desenvolvimento
dotnet watch run --project GameStore.Usuarios.API &
dotnet watch run --project GameStore.Catalogo.API &
dotnet watch run --project GameStore.Vendas.API &
```

---

## 📨 Problemas de Eventos

### ❌ Erro: "Event handler not found"

**Sintoma:**
Evento publicado mas nenhum handler executado.

**Causa:** Handler não registrado no EventBus.

**Solução:**
```csharp
// Program.cs
var eventBus = app.Services.GetRequiredService<IEventBus>();

// Registrar handlers
eventBus.Subscribe(new UsuarioAtivadoEventHandler(serviceProvider));
eventBus.Subscribe(new GameCompradoEventHandler(serviceProvider));
```

### ❌ Erro: Event Cross-Context não funcionando

**Sintoma:**
`UsuarioAtivadoEvent` publicado em Usuarios mas não recebido em Catalogo.

**Causa:** Event buses separados (em memória) por microservice.

**Solução:**
Para comunicação cross-microservice, usar Message Broker:

```yaml
# docker-compose.yml
services:
  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672"
```

```csharp
// Substituir SimpleEventBus por RabbitMQEventBus para cross-service
public class RabbitMQEventBus : IEventBus {
    // Implementar com RabbitMQ.Client
}
```

---

## 🔍 Como Reportar Novos Problemas

### Template de Issue:
```markdown
## Problema
[Descrição clara do problema]

## Sintoma
```
[Copiar mensagem de erro exata]
```

## Passos para Reproduzir
1. [Passo 1]
2. [Passo 2]
3. [Erro aparece aqui]

## Ambiente
- OS: Windows/Linux/Mac
- .NET Version: 9.0
- Bounded Context: Usuarios/Catalogo/Vendas
- Commit: [hash do commit]

## Tentativas de Solução
- [ ] Tentei X
- [ ] Tentei Y

## Logs Relevantes
```
[Colar logs aqui]
```
```

---

## 📚 Referências Úteis

- **Bounded Contexts Instructions:** `.github/instructions/bounded-contexts-migration.instructions.md`
- **Refactoring Plan:** `.github/REFACTORING_PLAN.md`
- **Architecture Decisions:** `ARCHITECTURE_README.md`
- **Deployment Guide:** `.github/DEPLOYMENT.md`

---

## ✅ Problemas Resolvidos (Histórico)

| Commit | Data | Problema | Solução |
|--------|------|----------|---------|
| b4548d0 | 08/01/26 | Bounded contexts architecture | Estrutura inicial |
| a505489 | 08/01/26 | Admin auth (37/48 tests) | Admin em UsuariosDbContext |
| 20477e6 | 08/01/26 | Legacy services registration | AddApplicationServices() |
| 1cb8057 | 08/01/26 | DI conflict + test concurrency (46/48) | Explicit namespace + unique emails |
| 455685f | 08/01/26 | **100% tests (48/48)** ✅ | PromotionEntity defaults |

---

**Última Atualização:** 08/01/2026  
**Contribuidores:** Development Team  
**Status:** ✅ 48/48 testes passando em master
