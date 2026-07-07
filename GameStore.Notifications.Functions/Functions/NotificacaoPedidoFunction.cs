using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using GameStore.Common.Events;
using GameStore.Common.Tracing;

namespace GameStore.Notifications.Functions.Functions;

/// <summary>
/// Consome PedidoFinalizadoEvent publicado por GameStore.Vendas via RabbitMQ (fila
/// "notificacoes.pedido-finalizado", uma cópia fan-out do evento — ver
/// GameStore.Common.Messaging.RabbitMqAdapter) e dispara a notificação do usuário.
/// </summary>
public class NotificacaoPedidoFunction
{
    private static readonly ActivitySource ActivitySource = new("GameStore.Notifications.Functions");

    private readonly ILogger<NotificacaoPedidoFunction> _logger;

    public NotificacaoPedidoFunction(ILogger<NotificacaoPedidoFunction> logger)
    {
        _logger = logger;
    }

    [Function("NotificacaoPedido")]
    public void Run(
        [RabbitMQTrigger("notificacoes.pedido-finalizado", ConnectionStringSetting = "RabbitMqConnection")] string message)
    {
        // Continua o trace distribuído embutido no payload pelo publisher (fase3-T08) — ver
        // GameStore.Common.Tracing.TraceContextPropagator para o motivo de não usar headers AMQP
        // (o binding [RabbitMQTrigger] do isolated worker só expõe o corpo da mensagem).
        var hasParent = TraceContextPropagator.TryExtract(message, out var parentContext);
        using var activity = ActivitySource.StartActivity(
            "NotificacaoPedido consume", ActivityKind.Consumer, hasParent ? parentContext : default);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination", "notificacoes.pedido-finalizado");

        var evento = JsonConvert.DeserializeObject<PedidoFinalizadoEvent>(message)
            ?? throw new InvalidOperationException("PedidoFinalizadoEvent inválido ou vazio recebido da fila.");

        activity?.SetTag("pedido.id", evento.PedidoId);

        _logger.LogInformation(
            "Notificando usuário {UserId} sobre pedido {PedidoId} finalizado (total: {TotalPrice}, itens: {ItemCount}, traceId: {TraceId})",
            evento.UserId, evento.PedidoId, evento.TotalPrice, evento.ItemCount, activity?.TraceId.ToString());
    }
}
