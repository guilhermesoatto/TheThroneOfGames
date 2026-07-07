# [WORKFLOW: NEW AGGREGATE (RALPH AUTONOMY LOOP)]

**Gatilho:** Acionado sempre que for necessário criar um novo domínio, microserviço, ou feature complexa.

## A FILOSOFIA "RALPH" (Anti-Context Rot)
Como um Agente de IA, você NUNCA deve tentar codificar todo o Domínio, Use Cases e Adapters de uma só vez. Isso causa degradação de contexto. A execução deve ser estritamente iterativa. Trate cada passo como um loop isolado, baseando sua "memória" em arquivos no disco e não no histórico desta conversa.

## FASE 1: GERAÇÃO DO ESTADO (O PRD)
Antes de escrever qualquer linha de código TypeScript:
1. Crie um arquivo de estado em `docs/ai/tasks/prd-[nome-do-dominio].json`.
2. Quebre o requisito solicitado pelo Arquiteto em micro-tarefas (User Stories). Cada tarefa DEVE ser pequena o suficiente para ser concluída em apenas um loop de contexto.
3. A ordem de execução estrita do DDD (De dentro para fora) deve ser mapeada no JSON:
   - [1] Criar Value Objects + Testes Nativos.
   - [2] Criar Entidades e Domain Events + Testes Nativos.
   - [3] Criar Interfaces de Aplicação (Ports).
   - [4] Criar Use Cases + Testes com Mocks Nativos.
   - [5] Criar Adapters (Infra/Presentation) + Testes de Integração.

## FASE 1.5: SCHEMA DO ARQUIVO DE ESTADO (`prd.json`)
O arquivo `docs/ai/tasks/prd-[nome-do-dominio].json` é o **cérebro persistente** do agente entre sessões. O agente DEVE lê-lo no início de cada sessão e atualizá-lo ao final de cada micro-tarefa.

### Schema Obrigatório:
```jsonc
{
  "$schema": "prd-v1",
  "aggregate": "Order",
  "boundedContext": "sales",
  "createdAt": "2026-03-14T10:00:00Z",
  "currentPhase": "2-use-cases",
  "iteration": 2,
  "maxIterations": 3,
  "tasks": [
    {
      "id": "1",
      "phase": "1-domain",
      "description": "Criar Value Object Email com validação regex",
      "status": "completed",
      "passes": true,
      "completedAt": "2026-03-14T10:05:00Z",
      "testCommand": "node --test src/domain/__tests__/Email.test.ts",
      "artifacts": ["src/domain/Email.ts", "src/domain/__tests__/Email.test.ts"]
    },
    {
      "id": "2",
      "phase": "2-use-cases",
      "description": "Criar CreateOrderUseCase com mock de IOrderRepository",
      "status": "in-progress",
      "passes": false,
      "lastError": "mockSave.mock.calls.length expected 1, got 0",
      "iteration": 2,
      "testCommand": "node --test src/application/__tests__/CreateOrderUseCase.test.ts",
      "artifacts": ["src/application/use-cases/CreateOrderUseCase.ts"],
      // Campos preenchidos somente em caso de rejeição pelo Domain Expert (Fase 3.5):
      "rejectedAt": null,       // ISO 8601 — preenchido ao rejeitar
      "rejectionReason": null   // resumo do motivo (máx 2 frases)
    }
  ],
  "acceptanceCriteria": {
    "domain": {
      "testsPass": true,
      "zeroExternalImports": true,
      "eslintDomainClean": true,
      "coveragePercent": 100
    },
    "application": {
      "testsPass": false,
      "portsDefinedForAllIO": true,
      "eitherPatternUsed": true
    },
    "infrastructure": {
      "testsPass": null,
      "correlationIdPropagated": null,
      "preparedStatementsOnly": null
    },
    "deployment": {
      "dockerfileExists": null,
      "k8sManifestsExist": null,
      "networkPolicyExists": null,
      "externalSecretsConfigured": null,
      "serviceAccountDedicated": null
    }
  }
}
```

### Regras de Checkpoint Recovery:
- **Início de Sessão:** O agente DEVE ler o `prd.json` ANTES de qualquer ação. Se o arquivo existir, retomar da tarefa com `status: "in-progress"` ou `"failed"`.
- **Fim de Tarefa:** Atualizar imediatamente o `prd.json` com o resultado (`passes`, `completedAt`, `lastError`).
- **Human-in-the-Loop:** Ao completar todas as tarefas de uma fase, pausar e apresentar resumo ao Domain Expert.

### Stop & Rollback Conditions:
- **Stop:** Se `iteration >= maxIterations` (3 tentativas) na mesma tarefa, PARAR e reportar ao Domain Expert com diagnóstico completo (`lastError` + artifacts afetados).
- **Rollback:** Se o Domain Expert solicitar, o agente pode reverter uma tarefa para `status: "pending"` e `passes: false`, permitindo re-execução.
- **Abort:** Se a modelagem parecer inadequada (Aggregate muito grande, responsabilidades cruzadas), o agente sugere split antes de continuar.
- **Nunca pular fase:** É PROIBIDO iniciar uma tarefa da fase `2-use-cases` se houver tarefas da fase `1-domain` com `passes: false`.

