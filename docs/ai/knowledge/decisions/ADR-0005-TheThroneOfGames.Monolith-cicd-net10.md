# ADR-0005 — TheThroneOfGames.Monolith — Fase 2 (CI/CD + .NET 10)

> **Gerado por:** manualmente (não via `tools/reflect.py`) — decisão de infraestrutura/esteira.
> **Data:** 2026-09-06
> **Status:** **Proposed** — aguarda aprovação do Domain Expert (agent-laws §3).

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
4. **Nova suíte E2E** `TheThroneOfGames.E2E.Tests` (xUnit + FluentAssertions + `WebApplicationFactory`
   + EF InMemory) exercitando a jornada `pre-register → activate → login → criar jogo (admin) → listar`.
5. **`Infrastructure.Tests` migrado de Testcontainers para EF Core InMemory.**
6. `Program.cs`: `Database.Migrate()` passa a ser guardado por `Database.IsRelational()` (permite host
   de teste in-process sem quebrar o `docker compose`).
7. **Lint** = `dotnet format --verify-no-changes` contra um `.editorconfig` novo (escopo pragmático).

## Consequências

### Positivas

- Esteira CI/CD completa e legível (um job por etapa), com bloqueio de merge em teste vermelho.
- `dotnet test` roda em **qualquer máquina/CI sem Docker** (14 testes: 9 unit + 5 E2E).
- **Zero pacotes vulneráveis** (antes: 1 HIGH + 2 Moderate).
- Toolchain determinística (`global.json`).
- Artefato de implantação real: imagem versionada por SHA no GHCR.

### Negativas / Trade-offs

- **Cobertura de testes ainda baixa (~9–18%)** — o critério `> 80%` do `prd-fase2.json` não é atingido.
- Frameworks de teste mistos: unit em MSTest+Moq, E2E em xUnit (convergência para xUnit adiada).
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
| Pipeline | `.github/workflows/ci-cd.yml` (reescrito) |
| Removido | `.github/workflows/ci-suitcase-gate0.yml` (pipeline TypeScript dormente) |
| Relatórios | `docs/reports/validacao-pos-migracao-net10.md`, `docs/reports/alinhamento-rubrica-fase2.md` |

## Supersede / Relacionados

- Supersede: *(nenhum)*
- Relacionado a: ADR-004 (suitcase dotnet). **Não** contradiz ADR-001..003, que valem para os
  microsserviços `GameStore.*` (fora do escopo desta branch).
