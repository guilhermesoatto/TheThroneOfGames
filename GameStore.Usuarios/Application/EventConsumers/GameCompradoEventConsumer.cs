using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using GameStore.Common.Events;
using GameStore.Usuarios.Domain.Entities;
using GameStore.Usuarios.Domain.Interfaces;

namespace GameStore.Usuarios.Application.EventConsumers
{
    /// <summary>
    /// Consumer para eventos GameCompradoEvent no contexto Usuarios.
    /// Adiciona o jogo ao Inventário do usuário (ver ItemInventario / IInventarioRepository) —
    /// fonte da verdade para "o jogador possui este jogo?", consultada pelo bounded context de
    /// Partidas (GET /api/usuario/possui-jogo/{jogoId}) antes de liberar a busca por partida.
    /// </summary>
    public class GameCompradoEventConsumer : GameStore.Common.Messaging.BaseEventConsumer<GameCompradoEvent>
    {
        // O consumer é singleton (vive pelo tempo de vida da aplicação), mas o
        // IInventarioRepository depende de um DbContext scoped — por isso resolvemos um novo
        // escopo a cada evento em vez de injetar o repositório diretamente no construtor.
        private readonly IServiceScopeFactory _scopeFactory;

        public GameCompradoEventConsumer(
            string host,
            int port,
            string username,
            string password,
            ILogger<GameCompradoEventConsumer> logger,
            IServiceScopeFactory scopeFactory)
            : base(host, port, username, password, logger, "usuarios.game-comprado")
        {
            _scopeFactory = scopeFactory;
        }

        public override async Task ProcessEventAsync(GameCompradoEvent domainEvent)
        {
            _logger.LogInformation("Processing GameCompradoEvent for user {UserId}, game {GameId}: {NomeJogo}",
                domainEvent.UserId, domainEvent.GameId, domainEvent.NomeJogo);

            using var scope = _scopeFactory.CreateScope();
            var inventarioRepository = scope.ServiceProvider.GetRequiredService<IInventarioRepository>();

            var item = new ItemInventario(domainEvent.UserId, domainEvent.GameId, domainEvent.NomeJogo);
            await inventarioRepository.AdicionarSeNaoExistirAsync(item);
        }
    }
}