using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheThroneOfGames.Application.Interface;
using TheThroneOfGames.Domain.Interfaces;
using TheThroneOfGames.Infrastructure.Entities;

namespace GameStore.Catalogo.Application
{
    public class GameService : IGameService
    {
        private readonly IBaseRepository<GameEntity> _gameRepository;
        private readonly IBaseRepository<Purchase> _purchaseRepository;

        public GameService(IBaseRepository<GameEntity> gameRepository, IBaseRepository<Purchase> purchaseRepository)
        {
            _gameRepository = gameRepository;
            _purchaseRepository = purchaseRepository;
        }

        public async Task AddAsync(GameEntity entity)
        {
            await _gameRepository.AddAsync(entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _gameRepository.DeleteAsync(id);
        }

        public async Task<IEnumerable<GameEntity>> GetAllAsync()
        {
            return await _gameRepository.GetAllAsync();
        }

        public async Task<GameEntity?> GetByIdAsync(Guid id)
        {
            return await _gameRepository.GetByIdAsync(id);
        }

        public async Task UpdateAsync(GameEntity entity)
        {
            await _gameRepository.UpdateAsync(entity);
        }

        public async Task BuyGame(Guid gameId, Guid userId)
        {
            var game = await _gameRepository.GetByIdAsync(gameId);
            if (game == null)
                throw new ArgumentException($"Game with ID {gameId} not found.");

            var purchase = new Purchase
            {
                GameId = gameId,
                UserId = userId,
                PurchaseDate = DateTime.UtcNow
            };

            await _purchaseRepository.AddAsync(purchase);
        }

        public async Task<List<GameEntity>> GetAllGames()
        {
            var games = await _gameRepository.GetAllAsync();
            return games.ToList();
        }

        public async Task<List<GameEntity>> GetAvailableGames(Guid userId)
        {
            var allGames = await _gameRepository.GetAllAsync();
            var ownedGameIds = (await GetOwnedGames(userId)).Select(g => g.Id);

            return allGames.Where(g => !ownedGameIds.Contains(g.Id)).ToList();
        }

        public async Task<List<GameEntity>> GetOwnedGames(Guid userId)
        {
            var purchases = await _purchaseRepository.GetAllAsync();
            var userPurchases = purchases.Where(p => p.UserId == userId);
            var gameIds = userPurchases.Select(p => p.GameId);
            
            var games = await _gameRepository.GetAllAsync();
            return games.Where(g => gameIds.Contains(g.Id)).ToList();
        }
    }
}
