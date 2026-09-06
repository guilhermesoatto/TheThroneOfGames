# Validação da branch `release/fase-2-monolito`

**Data:** 2026-09-06
**Executor:** Claude Code (Sonnet 5)
**Máquina:** Windows 11 — .NET SDK 10.0.302 / 9.0.315 (CI fixa `9.0.x`)
**Escopo:** solução `TheThroneOfGames.sln` (monólito da Fase 2 — 7 projetos)

---

## 1. Estado de Git

| Item | Valor |
|---|---|
| Branch | `release/fase-2-monolito` |
| Sincronia com origin | atualizada (0 à frente / 0 atrás de `origin/release/fase-2-monolito`) |
| Relação com `master` | 9 commits à frente, 0 atrás, histórico linear (`master` é ancestral de `HEAD`) |
| Working tree | limpo, exceto `docs/reports/` não rastreado (2 JSON de load-test antigos) |
| Diff `master...HEAD` | 439 arquivos, **+4.235 / −54.781** linhas |
| Último commit | `571f26a feat: fluxos completos de demo (registro->login->criar jogo / ...->comprar)` |

A branch **isola o monólito da Fase 2** e remove das rastreadas o conteúdo dos microsserviços
(`GameStore.*`) e tooling não-Fase-2 (daí o saldo de −54k linhas). As pastas `GameStore.*/bin|obj`
ainda existem em disco como saída de build obsoleta (ignoradas pelo Git — sem impacto).

---

## 2. Restore + Build (espelhando `ci-cd.yml`)

`dotnet restore` → **OK**
`dotnet build TheThroneOfGames.sln -c Release --no-restore` → **OK — 0 erros, 29 avisos (~8s)**

| Categoria de aviso | Qtd | Detalhe |
|---|---|---|
| CS8618 (non-nullable sem init) | ~22 | DTOs/entidades: `LoginRequest`, `UserEntity`, `Promotion*`, `GameDto`, `PromotionDto`, `PurchaseDto` |
| NU1603 | 6 | `MSTest.TestAdapter/Framework 3.1.5` indisponível → resolvido `3.2.0` nos 3 projetos de teste |
| NU1902 | 1 | `MimeKit 4.13.0` — vulnerabilidade moderada (GHSA-g7hc-96xr-gvvx) |
| CS0105 | 1 | `using` duplicado em `TheThroneOfGames.Infrastructure/Repository/AuthenticationRepository.cs:4` |
| CS0618 | 1 | ctor `MsSqlBuilder()` obsoleto em `GameRepositoryTests.cs:20` |

`Directory.Build.props`: `TreatWarningsAsErrors=false` e `EnforceCodeStyleInBuild=false` —
explicitamente contrário ao padrão da suitcase (`dotnet-clean-arch.md`), com comentário reconhecendo a dívida.

---

## 3. Bateria de testes mapeada

`dotnet test TheThroneOfGames.sln -c Release --no-build --logger trx`

| Projeto | Framework | Total | Passou | Falhou |
|---|---|---:|---:|---:|
| `TheThroneOfGames.Domain.Tests` | MSTest + Moq | 2 | **2** | 0 |
| `TheThroneOfGames.Application.Tests` | MSTest + Moq | 2 | **2** | 0 |
| `TheThroneOfGames.Infrastructure.Tests` | MSTest + Testcontainers.MsSql | 5 | 0 | **5** |
| **TOTAL** | | **9** | **4** | **5** |

### 3.1 As 5 falhas são 100% ambientais

Todas as 5 falham em `GameRepositoryTests.ClassInitialize` com:

```
Failed to connect to Docker endpoint at 'npipe://./pipe/docker_engine'
```

O **Docker Desktop não está em execução** nesta máquina. A classe sobe um container SQL Server
via Testcontainers no class-init; sem Docker, todos os 5 testes da classe caem no init
(inclusive o trivial `AppDbContext_TypeExists`), e o `ClassCleanup` dispara um
`NullReferenceException` secundário (container nulo).

Testes afetados: `AppDbContext_TypeExists`, `CanCreate_Real_DbContext_Against_MsSqlContainer`,
`GameEntityRepository_AddAsync_Then_GetByIdAsync_PersistsToRealDatabase`,
`GameEntityRepository_UpdateAsync_PersistsChangesToRealDatabase`,
`UsuarioRepository_AddAsync_Then_GetByEmailAsync_PersistsAggregateToRealDatabase`.

