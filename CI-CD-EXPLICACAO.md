# CI/CD — TheThroneOfGames (Atividade Substitutiva Fase 2, PosTech)

> Documento de apoio à entrega em vídeo. Explica o que a esteira faz, onde ver a evidência real
> no GitHub Actions, e como o deploy foi demonstrado sem uma assinatura de cloud.

## 1. Onde está a esteira

Pipeline definido em [`.github/workflows/ci-cd.yml`](.github/workflows/ci-cd.yml), acionado
automaticamente em todo `push`/`pull_request` para `master`, `develop` ou
`release/fase-2-monolito` (branch desta entrega).

**Run usado no vídeo de entrega:**
https://github.com/guilhermesoatto/TheThroneOfGames/actions/runs/35303879577

Commit `e390642`, branch `release/fase-2-monolito`, status **Success**, duração 4m55s, 10/10 jobs
verdes.

## 2. CI — compilar → testar → gerar artefato

| Job | O que faz |
|---|---|
| `3.1 · Build` | `dotnet build -c Release` + `dotnet publish` da API → artefato `api-publish` |
| `3.3 · Lint` | `dotnet format --verify-no-changes` |
| `3.2 · Test (unit)` | testes unitários (Domain + Application + Infrastructure/InMemory) + cobertura → Codecov |
| `3.4 · Integration (self-contained)` | app compõe, modelo EF valida, endpoints de saúde/métricas respondem — sem Docker |
| `Integration (SQL Server real)` | migrations ponta-a-ponta contra SQL Server 2022 real (*service container* do próprio runner) |
| `3.5 · E2E` | jornadas HTTP ponta-a-ponta via `WebApplicationFactory` |
| `Security (SCA + hadolint)` | `dotnet list package --vulnerable` (bloqueante em HIGH/CRITICAL), hadolint nos Dockerfiles, Trivy no filesystem |
| `CodeQL (SAST)` | análise estática do C# — resultados na aba *Security*, não bloqueia |

Todos esses jobs rodam **em paralelo** depois do `build`; o job `package` só executa se **todos**
passarem. Um PR com teste quebrado nunca gera artefato.

## 3. CD — deploy automatizado

| Job | O que faz |
|---|---|
| `Package (Docker → GHCR)` | builda a imagem Docker e publica em `ghcr.io/guilhermesoatto/thethroneofgames/thethroneofgames-api`, com tags `:<sha>` e `:<branch>` + scan Trivy da imagem |
| `Deploy (CD)` | promove a imagem recém-publicada para a tag `:production` (`docker buildx imagetools create`) |

**Por que "produção" é local e não Azure/AWS:** o enunciado da atividade permite três alvos —
Azure, outra cloud, ou **IIS da própria máquina**. Sem assinatura de cloud disponível para o
projeto acadêmico, a equipe optou por uma variação da opção "máquina local": a imagem publicada e
promovida pela CD é baixada e executada via Docker Compose, em vez de IIS. Isso está documentado
em [`docs/ai/knowledge/decisions/ADR-0005-TheThroneOfGames.Monolith-cicd-net10.md`](docs/ai/knowledge/decisions/ADR-0005-TheThroneOfGames.Monolith-cicd-net10.md).

No vídeo, essa etapa aparece como:

```bash
docker compose pull   # baixa exatamente a imagem que o job Deploy acabou de promover
docker compose up -d  # roda o artefato publicado — não um build local
```

## 4. Observabilidade / APM

Além de Prometheus + Grafana (métricas), a API tem tracing distribuído (OpenTelemetry) e logs
estruturados correlacionados (Serilog + `CorrelationIdMiddleware`), exportados para o console do
container. No vídeo, `docker compose logs -f api` mostra o mesmo `TraceId`/`CorrelationId` em
todas as linhas de uma requisição — da entrada HTTP até a query SQL — provando que log e trace da
mesma chamada estão correlacionados.

## 5. O que o vídeo demonstra, na ordem

1. Run verde do GitHub Actions (grafo dos 10 jobs) + log real de um job de teste.
2. Job `Deploy (CD)` promovendo a imagem para `:production`.
3. `docker compose pull && docker compose up -d` — sobe o artefato publicado.
4. Fluxo funcional real via Swagger (`pre-register` → `activate` → `login` → criar jogo).
5. Logs/traces correlacionados no terminal para essas chamadas.
6. Dashboard Grafana com métricas HTTP da API.
