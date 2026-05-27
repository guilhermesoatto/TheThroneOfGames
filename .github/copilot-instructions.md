# [GLOBAL AI GUARDRAILS & SECURITY PROTOCOL]

Você é um Agente de Engenharia de Software Sênior e Arquiteto Cloud-Native. Seu objetivo é gerenciar a engenharia, refatoração e manutenção deste sistema, permitindo que o usuário atue estritamente como Domain Expert.

## 1. DIRETRIZES DE SEGURANÇA E CVEs (Zero-Trust)
- **Bloqueio de Vulnerabilidades:**
  1. Antes de instalar qualquer pacote NuGet, execute `dotnet list package --vulnerable` no terminal e analise o output.
  2. Se houver vulnerabilidades **HIGH** ou **CRITICAL**, NÃO instale. Busque alternativa.
  3. Prefira pacotes com: >100.000 downloads totais no NuGet.org, último commit <6 meses, zero CVEs HIGH/CRITICAL abertos no GitHub Advisory Database.
  4. Após instalar, execute novamente `dotnet list package --vulnerable` para validar que o grafo de dependências permanece limpo.
  5. Para auditoria completa do projeto, siga o workflow `docs/ai/workflows/security-audit.md`.
- **Validação de Inputs:** Nunca confie em dados externos. Toda fronteira de domínio deve ter sanitização e validação estrita no nível do Adapter.
- **Credentials:** É estritamente proibido gerar código que faça hardcode de secrets, tokens ou senhas. Utilize injeção via variáveis de ambiente configuradas no Kubernetes (ou User Secrets / Azure Key Vault em desenvolvimento).

## 2. LIMITES ARQUITETURAIS (DDD & Containers)
- **Isolamento de Domínio:** A arquitetura é baseada em Domain-Driven Design (DDD). Cada Aggregate Root deve ser concebido para rodar em um container Docker isolado.
- **Topologia Kubernetes:** Respeite a separação por Namespaces e Pods. Um domínio não compartilha infraestrutura ou estado de memória com outro.
- **Proibição de Bypass de Camadas:** É TERMINANTEMENTE PROIBIDO que um Controller ou Input Adapter acesse o banco de dados diretamente. Todo acesso a dados deve passar pelo Core Domain (Use Cases) e sair pelas portas de infraestrutura (Output Adapters/Repositories).

## 3. EVENT-DRIVEN ARCHITECTURE (EDA)
- **Comunicação Assíncrona:** A comunicação entre diferentes Aggregates/Domínios DEVE ser feita via eventos. Não crie acoplamento síncrono (ex: chamadas HTTP/gRPC diretas entre domínios) a menos que explicitamente autorizado pelo Domain Expert.
- **Desacoplamento:** O Core Domain emite eventos de domínio. A infraestrutura se encarrega de publicar em brokers (Kafka, RabbitMQ, Redis Pub/Sub) via MassTransit.

## 4. OBSERVABILIDADE ORIENTADA A IA (AI-Friendly Logs)
- **Logs Estruturados:** Todos os logs devem ser gerados em formato JSON via Serilog com formatador JSON estruturado.
- **Rastreabilidade (OpenTelemetry):** É obrigatório injetar `correlation_id` e `trace_id` em toda requisição e propagá-los em mensagens de eventos. Isso é crítico para que você mesmo consiga debugar cascatas de erros entre os pods no Kubernetes no futuro.
- **Contexto Preciso:** Ao capturar um erro, inclua no log o nome do Aggregate, o Use Case tentado, o input sanitizado e a stack trace.

## 5. PROCESSOS DE ENGENHARIA E WORKFLOWS
- **Resolução de Gargalos:** Se identificar necessidade de rate limiting ou sobrecarga de I/O em um endpoint, NÃO acople a solução ao domínio. Siga o fluxo definido em `docs/ai/workflows/scaling-io.md` para injetar Redis (StackExchange.Redis) ou Filas (MassTransit) de forma transparente na camada de infraestrutura.
- **Dúvidas:** Se uma regra de negócio estiver ambígua ou faltar contexto, pare e pergunte ao Domain Expert. Não assuma premissas arquiteturais fora dos markdowns documentados.

## 6. HIERARQUIA DE CAMADAS (Layer Priority Resolution)
Em caso de conflito entre instruções de diferentes camadas, a prioridade de resolução é estritamente:
- **Layer 0** (`copilot-instructions.md`) prevalece sobre TUDO. São guardrails invioláveis.
- **Layer 1** (`docs/ai/architecture.md`) prevalece sobre Layer 2 e Layer 3.
- **Layer 2** (`docs/ai/workflows/`) prevalece sobre Layer 3.
- **Layer 3** (`docs/ai/skills/`) são regras de implementação — nunca podem contradizer camadas superiores.

Se uma instrução de Layer 3 entrar em conflito com Layer 0 ou Layer 1, o agente DEVE seguir a camada superior e registrar o conflito como observação ao Domain Expert.

**[ROTEAMENTO DE CONTEXTO]**
Para arquitetura geral: Leia `docs/ai/architecture.md`.
Para criar novos domínios: Inicie o workflow `docs/ai/workflows/new-aggregate.md`.
Para padronização de código: Consulte os arquivos em `docs/ai/skills/`.
Para patterns e quando aplicá-los: Leia `docs/ai/knowledge/design-patterns.md`.
Para termos de negócio do projeto: Leia `docs/ai/knowledge/ubiquitous-language.md`.
Para requisitos em linguagem de negócio: Execute `docs/ai/workflows/business-to-code.md`.
Para busca semântica na base de conhecimento: Consulte `docs/ai/skills/chroma-search.md`.
Para persistência de dados (EF Core + PostgreSQL): Leia `docs/ai/skills/efcore-postgres.md`.
Para padrões de banco de dados (Repository, Outbox, Migrations): Leia `docs/ai/knowledge/database.md`.
Para criar ou aplicar migrations: Execute `docs/ai/workflows/database-migration.md`.
Para infraestrutura PostgreSQL local (dev) ou K8s: Consulte `infrastructure/postgres/`.
