# ADR-0005 — TheThroneOfGames.Monolith — Fase 2 (CI/CD + .NET 10)

> **Gerado por:** manualmente (não via `tools/reflect.py`) — decisão de infraestrutura/esteira.
> **Data:** 2026-09-06
> **Status:** **Accepted** — aprovada pelo Domain Expert em 2026-09-06 (agent-laws §3).
> **Revisão pós-aprovação:** SQL Server passou a ser validado no CI via *service container*
> (job `integration-db`) + job `security` (CodeQL, hadolint, SCA bloqueante). Ver §Decisão itens 10–11.

---

## Contexto

A branch `release/fase-2-monolito` entrega o monólito `TheThroneOfGames.*` para a atividade
substitutiva da Fase 2 (POSTECH), cujo enunciado avalia **CI (compilar → testar → gerar artefato)
+ CD (deploy automatizado)** demonstrados em vídeo.

Estado anterior (ver `docs/reports/validacao-branch-fase-2-monolito-2026-09-06.md`):

- Todos os projetos em `net9.0`; CI fixava `9.0.x`; sem `global.json` (build local usava SDK 10 → drift).
- `.github/workflows/ci-cd.yml` tinha **um único job** `build-and-test`; sem jobs de lint nem e2e.
- `TheThroneOfGames.Infrastructure.Tests` dependia de **Docker + SQL Server** (`Testcontainers.MsSql`)
  e trazia `SSH.NET` (CVE **HIGH**) transitivamente. CVEs moderadas em `MimeKit` e `OpenTelemetry.Api`.

### Alternativas consideradas

| Tema | Alternativas | Escolha |
|---|---|---|
| Runtime | `.NET 8` (LTS antigo) vs **`.NET 10` (LTS)** | `.NET 10` — SDK já instalado, alinhado ao `Directory.Build.props` canônico da suitcase |
| Deploy (CD) | Azure/Cloud (exige subscription + secrets) · IIS local · **imagem GHCR + `docker compose`** | Imagem versionada no GHCR promovida para `:production`; sem credenciais de nuvem |
| Testes de integração no CI | Manter Testcontainers (exige Docker) · excluir do CI · **converter para EF InMemory** | EF InMemory — roda no CI sem slave services e elimina a CVE HIGH |
| Lint | `warnings-as-errors` (29 avisos a limpar) · **`dotnet format --verify`** | `dotnet format` + `.editorconfig` pragmático + passe único de formatação |
| Asserções nos testes | FluentAssertions (8.x é **licença paga**) · Shouldly (dep extra) · **xUnit `Assert` nativo** | `Assert` nativo — POC não depende de lib paga nem de dep extra |
| Gate de cobertura | SonarCloud (setup pesado) · floor fixo no job · **Codecov (ratchet)** | Codecov: 1 step + `codecov.yml`, grátis p/ repo público, gate como status check de PR |

### Constraints

- Monólito: **não há slave services** (SQL Server, Prometheus, Grafana, SMTP) disponíveis na esteira.
- agent-laws M-07/N-08: zero pacotes com CVE HIGH/CRITICAL.
- Não avançar fase do `prd-fase2.json` sem aprovação (continua em `4-containerization`).

## Decisão

1. **Migrar o monólito de `net9.0` para `net10.0`** em todos os projetos; `global.json` fixa o SDK
   `10.0.302`; pacotes `Microsoft.*` → `10.0.11`; imagens Docker base → `sdk:10.0` / `aspnet:10.0`.
   Nenhuma referência a `net9.0` / `9.0.x` permanece.
2. **Reorganizar o `ci-cd.yml` em jobs independentes:** `build` · `lint` · `test` · `integration`
   · `e2e` · `security-scan` · `package` · `deploy`. A CD (`package` → `deploy`) só dispara em `push`
   e depende de **toda** a CI verde.
3. **`integration` é self-contained:** valida composição de DI, modelo EF e endpoints de saúde/métricas
   via `WebApplicationFactory` — **sem** SQL/Prometheus/Grafana, que ficam fora do CI por decisão de escopo.
4. **Nova suíte E2E** `TheThroneOfGames.E2E.Tests` (xUnit + `Assert` nativo + `WebApplicationFactory`
   + EF InMemory) exercitando a jornada `pre-register → activate → login → criar jogo (admin) → listar`.
5. **`Infrastructure.Tests` migrado de Testcontainers para EF Core InMemory.**
6. `Program.cs`: `Database.Migrate()` passa a ser guardado por `Database.IsRelational()` (permite host
   de teste in-process sem quebrar o `docker compose`).
7. **Lint** = `dotnet format --verify-no-changes` contra um `.editorconfig` novo (escopo pragmático).
8. **Testes de unidade ampliados** em Domain e Application (14 → 63 testes no total) cobrindo invariantes
   de `Usuario`, validação de senha, ativação, compra de jogo e hashing PBKDF2.
