using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheThroneOfGames.Domain.Entities;

namespace TheThroneOfGames.Application.Interface
{
    public interface IGameService : IBaseService<GameEntity>
    {
        Task BuyGame(Guid gameId, Guid userId);
        Task<List<GameEntity>> GetOwnedGames(Guid userId);
        Task<List<GameEntity>> GetAvailableGames(Guid userId);
        Task<List<GameEntity>> GetAllGames();
    }
}
