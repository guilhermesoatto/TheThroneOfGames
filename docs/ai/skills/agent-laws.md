# [SKILL: AGENT LAWS — BEHAVIORAL CONSTITUTION]

**Camada:** Layer 3 (Skill) — aplicável a todos os stacks  
**Precedência:** Abaixo de Layer 0, Layer 1 e Layer 2. Em caso de conflito, as camadas superiores prevalecem.  
**Uso:** Lido pelo agente antes de qualquer tarefa de implementação. Define o que o agente PODE fazer autonomamente, o que EXIGE aprovação humana, e os controles obrigatórios de qualidade.

---

## §1 — ORDEM DE BOOTSTRAP DE CONTEXTO

Antes de qualquer tarefa (nova sessão, nova feature, novo aggregate), o agente DEVE executar esta sequência exatamente:

```
1. Ler docs/ai/knowledge/CONTEXT.md
   └→ Verificar: aggregates ativos, ADRs existentes, constraints em aberto

2. Ler docs/ai/architecture.md
   └→ Verificar: topologia K8s, contratos de evento, hierarquia de camadas

3. Consultar ChromaDB com a query da tarefa
   python tools/query-knowledge.py "<tarefa>" --all
   └→ Relevância ≥ 80%: aplicar diretamente
   └→ 60–79%: confirmar com Domain Expert antes de usar
   └→ < 60%: conhecimento ausente — parar e perguntar

4. Carregar o skill correspondente ao stack
   └→ TypeScript: docs/ai/skills/typescript-clean-arch.md
   └→ .NET: dotnet/docs/ai/skills/dotnet-clean-arch.md
   └→ Angular: angular/docs/ai/skills/angular-clean-arch.md

5. Verificar se existe prd.json em andamento
   docs/ai/tasks/prd-[nome].json
   └→ Se existir: retomar da última tarefa com status "in-progress" ou "not-started"
   └→ Se não existir: executar business-to-code.md antes de escrever qualquer código
```

**Nunca pule a etapa 1.** Ignorar CONTEXT.md causa contradição com ADRs e gera retrabalho.

---

## §2 — LEIS IMUTÁVEIS DE QUALIDADE (MUST / NEVER)

Estas leis não têm exceções técnicas. Para qualquer exceção, ver §6.

### MUST (obrigatório)

| # | Lei |
|---|---|
| M-01 | Todo Aggregate Root DEVE ter ao menos: 1 Value Object, 1 Domain Event, 1 Use Case coberto por teste. |
| M-02 | Todo Use Case DEVE retornar um tipo somático de erro: `Either<DomainError, T>` (TypeScript/Node.js), `Result<T, E>` (Angular) ou `Result<T, Error>` (.NET). Nunca `throw` como fluxo de negócio. |
| M-03 | Todo log de erro DEVE incluir `traceId`, `correlationId`, `aggregate`, `action` e `sanitizedPayload`. |
| M-04 | Toda fronteira de entrada (Controller, Input Adapter, MCP handler) DEVE validar e sanitizar o input antes de passar ao Use Case. |
| M-05 | Todo evento de domínio DEVE conter: `eventId`, `eventType`, `eventVersion`, `aggregateId`, `aggregateName`, `occurredAt`, `correlationId`, `payload`. |
| M-06 | Após cada fase aprovada pelo Domain Expert, DEVE executar: `python tools/reflect.py --prd ... --phase ...` |
| M-07 | Antes de instalar qualquer pacote, DEVE executar `npm audit` (TypeScript/Angular) ou `dotnet list package --vulnerable` (.NET) e aguardar resultado. |
| M-08 | Todo Dockerfile DEVE usar imagem com digest pinado, usuário não-root (UID ≥ 1000) e sem shell interativo. |
| M-09 | Testes de domínio DEVEM rodar sem qualquer dependência de infraestrutura (sem banco, sem HTTP, sem filesystem). |
| M-10 | CONTEXT.md DEVE ser a primeira leitura de qualquer sessão e a última escrita após aprovação de fase. |

### NEVER (proibido sem exceção EXC)

| # | Lei |
|---|---|
| N-01 | NUNCA faça um Controller ou Adapter acessar banco de dados diretamente — todo acesso passa pelo Use Case. |
| N-02 | NUNCA importe módulos de infraestrutura (`fs`, `http`, `pg`, `axios`, `@angular/*`) dentro de `/domain`. |
| N-03 | NUNCA hardcode secrets, tokens, senhas ou connection strings — use variáveis de ambiente. |
| N-04 | NUNCA crie acoplamento síncrono entre Bounded Contexts diferentes — use eventos assíncronos. |
| N-05 | NUNCA altere ou remova uma migration já aplicada — crie uma nova. |
| N-06 | NUNCA remova ou altere o tipo de um campo de evento já publicado em produção — use Event Versioning (ver `event-versioning.md`). |
| N-07 | NUNCA logue campos sensíveis (`password`, `token`, `secret`, `authorization`, PII) — sanitize antes de logar. |
| N-08 | NUNCA instale pacote com CVE HIGH ou CRITICAL — busque alternativa segura ou peça autorização explícita. |
| N-09 | NUNCA use `any` em TypeScript dentro de `/domain` ou `/application` — use tipos explícitos ou `unknown` com narrowing. |
| N-10 | NUNCA contradiga um ADR existente sem aprovação do Domain Expert e geração de ADR de supersedência. |

---

## §3 — FRONTEIRAS DE AUTONOMIA

Define o que o agente pode fazer **sozinho** versus o que **exige pausa e aprovação humana**.

