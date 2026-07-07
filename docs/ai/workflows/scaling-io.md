# [WORKFLOW: SCALING I/O & BOTTLECK RESOLUTION]

**Gatilho:** Este workflow DEVE ser ativado sempre que o usuário solicitar otimização de performance, relatar picos de acesso (demanda súbita), lentidão em banco de dados ou solicitar a implementação de cache/filas.

## FASE 1: DIAGNÓSTICO E ESTRATÉGIA (Não escreva código ainda)
Antes de propor qualquer alteração, analise a natureza do gargalo relatado:
1. **É um problema de Leitura (Read-Heavy)?** -> A estratégia será **Cache Distribuído (Redis)**.
2. **É um problema de Escrita (Write-Heavy) ou Processamento Longo?** -> A estratégia será **Desacoplamento Assíncrono via Filas (RabbitMQ, Kafka, Redis Streams)**.
3. **É um problema de Abuso/Sobrecarga de Requisições?** -> A estratégia será **Rate Limiting** na camada de Apresentação/Adapter.

## FASE 2: REGRAS DE IMPLEMENTAÇÃO (Hexagonal Architecture)
Siga estritamente as regras abaixo para implementar a solução escolhida:

### Cenário A: Injetando Redis (Cache / Rate Limiting)
- **Regra de Ouro:** NUNCA modifique a pasta `/domain`. A pasta `/application` só pode ser alterada para **ADICIONAR** novas Interfaces (Ports) — nunca para modificar Use Cases existentes ou importar implementações concretas. O domínio não pode saber que o Redis existe.
- **Como Fazer (Cache):** 1. Crie uma interface (Port) em `/application/ports/ICacheService.ts`.
  2. Implemente o Adapter em `/infrastructure/cache/RedisCacheAdapter.ts`.
  3. Utilize o padrão **Decorator** ou **Proxy** no repositório existente ou no Use Case para interceptar a chamada, verificar o cache e, se der *miss*, ir ao banco e salvar no Redis.
- **Como Fazer (Rate Limiting):** Crie um middleware na pasta `/presentation/middlewares` que utilize o Redis para controlar o limite de requisições por IP/User.

### Cenário B: Injetando Filas (Assincronismo)
- **Regra de Ouro:** O Controller não deve mais aguardar a resposta do banco de dados.
- **Como Fazer:**
  1. O Controller em `/presentation` recebe o request e imediatamente despacha um Comando/Evento para o Message Broker. Retorna HTTP 202 (Accepted).
  2. Crie uma interface em `/application/ports/IMessagePublisher.ts`.
  3. Implemente o Adapter em `/infrastructure/messaging/RabbitMQPublisher.ts` (ou tecnologia escolhida).
  4. Crie um *Worker/Consumer* em `/presentation/consumers` que escuta a fila, extrai o payload e chama o Use Case correspondente em `/application` de forma cadenciada.

## FASE 3: OBSERVABILIDADE E DEPLOYMENT
- **Logs e Traces:** Atualize a geração de logs para garantir que o `correlation_id` da requisição HTTP original seja repassado para o Redis ou embutido no header da mensagem da fila. Se o worker falhar, o log deve mostrar exatamente de qual requisição originou a falha.
- **Infraestrutura:** Atualize os arquivos de manifesto (ex: `docker-compose.yml` ou K8s Deployment) para incluir o novo container da ferramenta escolhida (Redis/RabbitMQ), garantindo que as variáveis de ambiente corretas sejam mapeadas.

## FASE 4: CHECKLIST FINAL DO AGENTE
Antes de entregar o código ao usuário, valide internamente:
- [ ] O Core Domain continua puro e sem imports de infraestrutura?
- [ ] A injeção de dependência foi configurada corretamente para os novos Adapters?
- [ ] Os logs possuem rastreabilidade (OpenTelemetry)?