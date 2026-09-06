# TheThroneOfGames — Fase 2 (Monolito)

> FIAP Cloud Games (FCG) — Tech Challenge, Fase 2: aplicação monolítica ASP.NET Core, containerizada, com pipeline de CI/CD e stack de observabilidade.
> Branch desta entrega: `release/fase-2-monolito`.

## Objetivos da Fase 2

Entregar um **Deployment Pipeline** totalmente automatizado para o monolito FCG:

- Aplicação ASP.NET Core (DDD: Domain / Application / Infrastructure / API) com autenticação JWT, CRUD de jogos e gestão de usuários.
- Imagem Docker enxuta, multi-stage, non-root.
- Pipeline de CI (build + testes em cada PR/commit) e CD (build + push da imagem, com scan de vulnerabilidades).
- Stack de observabilidade (Prometheus + Grafana) coletando métricas HTTP da aplicação.

Ver critérios de aceite completos em [docs/Objectives/sprint-1/DELIVERABLE.md](docs/Objectives/sprint-1/DELIVERABLE.md) e no PRD governado por IA em [docs/Objectives/sprint-1/prd-fase2.json](docs/Objectives/sprint-1/prd-fase2.json).

## Stack Tecnológico

- ASP.NET Core 10.0 Web API (`.NET 10 LTS`, SDK fixado em `global.json`)
- Entity Framework Core 10 + SQL Server (runtime) / EF Core InMemory (testes)
- JWT Bearer Authentication
- MSTest + Moq (testes unitários) e xUnit + `WebApplicationFactory` (testes E2E) — asserções nativas, sem libs pagas
- Docker / Docker Compose
- Prometheus + Grafana (métricas e dashboards)
- GitHub Actions (CI/CD em jobs separados: build · lint · test · integration · e2e · package · deploy)

## Como subir o Monolito + Banco + Monitoramento

