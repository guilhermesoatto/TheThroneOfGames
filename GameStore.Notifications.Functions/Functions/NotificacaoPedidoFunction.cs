using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using GameStore.Common.Events;

namespace GameStore.Notifications.Functions.Functions;

/// <summary>
/// Consome PedidoFinalizadoEvent publicado por GameStore.Vendas via RabbitMQ (fila
/// "notificacoes.pedido-finalizado", uma cópia fan-out do evento — ver
/// GameStore.Common.Messaging.RabbitMqAdapter) e dispara a notificação do usuário.
/// </summary>
public class NotificacaoPedidoFunction
{
    private readonly ILogger<NotificacaoPedidoFunction> _logger;

    public NotificacaoPedidoFunction(ILogger<NotificacaoPedidoFunction> logger)
    {
        _logger = logger;
    }

    [Function("NotificacaoPedido")]
    public void Run(
        [RabbitMQTrigger("notificacoes.pedido-finalizado", ConnectionStringSetting = "RabbitMqConnection")] string message)
    {
        var evento = JsonConvert.DeserializeObject<PedidoFinalizadoEvent>(message)
            ?? throw new InvalidOperationException("PedidoFinalizadoEvent inválido ou vazio recebido da fila.");

        _logger.LogInformation(
            "Notificando usuário {UserId} sobre pedido {PedidoId} finalizado (total: {TotalPrice}, itens: {ItemCount})",
            evento.UserId, evento.PedidoId, evento.TotalPrice, evento.ItemCount);
    }
}