## FASE 2: O LOOP DE EXECUÇÃO (Stateless Execution)
Para cada tarefa com o status `passes: false` no arquivo `prd.json`, siga este fluxo:
1. **Foco Estrito:** Leia as regras em `typescript-clean-arch.md`. Implemente APENAS o código exigido na tarefa atual. Ignore o resto do escopo.
2. **Qualidade Embutida:** Escreva o teste unitário correspondente à tarefa atual (usando `node:test`).
3. **O Portão (Feedback Loop):** Execute internamente a validação de tipos do TypeScript e o teste nativo criado.
4. **Auto-Correção:** Se quebrar, corrija o código imediatamente. É PROIBIDO avançar para a próxima tarefa se o estado atual do código não compilar ou o teste falhar.
5. **Avanço:** Se passar, altere a tarefa para `passes: true` no `prd.json`, documente rapidamente o que foi feito no arquivo `docs/ai/tasks/progress.txt` (a sua memória persistente) e prepare-se para o próximo loop.

## FASE 3: HUMAN-IN-THE-LOOP
Ao finalizar uma micro-tarefa com sucesso (CI verde local), pause a geração. Apresente um resumo sucinto do que foi feito e os resultados dos testes. Aguarde o comando "prosseguir" do usuário para carregar o `prd.json` e iniciar o próximo loop.

## FASE 3.1: REFLEXION LOOP (Após aprovação de cada fase)

Assim que o Domain Expert aprovar todas as tarefas de uma fase, execute **obrigatoriamente** o Reflexion Loop antes de iniciar a próxima fase:

```bash
# Substitua [caminho-do-prd] e [fase-aprovada] pelos valores reais
python tools/reflect.py --prd docs/ai/tasks/prd-[nome-do-dominio].json --phase [fase-aprovada]
```

**Fases válidas:** `1-domain` | `2-use-cases` | `3-infrastructure` | `4-presentation`

**O que o Reflexion Loop faz automaticamente:**
1. Lê o `prd.json` e extrai as tarefas completadas na fase aprovada
2. Gera um ADR (`docs/ai/knowledge/decisions/ADR-NNNN.md`) documentando as decisões tomadas
3. Atualiza `docs/ai/knowledge/CONTEXT.md` com o estado atual do Aggregate
4. Embeda o ADR na coleção `decision-log` do ChromaDB
5. Re-embeda cenários BDD se houver `.feature` files em `docs/ai/knowledge/bdd/`

> **Por que isso é obrigatório?** O Reflexion Pattern (Shinn et al. 2023) mostra que agentes que geram reflexão verbal após cada ciclo tomam decisões melhores nos ciclos subsequentes. O ADR gerado será consultado automaticamente pelo `query-knowledge.py` nas próximas sessões, evitando que o agente repita decisões já tomadas ou rejeitadas.

## FASE 3.5: ROLLBACK E REJEIÇÃO (Domain Expert rejeita o output)

### Quando esta fase é ativada
O Domain Expert sinaliza rejeição com qualquer variante de: "não está certo", "refazer", "rejeito", "volta para X", "recomeça a tarefa Y".

### Procedimento obrigatório de rollback (5 passos)

**Passo 1 — Identificar escopo da rejeição**
Confirmar com o Domain Expert: a rejeição é sobre (a) uma tarefa específica, (b) todas as tarefas de uma fase, ou (c) a modelagem do Aggregate inteiro.

**Passo 2 — Atualizar o `prd.json`**
Para cada tarefa rejeitada, aplicar as seguintes mutações de campos:
```jsonc
{
  "status": "rejected",           // era: "completed" ou "in-progress"
  "passes": false,                // forçar para false
  "iteration": 0,                 // resetar contador de tentativas
  "rejectedAt": "<ISO 8601>",     // timestamp da rejeição
  "rejectionReason": "<motivo>"   // resumo em até 2 frases do que o Domain Expert apontou
}
```

**Passo 3 — Propagar bloqueio para fases dependentes**
Toda tarefa de fase `N+1` que depende de uma tarefa rejeitada da fase `N` deve ser atualizada para:
```jsonc
{
  "status": "blocked",
  "passes": null
}
```
Isso garante que o agente não avance sobre um alicerce rejeitado.

**Passo 4 — Arquivar artefatos rejeitados (não deletar)**
Renomear os arquivos afetados adicionando o sufixo `.rejected` antes da extensão:
```
src/domain/Email.ts → src/domain/Email.rejected.ts
```
Isso preserva o histórico sem poluir o compilador. O `.gitignore` NÃO deve incluir `*.rejected.ts` — o Domain Expert pode querer reverter.

**Passo 5 — Re-entrar no loop (Fase 2)**
Antes de re-executar, o agente DEVE:
- [ ] Reler a regra de negócio rejeitada conforme o `ubiquitous-language.md`
- [ ] Consultar `docs/ai/knowledge/design-patterns.md` por um pattern mais adequado
- [ ] Confirmar com o Domain Expert o critério de aceite da nova tentativa
- [ ] Registrar o `rejectionReason` como contexto explícito no início da nova iteração

### Limite de rejeições por tarefa
Se uma mesma tarefa acumular **2 rejeições** (campo `rejectedAt` preenchido duas vezes), o agente DEVE parar e apresentar ao Domain Expert:
1. As duas versões rejeitadas e seus `rejectionReason`
2. Uma proposta de **split do Aggregate** — a tarefa pode indicar responsabilidade excessiva
3. Perguntar: "Devemos dividir este Aggregate ou revisar o Ubiquitous Language antes de continuar?"

**Nunca tentar uma 3ª vez na mesma tarefa sem validação explícita do Domain Expert.**
