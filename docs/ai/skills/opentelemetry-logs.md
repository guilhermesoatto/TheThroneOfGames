# [SKILL: OPENTELEMETRY & STRUCTURED LOGGING]

**Descrição:** Este documento define o padrão obrigatório de observabilidade. Como um agente de IA, você DEVE aplicar estas regras de instrumentação em todos os Adapters (Controllers, Repositories, Message Publishers/Consumers). O Core Domain (`/Domain`) NUNCA deve conter dependências de log.

## 1. FORMATO OBRIGATÓRIO (JSON Estruturado via Serilog)
- **Regra de Ouro:** NUNCA utilize `Console.WriteLine("texto livre")` em ambientes de produção.
- Todos os logs devem ser emitidos como objetos JSON estruturados via Serilog para que agentes como Fluentbit/Promtail no Kubernetes consigam indexar facilmente.
- **Campos Base Obrigatórios:** `Timestamp`, `Level` (Information, Warning, Error), `Aggregate` (nome do domínio atual), `Message`.

### Configuração Obrigatória do Serilog no Program.cs:
```csharp
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Service", "NomeDoAggregate")
        .WriteTo.Console(new RenderedCompactJsonFormatter()));
```

### Configuração OpenTelemetry no Program.cs:
```csharp
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("NomeDoAggregate"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("NomeDoAggregate")
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());
```

### Pacotes NuGet Obrigatórios:
```xml
<ItemGroup>
  <PackageReference Include="Serilog.AspNetCore" Version="9.*" />
  <PackageReference Include="Serilog.Formatting.Compact" Version="3.*" />
  <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.*" />
  <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.*" />
  <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.*" />
  <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.*" />
</ItemGroup>
```

## 2. PROPAGAÇÃO DE CONTEXTO (Trace & Correlation)
A rastreabilidade de uma requisição que passa por múltiplos containers depende de dois IDs:
- **`TraceId`:** Identifica a jornada inteira do usuário (ex: desde o clique no Frontend). Propagado automaticamente via `Activity.Current?.TraceId` (W3C Trace Context).
- **`CorrelationId`:** Identifica a transação específica atual (ex: a requisição HTTP batendo no API Gateway).
- **Extração (Entrada):** Controllers e Consumers de fila DEVEM extrair esses IDs dos cabeçalhos HTTP (`x-trace-id`, `x-correlation-id`) ou do metadata da mensagem (ex: headers do MassTransit). Se não existirem, o Controller DEVE gerá-los via `Guid.NewGuid()`.
- **Injeção (Saída):** Qualquer chamada de I/O (HttpClient, MassTransit Publisher) DEVE repassar esses IDs para frente.

### Middleware de Correlation ID:
```csharp
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "x-correlation-id";

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }

        context.Items["CorrelationId"] = correlationId.ToString();
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId.ToString()))
        {
            await _next(context);
        }
    }
}
```

## 3. PADRÃO DE LOG DE ERROS (Tratamento de Exceções)
Quando capturar um erro na camada de Aplicação/Infraestrutura, o log DEVE conter:
1. O `TraceId` e `CorrelationId`.
2. O nome da classe/método onde falhou.
3. A `Exception.Message` e o `Exception.StackTrace`.
4. O *payload* higienizado (NUNCA logue senhas, tokens ou PII - Personally Identifiable Information).

*Exemplo de Sintaxe Esperada no .NET:*
```csharp
using System.Diagnostics;
using Serilog;

try
{
    await ProcessPaymentAsync(payload, ct);
}
catch (Exception ex)
{
    Log.Error(ex,
        "Erro em {Aggregate}.{Action} | TraceId={TraceId} | CorrelationId={CorrelationId} | Payload={@SanitizedPayload}",
        "Billing",
        "ProcessPayment",
        Activity.Current?.TraceId.ToString(),
        correlationId,
        new { payload.UserId, payload.Amount }); // NUNCA inclua tokens, senhas ou PII

    throw;
}
```

### Padrão de Log em Operações de Sucesso:
```csharp
Log.Information(
    "Operação concluída em {Aggregate}.{Action} | TraceId={TraceId} | CorrelationId={CorrelationId} | Duration={Duration}ms",
    "Billing",
    "ProcessPayment",
    Activity.Current?.TraceId.ToString(),
    correlationId,
    stopwatch.ElapsedMilliseconds);
```

## 4. REGRAS DE DOMÍNIO (Zero Log no Core)
- O projeto `Domain` NUNCA deve referenciar `Serilog`, `Microsoft.Extensions.Logging` ou `OpenTelemetry`.
- Se o Domain precisar sinalizar algo observável, deve fazê-lo via Domain Events ou retornando informação no `Result<T>`.
- Logging é responsabilidade exclusiva das camadas Application (via `ILogger<T>` injetado) e Infrastructure/WebApi.
