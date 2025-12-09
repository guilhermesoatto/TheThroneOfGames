using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheThroneOfGames.Application.DTO;
using TheThroneOfGames.Application.Interface;

namespace TheThroneOfGames.Application
{
    public class PurchaseService : IPurchaseService
    {
        public Task<IEnumerable<PurchaseDto>> GetUserLibraryAsync(Guid userId)
        {
            throw new NotImplementedException();
        }

        public Task PurchaseGameAsync(Guid userId, Guid gameId)
        {
            throw new NotImplementedException();
        }
    }
}
