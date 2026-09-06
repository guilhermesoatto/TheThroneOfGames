# Roteiro — Vídeo de entrega Fase 2 (CI/CD)

**Objetivo do vídeo (rubric do PDF):** demonstrar a **esteira completa CI → CD** funcionando —
CI acionada automaticamente (compilação + testes + geração de artefato) e CD acionada após o
sucesso da CI (deploy automatizado + publicação do artefato), com a aplicação rodando.

**Duração alvo:** 6 a 10 minutos. **Branch:** `release/fase-2-monolito`.

---

## 0. Preparação (antes de gravar)

- [ ] **Docker Desktop** aberto e rodando.
- [ ] `git status` limpo na branch `release/fase-2-monolito`; `git pull` feito.
- [ ] Rodar **uma vez** `docker compose pull` para baixar as imagens (mssql/grafana/prometheus
      são grandes) — na gravação o `pull` fica rápido (já em cache) e você mostra que a imagem
      da API vem do `ghcr.io`.
- [ ] Se você já rodou os microsserviços das fases 3/4 nesta máquina, limpar containers órfãos:
      `docker compose down --remove-orphans` (evita o aviso "Found orphan containers" na gravação).
- [ ] Fechar abas/apps e **silenciar notificações** (Windows: modo foco).
- [ ] Navegador com **zoom 110–125%** e uma janela de terminal com **fonte grande** (≥ 16pt).
- [ ] Deixar abertas as abas do navegador, nesta ordem:
  1. `https://github.com/guilhermesoatto/TheThroneOfGames` (aba **Code**, no `README.md`)
  2. `https://github.com/guilhermesoatto/TheThroneOfGames/actions`
  3. `https://github.com/guilhermesoatto?tab=packages&repo_name=TheThroneOfGames` (Packages)
  4. (abrir durante a Parte 4) `http://localhost:5000/swagger`
  5. (abrir durante a Parte 4) `http://localhost:3000` (Grafana)
- [ ] Gravador: **OBS Studio** ou **Xbox Game Bar** (`Win+G`), 1080p, cursor visível.
- [ ] Ter em mãos o bloco de comandos (seção **Comandos prontos** no fim deste arquivo).
- [ ] Preparar a mudança que dispara a CI ao vivo: um commit trivial (ver Parte 1).

> **Dica:** grave em blocos e edite depois. Os tempos de espera da CI (~3–5 min) você **corta
> ou acelera** na edição — não precisa ficar parado na tela.

---

## 1. CI dispara automaticamente (~1 min)

**Mostrar:**
1. Aba **Code** → o `README.md`, seção **CI/CD**: o grafo de jobs
   `build → (lint · test · integration · integration-db · e2e · security-scan) → package → deploy`.
2. No terminal, fazer uma mudança pequena e commitar + empurrar **ao vivo**:
   ```bash
   git commit --allow-empty -m "chore: gatilho de demonstração do pipeline"
   git push origin release/fase-2-monolito
   ```
3. Ir na aba **Actions** → aparece um run novo, status **in progress**. Abrir o run.

**Falar:** *"Qualquer push na branch dispara a CI automaticamente. Cada etapa da CI é um job
independente; a CD só roda depois que toda a CI passa."*

---

## 2. CI — compilação, testes e artefato (~2–3 min)

Dentro do run, abrir os jobs e mostrar:

| Job | O que mostrar | O que falar |
|---|---|---|
| **3.1 · Build** | step *Build (Release)* → `0 Erro(s)`; step *Publish API*; step *Upload build artifact* | *"Compila em Release e publica a API. O `dotnet publish` é o artefato de implantação."* |
| Seção **Artifacts** do run (rodapé) | o artefato **`api-publish`** disponível para download | *"Artefato versionado, baixável."* |
| **3.2 · Test (unit)** | `Aprovado! ... 52` (Domain + Application + Infrastructure); step *Upload coverage to Codecov* | *"52 testes de unidade + cobertura enviada ao Codecov (gate que impede a cobertura de cair)."* |
| **3.5 · E2E** | `Aprovado! ... 3` — jornada registrar → ativar → logar → criar jogo | *"Testes ponta-a-ponta batendo na API via HTTP."* |
| **Integration (SQL Server real)** | o *service container* `mssql/server:2022` subindo + `6` testes verdes | *"Aqui sobe um SQL Server real como container lateral — valida as migrations e os repositórios contra o banco de verdade, não um fake."* |
| **Security (SCA + hadolint)** | step *dotnet — falha em pacote com CVE HIGH/CRITICAL* → `OK`; hadolint nos Dockerfiles | *"Análise de dependências que bloqueia CVE alta, e lint dos Dockerfiles."* |
| **CodeQL (SAST)** *(opcional)* | job verde | *"Análise estática de segurança do código C# — resultados vão para a aba Security."* |

---

## 3. CD dispara após a CI + publica o artefato (~1–2 min)

**Mostrar:**
1. No grafo do run, evidenciar que **`Package`** só começou **depois** de todos os jobs de CI
   ficarem verdes (a seta de dependência / a ordem no tempo).
2. Abrir **Package (Docker → GHCR)** → step *Build & push image* → as **tags publicadas**
   (`:<sha>`, `:release-fase-2-monolito`, e `:latest` na `master`).
3. Ir na aba **Packages** (do perfil/repo) → abrir **`thethroneofgames-api`** → mostrar as
   **versões publicadas** e as tags. *"O artefato — a imagem Docker versionada — está no
   Container Registry."*
4. Voltar ao run → abrir **Deploy (CD)** → step *Promote image → :production* e o
   **Job summary** (tabela com a imagem, a tag `:production` e o comando de execução).

