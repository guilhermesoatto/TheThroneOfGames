# [SKILL: TYPESCRIPT CLEAN ARCHITECTURE & DDD]

**Descrição:** Este documento define as regras estritas de codificação em TypeScript para este projeto. Como um agente de IA, você DEVE aplicar essas regras ao escrever ou refatorar qualquer código dentro de um Aggregate.

## 1. MODELAGEM DE DOMÍNIO (A Regra da Pureza)
O diretório `/domain` é sagrado. Ele deve conter apenas TypeScript puro.

### Imports Permitidos em `/domain` (Whitelist Estrita)
- ✅ Outros arquivos de `/domain` (Entities, VOs, Domain Events, Domain Errors, shared types).
- ✅ Tipos nativos do TypeScript e JavaScript (`Record`, `Map`, `Set`, `Date`, `crypto.randomUUID()`).
- 🚫 **PROIBIDO:** Qualquer import de `node_modules`, incluindo "utilitários" como `uuid`, `zod`, `dayjs`, `lodash`, `class-validator`. Se um ID único for necessário, crie um Value Object `UniqueId` que receba o valor gerado pela infraestrutura via factory ou injeção no Composition Root.
- 🚫 **PROIBIDO:** Imports de módulos Node.js com I/O (`fs`, `http`, `net`, `child_process`). Módulos puros como `crypto` (apenas para `randomUUID`) são a **única exceção tolerada**.

- **Value Objects (VOs):** Devem ser estritamente IMUTÁVEIS. Use `readonly` em todas as propriedades. Validações de estado devem ocorrer no construtor do VO. Se a validação falhar, **lance um `DomainError`** — isso é correto e esperado para VOs; o chamador do VO (que vive em `application/`) usa o `Either` para capturar o erro sem `try/catch`.
- **Use Cases:** NUNCA lançam exceções como controle de fluxo. SEMPRE retornam `Either<DomainError, T>`. Ver §4 para a implementação canônica.
- **Entities & Aggregates:** Os atributos internos devem ser `private` ou `protected`. O estado só pode ser alterado através de métodos de negócio com nomes claros (ex: `user.changePassword()`, nunca `user.password = newPassword`).
- **Anemic Models:** É proibido criar entidades anêmicas (apenas getters e setters). A lógica de negócio reside na Entidade.

## 2. INJEÇÃO DE DEPENDÊNCIA E PORTAS (DIP)
- **Construtores:** Toda dependência de infraestrutura deve ser injetada via construtor no Use Case (`/application`).
- **Tipagem de Portas:** Utilize `Interfaces` explícitas para definir contratos de saída (Ports). 
  *Exemplo:* `export interface IUserRepository { save(user: User): Promise<void>; }`
- Nunca instancie uma classe de infraestrutura (ex: `new PostgresUserRepository()`) dentro de um Use Case. O contêiner de injeção de dependência (ou o arquivo de factory/composition root) deve cuidar disso.

## 3. OBSERVABILIDADE E TRATAMENTO DE ERROS (OpenTelemetry Ready)
- **Domain Errors:** Crie classes de erro customizadas estendendo `Error` para o domínio (ex: `InvalidCpfError`, `InsufficientFundsError`). Nunca lance erros genéricos (`throw new Error()`) no Core.
- **Tratamento no Controller (Adapter):** O Controller deve capturar (catch) os erros do Use Case e mapeá-los para os HTTP Status Codes corretos (ex: DomainError -> 400 Bad Request, InfraError -> 500 Internal Server Error).
- **Rastreabilidade Exigida:**
  Todo método de Adapter/Infraestrutura que realiza I/O ou publica eventos DEVE aceitar um objeto de contexto contendo `correlation_id` e `trace_id`.
  *Exemplo de log obrigatório ao estourar erro na infra:*
  `logger.error({ message: error.message, stack: error.stack, traceId: context.traceId, aggregate: 'Billing' })`

