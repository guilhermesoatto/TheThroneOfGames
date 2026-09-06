# [REPO FAST CONTEXT] — TheThroneOfGames (GameStore)

> Leia este arquivo como **primeira ação** de qualquer sessão antes de consultar outros docs.
> Atualizado manualmente. Execute `python tools/reflect.py` para regenerar automaticamente.

---

## Estado do Sistema

```
Última atualização: 2026-09-06
Runtime: .NET 10 (LTS) / C# 14  — SDK fixado em global.json (10.0.302)
Banco de Dados: SQL Server (monólito Fase 2, EF Core 10) / EF Core InMemory nos testes
Infraestrutura: Docker Compose (local) + GitHub Actions (CI/CD em jobs separados)
Testes: MSTest + Moq (unitários) + xUnit + FluentAssertions + WebApplicationFactory (E2E)
Auth: JWT Bearer (Microsoft.AspNetCore.Authentication.JwtBearer 10.0.11)
Métricas: Prometheus (prometheus-net.AspNetCore + OpenTelemetry 1.18)
```

> Nota Fase 2 (`release/fase-2-monolito`): esta branch entrega o **monólito** `TheThroneOfGames.*`.
> Os microsserviços `GameStore.*`, PostgreSQL/K8s e a stack xUnit/NSubstitute pertencem às fases
> seguintes e estão fora do escopo desta branch.

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
| ADR-005 *(proposto)* | Monólito Fase 2: migração `net9.0` → `net10.0`, remoção de Testcontainers no CI (EF InMemory), pipeline CI/CD em jobs separados + suíte E2E via `WebApplicationFactory` | 2026-09-06 |

> ADR-005 — texto completo em [`decisions/ADR-0005-TheThroneOfGames.Monolith-cicd-net10.md`](decisions/ADR-0005-TheThroneOfGames.Monolith-cicd-net10.md).
> Status **Proposed**: aguarda aprovação do Domain Expert (agent-laws §3). Escopo restrito ao monólito
> `TheThroneOfGames.*`; não contradiz ADR-001..004, que valem para os microsserviços `GameStore.*`.

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
