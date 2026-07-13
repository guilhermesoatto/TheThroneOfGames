using GameStore.Partidas.Domain.Entities;

namespace GameStore.Partidas.Domain.Repositories;

public interface IPartidaRepository
{
    Task<Partida?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Partida partida, CancellationToken ct = default);
    Task UpdateAsync(Partida partida, CancellationToken ct = default);
}
