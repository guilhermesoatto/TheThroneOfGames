# [ARCHITECTURE BLUEPRINT: DDD + HEXAGONAL + K8S]

Este documento define a topologia arquitetural do sistema. Como um Agente de IA, você DEVE consultar este mapa antes de propor qualquer alteração de design, refatoração estrutural ou criação de novos domínios.

## 1. TOPOLOGIA FÍSICA E DEPLOYMENT (Kubernetes & Docker)
- **A Unidade de Deploy:** A unidade fundamental de deploy é o **Aggregate** (Raiz de Agregação no DDD). Cada Aggregate vive dentro do seu próprio container Docker.
- **Namespaces:** Os Bounded Contexts (Contextos Delimitados) são mapeados 1:1 para Namespaces no Kubernetes.
- **Pods:** Cada Pod no K8s representa uma instância de um Aggregate.
- **Regra de Isolamento:** Um Aggregate NUNCA compartilha banco de dados com outro. A persistência é privada por container.

## 2. MACRO-ARQUITETURA: EVENT-DRIVEN (EDA)
- **Intra-Domínio (Dentro do Aggregate):** Operações síncronas em memória. Fortemente tipadas.
- **Inter-Domínio (Entre Aggregates/Namespaces):** Comunicação ESTritamente ASSÍNCRONA.
- **Eventos de Domínio:** Quando uma regra de negócio altera o estado de um Aggregate, ele emite um `DomainEvent`.
- **Message Brokers:** A publicação e assinatura de eventos deve ser feita via infraestrutura de mensageria (ex: RabbitMQ, Kafka) utilizando MassTransit como abstração.
- **Proibição:** É proibido criar código onde o *Controller* de um Aggregate faz uma chamada HTTP direta para o *Controller* de outro Aggregate para processar regras de negócio.

### Contrato Obrigatório de Domain Events
Todo `DomainEvent` emitido DEVE herdar do seguinte abstract record base. Eventos sem esses campos serão rejeitados pelo Message Broker:
```csharp
namespace Domain.Shared;

public abstract record DomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public required string EventType { get; init; }        // Nome canônico (ex: "OrderPaid")
    public required int EventVersion { get; init; }         // Versionamento do schema (1, 2, 3...)
    public required Guid AggregateId { get; init; }         // ID do Aggregate que emitiu
    public required string AggregateName { get; init; }     // Nome do Aggregate (ex: "Order")
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow; // Timestamp ISO 8601
    public required string CorrelationId { get; init; }     // Propagado do request HTTP original
    public required IReadOnlyDictionary<string, object> Payload { get; init; } // Dados do evento (imutável)
}
```
**Regras de Versionamento de Eventos:**
- Ao adicionar campos opcionais ao payload: manter a mesma `EventVersion`.
- Ao remover campos ou alterar tipos: incrementar `EventVersion` (ex: `OrderPaidV1` → `OrderPaidV2`).
- Consumers devem ser backward-compatible: aceitar versões anteriores graciosamente.
- Nunca alterar o schema de um evento já publicado em produção — crie uma nova versão.

## 3. MICRO-ARQUITETURA: HEXAGONAL (Ports and Adapters)
Todo código dentro de um Aggregate DEVE seguir a Arquitetura Hexagonal. O fluxo de dependência aponta sempre para o centro (Core Domain).

### Estrutura de Diretórios Padrão por Aggregate:
```text
/src
  /Domain        # CORE: Entidades, Value Objects, Domain Events. ZERO dependência externa.
  /Application   # USE CASES: Orquestra o domínio. Define as Portas (Interfaces).
  /Infrastructure # ADAPTERS: Implementações técnicas (EF Core, StackExchange.Redis, MassTransit).
  /WebApi        # ADAPTERS DE ENTRADA: Controllers REST, Minimal APIs, Consumers de Eventos.
```

### Estrutura de Solução .NET Padrão por Aggregate:
```text
NomeDoProjeto.sln
├── Directory.Build.props         # Configurações globais (Nullable, TreatWarningsAsErrors, etc.)
├── src/
│   ├── NomeDoProjeto.Domain/     # Class Library — ZERO referência a outros projetos ou NuGet externos.
│   │   └── NomeDoProjeto.Domain.csproj
│   ├── NomeDoProjeto.Application/ # Class Library — Referencia APENAS Domain.
│   │   └── NomeDoProjeto.Application.csproj
│   ├── NomeDoProjeto.Infrastructure/ # Class Library — Referencia Application (e transitivamente Domain).
│   │   └── NomeDoProjeto.Infrastructure.csproj
│   └── NomeDoProjeto.WebApi/     # ASP.NET Core Web API — Referencia Application e Infrastructure.
│       └── NomeDoProjeto.WebApi.csproj
└── tests/
    ├── NomeDoProjeto.Domain.Tests/       # xUnit — Referencia APENAS Domain.
    ├── NomeDoProjeto.Application.Tests/  # xUnit — Referencia Application + NSubstitute.
    └── NomeDoProjeto.Integration.Tests/  # xUnit — Referencia WebApi + Testcontainers.NET.
```

### Directory.Build.props Canônico:
```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AnalysisLevel>latest-all</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
</Project>
```

### Convenção de Nomenclatura de Arquivos
Todos os Aggregates DEVEM seguir esta convenção sem exceções:
- **Domínio (Entities/VOs):** PascalCase → `User.cs`, `Email.cs`, `OrderStatus.cs`
- **Domain Events:** PascalCase + sufixo `Event` → `OrderPaidEvent.cs`, `UserCreatedEvent.cs`
- **Domain Errors:** PascalCase + sufixo `Error` → `InvalidEmailError.cs`, `InsufficientFundsError.cs`
- **Use Cases:** PascalCase + sufixo `UseCase` → `CreateUserUseCase.cs`, `PayOrderUseCase.cs`
- **Ports (Interfaces):** Prefixo `I` + PascalCase → `IUserRepository.cs`, `ICacheService.cs`, `IMessagePublisher.cs`
- **Adapters:** PascalCase + tecnologia → `EfCoreUserRepository.cs`, `RedisCacheAdapter.cs`, `MassTransitEventPublisher.cs`
- **Controllers:** PascalCase → `UserController.cs`, `OrderController.cs`
- **Testes:** Mesmo nome + sufixo `Tests` → `EmailTests.cs`, `CreateUserUseCaseTests.cs`
- **Shared/Cross-Cutting:** Em `/Domain/Shared/` → `Result.cs`, `DomainEvent.cs`
