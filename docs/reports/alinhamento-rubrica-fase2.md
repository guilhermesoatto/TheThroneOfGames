# Alinhamento com o rubric — Atividade Substitutiva Fase 2 (POSTECH)

**Fonte:** `fase 2.pdf` — "Arquitetura de Sistema .NET com Azure / Fase 2".
**Branch:** `release/fase-2-monolito` · **Data:** 2026-09-06

O enunciado é uma atividade de **CI/CD**. Este documento mapeia cada exigência do PDF para
onde ela é satisfeita no repositório e registra o que ficou **fora de escopo** por decisão.

---

## 1. Exigências do PDF → implementação

| Enunciado (PDF) | Onde no repo | Status |
|---|---|---|
| CI acionada automaticamente em mudanças no repositório | `.github/workflows/ci-cd.yml` — `on: push` / `pull_request` em `master`, `develop`, `release/fase-2-monolito` | ✅ |
| CI · **Compilação do código** | job `build` — `dotnet build -c Release` | ✅ |
| CI · **Execução de testes automatizados** | jobs `test` (unit, 52) + `e2e` (5) + `integration` (2) + `integration-db` (6, SQL real) | ✅ 63 testes |
| CI · **Construção de artefatos para implantação** | job `build` publica `api-publish` (`dotnet publish`); job `package` publica a **imagem Docker** em `ghcr.io` | ✅ |
| CD acionada automaticamente após sucesso da CI | job `deploy` — `needs: package`; `package` depende de toda a CI verde | ✅ |
| CD · **deploy automatizado em "produção"** (Azure / outra cloud / IIS) | job `deploy` promove a imagem publicada para a tag `:production` e emite instruções de execução; "produção" demonstrada = `docker compose pull && docker compose up -d` | ✅ (via GHCR + compose) |
| CI bloqueia código quebrado antes de produção | `package`/`deploy` têm `needs` de todos os jobs de CI; PR com teste vermelho ⇒ nenhum artefato, sem deploy | ✅ |
| Entregável = **vídeo** da esteira completa (CI → CD) | roteiro em [`validacao-pos-migracao-net10.md` §6](validacao-pos-migracao-net10.md); automação em `tools/record-delivery.js` | ▶️ a gravar |

## 2. Lista de ação do time → status

| # | Ação | Status |
|---|---|---|
| 1 | Migrar para .NET e remover toda referência ao .NET 9 | ✅ `net10.0` em todos os projetos; `global.json`; Dockerfiles; CI. Zero `net9.0` / `9.0.x` |
| 2 | Alinhar a expectativa do entregável | ✅ este documento + `DELIVERABLE.md` + `prd-fase2.json` atualizados |
| 3.1 | CI · job Build | ✅ job `build` (+ artefato `api-publish`) |
| 3.2 | CI · job Test | ✅ job `test` (57 testes: unit + cobertura → Codecov, gate *ratchet*) |
| 3.3 | CI · job Lint | ✅ job `lint` (`dotnet format --verify-no-changes` + `.editorconfig`) |
| 3.4 | CI · job Validar integrações | ✅ dois jobs: `integration` self-contained (InMemory, sem Docker) **+** `integration-db` contra **SQL Server real** via *service container* (migrations, dialeto, precisão decimal) |
| 3.5 | Incluir testes E2E + chamá-los no CI | ✅ projeto `TheThroneOfGames.E2E.Tests` + job `e2e` |
| extra | Segurança na esteira | ✅ job `security-scan` (SCA bloqueante `dotnet list --vulnerable` + hadolint + Trivy fs) + job `codeql` (SAST) + `.github/dependabot.yml` |

## 3. Fora de escopo (decisão consciente)

| Item | Motivo | Como fica coberto |
|---|---|---|
| ~~Validação de **SQL Server** no CI~~ | *(revertido)* — passou a ser feito | job `integration-db` sobe SQL Server 2022 via *service container* e roda migrations + repositórios reais. `Infrastructure.Tests` (InMemory) fica como smoke rápido |
| **Prometheus / Grafana** no CI (`T06`) | Sem valor num pipeline efêmero (métricas só fazem sentido com carga contínua) | job `integration` valida que `/metrics` responde; stack completa sobe no `docker compose` e aparece no vídeo |
| **Deploy em cloud real / Azure** (`T05`) | Sem subscription/segredos de nuvem | "Produção" = imagem versionada no GHCR promovida para `:production`; execução via compose |
| **Cobertura de testes > 80%** (`prd-fase2.json`) | Base legada mínima; migrations do EF (mantidas na contagem) puxam o total pra baixo | ~23% total (App 53% · Domain 36% · API 33% · Infra 6,8%). Gate *ratchet* via Codecov impede regressão; subida contínua. Não é critério do PDF |
| Migração dos testes unitários MSTest→xUnit (ADR-003) | Fora do escopo deste PR; risco desnecessário | E2E já em xUnit; convergência dos unitários fica para depois |
| `TreatWarningsAsErrors` + 20 avisos (CS8618, SYSLIB0060) | Limpeza ampla, fora do escopo | Registrado; ligar depois de zerar avisos |
| Swashbuckle na última versão | `9.0.4+` migra p/ Microsoft.OpenApi 2.x e quebra o `Program.cs` | Mantido em `9.0.1` (Microsoft.OpenApi 1.6.x); bump adiado |
| Digest-pinning das imagens base do Docker (agent-laws M-08) | Fora do escopo | Tags `sdk:10.0` / `aspnet:10.0`; pinar depois |

## 4. Governança

- **ADR-005 — Accepted** (aprovada pelo Domain Expert em 2026-09-06). Texto completo em
  [`../ai/knowledge/decisions/ADR-0005-TheThroneOfGames.Monolith-cicd-net10.md`](../ai/knowledge/decisions/ADR-0005-TheThroneOfGames.Monolith-cicd-net10.md).
  Escopo restrito ao monólito; não contradiz ADR-001..004 (microsserviços `GameStore.*`).
- `prd-fase2.json` continua na fase `4-containerization`; **nenhuma fase foi avançada** sem aprovação.
