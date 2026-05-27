# [REPO FAST CONTEXT] — TheThroneOfGames (GameStore)

> Leia este arquivo como **primeira ação** de qualquer sessão antes de consultar outros docs.
> Atualizado manualmente. Execute `python tools/reflect.py` para regenerar automaticamente.

---

## Estado do Sistema

```
Última atualização: 2026-05-26
Runtime: .NET 9 / C# 12
Banco de Dados: PostgreSQL 16 Alpine (Npgsql.EntityFrameworkCore.PostgreSQL 9.0.0)
Infraestrutura: Docker Compose (local) + Kubernetes GKE Autopilot (produção)
Testes: xUnit 2.9.0 + FluentAssertions 7.0.0 + NSubstitute 5.3.0
Auth: JWT Bearer (Microsoft.AspNetCore.Authentication.JwtBearer 9.0.0)
Métricas: Prometheus (prometheus-net.AspNetCore)
```

---

## Bounded Contexts

| Context | Projeto | Namespace Raiz | API Port |
|---|---|---|---|
| Catálogo | `GameStore.Catalogo` | `GameStore.Catalogo` | 5001 |
| Usuários | `GameStore.Usuarios` | `GameStore.Usuarios` | 5002 |
| Vendas | `GameStore.Vendas` | `GameStore.Vendas` | 5003 |

---

## Aggregates Ativos

| Aggregate | Bounded Context | Entidade Raiz | Repositório |
|---|---|---|---|
| Jogo | Catalogo | `Jogo` | `IJogoRepository` |
| Usuario | Usuarios | `Usuario` | `IUsuarioRepository` |
| Pedido | Vendas | `Pedido` | `IPedidoRepository` |

---

## Padrões Ativos no Codebase

- **CQRS**: `GameStore.CQRS.Abstractions` — `ICommandHandler<T>`, `IQueryHandler<T,R>`, `CommandResult`
- **Result<T>**: `GameStore.*.Domain.Shared.Result<T>` — todos os Value Objects e Use Cases retornam Result
- **DomainError**: `abstract record DomainError(string Code, string Message)` — erros tipados por domínio
- **DomainEvent**: `abstract record DomainEvent` com `CorrelationId`, `AggregateId`, `OccurredAt`
- **Event Bus**: `GameStore.Common.Events.IEventBus` — publicação assíncrona de eventos
- **Repository Port**: CancellationToken em todos os métodos async

---

## Convenções de Código

- Propriedades das entidades do Catalogo são em **português** (Nome, Preco, Genero, Disponivel)
- `Jogo` usa **constructor** — jamais object initializer
- `Preco` (Value Object) criado via `Preco.Create(valor)` retornando `Result<Preco>`
- `GetByNomeAsync()` retorna `IEnumerable<Jogo>` — usar `.FirstOrDefault()` para item único
- Namespaces de bounded contexts: `GameStore.Catalogo.*`, `GameStore.Usuarios.*`, `GameStore.Vendas.*`
- Namespace **PROIBIDO**: `TheThroneOfGames.Domain.*` (legado monolítico — migrado)

---

## ADRs Registrados

| ID | Decisão | Data |
|---|---|---|
| ADR-001 | Migração de SQL Server para PostgreSQL 16 Alpine | 2026-01-15 |
| ADR-002 | Adoção de Bounded Contexts (DDD) — saída de arquitetura monolítica | 2026-01-10 |
| ADR-003 | Testes migrados de NUnit+Moq para xUnit+FluentAssertions+NSubstitute | 2026-05-26 |
| ADR-004 | Aplicação do suitcase ia-arquiteto-hexagon-pattern (dotnet stack) | 2026-05-26 |

---

## Estrutura de Diretórios Chave

```
TheThroneOfGames/
├── GameStore.Catalogo/          # BC Catálogo (Domain + Application + Infrastructure)
│   ├── Domain/
│   │   ├── Entities/Jogo.cs
│   │   ├── Interfaces/IJogoRepository.cs
│   │   ├── ValueObjects/Preco.cs
│   │   └── Shared/             # Result<T>, DomainEvent, DomainErrors
│   ├── Application/            # Commands, Queries, Handlers, Mappers, DTOs
│   └── Infrastructure/         # EF Core DbContext, JogoRepository, Migrations
├── GameStore.Usuarios/          # BC Usuários
├── GameStore.Vendas/            # BC Vendas
├── GameStore.Catalogo.API/      # API Host para Catalogo
├── GameStore.Usuarios.API/      # API Host para Usuarios
├── GameStore.Vendas.API/        # API Host para Vendas
├── GameStore.Common/            # Shared kernel: IEventBus, base events
├── GameStore.CQRS.Abstractions/ # ICommandHandler, IQueryHandler, CommandResult
├── docs/ai/                     # Suitcase knowledge base
│   ├── architecture.md
│   ├── skills/
│   ├── workflows/
│   └── knowledge/
└── tools/                       # Python ChromaDB tools
```
