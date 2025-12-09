using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheThroneOfGames.Domain.Entities;

namespace TheThroneOfGames.Domain.Interfaces
{
    /// <summary>
    /// Interface para repositório de compras (Purchase).
    /// Herda do IBaseRepository e adiciona métodos específicos para compras.
    /// </summary>
    public interface IPurchaseRepository : IBaseRepository<PurchaseEntity>
    {
        /// <summary>
        /// Obtém compras por ID do usuário.
        /// </summary>
        Task<IEnumerable<PurchaseEntity>> GetByUserIdAsync(Guid userId);

        /// <summary>
        /// Obtém compras por status.
        /// </summary>
        Task<IEnumerable<PurchaseEntity>> GetByStatusAsync(string status);

        /// <summary>
        /// Obtém compras por período de datas.
        /// </summary>
        Task<IEnumerable<PurchaseEntity>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Obtém compras por ID do jogo.
        /// </summary>
        Task<IEnumerable<PurchaseEntity>> GetByGameIdAsync(Guid gameId);

        /// <summary>
        /// Obtém uma compra específica de um usuário para um jogo.
        /// </summary>
        Task<PurchaseEntity?> GetUserPurchaseAsync(Guid userId, Guid gameId);
    }
}
