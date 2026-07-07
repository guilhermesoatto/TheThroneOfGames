# [ARCHITECTURE BLUEPRINT: DDD + HEXAGONAL + K8S]

Este documento define a topologia arquitetural do sistema. Como um Agente de IA, você DEVE consultar este mapa antes de propor qualquer alteração de design, refatoração estrutural ou criação de novos domínios.

---

## §0 — CONCEITOS DDD (Full Quanta — Auto-Suficientes)

> Cada conceito abaixo é uma **unidade completa de conhecimento**: toda regra, invariante e restrição está aqui. O agente NÃO deve inferir comportamento além do que está explicitamente escrito.

### Bounded Context
- Uma fronteira semântica dentro da qual a Ubiquitous Language é consistente e não-ambígua.
- O mesmo termo pode ter significados diferentes em Bounded Contexts distintos — isso é correto e esperado.
- **Mapeamento nesta arquitetura:** 1 Bounded Context = 1 Namespace no Kubernetes.
- Nunca crie código que referencie conceitos de outro Bounded Context diretamente — use Domain Events publicados via message broker (Redis Pub/Sub, Kafka, RabbitMQ) para cruzar fronteiras.

### Aggregate (Raiz de Agregação)
- Um cluster de objetos de domínio (Entities + Value Objects) tratados como **uma única unidade para mudanças de dados**.
- Possui exatamente **um Aggregate Root** (Entity) — único ponto de entrada para toda operação externa.
- **Boundary de consistência:** todas as invariantes do Aggregate DEVEM ser satisfeitas em qualquer commit transacional.
- **Boundary de transação:** exatamente **1 Aggregate por transação de banco de dados**. Nunca modifique dois Aggregates na mesma transação.
- **Referência por identidade:** Aggregates externos são referenciados APENAS pelo seu ID (string/UUID). NUNCA por referência de objeto.
- **Sizing:** comece com uma Entity. Adicione ao Aggregate apenas o que DEVE mudar atomicamente junto.
- **Mapeamento nesta arquitetura:** 1 Aggregate = 1 container Docker = 1 Pod no K8s = 1 schema privado de banco.

### Entity
- Objeto com **identidade única** que persiste através de mudanças de estado.
- Duas Entities com os mesmos atributos mas IDs diferentes são objetos distintos.
- A identidade é estabelecida na criação e nunca muda.
- Contém comportamento de negócio (métodos que alteram estado e emitem eventos).

### Value Object
- Definido **exclusivamente pelos seus atributos** — não tem identidade própria.
- Dois Value Objects com os mesmos atributos são intercambiáveis.
- **Imutável** — nunca modifique um VO; crie um novo com o valor atualizado.
- **Validação (Node.js/TypeScript):** ocorre no construtor; falha lança `DomainError`. O chamador (Use Case) captura via `Either<DomainError, T>` — nunca use `try/catch` no Use Case.
- **Validação (Angular):** retorna `Result<T, E>` do factory method — nunca lança exceção diretamente.
- Exemplos: `Email`, `PointsAmount`, `Money`, `CPF`, `DateRange`.

### Domain Event
- Um **fato sobre algo que aconteceu no domínio** e que os domain experts se importam.
- **Nomenclatura obrigatória (TypeScript/Node.js):** passado composto, SEM sufixo `Event` → `PointsCredited`, `OrderPlaced`, `PaymentFailed`. O arquivo usa o sufixo `.event.ts` → `OrderPaid.event.ts`.
- **Nomenclatura obrigatória (.NET):** passado composto COM sufixo `Event` → `PointsCreditedEvent`, `OrderPlacedEvent`. Convenção diverge intencionalmente do TypeScript para respeitar o idioma C#.
- **Emitido pelo Aggregate Root**, nunca por Entities internas ou Value Objects.
- **Imutável** — representa um registro histórico que não pode ser alterado.
- **Auto-suficiente** — contém dados suficientes para ser interpretado sem re-consultar o banco.
- **Parte da Ubiquitous Language** — todo domain expert deve reconhecer o nome do evento.
- Contrato obrigatório de campos: ver §2 abaixo.

### Use Case (Application Service)
- Orquestra o domínio para realizar **uma única intenção de negócio**.
- Recebe um Command (input validado), executa no Aggregate, retorna `Either<DomainError, T>`.
- **Nunca contém regras de negócio** — regras vivem no Domain.
- **Nunca acessa banco diretamente** — usa Ports (interfaces) injetadas.
- **Nunca lança exceções** como controle de fluxo de negócio.

### Port (Interface de Aplicação)
- Define **o que o Use Case precisa** do mundo externo (sem saber como é implementado).
- Mora em `application/ports/` (TypeScript/Node.js) ou `Application/Ports/` (.NET).
- Exemplos: `IUserRepository`, `ICacheService`, `IMessagePublisher`.

### Adapter
- **Implementação concreta** de um Port.
- Mora em `infrastructure/` — nunca em `domain/` ou `application/`.
- Traduz entre o modelo de domínio e a tecnologia externa (EF Core, Redis, Kafka).