### Autônomo (sem aprovação humana)

- Escrever código dentro de uma tarefa do prd.json com status `not-started` ou `in-progress`
- Rodar testes localmente (`npm test`, `dotnet test`)
- Consultar ChromaDB (`query-knowledge.py`)
- Criar ou atualizar arquivos em `/src`, `/tests`, `/infrastructure`
- Criar `.feature` files para BDD com base em use cases já aprovados
- Executar `embed-knowledge.py` após editar arquivos de conhecimento
- Propor refatorações **sem executá-las** (apresentar ao Domain Expert primeiro)

### Requer aprovação humana (parar e perguntar)

- Nomear um novo Aggregate Root ou Bounded Context
- Alterar o schema de um evento de domínio já existente
- Adicionar uma nova dependência (npm/NuGet) — mostrar o audit primeiro
- Mudar a estratégia de persistência (trocar banco, trocar ORM)
- Criar uma exceção a qualquer lei de §2 (usar protocolo EXC de §6)
- Avançar para a próxima fase do prd.json
- Fazer merge, push ou qualquer operação git destrutiva
- Criar infraestrutura nova (novo serviço K8s, novo tópico Kafka, novo schema PostgreSQL)
- Dividir um Aggregate em dois (requer novo prd.json e nova série de ADRs)

---

## §4 — PROTOCOLO DE FALHA E BLOQUEIO

Quando o agente encontra um bloqueio genuíno:

```
1. PARAR — nunca avançar com premissas não validadas
2. DOCUMENTAR o bloqueio:
   - Qual tarefa do prd.json está bloqueada (id + description)
   - Por que está bloqueada (regra ambígua / conhecimento ausente / conflito de ADR)
   - Qual informação ou decisão do Domain Expert desbloquearia
3. ATUALIZAR o prd.json:
   status: "blocked"
   blockReason: "[descrição clara do bloqueio]"
4. REGISTRAR no CONTEXT.md:
   Adicionar linha na tabela "Constraints Ativas"
5. APRESENTAR ao Domain Expert com pergunta precisa e opções se existirem
```

**Nunca adivinhe regras de negócio.** Se o glossário em `ubiquitous-language.md` não define o termo, pare na etapa 1.

---

## §5 — CHECKLIST DE AUTO-VERIFICAÇÃO

Antes de apresentar qualquer fase ao Domain Expert, o agente DEVE verificar todos os itens:

```
DOMÍNIO
[ ] Camada /domain tem zero imports de infraestrutura
[ ] Todos os Value Objects são imutáveis e validam no construtor
[ ] Todos os Domain Events têm os 8 campos obrigatórios do contrato
[ ] Nenhum Entity tem setter público — apenas métodos de negócio

APLICAÇÃO
[ ] Todos os Use Cases retornam Either<L,R> ou Result<T,E>
[ ] Todos os ports (interfaces) estão em /domain ou /application
[ ] Nenhum Use Case instancia diretamente um Adapter ou Repository

QUALIDADE
[ ] Testes de domínio passam sem nenhum mock de infraestrutura
[ ] Cobertura de domínio: 100% dos casos de uso cobertos
[ ] Cobertura de falhas: ao menos 1 teste de invariante violado por entidade

OBSERVABILIDADE
[ ] correlation_id propagado em todas as chamadas I/O
[ ] Nenhum console.log livre — apenas structured log com campos obrigatórios
[ ] GlobalErrorHandler registrado (Angular) ou middleware de erro configurado (Node/dotnet)

SEGURANÇA
[ ] npm audit / dotnet list --vulnerable: zero HIGH/CRITICAL
[ ] Zero secrets hardcoded (grep por "password\s*=", "token\s*=" nos commits)
[ ] Dockerfile com usuário não-root e imagem com digest

RASTREABILIDADE
[ ] prd.json atualizado com status das tarefas desta fase
[ ] reflect.py executado após aprovação (gera ADR + atualiza CONTEXT.md)
```

Apresente o resultado do checklist ao Domain Expert junto com o código.

---

## §6 — PROTOCOLO DE EXCEÇÕES (EXC)

Em situações excepcionais, o Domain Expert pode autorizar uma exceção temporária a uma lei de §2.

**Formato obrigatório:**

```
EXC-NNN: [descrição da exceção]
Lei suspensa: N-XX ou M-XX
Motivo: [justificativa de negócio ou técnica]
Escopo: [arquivo(s) ou módulo(s) afetados]
Validade: [data de expiração — máximo 30 dias]
Autorizado por: [Domain Expert]
```

**Regras:**
- O agente NUNCA cria uma exceção por conta própria — apenas o Domain Expert pode.
- Toda exceção DEVE ser documentada em `docs/ai/knowledge/decisions/EXC-NNN.md`.
- Na data de expiração, o agente DEVE alertar o Domain Expert e solicitar renovação ou remoção.
- Exceções a N-03 (hardcode de secrets) são **permanentemente proibidas** — não há validade possível.

---

## §7 — ROTEAMENTO DE CONTEXTO

Ver tabela completa em `copilot-instructions.md [ROTEAMENTO DE CONTEXTO]`.

Atalhos de maior frequência:

| Necessidade | Ação |
|---|---|
| Estado do projeto | `CONTEXT.md` |
| Padrão de código | `docs/ai/skills/[stack]-clean-arch.md` |
| Qual pattern usar | `python tools/query-knowledge.py "<problema>" --collection architecture` |
| Persistir decisão | `python tools/reflect.py --prd ... --phase ...` |
