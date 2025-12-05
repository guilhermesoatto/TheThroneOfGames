using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheThroneOfGames.Infrastructure.Entities; // Corrigir namespace para Purchase

namespace TheThroneOfGames.Domain.Interfaces
{
    /// <summary>
    /// Interface para repositório de compras (Purchase).
    /// Herda do IBaseRepository e adiciona métodos específicos para compras.
    /// </summary>
    public interface IPurchaseRepository : IBaseRepository<Purchase>
    {
        /// <summary>
        /// Obtém compras por ID do usuário.
        /// </summary>
        Task<IEnumerable<Purchase>> GetByUserIdAsync(Guid userId);

        /// <summary>
        /// Obtém compras por status.
        /// </summary>
        Task<IEnumerable<Purchase>> GetByStatusAsync(string status);

        /// <summary>
        /// Obtém compras por período de datas.
        /// </summary>
        Task<IEnumerable<Purchase>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Obtém compras por ID do jogo.
        /// </summary>
        Task<IEnumerable<Purchase>> GetByGameIdAsync(Guid gameId);

        /// <summary>
        /// Obtém uma compra específica de um usuário para um jogo.
        /// </summary>
        Task<Purchase?> GetUserPurchaseAsync(Guid userId, Guid gameId);
    }
}
