# TheThroneOfGames — Fase 4 (Kubernetes & Escala)

## Visão Geral

FIAP Cloud Games (FCG) evoluiu em duas fases sobre a mesma base de **3 microsserviços independentes** (Fase 3 — Usuários, Catálogo, Vendas):

- **Fase 3**: busca avançada via **Elasticsearch**, processamento assíncrono via **Serverless Functions**, um **API Gateway** único e **Event Sourcing** no fluxo de pedidos.
- **Fase 4** (esta branch, `release/fase-4-kubernetes`): imagens Docker otimizadas (Alpine, non-root), orquestração completa via **Kubernetes** (Deployments, Services, ConfigMaps/Secrets, Ingress), **autoscaling horizontal (HPA)** validado com teste de carga real, **Monitoramento** (Prometheus + Grafana) e **APM** (Jaeger) rodando no cluster.

Requisitos originais do desafio: [`docs/Objectives/TC NETT - Fase 4 (1).md`](<docs/Objectives/TC%20NETT%20-%20Fase%204%20(1).md>) (Fase 4) e [`docs/Objectives/TC NETT - Fase 3.md`](docs/Objectives/TC%20NETT%20-%20Fase%203.md) (Fase 3).
Governança/IA (Suitcase): [`CLAUDE.md`](CLAUDE.md).
PRD técnico Fase 4 + status real tarefa a tarefa: [`docs/Objectives/sprint-3/prd-fase4.json`](docs/Objectives/sprint-3/prd-fase4.json) — validar com `node tools/validate-prd.js docs/Objectives/sprint-3/prd-fase4.json`.
PRD técnico Fase 3: [`docs/ai/tasks/prd-sprint-02-fase3.json`](docs/ai/tasks/prd-sprint-02-fase3.json) e [`docs/Objectives/sprint-2/DELIVERABLE.md`](docs/Objectives/sprint-2/DELIVERABLE.md).
Arquitetura Fase 3 (fluxo assíncrono): [`docs/architecture-flow.md`](docs/architecture-flow.md).
Arquitetura Fase 4 (fluxo de rede no Kubernetes): [`docs/k8s-architecture-flow.md`](docs/k8s-architecture-flow.md).

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
- **Serverless (Azure Functions, isolated worker)**: `GameStore.Notifications.Functions` roda containerizado (`Dockerfile` + Azurite para `AzureWebJobsStorage`) e consome `PedidoFinalizadoEvent` via `[RabbitMQTrigger]` real (filas fan-out dedicadas, sem competir com outros consumers) para notificação e processamento de pagamento assíncronos.
- **Event Sourcing**: `GameStore.Vendas/Domain/EventSourcing` + `Infrastructure/EventSourcing` registram toda transição de estado do aggregate `Pedido` em um Event Store append-only, e `FinalizarPedidoCommandHandler` publica `PedidoFinalizadoEvent` no RabbitMQ (sem replay/projeções ainda — ver PRD).
- **Distributed Tracing**: OpenTelemetry (HTTP + EF Core) exportado via OTLP para Jaeger (`docker-compose.yml`, UI em `http://localhost:16686`); o `trace_id` propaga entre serviços mesmo através do RabbitMQ, embutido no payload da mensagem (`GameStore.Common.Tracing.TraceContextPropagator`) já que o binding `[RabbitMQTrigger]` do Functions isolated worker não expõe headers AMQP.

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

Isso sobe: PostgreSQL, RabbitMQ, Elasticsearch, Jaeger, Azurite, os 3 microsserviços, o Serverless (`notifications-functions`), o API Gateway (nginx) e Prometheus/Grafana. Cada microsserviço aplica suas próprias EF Core migrations na inicialização.

**Serviços disponíveis:**
- API Gateway: http://localhost:8080
- Usuários API: http://localhost:5001/swagger
- Catálogo API: http://localhost:5002/swagger
- Vendas API: http://localhost:5003/swagger
- Elasticsearch: http://localhost:9200
- Jaeger UI (traces): http://localhost:16686
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

## Kubernetes (Fase 4)

Manifestos completos em [`k8s/`](k8s/): namespace, ConfigMaps/Secrets, Deployments/Services das 3 APIs + Function serverless + API Gateway + infraestrutura (Postgres, RabbitMQ, Elasticsearch, Azurite), HPA, Ingress (TLS) e a stack de observabilidade (Prometheus, Grafana, Jaeger).