**Falar:** *"A CD roda sozinha após a CI. Ela publica a imagem no GHCR e promove a versão do
commit para a tag `:production` — é essa imagem que a 'produção' consome."*

---

## 4. "Produção" rodando (~2–3 min)

No terminal:

```bash
docker compose pull          # puxa a imagem da API publicada pela CD (ghcr.io/...:release-fase-2-monolito)
docker compose up -d
docker compose ps            # 4 containers: api (healthy), mssql, prometheus, grafana
```

**Mostrar no `docker compose pull`** que a linha do serviço `api` aponta para
`ghcr.io/guilhermesoatto/thethroneofgames/thethroneofgames-api` — é o artefato da esteira.

### 4a. Swagger — fluxo funcional (`http://localhost:5000/swagger`)

1. **`POST /api/usuario/pre-register`** — *Try it out* → body:
   ```json
   { "name": "Video Demo", "email": "demo@throne.test", "password": "Str0ng!Pass", "role": "Admin" }
   ```
   → resposta `200` com `activationToken`. **Copiar o token.**
2. **`POST /api/usuario/activate`** — parâmetro `activationToken` = (colar) → `200`.
3. **`POST /api/usuario/login`** — body `{ "email": "demo@throne.test", "password": "Str0ng!Pass" }`
   → `200` com `token` (JWT). **Copiar o token.**
4. Botão **Authorize** (cadeado, topo) → colar `Bearer <token>` → *Authorize*.
5. **`POST /api/admin/game`** — body:
   ```json
   { "name": "Elden Ring", "genre": "RPG", "price": 199.90, "isAvailable": true }
   ```
   → `201 Created`.
6. **`GET /api/admin/game`** → `200` com o jogo na lista.

### 4b. Observabilidade

- `http://localhost:5000/metrics` → texto Prometheus cru (métricas da API).
- `http://localhost:9090` (Prometheus) → *Graph* → query `http_requests_received_total`
  (ou digitar `http_` e ver o autocomplete) → *Execute* → mostra as requisições que você
  acabou de fazer no Swagger.
- `http://localhost:3000` (Grafana — dê ~20s após o `up` para terminar de subir) → login
  `admin` / `admin` (pular troca de senha) → *Dashboards* → abrir o dashboard provisionado →
  painéis com taxa de requests / latência.

**Falar:** *"A stack de observabilidade coleta as métricas HTTP da aplicação em produção."*

---

## 5. Cenário de bloqueio — CI barra código quebrado (~1 min, opcional mas recomendado)

Reforça o critério *"CI bloqueia código quebrado antes de produção"*.

**Opção A (ao vivo, exige um PR):** criar um branch, quebrar um teste de propósito
(ex.: trocar um `Assert.Equal` no `UsuarioTests.cs`), abrir PR → a CI falha no job `test` →
os jobs `package`/`deploy` aparecem como **skipped** → *"nenhum artefato é gerado, o merge fica bloqueado"*.

**Opção B (mais simples):** mostrar no histórico do **Actions** o run **#75** (commit `c1d8737`),
que ficou **vermelho** — os 6 jobs de CI passaram e só o `package` falhou (bug do Dockerfile,
corrigido em seguida). Evidencia que uma falha na esteira **impede a publicação**.

---

## 6. Encerramento (~30s)

```bash
docker compose down
```

**Falar (resumo):** *"A esteira: um push dispara a CI, que compila, roda 63 testes automatizados
— incluindo integração contra SQL Server real — gera o artefato e faz a análise de segurança.
Passando tudo, a CD publica a imagem versionada no Container Registry e promove a versão de
produção. A aplicação sobe a partir dessa imagem, com autenticação, CRUD e observabilidade
funcionando. Tudo sem intervenção manual."*

---

## Comandos prontos (copiar/colar)

```bash
# Parte 1 — disparar a CI
git commit --allow-empty -m "chore: gatilho de demonstração do pipeline"
git push origin release/fase-2-monolito

# Parte 4 — subir a "produção" com a imagem publicada pela CD
docker compose pull
docker compose up -d
docker compose ps

# checagens rápidas (opcional, para narrar)
curl -s http://localhost:5000/api/usuario/public-info
curl -s http://localhost:5000/metrics | head -20

# Parte 6 — derrubar
docker compose down
```

**Payloads do Swagger** (seção 4a):

```json
// pre-register
{ "name": "Video Demo", "email": "demo@throne.test", "password": "Str0ng!Pass", "role": "Admin" }

// login
{ "email": "demo@throne.test", "password": "Str0ng!Pass" }

// criar jogo (com Authorize: Bearer <token>)
{ "name": "Elden Ring", "genre": "RPG", "price": 199.90, "isAvailable": true }
```

---

## Checklist final antes de submeter o vídeo

- [ ] Aparece o **push disparando a CI** automaticamente.
- [ ] Aparece a **compilação** (`dotnet build` sem erros).
- [ ] Aparecem os **testes automatizados** passando (unit + e2e + integração com SQL real).
- [ ] Aparece a **geração do artefato** (`api-publish` e/ou a imagem Docker).
- [ ] Aparece a **CD rodando automaticamente após a CI**.
- [ ] Aparece a **publicação do artefato** no Container Registry (aba Packages).
- [ ] Aparece a **aplicação rodando** (Swagger executando um fluxo real).
- [ ] (Bônus) Aparece o **cenário de falha** bloqueando a publicação.
- [ ] Áudio limpo, texto legível, sem dados sensíveis reais na tela.
