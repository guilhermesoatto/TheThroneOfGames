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

- ASP.NET Core 9.0 Web API
- Entity Framework Core + SQL Server
- JWT Bearer Authentication
- MSTest + Moq (testes unitários) e Testcontainers.MsSql (testes de integração com banco real)
- Docker / Docker Compose
- Prometheus + Grafana (métricas e dashboards)
- GitHub Actions (CI/CD)

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

Isso executa os 9 testes automatizados da Fase 2:

| Projeto | Testes | O que valida |
|---|---|---|
| `TheThroneOfGames.Domain.Tests` | 2 | Entidades de domínio |
| `TheThroneOfGames.Application.Tests` | 2 | Serviços de aplicação (`GameService`) |
| `TheThroneOfGames.Infrastructure.Tests` | 5 | Persistência real via **Testcontainers.MsSql** (sobe um container SQL Server, aplica as migrations e valida `GameEntityRepository`/`UsuarioRepository` contra o banco de verdade) |

O relatório de execução completo, incluindo cobertura de código, está em [docs/phase-2-evidence/RELATORIO_TESTES_FASE2.md](docs/phase-2-evidence/RELATORIO_TESTES_FASE2.md).

> Requer Docker Desktop rodando — o projeto `Infrastructure.Tests` sobe um container SQL Server real via Testcontainers para cada execução.

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

Pipeline definido em [.github/workflows/ci-cd.yml](.github/workflows/ci-cd.yml):

1. **Build & Test** — restaura, builda e roda `dotnet test TheThroneOfGames.sln` em cada PR/commit.
2. **Build & Push Docker Image** — builda a imagem a partir do `Dockerfile` raiz, roda scan de vulnerabilidades (Trivy) e publica em `ghcr.io`.
3. **Security Scan** — Trivy scan do filesystem/dependências.
4. **Deploy to Cloud Provider** — job condicional (habilitado apenas quando a variável de repositório `CLOUD_DEPLOY_ENABLED` estiver configurada), pronto para receber o comando específico do provedor de nuvem escolhido. Ver [docs/Objectives/sprint-1/tasks/T05-cloud-deploy.md](docs/Objectives/sprint-1/tasks/T05-cloud-deploy.md) para as opções sugeridas (AWS ECS Fargate, Azure Container Apps, GCP Cloud Run).

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
