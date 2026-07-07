# BDD Scenarios — Gherkin Feature Files

Esta pasta contém os **cenários BDD** gerados pelo agente durante o workflow `business-to-code.md` (Fase 3.1) e indexados no ChromaDB como coleção `bdd`.

## Convenção de nomes

```
[aggregate-name-kebab-case].feature

Exemplos:
  loyalty-account.feature
  redemption-order.feature
  payment-processing.feature
```

## Estrutura de um arquivo .feature

```gherkin
Feature: [Nome do Aggregate em linguagem de negócio]
  Como [persona / ator do negócio]
  Quero [ação de negócio]
  Para [benefício / valor entregue]

  Background:
    Given [estado inicial comum a todos os cenários]

  # --- Use Case: [NomeDoUseCase] ---

  Scenario: [Caminho feliz — descrição em linguagem de negócio]
    Given [pré-condição específica]
    When  [ação que dispara o use case]
    Then  [resultado esperado observável]
    And   [evento de domínio emitido, se aplicável]

  Scenario: [Cenário de falha / invariante violado]
    Given [contexto que leva à falha]
    When  [ação tentada]
    Then  [erro de domínio esperado]
```

## Exemplo preenchido — LoyaltyAccount

```gherkin
Feature: Gerenciamento de Pontos de Fidelidade
  Como cliente com conta ativa
  Quero acumular e resgatar pontos
  Para obter benefícios exclusivos

  Background:
    Given que existe uma LoyaltyAccount com id "acc-001" no status ACTIVE
    And   o saldo atual é 1000 pontos

  # --- Use Case: CreditPoints ---

  Scenario: Crédito de pontos por compra válida
    Given que a compra tem valor R$ 150,00
    When  o use case CreditPoints é executado com earnRate 1pt/R$
    Then  o saldo deve ser 1150 pontos
    And   o evento PointsCredited deve ser emitido com payload { amount: 150, newBalance: 1150 }

  Scenario: Tentativa de crédito em conta suspensa
    Given que a LoyaltyAccount está no status SUSPENDED
    When  o use case CreditPoints é executado
    Then  deve retornar Left com DomainError "AccountSuspendedError"

  # --- Use Case: RedeemPoints ---

  Scenario: Resgate com saldo suficiente
    Given que o saldo é 1000 pontos e o mínimo de resgate é 500
    When  o use case RedeemPoints é executado com amount 500
    Then  o saldo deve ser 500 pontos
    And   o evento PointsRedeemed deve ser emitido

  Scenario: Resgate abaixo do mínimo [RN-002]
    Given que o saldo é 1000 pontos
    When  o use case RedeemPoints é executado com amount 200
    Then  deve retornar Left com DomainError "InsufficientRedemptionAmountError"
```

## Fluxo de embedding

```
business-to-code.md Fase 3.1
        ↓
Agente gera [aggregate].feature nesta pasta
        ↓
python tools/embed-knowledge.py --collection bdd
        ↓
ChromaDB coleção "bdd" atualizada

OU via Reflexion Loop (automático):
python tools/reflect.py --prd ... --phase ...
  └→ detecta .feature do aggregate e re-embeda
```

## Consultando cenários via ChromaDB

```bash
# Encontrar cenários de um aggregate
python tools/query-knowledge.py "cenários loyalty account" --collection bdd

# Encontrar critérios de aceite para um use case
python tools/query-knowledge.py "critérios aceite RedeeemPoints" --collection bdd

# Busca cross-coleção (cenário + regra de negócio)
python tools/query-knowledge.py "resgate mínimo pontos" --all
```

## Regras

- Linguagem dos cenários: **português do negócio** (termos do ubiquitous-language).
- `Given/When/Then` mapeiam diretamente para testes de domínio (sem infraestrutura).
- Cenários de domínio **não** devem mencionar HTTP, banco de dados ou frameworks.
- Cada Use Case deve ter ao menos: 1 caminho feliz + 1 invariante de domínio violado.
