# [WORKFLOW: SCALING I/O & BOTTLENECK RESOLUTION]

**Gatilho:** Este workflow DEVE ser ativado sempre que o usuário solicitar otimização de performance, relatar picos de acesso (demanda súbita), lentidão em banco de dados ou solicitar a implementação de cache/filas.

## FASE 1: DIAGNÓSTICO E ESTRATÉGIA (Não escreva código ainda)
Antes de propor qualquer alteração, analise a natureza do gargalo relatado:
1. **É um problema de Leitura (Read-Heavy)?** -> A estratégia será **Cache Distribuído (StackExchange.Redis)**.
2. **É um problema de Escrita (Write-Heavy) ou Processamento Longo?** -> A estratégia será **Desacoplamento Assíncrono via Filas (MassTransit sobre RabbitMQ, Kafka ou Azure Service Bus)**.
3. **É um problema de Abuso/Sobrecarga de Requisições?** -> A estratégia será **Rate Limiting** via `Microsoft.AspNetCore.RateLimiting` na camada WebApi/Adapter.

## FASE 2: REGRAS DE IMPLEMENTAÇÃO (Hexagonal Architecture)
Siga estritamente as regras abaixo para implementar a solução escolhida:

### Cenário A: Injetando Redis (Cache / Rate Limiting)
- **Regra de Ouro:** NUNCA modifique o projeto `/Domain`. O projeto `/Application` só pode ser alterado para **ADICIONAR** novas Interfaces (Ports) — nunca para modificar Use Cases existentes ou importar implementações concretas. O domínio não pode saber que o Redis existe.
- **Como Fazer (Cache):**
  1. Crie uma interface (Port) em `/Application/Ports/ICacheService.cs`.
  2. Implemente o Adapter em `/Infrastructure/Cache/RedisCacheAdapter.cs` usando `StackExchange.Redis`.
  3. Utilize o padrão **Decorator** via DI (ex: `Scrutor` ou registro manual) no repositório existente para interceptar a chamada, verificar o cache e, se der *miss*, ir ao banco e salvar no Redis.
  4. Registre o Decorator no `Program.cs`:
  ```csharp
  services.AddSingleton<IConnectionMultiplexer>(
      ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));
  services.AddScoped<IUserRepository, EfCoreUserRepository>();
  services.Decorate<IUserRepository, CachedUserRepository>(); // via Scrutor
  ```
- **Como Fazer (Rate Limiting):** Utilize o middleware nativo do ASP.NET Core:
  ```csharp
  builder.Services.AddRateLimiter(options =>
  {
      options.AddFixedWindowLimiter("default", config =>
      {
          config.PermitLimit = 100;
          config.Window = TimeSpan.FromMinutes(1);
          config.QueueLimit = 0;
      });
  });

  app.UseRateLimiter();
  ```

### Cenário B: Injetando Filas (Assincronismo via MassTransit)
- **Regra de Ouro:** O Controller não deve mais aguardar a resposta do banco de dados.
- **Como Fazer:**
  1. O Controller em `/WebApi` recebe o request e imediatamente despacha um Comando para o MassTransit. Retorna HTTP 202 (Accepted).
  2. Crie uma interface em `/Application/Ports/IMessagePublisher.cs`.
  3. Implemente o Adapter em `/Infrastructure/Messaging/MassTransitEventPublisher.cs`.
  4. Crie um *Consumer* em `/WebApi/Consumers/` que escuta a fila, extrai o payload e chama o Use Case correspondente em `/Application` de forma cadenciada.
  5. Configure o MassTransit no `Program.cs`:
  ```csharp
  builder.Services.AddMassTransit(x =>
  {
      x.AddConsumer<CreateOrderConsumer>();
      x.UsingRabbitMq((context, cfg) =>
      {
          cfg.Host(builder.Configuration.GetConnectionString("RabbitMQ"));
          cfg.ConfigureEndpoints(context);
      });
  });
  ```

## FASE 3: OBSERVABILIDADE E DEPLOYMENT
- **Logs e Traces:** Atualize a geração de logs (Serilog) para garantir que o `CorrelationId` da requisição HTTP original seja repassado para o Redis ou embutido no header da mensagem MassTransit. Se o consumer falhar, o log deve mostrar exatamente de qual requisição originou a falha.
- **Infraestrutura:** Atualize os arquivos de manifesto (ex: `docker-compose.yml` ou K8s Deployment) para incluir o novo container da ferramenta escolhida (Redis/RabbitMQ), garantindo que as variáveis de ambiente corretas sejam mapeadas.
- **Health Checks:** Registre health checks para as novas dependências:
  ```csharp
  builder.Services.AddHealthChecks()
      .AddRedis(builder.Configuration.GetConnectionString("Redis")!)
      .AddRabbitMQ(rabbitConnectionString: builder.Configuration.GetConnectionString("RabbitMQ")!);
  ```

## FASE 4: CHECKLIST FINAL DO AGENTE
Antes de entregar o código ao usuário, valide internamente:
- [ ] O Core Domain continua puro e sem referências a pacotes NuGet de infraestrutura?
- [ ] A injeção de dependência foi configurada corretamente para os novos Adapters no `Program.cs`?
- [ ] Os logs possuem rastreabilidade (OpenTelemetry + Serilog)?
- [ ] Os Health Checks foram registrados para as novas dependências externas?
