using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameStore.Catalogo.Domain.Entities;

namespace GameStore.Catalogo.Domain.Interfaces
{
    public interface IJogoRepository
    {
        Task<IEnumerable<Jogo>> GetAllAsync();
        Task<Jogo?> GetByIdAsync(Guid id);
        Task AddAsync(Jogo jogo);
        Task UpdateAsync(Jogo jogo);
        Task DeleteAsync(Guid id);
        Task<IEnumerable<Jogo>> GetByGeneroAsync(string genero);
        Task<IEnumerable<Jogo>> GetDisponiveisAsync();
        Task<IEnumerable<Jogo>> GetByNomeAsync(string nome);
        Task<IEnumerable<Jogo>> GetByFaixaPrecoAsync(decimal precoMinimo, decimal precoMaximo);
        Task<bool> ExistsAsync(Guid id);
    }
}