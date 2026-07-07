# [SKILL: CHROMADB SEMANTIC SEARCH — Multi-Collection RAG]

**Descrição:** Este documento define como o agente de IA deve consultar a base de conhecimento vetorial (ChromaDB) antes de tomar decisões arquiteturais, de design ou de negócio. Você DEVE executar uma consulta antes de propor padrões, criar Aggregates, ou responder sobre o domínio do projeto.

---

## COLEÇÕES DISPONÍVEIS

O ChromaDB possui 4 coleções particionadas por domínio de consulta:

| Coleção | Conteúdo | Caminho no disco |
|---|---|---|
| `architecture` | Design patterns, decisões de banco, padrões estruturais | `docs/ai/knowledge/architecture/` |
| `business-rules` | Ubiquitous language, glossário, regras de domínio | `docs/ai/knowledge/business-rules/` |
| `bdd` | Cenários Gherkin `.feature` validados pelo Domain Expert | `docs/ai/knowledge/bdd/` |
| `decision-log` | ADRs gerados por `reflect.py` após aprovação de fases | `docs/ai/knowledge/decisions/` |

---

## QUANDO CONSULTAR O CHROMADB

O agente DEVE executar uma consulta sempre que:
1. Receber um requisito em linguagem de negócio (ver `docs/ai/workflows/business-to-code.md`).
2. Precisar decidir qual Design Pattern aplicar a um problema.
3. Precisar verificar se um termo de negócio existe no glossário do projeto.
4. Precisar buscar precedentes arquiteturais ou decisões anteriores no projeto.
5. Precisar verificar se já existe cenário BDD que cobre um determinado comportamento.

---

## ROTEAMENTO AUTOMÁTICO POR INTENÇÃO

Quando você **não especificar `--collection`**, o `query-knowledge.py` detecta automaticamente a coleção correta pela intenção da query:

| Padrão na query | Coleção roteada |
|---|---|
| "qual pattern", "como implementar", "decisão estrutural", "outbox", "saga" | `architecture` |
| "como se chama", "termo", "glossário", "domínio", "o que é" | `business-rules` |
| "o que foi decidido", "histórico", "ADR", "por que escolhemos" | `decision-log` |
| "cenário", "dado que", "quando", "então", "Gherkin", "BDD", "critério de aceite" | `bdd` |

---

## PRÉ-REQUISITO: VERIFICAR AMBIENTE

**Execute antes de qualquer consulta:**
```bash
python --version   # deve ser 3.10+
pip show chromadb  # deve retornar Name: chromadb
```
Se qualquer um falhar → **NÃO tente rodar query-knowledge.py**. Use leitura direta dos arquivos `.md` em `docs/ai/knowledge/` como fallback. Retome quando `pip install -r tools/requirements.txt` for executado.

## POPULAR O BANCO (uma vez por ambiente)

Verifique se `docs/ai/knowledge/chroma/` existe. Se não existir:

```bash
pip install -r tools/requirements.txt
python tools/embed-knowledge.py                          # embeda todas as coleções
python tools/embed-knowledge.py --collection architecture  # embeda só uma coleção
```

---

## COMANDOS DE CONSULTA

### Roteamento automático (recomendado para uso cotidiano)
```bash
python tools/query-knowledge.py "qual pattern usar quando a regra de desconto muda por cliente"
python tools/query-knowledge.py "como se chama o aggregate de pedido neste projeto"
python tools/query-knowledge.py "o que foi decidido sobre autenticação no bounded context de identity"
python tools/query-knowledge.py "cenário de cancelamento de pedido"
```

### Forçar uma coleção específica
```bash
python tools/query-knowledge.py "outbox pattern para eventos de pagamento" --collection architecture
python tools/query-knowledge.py "Customer aggregate" --collection business-rules
python tools/query-knowledge.py "por que escolhemos PostgreSQL" --collection decision-log
python tools/query-knowledge.py "pontos de fidelidade expiram" --collection bdd
```

### Busca cross-coleção (quando não souber onde buscar)
```bash
python tools/query-knowledge.py "pontos de fidelidade" --all
```

### Ajustar quantidade de resultados
```bash
python tools/query-knowledge.py "saga para checkout" --n 5
python tools/query-knowledge.py "Customer" --collection business-rules --n 2
```

---

## PARÂMETROS

| Parâmetro | Descrição | Padrão |
|---|---|---|
| `"<query>"` | Query em linguagem natural ou termos técnicos | obrigatório |
| `--n <número>` | Quantidade de resultados por coleção | 3 |
| `--collection <nome>` | Forçar coleção específica (ignora roteamento automático) | auto-roteado |
| `--all` | Buscar em todas as 4 coleções e consolidar por relevância | false |

