using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheThroneOfGames.Application.Interface;

namespace TheThroneOfGames.Application
{
    public class GameService : IGameService
    {
        public Task ActivateGameAsync(string activationToken)
        {
            throw new NotImplementedException();
        }

        public Task DeactivateGameAsync(string gameId)
        {
            throw new NotImplementedException();
        }

        public Task<List<string>> GetAllGamesAsync()
        {
            throw new NotImplementedException();
        }

        public Task<string> GetGameByIdAsync(string gameId)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetGameByNameAsync(string gameName)
        {
            throw new NotImplementedException();
        }

        public Task PreRegisterGameAsync(string gameName, string gameDescription, string gameGenre)
        {
            throw new NotImplementedException();
        }

        public Task UpdateGameAsync(string gameId, string gameName, string gameDescription, string gameGenre)
        {
            throw new NotImplementedException();
        }
    }
}
