using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using GameStore.Common.Events;

namespace GameStore.Notifications.Functions.Functions;

/// <summary>
/// Consome PedidoFinalizadoEvent publicado por GameStore.Vendas via RabbitMQ (fila
/// "pagamentos.pedido-finalizado", uma cópia fan-out do evento — ver
/// GameStore.Common.Messaging.RabbitMqAdapter) e processa o pagamento de forma assíncrona,
/// desacoplado da requisição HTTP original do usuário.
/// </summary>
public class ProcessarPagamentoFunction
{
    private readonly ILogger<ProcessarPagamentoFunction> _logger;

    public ProcessarPagamentoFunction(ILogger<ProcessarPagamentoFunction> logger)
    {
        _logger = logger;
    }

    [Function("ProcessarPagamento")]
    public void Run(
        [RabbitMQTrigger("pagamentos.pedido-finalizado", ConnectionStringSetting = "RabbitMqConnection")] string message)
    {
        var evento = JsonConvert.DeserializeObject<PedidoFinalizadoEvent>(message)
            ?? throw new InvalidOperationException("PedidoFinalizadoEvent inválido ou vazio recebido da fila.");

        _logger.LogInformation(
            "Processando pagamento do pedido {PedidoId} (usuário {UserId}, total: {TotalPrice})",
            evento.PedidoId, evento.UserId, evento.TotalPrice);
    }
}
