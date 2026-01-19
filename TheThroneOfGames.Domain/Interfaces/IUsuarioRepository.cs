using TheThroneOfGames.Domain.Entities;

namespace TheThroneOfGames.Domain.Interfaces;

public interface IUsuarioRepository : IBaseRepository<Usuario>
{
    Task<Usuario?> GetByActivationTokenAsync(string activationToken);
    Task<Usuario?> GetByEmailAsync(string email);
}