No CI (`ubuntu-latest`, com Docker) espera-se que os 5 passem. **Não foi possível confirmar o
status do pipeline** desta branch a partir daqui (`gh` CLI ausente).

### 3.2 Cobertura é rasa

3 arquivos de teste, **207 linhas**, para **81 arquivos-fonte** do monólito. Domain/Application
são smoke tests de reflexão/DI (`*_TypeExists`, `InterfaceTypes_AreResolvable`,
`CanCreate...Instance`, `AddAsync_ShouldCallRepository`), sem asserções de regra de negócio nem
de invariantes. Fere `agent-laws` M-01 / M-09 / §5 (teste de domínio deve rodar sem infra —
aqui até "type exists" depende de Docker).

---

## 4. Segurança de dependências

`dotnet list package --vulnerable --include-transitive`

| Pacote | Versão | Severidade | Advisory | Origem |
|---|---|---|---|---|
| **SSH.NET** | 2025.1.0 | **HIGH** | GHSA-q939-rpr3-3284 | transitiva (Testcontainers, só projeto de teste) |
| MimeKit | 4.13.0 | Moderate | GHSA-g7hc-96xr-gvvx | direta em `Infrastructure` |
| OpenTelemetry.Api | 1.15.0 | Moderate | GHSA-g94r-2vxg-569j | transitiva |

O HIGH fere `agent-laws` N-08. O gate Trivy do `ci-cd.yml` está com `exit-code: '0'`
(não bloqueante, por decisão explícita "while the academic delivery stabilizes").

---

## 5. Dockerfile

Multi-stage, `sdk:9.0` → `aspnet:9.0`, usuário não-root (`appuser`), instala `curl` para o
healthcheck do compose. **Imagens base fixadas por tag, não por digest** — `agent-laws` M-08
pede digest pinado.

---

## 6. Observações de governança

- A Fase 2 é, por decisão, o **monólito legado** (namespace `TheThroneOfGames.*`, domínio anêmico
  `*Entity`, MSTest+Moq). A arquitetura DDD/Hexagonal e a stack xUnit descritas em
  `CONTEXT.md` / `architecture.md` pertencem às fases seguintes (`GameStore.*`), fora do escopo
  desta branch. As divergências acima são esperadas para a branch, mas ficam registradas.
- O PRD ativo (`prd-sprint-00-migration.json`) é um PoC de injeção de governança
  (`currentPhase: "1-domain"`, 1 tarefa pendente = rodar `validate-prd.js`); não cobre a bateria
  de testes do monólito.
- Toolchain: ausência de `global.json` — build local usou SDK 10.0.302 para alvos `net9.0`
  enquanto o CI fixa `9.0.x`. Funcionou, mas é drift a corrigir (adicionar `global.json`).

---

## 7. Veredito

| Dimensão | Status |
|---|---|
| Compila (Release) | ✅ 0 erros |
| Testes unitários (Domain + Application) | ✅ 4/4 |
| Testes de integração (Infrastructure) | ⚠️ 5/5 vermelhos **localmente** — falta Docker; sem veredito real |
| Pipeline CI da branch | ❓ não verificável a partir desta máquina |
| Higiene | ⚠️ 29 avisos, 1 CVE HIGH transitiva, cobertura rasa, drift de SDK |

**Conclusão:** a branch está **buildável e com a camada unitária verde**. A validação da bateria
completa depende de subir o Docker Desktop (ou confiar na execução do CI). Nenhum erro de
compilação ou falha de teste *de código* foi encontrado — as 5 falhas são de ambiente.

### Próximos passos sugeridos
1. Subir Docker Desktop e reexecutar `dotnet test TheThroneOfGames.sln -c Release` para validar os 5 de integração.
2. Confirmar o status do workflow `CI/CD Pipeline - The Throne of Games (Fase 2 - Monolito)` no GitHub Actions.
3. Adicionar `global.json` fixando `9.0.x`.
4. Atualizar `MimeKit` e endereçar a transitiva `SSH.NET` HIGH (ou documentar exceção EXC).
5. Corrigir os avisos baratos (CS0105 `using` duplicado; ctor obsoleto do `MsSqlBuilder`).
