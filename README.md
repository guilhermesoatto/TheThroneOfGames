# TheThroneOfGames — Fase 3 (Microsserviços)

## Visão Geral

FIAP Cloud Games (FCG) na Fase 3: migração do monólito (Fase 2) para **3 microsserviços independentes**, com busca avançada via **Elasticsearch**, processamento assíncrono via **Serverless Functions**, um **API Gateway** único e **Event Sourcing** no fluxo de pedidos.

Requisitos originais do desafio: [`docs/Objectives/TC NETT - Fase 3.md`](docs/Objectives/TC%20NETT%20-%20Fase%203.md).
Governança/IA (Suitcase): [`CLAUDE.md`](CLAUDE.md).
PRD técnico + status real tarefa a tarefa: [`docs/ai/tasks/prd-sprint-02-fase3.json`](docs/ai/tasks/prd-sprint-02-fase3.json) e [`docs/Objectives/sprint-2/DELIVERABLE.md`](docs/Objectives/sprint-2/DELIVERABLE.md).

## Bounded Contexts

| Bounded Context | Projetos | Responsabilidade | Rota via API Gateway |
|---|---|---|---|
| Usuários | `GameStore.Usuarios` / `GameStore.Usuarios.API` | Login, cadastro, ativação, perfil | `/api/usuario/*`, `/api/admin/user-management/*` |
| Catálogo | `GameStore.Catalogo` / `GameStore.Catalogo.API` | Listagem, busca (Elasticsearch), promoções | `/api/game/*`, `/api/admin/game/*`, `/api/admin/promotion/*` |
| Vendas | `GameStore.Vendas` / `GameStore.Vendas.API` | Pedidos, itens, pagamento, Event Sourcing | `/api/pedidos/*` |

Compartilhado: `GameStore.Common` (eventos de domínio + mensageria RabbitMQ) e `GameStore.CQRS.Abstractions` (Commands/Queries). Cada bounded context mantém seu próprio `DbContext` — nenhum serviço acessa a tabela/schema de outro.

## Arquitetura

- **API Gateway** (`api-gateway/nginx.conf`): único ponto de entrada externo (porta 8080), roteia para os 3 microsserviços com rate limiting.
- **Comunicação Event-Driven**: `GameStore.Common.Messaging.RabbitMqAdapter` publica eventos de domínio (`PedidoFinalizadoEvent`, `UsuarioAtivadoEvent`, `GameCompradoEvent`) em filas dedicadas por consumidor.
- **Busca via Elasticsearch**: jogos são indexados em Create/Update/Remove (`GameStore.Catalogo/Infrastructure/Search/ElasticsearchJogoIndexer.cs`) e expostos via `GET /api/game/search?q=<termo>` — com fallback automático para busca no banco se o Elasticsearch estiver indisponível.
- **Serverless (Azure Functions, isolated worker)**: `GameStore.Notifications.Functions` consome `PedidoFinalizadoEvent` via `[RabbitMQTrigger]` real (filas fan-out dedicadas, sem competir com outros consumers) para notificação e processamento de pagamento assíncronos.
- **Event Sourcing**: `GameStore.Vendas/Domain/EventSourcing` + `Infrastructure/EventSourcing` registram toda transição de estado do aggregate `Pedido` em um Event Store append-only (sem replay/projeções ainda — ver PRD).

## Stack Tecnológico

- ASP.NET Core 9.0 Web API, Entity Framework Core, PostgreSQL
- RabbitMQ (mensageria), Elasticsearch 8.x (busca), nginx (API Gateway)
- Azure Functions Worker (isolated model)
- xUnit/NUnit + Testcontainers (RabbitMQ e Elasticsearch reais em testes de integração)
- Docker Compose para orquestração local

## Executando localmente

### Pré-requisitos
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Docker Desktop

### Subir a stack completa

```sh
docker compose up -d --build
```

Isso sobe: PostgreSQL, RabbitMQ, Elasticsearch, os 3 microsserviços, o API Gateway (nginx) e Prometheus/Grafana.

**Serviços disponíveis:**
- API Gateway: http://localhost:8080
- Usuários API: http://localhost:5001/swagger
- Catálogo API: http://localhost:5002/swagger
- Vendas API: http://localhost:5003/swagger
- Elasticsearch: http://localhost:9200
- RabbitMQ Management: http://localhost:15672 (guest/guest)
- Prometheus: http://localhost:9090
- Grafana: http://localhost:3000 (admin/admin)

### Endpoints principais

```
POST /api/usuario/pre-register       — cadastro de usuário (ativação por token)
POST /api/usuario/activate           — ativação de conta
POST /api/usuario/login              — autenticação (JWT)
GET  /api/usuario/profile            — perfil do usuário autenticado

GET  /api/game                       — lista todos os jogos
GET  /api/game/{id}                  — jogo por ID
GET  /api/game/available             — jogos disponíveis
GET  /api/game/genre/{genero}        — jogos por gênero
GET  /api/game/price?min=&max=       — jogos por faixa de preço
GET  /api/game/search?q=<termo>      — busca full-text via Elasticsearch (fallback: banco)
POST /api/admin/game                 — cria jogo (Admin)
PUT  /api/admin/game/{id}            — atualiza jogo (Admin)
DELETE /api/admin/game/{id}          — remove jogo (Admin, soft-delete)

/api/pedidos/*                       — pedidos e pagamento (ver GameStore.Vendas.API)
```

### Rodando os testes

```sh
dotnet test TheThroneOfGames.sln
```

Os testes de integração sobem containers reais via Testcontainers (RabbitMQ, Elasticsearch) — não usam mocks para infraestrutura externa. Alguns projetos `*.API.Tests` esperam um PostgreSQL alcançável em `localhost:5432` (ver `docker-compose.yml`).

## Estrutura do Projeto

```
TheThroneOfGames.sln
├── GameStore.Usuarios(.API)(.Tests)        # Bounded Context: Usuários
├── GameStore.Catalogo(.API)(.Tests)        # Bounded Context: Catálogo
├── GameStore.Vendas(.API)(.Tests)          # Bounded Context: Vendas
├── GameStore.Common(.Tests)                # Eventos, mensageria RabbitMQ
├── GameStore.CQRS.Abstractions             # Abstrações Commands/Queries
├── GameStore.Notifications.Functions       # Azure Functions (isolated worker)
├── api-gateway/                            # nginx (API Gateway)
├── monitoring/                             # Prometheus/Grafana provisioning
├── docs/ai/                                # Governança IA (Suitcase): knowledge base, tasks, skills
└── docs/Objectives/sprint-2/               # PRD e deliverable da Fase 3
```

## Status Real da Fase 3

Ver `docs/Objectives/sprint-2/DELIVERABLE.md` para o checklist tarefa a tarefa. Resumo:

| Item | Status |
|---|---|
| 3 microsserviços independentes | ✅ |
| API Gateway | ✅ |
| Busca via Elasticsearch (indexação + endpoint HTTP) | ✅ |
| Serverless (triggers reais via RabbitMQ) | ✅ |
| Event Sourcing (append-only, sem replay/projeções) | ⚠️ Parcial |
| Distributed Tracing (instrumentado, sem collector) | ⚠️ Em andamento |

## Licença
Licença MIT
