# [SKILL: .NET CLEAN ARCHITECTURE & DDD]

**Descrição:** Este documento define as regras estritas de codificação em C# 13 / .NET 10 para este projeto. Como um agente de IA, você DEVE aplicar essas regras ao escrever ou refatorar qualquer código dentro de um Aggregate.

## 1. MODELAGEM DE DOMÍNIO (A Regra da Pureza)
O projeto `/Domain` é sagrado. Ele deve conter apenas C# puro sem referências a pacotes NuGet externos.

### Referências Permitidas em `/Domain` (Whitelist Estrita)
- ✅ Outros arquivos do projeto `Domain` (Entities, VOs, Domain Events, Domain Errors, shared types).
- ✅ Tipos nativos do .NET BCL (`Dictionary<,>`, `HashSet<>`, `List<>`, `DateTimeOffset`, `Guid.NewGuid()`).
- 🚫 **PROIBIDO:** Qualquer referência a pacotes NuGet, incluindo "utilitários" como `FluentValidation`, `AutoMapper`, `MediatR`, `Humanizer`, `Newtonsoft.Json`. Se um ID único for necessário, utilize `Guid.NewGuid()` diretamente ou crie um Value Object `UniqueId` que receba o valor gerado pela infraestrutura via factory ou injeção no Composition Root.
- 🚫 **PROIBIDO:** Referências a namespaces com I/O (`System.IO`, `System.Net.Http`, `System.Net.Sockets`, `System.Diagnostics.Process`). Tipos puros do BCL como `Guid`, `DateTime`, `DateTimeOffset` são a **única exceção tolerada**.
- 🚫 **PROIBIDO:** O arquivo `.csproj` do projeto Domain não pode conter `<PackageReference>`. Apenas `<ProjectReference>` a si mesmo (implícito) é aceita.

- **Value Objects (VOs):** Devem ser estritamente IMUTÁVEIS. Utilize `record` ou `readonly struct` com propriedades `init`-only. Validações de estado devem ocorrer no factory method estático do VO. Se a validação falhar, retorne um `Result<T>` com o `DomainError`.
- **Entities & Aggregates:** Os atributos internos devem ser `private` ou `protected`. O estado só pode ser alterado através de métodos de negócio com nomes claros (ex: `user.ChangePassword()`, nunca `user.Password = newPassword`).
- **Anemic Models:** É proibido criar entidades anêmicas (apenas getters e setters). A lógica de negócio reside na Entidade.

## 2. INJEÇÃO DE DEPENDÊNCIA E PORTAS (DIP)
- **Construtores:** Toda dependência de infraestrutura deve ser injetada via construtor no Use Case (`/Application`). O container de DI nativo (`Microsoft.Extensions.DependencyInjection`) faz o wiring no `Program.cs` do WebApi.
- **Tipagem de Portas:** Utilize `Interfaces` explícitas para definir contratos de saída (Ports).
  *Exemplo:* `public interface IUserRepository { Task SaveAsync(User user, CancellationToken ct); }`
- Nunca instancie uma classe de infraestrutura (ex: `new EfCoreUserRepository()`) dentro de um Use Case. O contêiner de injeção de dependência (registrado em `Program.cs` ou em extension methods de `IServiceCollection`) deve cuidar disso.
- **CancellationToken:** Todo método assíncrono de Port DEVE aceitar `CancellationToken` como último parâmetro.

## 3. OBSERVABILIDADE E TRATAMENTO DE ERROS (OpenTelemetry Ready)
- **Domain Errors:** Crie classes de erro customizadas como `sealed record` herdando de uma base `DomainError` para o domínio (ex: `InvalidCpfError`, `InsufficientFundsError`). Nunca lance exceções genéricas (`throw new Exception()`) no Core. Use o padrão `Result<T>` para fluxos de erro de negócio.
- **Tratamento no Controller (Adapter):** O Controller ou Minimal API endpoint deve inspecionar o `Result<T>` retornado pelo Use Case e mapeá-lo para os HTTP Status Codes corretos (ex: `DomainError` → 400 Bad Request, exceção inesperada → 500 Internal Server Error).
- **Rastreabilidade Exigida:**
  Todo método de Adapter/Infraestrutura que realiza I/O ou publica eventos DEVE aceitar um objeto de contexto contendo `CorrelationId` e `TraceId` (ou extraí-los do `Activity.Current` via OpenTelemetry).
  *Exemplo de log obrigatório ao estourar erro na infra:*
  ```csharp
  _logger.LogError(exception, "Erro em {Aggregate}.{Action} | TraceId={TraceId} | CorrelationId={CorrelationId}",
      "Billing", "ProcessPayment", Activity.Current?.TraceId, correlationId);
  ```

## 4. REGRAS ESTRITAS DE C# 13
- **Proibido `dynamic`:** O uso do tipo `dynamic` é terminantemente proibido. Utilize `object` com pattern matching se o payload for incerto e faça validação antes de usar.
- **Nullable Reference Types:** Habilite `<Nullable>enable</Nullable>` e respeite os avisos de nullability. Se algo pode ser nulo, trate o cenário explicitamente com `is null`, `??` ou pattern matching.
- **Retornos de Use Case:** Prefira retornar o padrão `Result<T>` (Success/Failure) para fluxos de exceção controlada, evitando o uso excessivo de `try/catch` para regras de negócio normais.
- **Warnings como Erros:** O `Directory.Build.props` DEVE conter `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`. Código com warnings não compila.

