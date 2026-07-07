# [SKILL: NODE.JS NATIVE TESTING & ASSERTIONS]

**Descrição:** Este documento define a sintaxe e as ferramentas estritas para a criação de testes automatizados. A regra de ouro é: ZERO dependências externas para testes. Como um agente de IA, você DEVE usar exclusivamente os módulos nativos do Node.js (Node 20+).

## ESCOPO DE APLICAÇÃO
- **Este skill cobre:** testes de domínio (Value Objects, Entities, Use Cases) e testes de infra em projetos TypeScript/Node.js puro.
- **Não se aplica a:** projetos Angular (use `angular-testing.md`) ou .NET (use `xunit-native-testing.md`).
- **Exceção importante:** Value Objects que validam no **construtor** DEVEM usar `assert.throws()`. Use Cases que retornam `Either<L,R>` usam `assert.deepEqual()` no resultado — NÃO `assert.throws()`. Esta distinção é intencional (ver `typescript-clean-arch.md §4`).

## 1. IMPORTS OBRIGATÓRIOS E ASSERÇÕES
- **Test Runner:** Utilize estritamente `import { describe, it, mock } from 'node:test';`. Nunca importe de bibliotecas externas.
- **Asserções:** Utilize SEMPRE o modo estrito do assert nativo: `import assert from 'node:assert/strict';`.
- **Proibições:** Não use `expect()`, `toBe()`, ou sintaxes do ecossistema Jest/Chai.

## 2. SINTAXE DE ASSERÇÃO (node:assert/strict)
O agente deve mapear mentalmente a intenção do teste para a sintaxe nativa:
- **Igualdade de Valor:** `assert.equal(actual, expected)` (para primitivos).
- **Igualdade Profunda:** `assert.deepEqual(actual, expected)` (para objetos, DTOs e Arrays).
- **Verificação de Exceções (Domain Errors):**
  Para garantir que um Value Object ou Entity lance um erro de domínio:
  ```typescript
  assert.throws(
    () => new Email('invalid-email'),
    { name: 'InvalidEmailError', message: 'The provided email is invalid' }
  );