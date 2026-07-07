# [WORKFLOW: BUSINESS-TO-CODE (TypeScript / Node.js)]

**Gatilho:** Acionado SEMPRE que o usuário fornecer um requisito em linguagem de negócio (ex: "Preciso gerenciar pontos de fidelidade", "Quero que o sistema notifique clientes inativos"). Este workflow DEVE ser concluído ANTES de qualquer scaffold de código.

## A FILOSOFIA
O código fala a língua do negócio. Nenhum nome de classe, método, variável ou evento deve ser gerado sem primeiro validar que o termo existe e é aprovado no glossário do projeto (`docs/ai/knowledge/ubiquitous-language.md`). O agente atua como tradutor entre linguagem de negócio e código — não como inventor de nomes.

---

## FASE 0: CONSULTA SEMÂNTICA (ChromaDB)

Antes de qualquer proposta, execute:

```bash
# 1. Buscar padrões relevantes ao problema descrito
python tools/query-knowledge.py "<requisito do usuário em linguagem natural>" --n 5

# 2. Buscar termos do glossário do projeto
python tools/query-knowledge.py "<entidades mencionadas>" --source ubiquitous-language --n 3
```

**Interpretação dos resultados:**
- Se relevância ≥ 80% em `ubiquitous-language` → use EXATAMENTE o termo encontrado.
- Se relevância ≥ 80% em `design-patterns` → proponha o padrão identificado ao Domain Expert.
- Se nenhum resultado relevante → sinalize ao Domain Expert que o termo precisa ser adicionado ao glossário.

---

## FASE 1: EXTRAÇÃO DE CONCEITOS

Com base no requisito e nos resultados do ChromaDB, extraia os conceitos de negócio:

### Template de Extração (apresentar ao Domain Expert para validação):

```markdown
## Proposta de Modelagem — [Nome do Requisito]

**Bounded Context:** [Nome do contexto — validar com ubiquitous-language.md]
**Aggregate Root:** [Nome da entidade central — validar com glossário]

**Value Objects identificados:**
- `[VO1]` — [descrição curta, regras de validação]
- `[VO2]` — [descrição curta]

**Use Cases (ações de negócio):**
- `[UseCase1]` — [ator que executa, pré-condição, resultado]
- `[UseCase2]` — [ator que executa, pré-condição, resultado]

**Eventos de Domínio emitidos:**
- `[EventName]` — [quando, quem consome, payload]

**Design Patterns identificados (via ChromaDB):**
- [Pattern name] — [motivo: "a regra X varia conforme Y"]

**Termos validados no glossário:** ✅ / ❌ Pendente validação
```

---

## FASE 2: HUMAN-IN-THE-LOOP (Validação obrigatória)

**PARAR.** Apresente o template da Fase 1 ao Domain Expert e aguarde:

1. **Confirmação ou correção** dos nomes de Aggregate, Value Objects e Use Cases.
2. **Aprovação** do Bounded Context em que a feature pertence.
3. **Clarificação** de quaisquer regras de negócio ambíguas (RNs não documentadas).
4. **Adição ao glossário** se algum termo novo foi proposto: pedir ao Domain Expert que atualize `docs/ai/knowledge/ubiquitous-language.md`.

> **Regra:** O agente NÃO avança para a Fase 3 sem a aprovação explícita do Domain Expert. Não implemente premissas de negócio.

---

## FASE 3: GERAÇÃO DO PRD

Após validação, gere o arquivo `docs/ai/tasks/prd-[nome-do-aggregate].json` seguindo o schema definido em `docs/ai/workflows/new-aggregate.md §FASE 1.5`.

O PRD deve incluir:
- `aggregate`: nome validado pelo Domain Expert
- `boundedContext`: contexto validado
- `tasks`: lista completa das micro-tarefas (Domain → Use Cases → Adapters), cada uma com `testCommand`
- `acceptanceCriteria`: preenchido com base nas regras de negócio validadas

### 3.1 — Salvar Cenários BDD (obrigatório se Gherkin for validado)

Se durante a Fase 2 o Domain Expert validou critérios de aceite em linguagem Gherkin, salve os cenários antes de iniciar o scaffold:

```bash
# Criar (ou atualizar) o arquivo de feature do Aggregate
docs/ai/knowledge/bdd/[nome-do-aggregate].feature
```

Formato obrigatório:
```gherkin
# [NomeDoAggregate] — Cenários de Aceite
# Gerado em: <data ISO 8601>
# Bounded Context: <contexto>

Feature: <nome do Aggregate em linguagem de negócio>

  Scenario: <critério de aceite 1 validado pelo Domain Expert>
    Given <pré-condição>
    When  <ação de negócio>
    Then  <resultado esperado>

  Scenario: <critério de aceite 2>
    ...
```

Após salvar, re-embeda a coleção BDD no ChromaDB:
```bash
python tools/embed-knowledge.py --collection bdd
```

> **Por quê?** Os cenários Gherkin ficam disponíveis para consulta semântica via `python tools/query-knowledge.py "<query>" --collection bdd`, permitindo que o agente verifique cobertura de cenários em sessões futuras sem precisar reler o histórico da conversa.

---

## FASE 4: EXECUÇÃO (Delegar ao new-aggregate.md)

Com o PRD gerado e validado, inicie o workflow `docs/ai/workflows/new-aggregate.md` na **Fase 2** (Loop de Execução).

O agente lê o `prd.json` como única fonte de verdade — não o histórico da conversa.

---

## CHECKLIST DO AGENTE (antes de codificar)

- [ ] ChromaDB consultado para o requisito
- [ ] Todos os nomes propostos existem no glossário ou foram aprovados pelo Domain Expert
- [ ] Nenhum sinônimo proibido usado (validar coluna "Proibido" em `ubiquitous-language.md`)
- [ ] Design Pattern identificado tem relevância ≥ 80% ou foi explicitamente aprovado
- [ ] `prd.json` criado e validado antes de qualquer linha de código TypeScript
- [ ] Regras de negócio ambíguas resolvidas com o Domain Expert
