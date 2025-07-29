using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheThroneOfGames.Application.Interface
{
    public interface IGameService
    {
        public Task PreRegisterGameAsync(string gameName, string gameDescription, string gameGenre);
        public Task ActivateGameAsync(string activationToken);
        public Task DeactivateGameAsync(string gameId);
        public Task UpdateGameAsync(string gameId, string gameName, string gameDescription, string gameGenre);
        public Task<List<string>> GetAllGamesAsync();
        public Task<string> GetGameByIdAsync(string gameId);
        public Task<string> GetGameByNameAsync(string gameName);
    }
}