### Implementação Canônica do Result (Obrigatória)
Utilize a seguinte implementação em TODOS os Aggregates. Não importe `ErrorOr`, `OneOf`, `FluentResults` ou crie variações. Este arquivo deve existir em `/Domain/Shared/Result.cs` dentro de cada Aggregate:
```csharp
namespace Domain.Shared;

public abstract record DomainError(string Code, string Message);

public readonly struct Result<T>
{
    private readonly T? _value;
    private readonly DomainError? _error;

    private Result(T value)
    {
        _value = value;
        _error = null;
        IsSuccess = true;
    }

    private Result(DomainError error)
    {
        _value = default;
        _error = error;
        IsSuccess = false;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failed Result.");

    public DomainError Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot access Error on a successful Result.");

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(DomainError error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(DomainError error) => Failure(error);

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<DomainError, TOut> onFailure)
        => IsSuccess ? onSuccess(_value!) : onFailure(_error!);
}
```
**Uso no Use Case:**
```csharp
public async Task<Result<User>> ExecuteAsync(CreateUserInput input, CancellationToken ct)
{
    var emailResult = Email.Create(input.Email);
    if (emailResult.IsFailure) return emailResult.Error; // propaga o erro via implicit operator

    // ... lógica de negócio

    return user; // implicit operator converte para Result<User>.Success(user)
}
```

## 5. ENFORCEMENT AUTOMATIZADO (ArchUnitNET + Roslyn Analyzers — Domain Purity Guard)
A whitelist de referências do `/Domain` (§1) DEVE ser enforced por testes de arquitetura, não apenas por memória do agente. Ao fazer scaffold de um novo Aggregate, o agente DEVE criar o seguinte teste ArchUnitNET no projeto de testes de domínio:

```csharp
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Domain.Tests.Architecture;

public class DomainPurityTests
{
    private static readonly ArchUnitNET.Domain.Architecture Architecture =
        new ArchLoader()
            .LoadAssemblies(typeof(Domain.Shared.DomainError).Assembly)
            .Build();

    private static readonly IObjectProvider<IType> DomainLayer =
        Types().That().ResideInNamespace("Domain", useRegularExpressions: false).As("Domain Layer");

    [Fact]
    public void Domain_Should_Not_Reference_Infrastructure()
    {
        Types().That().ResideInNamespace("Domain")
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("Infrastructure")
            .Check(Architecture);
    }

    [Fact]
    public void Domain_Should_Not_Reference_Application()
    {
        Types().That().ResideInNamespace("Domain")
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("Application")
            .Check(Architecture);
    }

    [Fact]
    public void Domain_Should_Not_Reference_WebApi()
    {
        Types().That().ResideInNamespace("Domain")
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("WebApi")
            .Check(Architecture);
    }

    [Fact]
    public void Domain_Should_Not_Reference_EntityFramework()
    {
        Types().That().ResideInNamespace("Domain")
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("Microsoft.EntityFrameworkCore")
            .Check(Architecture);
    }

    [Fact]
    public void Domain_Should_Not_Reference_Serilog()
    {
        Types().That().ResideInNamespace("Domain")
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("Serilog")
            .Check(Architecture);
    }

    [Fact]
    public void Domain_Should_Not_Reference_MassTransit()
    {
        Types().That().ResideInNamespace("Domain")
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("MassTransit")
            .Check(Architecture);
    }
}
```

### Enforcement Adicional via .editorconfig e Roslyn Analyzers:
```editorconfig
# .editorconfig — aplicado ao projeto inteiro
[*.cs]
dotnet_diagnostic.CA1848.severity = warning  # Use LoggerMessage delegates
dotnet_diagnostic.CA2007.severity = warning  # ConfigureAwait
dotnet_diagnostic.CA1062.severity = error    # Validate arguments of public methods
dotnet_diagnostic.CA2227.severity = error    # Collection properties should be read only
```

### Regras de Execução:
- **No Scaffold:** O agente DEVE criar os testes ArchUnitNET durante a Fase 1 do workflow `new-aggregate.md`.
- **No CI/CD:** O pipeline DEVE executar `dotnet test` incluindo os testes de arquitetura como gate obrigatório.
- **Verificação Local:** Antes de reportar sucesso, o agente deve rodar `dotnet test --filter "FullyQualifiedName~DomainPurityTests"` e garantir zero falhas.
- **Benefício:** Mesmo que o agente "esqueça" a regra, os testes de arquitetura capturam a violação mecanicamente.

## 6. BOOTSTRAP DO PROJETO (Templates Obrigatórios)
Ao criar um novo Aggregate do zero, o agente DEVE gerar os seguintes arquivos de configuração antes de qualquer código de domínio.

### `Directory.Build.props` (na raiz da solution)
```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
</Project>
```

