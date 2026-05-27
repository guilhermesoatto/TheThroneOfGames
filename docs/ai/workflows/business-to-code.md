# [WORKFLOW: BUSINESS-TO-CODE (C# / .NET 10)]

**Gatilho:** Acionado SEMPRE que o usuário fornecer um requisito em linguagem de negócio (ex: "Preciso gerenciar pontos de fidelidade", "Quero que o sistema notifique clientes inativos"). Este workflow DEVE ser concluído ANTES de qualquer scaffold de código C#.

## A FILOSOFIA
O código fala a língua do negócio. Nenhum nome de classe, método, propriedade ou evento deve ser gerado sem primeiro validar que o termo existe e é aprovado no glossário do projeto (`docs/ai/knowledge/ubiquitous-language.md`). O agente atua como tradutor entre linguagem de negócio e código — não como inventor de nomes.

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
**Namespace .NET sugerido:** `[BoundedContext].[AggregateName]`

**Value Objects identificados:**
- `[VO1]` — [descrição curta, regras de validação, tipo C# sugerido: record/readonly struct]
- `[VO2]` — [descrição curta]

**Use Cases (ações de negócio):**
- `[UseCase1UseCase]` — [ator, pré-condição, retorno: `Result<T>`]
- `[UseCase2UseCase]` — [ator, pré-condição, retorno]

**Eventos de Domínio emitidos:**
- `[AggregateName][Event]Event : DomainEvent` — [quando, quem consome via MassTransit, payload]

**Design Patterns identificados (via ChromaDB):**
- [Pattern name] — [motivo: "a regra X varia conforme Y"]

**Estrutura de projetos .NET:**
- `[AggregateName].Domain` — sem PackageReference
- `[AggregateName].Application` — ref Domain
- `[AggregateName].Infrastructure` — EF Core, MassTransit, StackExchange.Redis
- `[AggregateName].WebApi` — ASP.NET Core

**Termos validados no glossário:** ✅ / ❌ Pendente validação
```

---

## FASE 2: HUMAN-IN-THE-LOOP (Validação obrigatória)

**PARAR.** Apresente o template da Fase 1 ao Domain Expert e aguarde:

1. **Confirmação ou correção** dos nomes de Aggregate, Value Objects (record vs readonly struct) e Use Cases.
2. **Aprovação** do Bounded Context e do namespace .NET correspondente.
3. **Clarificação** de regras de negócio ambíguas (RNs não documentadas).
4. **Adição ao glossário** se algum termo novo foi proposto: pedir ao Domain Expert que atualize `docs/ai/knowledge/ubiquitous-language.md`.

> **Regra:** O agente NÃO avança para a Fase 3 sem a aprovação explícita do Domain Expert.

---

## FASE 3: GERAÇÃO DO PRD

Após validação, gere o arquivo `docs/ai/tasks/prd-[nome-do-aggregate].json` seguindo o schema definido em `dotnet/docs/ai/workflows/new-aggregate.md §FASE 1.5`.

O PRD deve incluir:
- `aggregate`: nome validado (PascalCase)
- `boundedContext`: contexto validado
- `tasks`: micro-tarefas na ordem DDD — Value Objects xUnit → Entities xUnit → Ports → Use Cases NSubstitute → Infrastructure Testcontainers
- `acceptanceCriteria`: baseado nas regras de negócio validadas

### Ordens obrigatórias de criação em .NET:
```
[1] Domain.Tests/[VO]Tests.cs — xUnit + FluentAssertions
[2] Domain/[VO].cs — record com factory método retornando Result<T>
[3] Domain.Tests/[Aggregate]Tests.cs
[4] Domain/[Aggregate].cs — com métodos de negócio
[5] Application/Ports/I[Aggregate]Repository.cs — CancellationToken em todo método
[6] Application/UseCases/[Action]UseCase.cs
[7] Application.Tests/[Action]UseCaseTests.cs — NSubstitute
[8] Infrastructure/EfCore[Aggregate]Repository.cs
[9] Integration.Tests/[Aggregate]IntegrationTests.cs — Testcontainers.NET
[10] WebApi/Controllers/[Aggregate]Controller.cs
```

---

## FASE 4: EXECUÇÃO (Delegar ao new-aggregate.md)

Com o PRD gerado e validado, inicie o workflow `dotnet/docs/ai/workflows/new-aggregate.md` na **Fase 2** (Loop de Execução).

O agente lê o `prd.json` como única fonte de verdade — não o histórico da conversa.

---

## CHECKLIST DO AGENTE (antes de codificar)

- [ ] ChromaDB consultado para o requisito
- [ ] Todos os nomes C# propostos existem no glossário ou foram aprovados pelo Domain Expert
- [ ] Nenhum sinônimo proibido usado (validar `ubiquitous-language.md`)
- [ ] Design Pattern identificado tem relevância ≥ 80% ou foi explicitamente aprovado
- [ ] `prd.json` criado com tasks em ordem DDD estrita
- [ ] Projeto Domain.csproj sem `<PackageReference>` — apenas BCL nativo
- [ ] `Directory.Build.props` com `TreatWarningsAsErrors=true`
- [ ] Regras de negócio ambíguas resolvidas antes de qualquer classe C#
