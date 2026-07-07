using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        // OpenTelemetry — Distributed Tracing (fase3-T08). "GameStore.Common.Messaging" é a
        // ActivitySource usada por RabbitMqAdapter/BaseEventConsumer; sem registrá-la aqui os
        // spans criados ao continuar o trace do publisher não seriam exportados.
        var otlpEndpoint = context.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317";

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService("notifications-functions"))
            .WithTracing(t => t
                .AddSource("GameStore.Common.Messaging")
                .AddSource("GameStore.Notifications.Functions")
                .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint)));
    })
    .Build();

host.Run();
