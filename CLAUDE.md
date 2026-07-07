# TheThroneOfGames — Governança IA (Fase 3 — Microsserviços)

**Suitcase injetada em:** 2026-07-07
**Stack:** dotnet
**Branch:** release/fase-3-microservices
**Framework:** ia-arquiteto-hexagon-pattern (DDD + Hexagonal + CQRS)

## Ordem de Leitura Obrigatória (Bootstrap)

```
1. docs/Objectives/sprint-2/DELIVERABLE.md        — objetivos e critérios de aceite da Fase 3
2. docs/architecture-flow.md                      — fluxo de comunicação entre serviços (Mermaid)
3. docs/ai/tasks/prd-sprint-02-fase3.json          — PRD técnico ativo desta fase
4. Este arquivo (CLAUDE.md) §Bounded Contexts       — fronteiras de cada microsserviço
```

## Bounded Contexts (obrigatórios — Fase 3)

| Bounded Context | Projeto real | Responsabilidade | Rota via API Gateway |
|---|---|---|---|
| Usuários | `GameStore.Usuarios` / `GameStore.Usuarios.API` | Login, cadastro, ativação, perfil | `/api/usuario/*`, `/api/admin/user-management/*` |
| Catálogo (Jogos) | `GameStore.Catalogo` / `GameStore.Catalogo.API` | Listagem, busca (Elasticsearch), promoções | `/api/game/*`, `/api/admin/game/*`, `/api/admin/promotion/*` |
| Vendas (Pagamentos) | `GameStore.Vendas` / `GameStore.Vendas.API` | Pedidos, itens, pagamento, Event Sourcing | `/api/pedidos/*` |

Compartilhado entre os três: `GameStore.Common` (mensageria/RabbitMQ) e `GameStore.CQRS.Abstractions` (Commands/Queries). Não existe um projeto `GameStore.Pagamentos` — o bounded context de pagamentos/transações é implementado dentro de `GameStore.Vendas`.

## Leis Invioláveis desta Fase

- **NUNCA** crie uma dependência de projeto (`ProjectReference`) de um `GameStore.*` para `TheThroneOfGames.*` (monolito legado, isolado em `release/fase-2-monolito`). Se encontrar uma, é resíduo a remover — ver histórico de commits desta branch.
- Comunicação síncrona (HTTP/REST) entre `GameStore.Usuarios`, `GameStore.Catalogo` e `GameStore.Vendas` **é permitida nesta fase** (ver `docs/Objectives/sprint-2/DELIVERABLE.md` §Architectural Constraints — mensageria assíncrona obrigatória só é exigida a partir da Fase 4, `release/fase-4-kubernetes`). Hoje nenhum dos 3 serviços chama o outro (nem síncrona nem assincronamente) — ver `docs/architecture-flow.md` §2.
- **NUNCA** deixe um Microsserviço acessar diretamente o `DbContext`/schema de outro bounded context — cada um mantém seu próprio banco (`UsuariosDbContext`, `CatalogoDbContext`, `VendasDbContext`).
- **TODA** rota pública externa deve passar pelo API Gateway (`api-gateway/nginx.conf` + serviço `api-gateway` no `docker-compose.yml`) — nenhum microsserviço deve ser exposto diretamente ao cliente final em produção.
- **TODA** mudança de estado do aggregate `Pedido` (`GameStore.Vendas`) deve ser registrada no Event Store (`IEventStore.AppendAsync`) além de persistida no snapshot via `IPedidoRepository` — ver `GameStore.Vendas/Domain/EventSourcing/`.
- **NUNCA** avance de fase no PRD (`docs/ai/tasks/prd-sprint-02-fase3.json`) sem rodar `node tools/validate-prd.js docs/ai/tasks/prd-sprint-02-fase3.json` e sem aprovação do Domain Expert.
- Ao adicionar uma funcionalidade nova, identifique primeiro a qual bounded context ela pertence (tabela acima) — código de um contexto nunca deve viver dentro do projeto de outro.

## Estado Real de Infraestrutura (não presuma — confira antes de afirmar "concluído")

| Requisito FIAP | Status real nesta branch |
|---|---|
| 3 microsserviços independentes | ✅ Implementado (`GameStore.Usuarios`, `GameStore.Catalogo`, `GameStore.Vendas`) |
| Event Sourcing | ⚠️ Esboço — Event Store append-only em `GameStore.Vendas` (ver `docs/architecture-flow.md`), sem replay/projeções |
| Distributed Tracing | ⚠️ OpenTelemetry configurado nos 3 serviços (`AddAspNetCoreInstrumentation`, `AddHttpClientInstrumentation`, `AddOtlpExporter`), sem collector validado neste repositório |
| Elasticsearch | ⚠️ Container adicionado ao `docker-compose.yml`; indexação do Catálogo ainda não implementada em código |
| Serverless (pagamentos/notificações) | ⚠️ Esboço de estrutura em `src/Functions/` (ver README local); sem deploy real configurado |
| API Gateway | ✅ Stub funcional via nginx (`api-gateway/nginx.conf`) |

Consulte `docs/ai/tasks/prd-sprint-02-fase3.json` para o detalhamento tarefa a tarefa com `testCommand`/`artifacts`.
