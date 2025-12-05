using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheThroneOfGames.Infrastructure.Entities; // Corrigir namespace para GameEntity

namespace TheThroneOfGames.Domain.Interfaces
{
    /// <summary>
    /// Interface para repositório de jogos (GameEntity).
    /// Herda do IBaseRepository e adiciona métodos específicos para jogos.
    /// </summary>
    public interface IGameRepository : IBaseRepository<GameEntity>
    {
        /// <summary>
        /// Obtém jogo por nome.
        /// </summary>
        Task<GameEntity?> GetByNameAsync(string name);

        /// <summary>
        /// Obtém jogos por gênero.
        /// </summary>
        Task<IEnumerable<GameEntity>> GetByGenreAsync(string genre);

        /// <summary>
        /// Obtém jogos disponíveis.
        /// </summary>
        Task<IEnumerable<GameEntity>> GetAvailableGamesAsync();

        /// <summary>
        /// Obtém jogos por faixa de preço.
        /// </summary>
        Task<IEnumerable<GameEntity>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice);

        /// <summary>
        /// Busca jogos por nome ou gênero.
        /// </summary>
        Task<IEnumerable<GameEntity>> SearchGamesAsync(string searchTerm);

        /// <summary>
        /// Verifica se jogo tem compras ativas.
        /// </summary>
        Task<bool> HasActivePurchasesAsync(Guid gameId);
    }
}
