using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TheThroneOfGames.Domain.Events;

namespace GameStore.Usuarios.Application.EventConsumers
{
    /// <summary>
    /// Consumer para eventos PedidoFinalizadoEvent no contexto Usuarios.
    /// Processa notificações quando pedidos são finalizados para atualizar perfil do usuário.
    /// </summary>
    public class PedidoFinalizadoEventConsumer : GameStore.Common.Messaging.BaseEventConsumer<PedidoFinalizadoEvent>
    {
        public PedidoFinalizadoEventConsumer(
            string host,
            int port,
            string username,
            string password,
            ILogger<PedidoFinalizadoEventConsumer> logger)
            : base(host, port, username, password, logger, "usuarios.pedido-finalizado")
        {
        }

        public override async Task ProcessEventAsync(PedidoFinalizadoEvent domainEvent)
        {
            _logger.LogInformation("Processing PedidoFinalizadoEvent for order {PedidoId}, user {UserId}, total {TotalPrice}",
                domainEvent.PedidoId, domainEvent.UserId, domainEvent.TotalPrice);

            // Lógica específica do contexto Usuários quando um pedido é finalizado
            // Por exemplo: atualizar estatísticas do usuário, histórico de compras, etc.

            // No momento, apenas logamos o evento
            // Em um cenário real, poderíamos:
            // - Atualizar estatísticas de compras do usuário
            // - Adicionar pontos de fidelidade
            // - Atualizar nível/status do usuário
            // - Enviar confirmação de compra

            await Task.CompletedTask;
        }
    }
}