## 4. REGRAS ESTRITAS DE TYPESCRIPT
- **Proibido `any`:** O uso do tipo `any` é terminantemente proibido. Utilize `unknown` se o payload for incerto e faça Type Guarding (validação) antes de usar.
- **Null Safety:** Habilite e respeite o `strictNullChecks`. Se algo pode ser nulo, trate o cenário explicitamente.
- **Retornos de Use Case:** Prefira retornar objetos ricos ou o padrão `Either` (Left/Right) para fluxos de exceção controlada, evitando o uso excessivo de `try/catch` para regras de negócio normais.

### Implementação Canônica do Either (Obrigatória)
Utilize a seguinte implementação em TODOS os Aggregates. Não importe `fp-ts`, `purify-ts` ou crie variações. Este arquivo deve existir em `/domain/shared/either.ts` dentro de cada Aggregate:
```typescript
export type Either<L, R> =
  | { readonly _tag: 'Left'; readonly value: L }
  | { readonly _tag: 'Right'; readonly value: R };

export const left  = <L>(value: L): Either<L, never> => ({ _tag: 'Left', value });
export const right = <R>(value: R): Either<never, R> => ({ _tag: 'Right', value });

export const isLeft  = <L, R>(e: Either<L, R>): e is { _tag: 'Left'; value: L } => e._tag === 'Left';
export const isRight = <L, R>(e: Either<L, R>): e is { _tag: 'Right'; value: R } => e._tag === 'Right';
```
**Uso no Use Case:**
```typescript
async execute(input: CreateUserInput): Promise<Either<DomainError, User>> {
  const emailOrError = Email.create(input.email);
  if (isLeft(emailOrError)) return emailOrError; // propaga o erro
  // ... lógica de negócio
  return right(user);
}
```

## 5. ENFORCEMENT AUTOMATIZADO (ESLint — Domain Purity Guard)
A whitelist de imports do `/domain` (§1) DEVE ser enforced por linter, não apenas por memória do agente. Ao fazer scaffold de um novo Aggregate, o agente DEVE criar o seguinte arquivo `.eslintrc.domain.json` na raiz do Aggregate:

```jsonc
// .eslintrc.domain.json — aplicado APENAS a /src/domain/**
{
  "overrides": [
    {
      "files": ["src/domain/**/*.ts"],
      "rules": {
        "no-restricted-imports": ["error", {
          "patterns": [
            {
              "group": ["**/infrastructure/**", "**/presentation/**", "**/application/**"],
              "message": "Domain layer cannot import from outer layers (Hexagonal violation)."
            }
          ],
          "paths": [
            { "name": "uuid", "message": "Use /domain/shared/UniqueId.ts instead." },
            { "name": "zod", "message": "Validation belongs in the VO constructor, not in Zod." },
            { "name": "dayjs", "message": "Use native Date in domain." },
            { "name": "lodash", "message": "No external utilities in domain." },
            { "name": "class-validator", "message": "Validation belongs in the VO constructor." },
            { "name": "class-transformer", "message": "Transformation belongs in Adapters." }
          ]
        }],
        "no-restricted-modules": ["error", {
          "patterns": ["fs", "http", "https", "net", "child_process", "worker_threads"]
        }]
      }
    }
  ]
}
```

### Regras de Execução:
- **No Scaffold:** O agente DEVE criar este arquivo durante a Fase 1 do workflow `new-aggregate.md`.
- **No CI/CD:** O pipeline DEVE executar `eslint --config .eslintrc.domain.json` como gate obrigatório.
- **Verificação Local:** Antes de reportar sucesso, o agente deve rodar `npx eslint src/domain/ --config .eslintrc.domain.json` e garantir zero erros.
- **Benefício:** Mesmo que o agente "esqueça" a regra, o linter captura a violação mecanicamente.

## 6. BOOTSTRAP DO PROJETO (Templates Obrigatórios)
Ao criar um novo Aggregate do zero, o agente DEVE gerar os seguintes arquivos de configuração antes de qualquer código de domínio.