### Estrutura de `.csproj` por camada
```text
MySolution.sln
├── src/
│   ├── Domain/Domain.csproj              # ZERO <PackageReference>
│   ├── Application/Application.csproj    # Referencia apenas Domain
│   ├── Infrastructure/Infrastructure.csproj  # Referencia Application + NuGet packages
│   └── WebApi/WebApi.csproj              # Referencia Application + Infrastructure
└── tests/
    ├── Domain.Tests/Domain.Tests.csproj
    ├── Application.Tests/Application.Tests.csproj
    ├── Infrastructure.Tests/Infrastructure.Tests.csproj
    └── Architecture.Tests/Architecture.Tests.csproj
```

### Domain.csproj (deve ser vazio de NuGet)
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <!-- Herda tudo de Directory.Build.props -->
  <!-- PROIBIDO: <PackageReference> aqui -->
</Project>
```

## 7. COMPOSITION ROOT (Program.cs & DI Wiring)
O arquivo `Program.cs` do projeto `WebApi` é o **Composition Root** — o único lugar onde classes concretas de infraestrutura são registradas no container de DI. O domínio e a aplicação NUNCA sabem qual implementação concreta estão usando.

### Template obrigatório: `WebApi/Program.cs`
```csharp
// WebApi/Program.cs — Composition Root (único ponto de wiring)
using Application.UseCases;
using Application.Ports;
using Infrastructure.Repositories;
using Infrastructure.Cache;
using Infrastructure.Messaging;

var builder = WebApplication.CreateBuilder(args);

// --- Observabilidade (Serilog + OpenTelemetry) ---
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console(new RenderedCompactJsonFormatter()));

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());

// --- Infrastructure (concretas registradas como interfaces) ---
builder.Services.AddScoped<IUserRepository, EfCoreUserRepository>();
builder.Services.AddSingleton<ICacheService, RedisCacheAdapter>();
builder.Services.AddSingleton<IMessagePublisher, MassTransitEventPublisher>();

// --- Application (Use Cases) ---
builder.Services.AddScoped<CreateUserUseCase>();

// --- Presentation ---
builder.Services.AddControllers();
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Default")!)
    .AddRedis(builder.Configuration["Redis:ConnectionString"]!);

var app = builder.Build();

// --- Middleware ---
app.UseSerilogRequestLogging();
app.MapControllers();
app.MapHealthChecks("/health/live", new() { Predicate = _ => false });
app.MapHealthChecks("/health/ready");

app.Run();
```

### Regras do Composition Root:
- **Único local de registro:** Apenas `Program.cs` registra implementações concretas no `IServiceCollection`.
- **Proibido `new` em Controllers:** Controllers recebem Use Cases via construtor (DI).
- **Configuration:** Toda configuração vem de `appsettings.json` / `appsettings.{env}.json` / variáveis de ambiente. Nunca hardcode.
- **Ordem de registro:** Infrastructure → Application → Presentation → Middleware → Endpoints.

## 8. GOTCHA CONHECIDO: Resolver um Singleton do DI NÃO inicia seu lifecycle

Registrar uma classe de infraestrutura com lifecycle próprio (ex.: `KestrelMetricServer` do
`prometheus-net`, que expõe `.Start()`/`.Stop()`) via `AddSingleton<TInterface>(new Impl(...))`
e depois só resolvê-la (`app.Services.GetRequiredService<TInterface>()`) **não a inicia**.
Resolver um singleton do container garante apenas que o objeto existe — nenhum método de
lifecycle é chamado automaticamente por isso.

**Incidente real neste framework (2026-07-13, TheThroneOfGames/Fase 4):** 3 microsserviços
registravam `IMetricServer` (porta dedicada de métricas Prometheus, separada da porta HTTP
principal) e resolviam a instância assim:
```csharp
var metricsServer = app.Services.GetRequiredService<IMetricServer>();
_ = metricsServer; // Ensures server is started  <-- comentário FALSO
```
A porta dedicada nunca abria (nada ouvia nela dentro do container), o Prometheus marcava o
alvo como `down`/`connection refused`, e o Grafana reportava erro de datasource. Ninguém
percebeu antes porque `/metrics` também respondia — por outro caminho, `app.MapMetrics()` —
na porta HTTP principal, mascarando o sintoma mais óbvio.

**Regra:** se uma dependência de infraestrutura expõe um método de lifecycle explícito
(`.Start()`, `.StartAsync()`, `.Open()`, etc.), chame-o explicitamente depois de resolver a
instância:
```csharp
var metricsServer = app.Services.GetRequiredService<IMetricServer>();
metricsServer.Start();
```
Melhor ainda: se a classe permitir, implemente `IHostedService`/`BackgroundService` e deixe o
host do ASP.NET Core cuidar do start/stop automaticamente pelo lifetime da aplicação — assim
não existe um passo manual para esquecer. Nunca assuma que "resolver do DI" e "iniciar" são a
mesma coisa; ao fazer o scaffold de qualquer serviço com um servidor/listener de infraestrutura
próprio, teste (`curl`/`wget` a porta esperada dentro do container) em vez de confiar no
comentário do código anterior.