---

## 1. TOPOLOGIA FÍSICA E DEPLOYMENT (Kubernetes & Docker)
- **A Unidade de Deploy:** A unidade fundamental de deploy é o **Aggregate** (Raiz de Agregação no DDD). Cada Aggregate vive dentro do seu próprio container Docker.
- **Namespaces:** Os Bounded Contexts (Contextos Delimitados) são mapeados 1:1 para Namespaces no Kubernetes. 
- **Pods:** Cada Pod no K8s representa uma instância de um Aggregate.
- **Regra de Isolamento:** Um Aggregate NUNCA compartilha banco de dados com outro. A persistência é privada por container.

## 2. MACRO-ARQUITETURA: EVENT-DRIVEN (EDA)
- **Intra-Domínio (Dentro do Aggregate):** Operações síncronas em memória. Fortemente tipadas.
- **Inter-Domínio (Entre Aggregates/Namespaces):** Comunicação ESTritamente ASSÍNCRONA. 
- **Eventos de Domínio:** Quando uma regra de negócio altera o estado de um Aggregate, ele emite um `DomainEvent`.
- **Message Brokers:** A publicação e assinatura de eventos deve ser feita via infraestrutura de mensageria (ex: Redis Pub/Sub, Kafka, RabbitMQ). 
- **Proibição:** É proibido criar código onde o *Controller* de um Aggregate faz uma chamada HTTP direta para o *Controller* de outro Aggregate para processar regras de negócio.

### Contrato Obrigatório de Domain Events
Todo `DomainEvent` emitido DEVE implementar a seguinte interface base. Eventos sem esses campos serão rejeitados pelo Message Broker:
```typescript
export interface IDomainEvent {
  readonly eventId: string;          // UUID v4 gerado no momento da emissão
  readonly eventType: string;        // Nome canônico (ex: 'OrderPaid')
  readonly eventVersion: number;     // Versionamento do schema (1, 2, 3...)
  readonly aggregateId: string;      // ID do Aggregate que emitiu
  readonly aggregateName: string;    // Nome do Aggregate (ex: 'Order')
  readonly occurredAt: Date;         // Timestamp ISO 8601
  readonly correlationId: string;    // Propagado do request HTTP original
  readonly payload: Readonly<Record<string, unknown>>; // Dados do evento (imutável)
}
```
**Regras de Versionamento de Eventos:**
- Ao adicionar campos opcionais ao payload: manter a mesma `eventVersion`.
- Ao remover campos ou alterar tipos: incrementar `eventVersion` (ex: `OrderPaidV1` → `OrderPaidV2`).
- Consumers devem ser backward-compatible: aceitar versões anteriores graciosamente.
- Nunca alterar o schema de um evento já publicado em produção — crie uma nova versão.
- **Para estratégias detalhadas de evolução de schema (Payload Expansion, Dual-Write, Parallel Version), consulte `docs/ai/skills/event-versioning.md`.**

## 3. MICRO-ARQUITETURA: HEXAGONAL (Ports and Adapters)
Todo código dentro de um Aggregate DEVE seguir a Arquitetura Hexagonal. O fluxo de dependência aponta sempre para o centro (Core Domain).

### Estrutura de Diretórios Padrão por Aggregate:
```text
/src
  /domain        # CORE: Entidades, Value Objects, Domain Events. ZERO dependência externa.
  /application   # USE CASES: Orquestra o domínio. Define as Portas (Interfaces).
  /infrastructure # ADAPTERS: Implementações técnicas (DB, Redis, Mensageria, APIs externas).
  /presentation  # ADAPTERS DE ENTRADA: Controllers REST, GraphQL, Listeners de Eventos.
```

### Convenção de Nomenclatura de Arquivos
Todos os Aggregates DEVEM seguir esta convenção sem exceções:
- **Domínio (Entities/VOs):** PascalCase → `User.ts`, `Email.ts`, `OrderStatus.ts`
- **Domain Events:** PascalCase + sufixo `.event.ts` → `OrderPaid.event.ts`, `UserCreated.event.ts`
- **Domain Errors:** PascalCase + sufixo `.error.ts` → `InvalidEmailError.ts`, `InsufficientFundsError.ts`
- **Use Cases:** PascalCase + sufixo `UseCase` → `CreateUserUseCase.ts`, `PayOrderUseCase.ts`
- **Ports (Interfaces):** Prefixo `I` + PascalCase → `IUserRepository.ts`, `ICacheService.ts`, `IMessagePublisher.ts`
- **Adapters:** PascalCase + tecnologia → `PostgresUserRepository.ts`, `RedisCacheAdapter.ts`, `KafkaEventPublisher.ts`
- **Controllers:** PascalCase → `UserController.ts`, `OrderController.ts`
- **Testes:** Mesmo nome + `.test.ts` → `Email.test.ts`, `CreateUserUseCase.test.ts`
- **Shared/Cross-Cutting:** Em `/domain/shared/` → `either.ts`, `domain-event.base.ts`
