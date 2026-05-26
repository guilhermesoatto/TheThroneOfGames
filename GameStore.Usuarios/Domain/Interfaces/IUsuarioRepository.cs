using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GameStore.Usuarios.Domain.Entities;

namespace GameStore.Usuarios.Domain.Interfaces;

public interface IUsuarioRepository
{
    Task<IEnumerable<Usuario>> GetAllAsync(CancellationToken ct = default);
    Task<Usuario?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Usuario usuario, CancellationToken ct = default);
    Task UpdateAsync(Usuario usuario, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Usuario?> GetByActivationTokenAsync(string activationToken, CancellationToken ct = default);
    Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct = default);
}