### Pré-requisitos
- Um cluster Kubernetes acessível via `kubectl` — gerenciado na cloud (GKE/EKS/AKS) **ou** local para desenvolvimento/validação ([kind](https://kind.sigs.k8s.io/) ou [minikube](https://minikube.sigs.k8s.io/)).
- As 4 imagens (`GameStore.Usuarios.API`, `GameStore.Catalogo.API`, `GameStore.Vendas.API`, `GameStore.Notifications.Functions`) publicadas num registry acessível pelo cluster — os manifestos usam `image: gamestore/<serviço>:latest` como placeholder; ajuste antes do deploy (ou use `kind load docker-image` para um cluster kind local).

### Deploy

```sh
# build das 4 imagens (repita para cada Dockerfile em GameStore.*.API/ e GameStore.Notifications.Functions/)
docker build -t gamestore/usuarios-api:latest -f GameStore.Usuarios.API/Dockerfile .
# ... catalogo-api, vendas-api, notifications-functions

kubectl apply -f k8s/namespaces.yaml
kubectl apply -f k8s/configmaps/ -f k8s/secrets/
kubectl apply -f k8s/deployments/ -f k8s/services/
kubectl apply -f k8s/hpa/ -f k8s/ingress/

kubectl -n gamestore get pods -w
```

### Validar o autoscaling (HPA) com carga real

```sh
kubectl apply -f k8s/load-test/load-test-job.yaml
kubectl -n gamestore get hpa -w          # observar TARGETS (%CPU) e REPLICAS subindo
kubectl delete job load-test -n gamestore  # remover depois do teste
```

**Status real:** toda a stack acima foi implantada e validada de ponta a ponta contra um cluster Kubernetes real local (kind) durante o desenvolvimento (ver `docs/k8s-architecture-flow.md` §7 para o relato completo, incluindo dois bugs reais encontrados e corrigidos: bind non-root na porta 80 e esgotamento de conexões do Postgres sob HPA escalado). O teste de carga confirmou o `catalogo-api-hpa` escalando de 2 para 6 réplicas (105% de CPU) e voltando ao mínimo após o fim da carga.

### Deploy na nuvem (Amazon EKS) via pipeline

Pipeline dedicado em [`.github/workflows/deploy-eks.yml`](.github/workflows/deploy-eks.yml): builda as 4 imagens, publica no Amazon ECR e implanta `k8s/` inteiro num cluster EKS a cada push em `release/fase-4-kubernetes` (ou sob demanda, com opção de rodar o teste de carga do HPA contra o cluster real). Passo a passo completo — criação do cluster (manual, deliberada, para não gerar custo a cada push), permissões IAM necessárias e configuração dos Secrets do GitHub — em [`docs/eks-deploy.md`](docs/eks-deploy.md).

### Gravação do vídeo de demonstração

[`tools/record-delivery.js`](tools/record-delivery.js) (Playwright) automatiza a navegação pelos Swagger de cada serviço (executando uma chamada real) e pelo Jaeger UI (grafo de traces), gravando em `docs/videos/FCG_ENTREGA_FASE_4.mp4`. Pressupõe o ambiente já rodando (local via `docker compose up`/`kubectl`, ou os `BASE_URL_*` apontando para o Ingress do EKS — ver `docs/eks-deploy.md` §5).

```sh
cd tools && npm install && npm run playwright:install
node record-delivery.js
```

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
├── k8s/                                    # Manifestos Kubernetes (Fase 4)
├── monitoring/                             # Prometheus/Grafana provisioning (docker-compose)
├── docs/ai/                                # Governança IA (Suitcase): knowledge base, tasks, skills
├── docs/Objectives/sprint-2/               # PRD e deliverable da Fase 3
└── docs/Objectives/sprint-3/               # PRD da Fase 4
```

## Status Real da Fase 4

Ver `docs/Objectives/sprint-3/prd-fase4.json` para o checklist tarefa a tarefa. Resumo:

| Item | Status |
|---|---|
| Comunicação assíncrona entre microsserviços (RabbitMQ) | ✅ (herdado da Fase 3) |
| Imagens Docker otimizadas (Alpine, non-root) para os 3 APIs + Function | ✅ |
| Manifestos Kubernetes (Namespace, Deployments, Services, ConfigMaps, Secrets, Ingress) | ✅ |
| HPA (autoscaling por CPU, 3 APIs) — validado com teste de carga real | ✅ |
| Monitoramento (Prometheus + Grafana) | ✅ |
| APM (Jaeger) | ✅ |
| Validação de ponta a ponta em cluster Kubernetes real (local — kind) | ✅ |
| Pipeline de deploy no Amazon EKS (build ECR + kubectl apply + HPA opcional) | ✅ pronto (`.github/workflows/deploy-eks.yml`, ver `docs/eks-deploy.md`) |
| Cluster EKS **provisionado e pipeline executada contra ele** | ⚠️ pendente — depende das credenciais AWS/execução do pipeline, ver PRD `fase4-T03` |
| Retry/DLQ em mensageria (opcional) | ❌ não implementado (flag opcional no edital) |

## Status Real da Fase 3

Ver `docs/Objectives/sprint-2/DELIVERABLE.md` para o checklist tarefa a tarefa. Resumo:

| Item | Status |
|---|---|
| 3 microsserviços independentes | ✅ |
| API Gateway | ✅ |
| Busca via Elasticsearch (indexação + endpoint HTTP) | ✅ |
| Serverless (triggers reais via RabbitMQ) | ✅ |
| Event Sourcing (append-only + publish real no RabbitMQ; sem replay/projeções) | ✅ |
| Distributed Tracing (Jaeger real — trace única Vendas→Usuarios→Functions verificada ao vivo) | ✅ |

## Licença
Licença MIT
