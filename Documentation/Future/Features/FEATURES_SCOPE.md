## Escopo de Mudanças Recomendadas (Future Features)

Objetivo: documentar lacunas observadas durante a validação dos OKRs (arquivo `TheThroneOfGames.API/objetivo1.md`) e propor um conjunto prioritário de mudanças / PRs para melhorar qualidade, segurança e manutenção antes de evoluir para microservices.

**Resumo**
- **Status atual:** Monólito organizado por contextos (`GameStore.Usuarios`, `GameStore.Catalogo`, `GameStore.Vendas`), com autenticação JWT, EF Core + migrations, Swagger em ambiente de desenvolvimento e cobertura de testes já existente.
- **Principais gaps:** inconsistência de nomes e modelos entre Domain ↔ Infrastructure, duplicidade de contratos CQRS/validators, gestão de segredos, documentações e testes de integração para fluxos críticos.

**Mapeamento OKR → Estado → Ação Recomendada**

- **Cadastro de usuários (OKR)**: implementado via `UsuarioController` e `UsuarioService`.
  - **Evidência:** `TheThroneOfGames.API/Controllers/UsuarioController.cs`, `GameStore.Usuarios/Application/Services/UsuarioService.cs`.
  - **Ação:** adicionar testes de integração que cubram registro → ativação → login com claims/roles verificadas.
  - **Prioridade:** Alta
  - **Esforço estimado:** 1-2 dias

- **Validação de e-mail e senha (OKR)**: presente, mas alinhamento de mensagens e política pode ser melhorado.
  - **Evidência:** validação no controller; `ValidatePassword`/hash em `UsuarioService` e seeds em migrations.
  - **Ação:** consolidar política de senha em um serviço `PasswordPolicy` (único ponto), alinhar mensagens e adicionar testes unitários/BDD para regras críticas.
  - **Prioridade:** Medium
  - **Esforço estimado:** 1 dia

- **Autenticação JWT + Roles (OKR)**: presente e configurada.
  - **Evidência:** `TheThroneOfGames.API/Program.cs` (AddJwtBearer), controllers usam `[Authorize]` e checam roles no token.
  - **Ação:** garantir que `appsettings.*` não contenham chaves em texto; mover para variáveis de ambiente/Secret Manager; adicionar teste que valida renovação/expiração de token.
  - **Prioridade:** Alta
  - **Esforço estimado:** 0.5-1 dia

- **Persistência (EF Core / Migrations) (OKR)**: presente e com migrations/seed.
  - **Evidência:** `TheThroneOfGames.Infrastructure/Persistence/MainDbContext.cs`, `TheThroneOfGames.Infrastructure/Migrations/*`.
  - **Ação:** unificar nomenclatura (ex.: `Usuario` vs `User`, `GameEntity` vs `Game`) — criar PR de refactor pequeno com mapeamentos e testes; revisar `OnModelCreating` para consistência e evitar tabelas mal nomeadas (ex.: `Promotion` sendo usado para `User` no código atual).
  - **Prioridade:** Medium
  - **Esforço estimado:** 1-3 dias (depende do alcance do rename)

- **API / Middleware / Swagger (OKR)**: presente.
  - **Evidência:** `TheThroneOfGames.API/Program.cs`, `TheThroneOfGames.API/Middleware/ExceptionMiddleware.cs`.
  - **Ação:** proteger Swagger em non-dev (ou requerer autenticação), estender middleware para logs estruturados e correlacionamento de requests (request id). Adicionar exemplos de uso no `README.md` da API.
  - **Prioridade:** Low → Medium
  - **Esforço estimado:** 0.5-1 dia

- **Qualidade / Testes (OKR)**: boa cobertura atual; adicionar cenários de integração ponta-a-ponta.
  - **Evidência:** `GameStore.Usuarios.Tests/`, `GameStore.Catalogo.Tests/`, `Test/Integration/*`.
  - **Ação:** criar testes de integração que utilizem `CustomWebApplicationFactory` para validar cenários críticos (registro → ativação → login → role-based access). Incluir CI step que execute testes e gere relatório TRX/coverage.
  - **Prioridade:** Alta
  - **Esforço estimado:** 1-2 dias

- **DDD / Organização por Contextos (OKR)**: bom alinhamento estrutural.
  - **Evidência:** pastas `Domain/`, `Application/`, `Infrastructure/` em cada contexto.
  - **Ação:** consolidar contratos de CQRS em pacote `GameStore.CQRS.Abstractions` compartilhado; remover duplicidades de tipos (`ValidationResult`, handlers); atualizar projetos que hoje usam wrappers temporários.
  - **Prioridade:** Medium → High (para longo prazo)
  - **Esforço estimado:** 2-5 dias (refactor sequencial)


**PRs sugeridos (pequenos, com escopo claro)**

- PR #1 — Tests: "Integration test: user registration → activation → login"
  - Adiciona teste de integração que usa `CustomWebApplicationFactory` e valida token claims/roles.
  - Arquivos afetados: `Test/Integration/*` + novo teste em `GameStore.Usuarios.Tests`.
  - Prioridade: Alta

- PR #2 — Security: "Move JWT key to env/Secret Manager + docs"
  - Remove secrets de `appsettings.json` (se existentes) e atualiza `Program.cs` para ler de `IConfiguration`/env vars; documenta como configurar localmente com `dotnet user-secrets`.
  - Prioridade: Alta

- PR #3 — Refactor: "Unify domain/infrastructure naming"
  - Padroniza nomes de entidades (`Usuario`/`User`, `Game`), corrige `OnModelCreating` e adiciona mapeamentos para compatibilidade; atualiza migrations ou cria nova migration de renome.
  - Prioridade: Medium

- PR #4 — Architecture: "Extract CQRS Abstractions and remove wrapper validators"
  - Cria/atualiza projeto compartilhado `GameStore.CQRS.Abstractions` (se necessário), remove duplicidades e altera handlers/tests para usar o tipo único de ValidationResult.
  - Prioridade: Medium

- PR #5 — Ops: "Protect Swagger and add structured request logging"
  - Habilita Swagger apenas para dev + adiciona autenticação para acessar doc em staging; adiciona logging middleware com correlação.
  - Prioridade: Low


**Estimativas e Prioridade (visão rápida)**
- Alta: PR #1 (Integration tests), PR #2 (JWT secrets) — entregar antes de release/entrega.
- Média: PR #3 (unify names), PR #4 (CQRS consolidation) — refactors que reduzem dívida técnica.
- Baixa: PR #5 (Swagger + logging) — melhorias operacionais.


**Checklist de aceitação por PR**

- PR #1: testes passam localmente e no CI; coverage mínima para cenário crítico; não alterar contratos públicos.
- PR #2: chave JWT não presente no repositório; instruções para uso de `dotnet user-secrets` e variáveis de ambiente adicionadas ao README.
- PR #3: migrations atualizadas; script/guia para migração de banco em produção (se aplicável).
- PR #4: builds em toda a solução passam; removidos wrappers temporários; testes atualizados.


**Notas finais e recomendações rápidas**
- Priorize automatizar a execução de testes no CI e a geração de relatórios TRX para regressão rápida.
- Planeje o refactor do CQRS e dos validators em pequenas etapas para reduzir risco. Comece pelo pacote de abstrações e depois adapte consumidores.
- Remova seeds com senhas padrão em ambientes públicos; mantenha hashes apenas para ambientes de desenvolvimento controlados.

---
Arquivo gerado automaticamente com base na análise do repositório e do `objetivo1.md`.