---

## COMO INTERPRETAR OS RESULTADOS

```
======================================================================
Query: "qual pattern usar quando regra de desconto muda por cliente"
Coleção: architecture
======================================================================

--- Resultado 1 (relevância: 92.3%) ---
Fonte:    docs/ai/knowledge/architecture/design-patterns.md
Seção:    1. STRATEGY
Conteúdo:
**Gatilho de negócio:** "A regra de cálculo muda conforme o tipo de cliente...
```

### Regras de interpretação
- **Relevância ≥ 80%:** Resultado altamente confiável. Use sem hesitação.
- **Relevância 60-79%:** Resultado relacionado mas não exato. Valide com o Domain Expert.
- **Relevância < 60%:** Resultado marginal. Mencione ao Domain Expert que não encontrou precedente claro.

---

## PROTOCOLO DE USO DO RESULTADO

### Para Design Patterns (`architecture`)
1. Execute a query com o problema de negócio em linguagem natural.
2. Se relevância ≥ 80% → apresente o padrão ao Domain Expert e confirme antes de gerar código.
3. Use o stub canônico do resultado como base — não reinvente.

### Para Termos de Negócio (`business-rules`)
1. Se o termo existe → use EXATAMENTE o termo descrito (evite sinônimos proibidos).
2. Se o termo NÃO existe → pause e pergunte ao Domain Expert qual o nome correto antes de nomear qualquer classe.

### Para Histórico de Decisões (`decision-log`)
1. Consulte antes de propor uma decisão arquitetural.
2. Se já existe um ADR cobrindo o mesmo problema → siga a decisão registrada ou proponha revisão explícita ao Domain Expert.
3. Nunca contradiga um ADR silenciosamente.

### Para Cenários BDD (`bdd`)
1. Consulte ao iniciar um novo Use Case para verificar cobertura existente.
2. Use os cenários existentes para orientar a implementação de testes de integração.
3. Se um comportamento não tem cenário BDD → volte ao workflow `business-to-code.md` Fase 3.1 antes de codificar.

---

## ADICIONAR NOVO CONHECIMENTO

### Patterns e regras de domínio
```bash
# Edite o arquivo correspondente
docs/ai/knowledge/architecture/design-patterns.md
docs/ai/knowledge/business-rules/ubiquitous-language.md

# Re-embeda a coleção afetada
python tools/embed-knowledge.py --collection architecture
python tools/embed-knowledge.py --collection business-rules
```

### Cenários BDD (após Fase 3.1 do business-to-code.md)
```bash
# Adicione o .feature em:
docs/ai/knowledge/bdd/[aggregate].feature

# Re-embeda
python tools/embed-knowledge.py --collection bdd
```

### ADRs (gerados automaticamente pelo Reflexion Loop)
```bash
# Nunca edite ADRs manualmente. Use o reflect.py:
python tools/reflect.py --prd docs/ai/tasks/prd-[aggregate].json --phase [fase]
# O reflect.py gera o ADR, atualiza CONTEXT.md e re-embeda decision-log automaticamente.
```

---

## REFLEXION LOOP (ChromaDB + Memória Persistente)

O `reflect.py` implementa o **Reflexion Pattern** (Shinn et al. 2023): após cada fase aprovada pelo Domain Expert, o agente gera uma reflexão verbal (ADR) e a persiste no ChromaDB.

```
[Domain Expert aprova fase]
         ↓
python tools/reflect.py --prd <prd.json> --phase <fase>
         ↓
┌─────────────────────────────────────────────────┐
│ 1. Lê prd.json → extrai tarefas concluídas      │
│ 2. Gera docs/ai/knowledge/decisions/ADR-NNNN.md │
│ 3. Atualiza docs/ai/knowledge/CONTEXT.md        │
│ 4. Embeda ADR → ChromaDB "decision-log"         │
│ 5. Re-embeda .feature se existir → "bdd"        │
└─────────────────────────────────────────────────┘
         ↓
[Próxima sessão: query-knowledge consulta ADRs automaticamente]
```

---

## MANUTENÇÃO DO BANCO

```bash
# Verificar quantos documentos estão indexados por coleção
python -c "
import chromadb
from pathlib import Path
c = chromadb.PersistentClient(str(Path('docs/ai/knowledge/chroma')))
for name in ['architecture', 'business-rules', 'bdd', 'decision-log']:
    try:
        col = c.get_collection(name)
        print(f'{name}: {col.count()} documentos')
    except Exception:
        print(f'{name}: nao encontrada')
"

# Re-indexar tudo do zero (após reestruturar diretórios)
python tools/embed-knowledge.py

# Re-indexar só uma coleção
python tools/embed-knowledge.py --collection architecture
```