9. **Gate de cobertura via Codecov** (`codecov.yml`, modelo *ratchet*: cobertura não pode cair;
   patch novo ≥ 60%). Sem FluentAssertions — asserções em xUnit `Assert` nativo (a 8.x virou licença paga).
10. **Job `integration-db`** (revisão pós-aprovação): novo projeto `TheThroneOfGames.Integration.Tests`
    (xUnit) roda contra **SQL Server real** via *service container* do GitHub Actions — valida migrations
    ponta-a-ponta, dialeto SQL, precisão de coluna decimal e tradução de query (o que o EF InMemory não pega).
    O job `integration` self-contained (InMemory, sem Docker) é mantido como camada rápida.
11. **Job `security`** (revisão pós-aprovação): SCA bloqueante (`dotnet list package --vulnerable`
    falha em HIGH/CRITICAL — agent-laws N-08) + **hadolint** nos dois Dockerfiles + Trivy fs; e job
    **CodeQL** (SAST C#) cujos achados vão para a aba *Security* mas **não bloqueiam** o deploy.
    `.github/dependabot.yml` abre PRs semanais (NuGet + Actions), com `FluentAssertions` e
    `Swashbuckle.AspNetCore` em `ignore` (travas das dívidas conhecidas).

## Consequências

### Positivas

- Esteira CI/CD completa e legível (um job por etapa), com bloqueio de merge em teste vermelho.
- `dotnet test` roda em **qualquer máquina/CI sem Docker** (14 testes: 9 unit + 5 E2E).
- **Zero pacotes vulneráveis** (antes: 1 HIGH + 2 Moderate).
- Toolchain determinística (`global.json`).
- Artefato de implantação real: imagem versionada por SHA no GHCR.

### Negativas / Trade-offs

- **Cobertura ainda abaixo de 80%** (~23% total; Application 53%, Domain 36%, API 33%, Infrastructure 6,8%).
  O denominador de Infrastructure é dominado por migrations do EF (mantidas na contagem por decisão do time).
  O gate *ratchet* impede regressão; subir o alvo é trabalho contínuo.
- Frameworks de teste mistos: unit em MSTest+Moq, E2E/Integration em xUnit (convergência para xUnit adiada).
- CodeQL adiciona ~4–6 min ao pipeline (job paralelo, não bloqueia `package`).
- O job `integration-db` depende de Docker no runner (`ubuntu-latest` já tem); localmente exige
  `docker compose up mssql` ou `INTEGRATION_DB_CONNECTION` apontando para um SQL Server.
- `Swashbuckle` preso em `9.0.1` (a linha `9.0.4+` migra para `Microsoft.OpenApi 2.x` e quebra o
  `Program.cs`). Bump adiado.
- 22 avisos de compilação remanescentes (CS8618, `Rfc2898DeriveBytes`/SYSLIB0060); `TreatWarningsAsErrors`
  continua `false`.
- Imagens Docker fixadas por tag, não por digest (agent-laws M-08).
- Validação de integração real com SQL Server / observabilidade sai do CI — coberta só manualmente via `docker compose`.

## Artefatos gerados

| Tipo | Caminho |
|---|---|
| Toolchain | `global.json` |
| Lint | `.editorconfig` |
| Projeto E2E | `TheThroneOfGames.E2E.Tests/` (`CustomWebApplicationFactory`, `UserJourneyTests`, `HealthAndMetricsTests`) |
| Projeto Integration | `TheThroneOfGames.Integration.Tests/` (`SqlServerFixture`, `MigrationsTests`, `RepositorySqlServerTests`, `ApiJourneySqlServerTests`) |
| Testes de unidade | `Domain.Tests/Entities/UsuarioTests.cs`; `Application.Tests/Services/{UsuarioServiceTests,GameServiceBehaviorTests,PasswordHashingTests}.cs` |
| Cobertura | `codecov.yml` (gate ratchet) + steps `codecov/codecov-action` nos jobs `test`/`integration`/`e2e` |
| Segurança | `.github/dependabot.yml`, `.hadolint.yaml`, jobs `security-scan` (SCA+hadolint) e `codeql` no pipeline |
| Pipeline | `.github/workflows/ci-cd.yml` (reescrito) |
| Removido | `.github/workflows/ci-suitcase-gate0.yml` (pipeline TypeScript dormente) |
| Relatórios | `docs/reports/validacao-pos-migracao-net10.md`, `docs/reports/alinhamento-rubrica-fase2.md` |

## Supersede / Relacionados

- Supersede: *(nenhum)*
- Relacionado a: ADR-004 (suitcase dotnet). **Não** contradiz ADR-001..003, que valem para os
  microsserviços `GameStore.*` (fora do escopo desta branch).
