using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TheThroneOfGames.Domain.Events;

namespace GameStore.Catalogo.Application.EventConsumers
{
    /// <summary>
    /// Consumer para eventos UsuarioAtivadoEvent no contexto Catalogo.
    /// Processa notificações quando usuários são ativados para atualizar catálogo se necessário.
    /// </summary>
    public class UsuarioAtivadoEventConsumer : GameStore.Common.Messaging.BaseEventConsumer<UsuarioAtivadoEvent>
    {
        public UsuarioAtivadoEventConsumer(
            string host,
            int port,
            string username,
            string password,
            ILogger<UsuarioAtivadoEventConsumer> logger)
            : base(host, port, username, password, logger, "catalogo.usuario-ativado")
        {
        }

        public override async Task ProcessEventAsync(UsuarioAtivadoEvent domainEvent)
        {
            _logger.LogInformation("Processing UsuarioAtivadoEvent for user {UserId}: {Nome}",
                domainEvent.UsuarioId, domainEvent.Nome);

            // Lógica específica do contexto Catálogo quando um usuário é ativado
            // Por exemplo: atualizar estatísticas, liberar conteúdo, etc.

            // No momento, apenas logamos o evento
            // Em um cenário real, poderíamos:
            // - Atualizar cache de usuários ativos
            // - Notificar outros sistemas
            // - Atualizar métricas de engajamento

            await Task.CompletedTask;
        }
    }
}