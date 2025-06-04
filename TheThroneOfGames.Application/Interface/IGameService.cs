using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheThroneOfGames.Application.DTO;

namespace TheThroneOfGames.Application.Interface
{
    public interface IGameService
    {
        Task AddGameAsync(string name, string genre, decimal price);
        Task<IEnumerable<GameDto>> GetGamesAsync(string? name, string? genre, int page, int pageSize);
        Task<GameDto> GetGameDetailsAsync(Guid gameId);
    }
}
