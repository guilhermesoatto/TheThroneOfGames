using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GameStore.Catalogo.Domain.Entities;

namespace GameStore.Catalogo.Domain.Interfaces;

public interface IJogoRepository
{
    Task<IEnumerable<Jogo>> GetAllAsync(CancellationToken ct = default);
    Task<Jogo?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Jogo jogo, CancellationToken ct = default);
    Task UpdateAsync(Jogo jogo, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Jogo>> GetByGeneroAsync(string genero, CancellationToken ct = default);
    Task<IEnumerable<Jogo>> GetDisponiveisAsync(CancellationToken ct = default);
    Task<IEnumerable<Jogo>> GetByNomeAsync(string nome, CancellationToken ct = default);
    Task<IEnumerable<Jogo>> GetByFaixaPrecoAsync(decimal precoMinimo, decimal precoMaximo, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
}