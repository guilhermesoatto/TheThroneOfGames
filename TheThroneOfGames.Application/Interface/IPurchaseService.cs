using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheThroneOfGames.Application.DTO;

namespace TheThroneOfGames.Application.Interface
{
    public interface IPurchaseService
    {
        Task PurchaseGameAsync(Guid userId, Guid gameId);
        Task<IEnumerable<PurchaseDto>> GetUserLibraryAsync(Guid userId);
    }
}