### `tsconfig.json`
```jsonc
{
  "compilerOptions": {
    "target": "ES2022",
    "module": "NodeNext",
    "moduleResolution": "NodeNext",
    "lib": ["ES2022"],
    "outDir": "./dist",
    "rootDir": "./src",
    "strict": true,
    "noImplicitAny": true,
    "strictNullChecks": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noImplicitReturns": true,
    "esModuleInterop": true,
    "forceConsistentCasingInFileNames": true,
    "skipLibCheck": true,
    "declaration": true,
    "declarationMap": true,
    "sourceMap": true,
    "paths": {
      "@domain/*": ["./src/domain/*"],
      "@application/*": ["./src/application/*"],
      "@infrastructure/*": ["./src/infrastructure/*"],
      "@presentation/*": ["./src/presentation/*"]
    }
  },
  "include": ["src/**/*.ts"],
  "exclude": ["node_modules", "dist", "**/*.test.ts"]
}
```

### `package.json` (scripts mínimos)
```jsonc
{
  "name": "@bounded-context/aggregate-name",
  "version": "0.1.0",
  "type": "module",
  "scripts": {
    "build": "tsc",
    "start": "node dist/main.js",
    "test": "node --test --experimental-test-coverage ./src/**/*.test.ts",
    "test:unit": "node --test ./src/domain/**/*.test.ts ./src/application/**/*.test.ts",
    "test:integration": "node --test ./src/infrastructure/**/*.test.ts ./src/presentation/**/*.test.ts",
    "lint:domain": "npx eslint src/domain/ --config .eslintrc.domain.json",
    "typecheck": "tsc --noEmit"
  },
  "engines": {
    "node": ">=20.0.0"
  }
}
```

## 7. COMPOSITION ROOT (Entry Point & DI Wiring)
O arquivo `src/main.ts` é o **Composition Root** — o único lugar onde classes concretas de infraestrutura são instanciadas e injetadas nos Use Cases. O domínio e a aplicação NUNCA sabem qual implementação concreta estão usando.

### Template obrigatório: `src/main.ts`
```typescript
// src/main.ts — Composition Root (único ponto de wiring)
import { CreateUserUseCase } from '@application/use-cases/CreateUserUseCase.js';
import { PostgresUserRepository } from '@infrastructure/repositories/PostgresUserRepository.js';
import { RedisCacheAdapter } from '@infrastructure/cache/RedisCacheAdapter.js';
import { KafkaEventPublisher } from '@infrastructure/messaging/KafkaEventPublisher.js';
import { UserController } from '@presentation/controllers/UserController.js';
import { createApp } from '@presentation/app.js';

// --- Infrastructure (concretas) ---
const userRepository = new PostgresUserRepository(process.env.DATABASE_URL!);
const cacheService = new RedisCacheAdapter(process.env.REDIS_URL!);
const eventPublisher = new KafkaEventPublisher(process.env.KAFKA_BROKERS!);

// --- Application (Use Cases recebem Ports) ---
const createUserUseCase = new CreateUserUseCase(userRepository, eventPublisher);

// --- Presentation (Controllers recebem Use Cases) ---
const userController = new UserController(createUserUseCase);

// --- HTTP Server ---
const app = createApp({ userController });
const port = Number(process.env.PORT ?? 3000);

app.listen(port, () => {
  process.stdout.write(JSON.stringify({
    timestamp: new Date().toISOString(),
    level: 'info',
    aggregate: 'User',
    message: `Server running on port ${port}`,
  }) + '\n');
});
```

### Regras do Composition Root:
- **Único local de `new`:** Apenas `src/main.ts` instancia classes concretas.
- **Proibido import circular:** O Composition Root importa de todas as camadas, mas nenhuma camada importa dele.
- **Environment Variables:** Toda configuração vem de `process.env`. Nunca hardcode.
- **Ordem de inicialização:** Infrastructure → Application → Presentation → Server.
