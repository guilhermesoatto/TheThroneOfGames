using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using GameStore.Common.Events;
using GameStore.Common.Tracing;

namespace GameStore.Notifications.Functions.Functions;

/// <summary>
/// Consome PedidoFinalizadoEvent publicado por GameStore.Vendas via RabbitMQ (fila
/// "pagamentos.pedido-finalizado", uma cópia fan-out do evento — ver
/// GameStore.Common.Messaging.RabbitMqAdapter) e processa o pagamento de forma assíncrona,
/// desacoplado da requisição HTTP original do usuário.
/// </summary>
public class ProcessarPagamentoFunction
{
    private static readonly ActivitySource ActivitySource = new("GameStore.Notifications.Functions");

    private readonly ILogger<ProcessarPagamentoFunction> _logger;

    public ProcessarPagamentoFunction(ILogger<ProcessarPagamentoFunction> logger)
    {
        _logger = logger;
    }

    [Function("ProcessarPagamento")]
    public void Run(
        [RabbitMQTrigger("pagamentos.pedido-finalizado", ConnectionStringSetting = "RabbitMqConnection")] string message)
    {
        // Continua o trace distribuído embutido no payload pelo publisher (fase3-T08) — ver
        // GameStore.Common.Tracing.TraceContextPropagator para o motivo de não usar headers AMQP.
        var hasParent = TraceContextPropagator.TryExtract(message, out var parentContext);
        using var activity = ActivitySource.StartActivity(
            "ProcessarPagamento consume", ActivityKind.Consumer, hasParent ? parentContext : default);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination", "pagamentos.pedido-finalizado");

        var evento = JsonConvert.DeserializeObject<PedidoFinalizadoEvent>(message)
            ?? throw new InvalidOperationException("PedidoFinalizadoEvent inválido ou vazio recebido da fila.");

        activity?.SetTag("pedido.id", evento.PedidoId);

        _logger.LogInformation(
            "Processando pagamento do pedido {PedidoId} (usuário {UserId}, total: {TotalPrice}, traceId: {TraceId})",
            evento.PedidoId, evento.UserId, evento.TotalPrice, activity?.TraceId.ToString());
    }
}
