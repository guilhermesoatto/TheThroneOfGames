using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameStore.Catalogo.Domain.Entities;
using GameStore.Catalogo.Domain.Interfaces;

namespace GameStore.Catalogo.Application.Services
{
    public class GameService
    {
        private readonly IJogoRepository _jogoRepository;

        public GameService(IJogoRepository jogoRepository)
        {
            _jogoRepository = jogoRepository;
        }

        public async Task AddAsync(Jogo jogo)
        {
            await _jogoRepository.AddAsync(jogo);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _jogoRepository.DeleteAsync(id);
        }

        public async Task<IEnumerable<Jogo>> GetAllAsync()
        {
            return await _jogoRepository.GetAllAsync();
        }

        public async Task<Jogo?> GetByIdAsync(Guid id)
        {
            return await _jogoRepository.GetByIdAsync(id);
        }

        public async Task UpdateAsync(Jogo jogo)
        {
            await _jogoRepository.UpdateAsync(jogo);
        }

        public async Task<List<Jogo>> GetAllGames()
        {
            var jogos = await _jogoRepository.GetAllAsync();
            return jogos.ToList();
        }

        public async Task<List<Jogo>> GetAvailableGames()
        {
            var jogos = await _jogoRepository.GetDisponiveisAsync();
            return jogos.ToList();
        }

        public async Task<List<Jogo>> GetGamesByGenre(string genero)
        {
            var jogos = await _jogoRepository.GetByGeneroAsync(genero);
            return jogos.ToList();
        }

        public async Task<List<Jogo>> GetGamesByPriceRange(decimal precoMinimo, decimal precoMaximo)
        {
            var jogos = await _jogoRepository.GetByFaixaPrecoAsync(precoMinimo, precoMaximo);
            return jogos.ToList();
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _jogoRepository.ExistsAsync(id);
        }
    }
}