Pré-requisitos: [Docker Desktop](https://www.docker.com/products/docker-desktop/) instalado e rodando.

```bash
docker-compose up --build
```

Isso sobe 4 containers na rede `thethroneofgames-network`:

| Serviço | Descrição | URL |
|---|---|---|
| `api` | TheThroneOfGames.API (monolito) | http://localhost:5000 |
| `mssql` | SQL Server 2019 | localhost:1433 |
| `prometheus` | Coleta de métricas (`/metrics` da API a cada 5s) | http://localhost:9090 |
| `grafana` | Dashboards (login padrão: `admin` / `admin`) | http://localhost:3000 |

As migrations do Entity Framework são aplicadas **automaticamente** no startup da API (`Program.cs`) — não é necessário rodar `dotnet ef database update` manualmente ao usar o Docker Compose.

Para derrubar o ambiente:

```bash
docker-compose down
```

### Rodando localmente sem Docker (desenvolvimento)

Pré-requisito: .NET SDK 10 (a versão exata é fixada em [`global.json`](global.json)).

```bash
dotnet restore
dotnet ef database update --project TheThroneOfGames.Infrastructure --startup-project TheThroneOfGames.API --context MainDbContext
dotnet run --project TheThroneOfGames.API
```

Configure a connection string e o segredo JWT em `TheThroneOfGames.API/appsettings.Development.json` ou via variáveis de ambiente (`ConnectionStrings__DefaultConnection`, `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`).

## Rodando os Testes

```bash
dotnet test TheThroneOfGames.sln
```

Isso executa os **57 testes automatizados** da Fase 2, **sem necessidade de Docker ou SQL Server**:

| Projeto | Testes | O que valida |
|---|---|---|
| `TheThroneOfGames.Domain.Tests` | 15 | Invariantes e comportamento de `Usuario` (ativação, papéis, perfil) |
| `TheThroneOfGames.Application.Tests` | 32 | `UsuarioService` (validação de senha, ativação, perfil), `GameService` (compra, catálogo), hashing PBKDF2 |
| `TheThroneOfGames.Infrastructure.Tests` | 5 | Persistência (`GameEntityRepository`/`UsuarioRepository`) via **EF Core InMemory** |
| `TheThroneOfGames.E2E.Tests` | 5 | Jornadas HTTP ponta-a-ponta via `WebApplicationFactory` (registrar → ativar → logar → criar jogo) + saúde/métricas |

Cobertura de linha é medida no CI (`--collect:"XPlat Code Coverage"`) e enviada ao **Codecov**,
que aplica um gate *ratchet* (a cobertura não pode cair) — ver [`codecov.yml`](codecov.yml).

> Os testes de integração antes usavam Testcontainers + SQL Server real; foram convertidos para
> EF Core InMemory para rodarem no CI sem slave services (ver
> [docs/reports/alinhamento-rubrica-fase2.md](docs/reports/alinhamento-rubrica-fase2.md)).

## Gravação do vídeo de demonstração

[`tools/record-delivery.js`](tools/record-delivery.js) (Playwright) automatiza a navegação pelo Swagger da API (executando um `POST /api/usuario/pre-register` real) e pelo Grafana/Prometheus, gravando em `docs/videos/FCG_ENTREGA_FASE_2.mp4`. Pressupõe a stack já rodando (`docker-compose up --build`, ver acima).

```sh
cd tools && npm install && npm run playwright:install
node record-delivery.js
```

## Endpoints Principais

Com a API rodando, acesse `http://localhost:5000/swagger` para a documentação interativa.

- `POST /api/usuario/pre-register` — registro de usuário (envia token de ativação)
- `POST /api/usuario/activate` — ativação de conta via token
- `POST /api/usuario/login` — autenticação, retorna JWT
- `GET /api/admin/game` — CRUD de jogos (requer JWT com role `Admin`)
- `GET /api/admin/user-management` — gestão de usuários (requer JWT com role `Admin`)
- `GET /metrics` — métricas Prometheus

## CI/CD

Pipeline definido em [.github/workflows/ci-cd.yml](.github/workflows/ci-cd.yml). Cada ação da CI é
um **job independente**; a CD dispara automaticamente após a CI verde (push em `master` ou
`release/fase-2-monolito`):

```
build ─┬─ lint ──────────────┐
       ├─ test (unit) ───────┤
       ├─ integration ───────┼─→ package (Docker → ghcr.io) ─→ deploy (promove :production)
       ├─ e2e ───────────────┤
       └─ security-scan ─────┘
```

| Job | Ação |
|---|---|
| `build` | `dotnet build -c Release` + `dotnet publish` da API → artefato `api-publish` |
| `lint` | `dotnet format --verify-no-changes` (formatação/estilo via `.editorconfig`) |
| `test` | testes unitários (Domain + Application + Infrastructure/InMemory) + cobertura → Codecov (gate *ratchet*) |
| `integration` | validação self-contained: a app compõe, o modelo EF valida, `/api/usuario/public-info` e `/metrics` respondem — **sem** SQL/Prometheus/Grafana (fora de escopo nesta fase, ver [alinhamento-rubrica-fase2.md](docs/reports/alinhamento-rubrica-fase2.md)) |
| `e2e` | jornadas HTTP ponta-a-ponta (`WebApplicationFactory` + InMemory) |
| `security-scan` | Trivy no filesystem/dependências |
| `package` | build + push da imagem Docker em `ghcr.io` + Trivy na imagem (só em push, depende de toda a CI verde) |
| `deploy` | promove a imagem publicada para `:production`; o deploy demonstrado no vídeo é `docker compose pull && docker compose up -d` puxando essa imagem |

Um PR com testes quebrados falha a CI e **nenhum artefato é criado** — o merge para produção fica bloqueado.

## Arquitetura

Monolito em camadas (DDD):

```
TheThroneOfGames.API/             # Controllers, Program.cs, autenticação JWT, Swagger
TheThroneOfGames.Application/     # Serviços de aplicação (GameService, UsuarioService, PromotionService)
TheThroneOfGames.Domain/          # Entidades, eventos de domínio, interfaces de repositório
TheThroneOfGames.Infrastructure/  # EF Core (MainDbContext), repositórios, migrations
```

Comunicação de eventos de domínio (ex.: `UsuarioAtivadoEvent`) é feita via `SimpleEventBus` (barramento em memória) — sem dependências externas de mensageria nesta fase, conforme restrição arquitetural do sprint (ver [DELIVERABLE.md](docs/Objectives/sprint-1/DELIVERABLE.md)).

## Governança de IA (Suitcase)

Este projeto usa uma governança de IA baseada em PRDs versionados e validados mecanicamente. Ver `CLAUDE.md` na raiz e [docs/ai/skills/agent-laws.md](docs/ai/skills/agent-laws.md) para as regras seguidas por agentes de IA neste repositório.

```bash
node tools/validate-prd.js docs/Objectives/sprint-1/prd-fase2.json
```

## Licença

Licença MIT
