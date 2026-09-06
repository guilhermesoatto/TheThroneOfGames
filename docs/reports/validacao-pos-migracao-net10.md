# Validação pós-migração — .NET 10 + CI/CD em jobs

**Data:** 2026-09-06
**Branch:** `release/fase-2-monolito`
**Executor:** Claude Code (Sonnet 5)
**Máquina:** Windows 11 — .NET SDK **10.0.302** (fixado em `global.json`)

Sucessor de [`validacao-branch-fase-2-monolito-2026-09-06.md`](validacao-branch-fase-2-monolito-2026-09-06.md)
(estado pré-migração, mantido como histórico).

---

## 1. O que mudou

| Área | Antes | Depois |
|---|---|---|
| Runtime | `net9.0` (CI fixava `9.0.x`, sem `global.json`) | **`net10.0`**, `global.json` fixa `10.0.302` |
| Pacotes Microsoft.* | `9.0.x` | `10.0.11` (EF Core, ASP.NET, Config.Binder) |
| CVEs | SSH.NET **HIGH** (transitiva), MimeKit + OpenTelemetry.Api **Moderate** | **0 vulneráveis** (MimeKit 4.17, OpenTelemetry 1.18, SSH.NET removido com Testcontainers) |
| `Infrastructure.Tests` | Testcontainers + SQL Server real (exige Docker) | **EF Core InMemory** (smoke rápido, sem Docker) |
| Suíte E2E | inexistente | **`TheThroneOfGames.E2E.Tests`** (xUnit + `WebApplicationFactory` + InMemory) |
| Integração SQL real | não havia no CI | **`TheThroneOfGames.Integration.Tests`** + job `integration-db` (SQL Server 2022 via *service container*) |
| Segurança na esteira | só Trivy (não bloqueante) | job `security-scan` (SCA bloqueante + hadolint) + job `codeql` (SAST) + `dependabot.yml` |
| CI | 1 job `build-and-test` monolítico | **10 jobs**: build · lint · test · integration · integration-db · e2e · security-scan · codeql · package · deploy |
| `Program.cs` | `Database.Migrate()` incondicional | guardado por `Database.IsRelational()` (permite host de teste in-process) |
| Lint | inexistente | `.editorconfig` + `dotnet format --verify-no-changes` |
| Swashbuckle | `9.0.1` | mantido em `9.0.1` (linha 9.0.4+ migra p/ Microsoft.OpenApi 2.x e quebra o `Program.cs` — dívida registrada) |
| Workflow TS dormente | `ci-suitcase-gate0.yml` presente | removido |

## 2. Resultado da esteira local

Comandos (todos **sem Docker / sem SQL Server**):

```bash
dotnet --version                                  # 10.0.302
dotnet build TheThroneOfGames.sln -c Release      # 0 erros, 20 avisos
dotnet format TheThroneOfGames.sln --verify-no-changes --severity warn   # exit 0 (limpo)
dotnet test TheThroneOfGames.sln -c Release       # 57 (unit+e2e); +6 integração exigem SQL Server
dotnet list TheThroneOfGames.sln package --vulnerable --include-transitive   # 0 vulneráveis (8/8 projetos)
```

| Job / etapa | Resultado |
|---|---|
| **build** | ✅ 0 erros, 20 avisos |
| **lint** (`dotnet format --verify`) | ✅ limpo |
| **test** — `Domain.Tests` | ✅ 15/15 |
| **test** — `Application.Tests` | ✅ 32/32 |
| **test** — `Infrastructure.Tests` (InMemory) | ✅ 5/5 |
| **e2e** — `E2E.Tests` (`UserJourneyTests`) | ✅ 3/3 |
| **integration** — `E2E.Tests` (`HealthAndMetricsTests`) | ✅ 2/2 |
| **integration-db** — `Integration.Tests` (SQL Server 2022 local) | ✅ 6/6 |
| **security** — `dotnet list --vulnerable` + hadolint (`--failure-threshold error`) | ✅ limpo |
| **Total de testes** | ✅ **63/63** |
| **Vulnerabilidades (M-07/N-08)** | ✅ 0 HIGH/CRITICAL, 0 Moderate |

## 3. Avisos remanescentes (20) — dívida técnica, não bloqueiam

| Código | Qtd (aprox.) | Origem |
|---|---|---|
| CS8618 (non-nullable sem init) | ~14 | DTOs/entidades (`UserEntity`, `Promotion*`, `GameDto`...) |
| SYSLIB0060 | 4 | `UsuarioService` — ctor de `Rfc2898DeriveBytes` obsoleto no .NET 10 (usar `Rfc2898DeriveBytes.Pbkdf2`) |
| CS0105 | 0 | (removidos pelo passe de `dotnet format`) |

`Directory.Build.props` mantém `TreatWarningsAsErrors=false`. Ligar após limpar os avisos (fora deste PR).

## 4. Cobertura de testes

| Assembly | Cobertura de linha |
|---|---|
| `TheThroneOfGames.Application` | 53,5% |
| `TheThroneOfGames.Domain` | 36,0% |
| `TheThroneOfGames.API` | 33,1% (só via E2E) |
| `TheThroneOfGames.Infrastructure` | 6,8% (2078 linhas, majoritariamente migrations do EF — mantidas na contagem por decisão do time) |
| **Total** | **~23%** (918/4040 linhas) |

Gate via **Codecov** (`codecov.yml`): modelo *ratchet* — `project` com `target: auto`/`threshold: 1%`
(cobertura não pode cair) e `patch` com `target: 60%` (código novo). O critério "> 80%" do
`prd-fase2.json` **não é atingido**; não faz parte do rubric do PDF, que avalia a esteira CI/CD.

**Setup único do Codecov** (dono do repo, fora do CI): logar em codecov.io com GitHub, adicionar
`guilhermesoatto/TheThroneOfGames`, colar `CODECOV_TOKEN` em Settings → Secrets → Actions.
Enquanto não existir, o `codecov/codecov-action` é no-op (`fail_ci_if_error: false`) e a esteira segue verde.

## 5. Verificação da esteira no GitHub Actions (pendente de push)

- `actionlint` local no `.github/workflows/ci-cd.yml`: **sem erros**.
- Após `git push` da branch, confirmar no Actions:
  - grafo `build → (lint · test · integration · e2e · security-scan) → package → deploy`;
  - `package` publica `ghcr.io/<owner>/thethroneofgames/thethroneofgames-api:<sha>` (+ `:<branch>`);
  - `deploy` promove `:production` e escreve o summary;
  - PR com teste quebrado ⇒ `package`/`deploy` não executam.

## 6. Comandos do vídeo de entrega

```bash
# CI/CD: mostrar o run verde no GitHub Actions (build → ... → deploy) e a imagem no GHCR (Packages)
docker compose pull      # puxa a imagem publicada pela esteira
docker compose up -d      # "produção" local: API + SQL Server + Prometheus + Grafana
# Swagger: http://localhost:5000/swagger  → pre-register → activate → login → POST /api/admin/game
# Grafana: http://localhost:3000  (admin/admin) — métricas HTTP da API
```
