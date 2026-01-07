using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TheThroneOfGames.Domain.Events;

namespace GameStore.Usuarios.Application.EventConsumers
{
    /// <summary>
    /// Consumer para eventos GameCompradoEvent no contexto Usuarios.
    /// Processa notificações quando jogos são comprados para atualizar biblioteca do usuário.
    /// </summary>
    public class GameCompradoEventConsumer : GameStore.Common.Messaging.BaseEventConsumer<GameCompradoEvent>
    {
        public GameCompradoEventConsumer(
            string host,
            int port,
            string username,
            string password,
            ILogger<GameCompradoEventConsumer> logger)
            : base(host, port, username, password, logger, "usuarios.game-comprado")
        {
        }

        public override async Task ProcessEventAsync(GameCompradoEvent domainEvent)
        {
            _logger.LogInformation("Processing GameCompradoEvent for user {UserId}, game {GameId}: {NomeJogo}",
                domainEvent.UserId, domainEvent.GameId, domainEvent.NomeJogo);

            // Lógica específica do contexto Usuários quando um jogo é comprado
            // Por exemplo: adicionar jogo à biblioteca do usuário, atualizar estatísticas, etc.

            // No momento, apenas logamos o evento
            // Em um cenário real, poderíamos:
            // - Adicionar jogo à biblioteca do usuário
            // - Atualizar histórico de compras
            // - Enviar notificações push/email
            // - Atualizar pontos de fidelidade

            await Task.CompletedTask;
        }
    }
}