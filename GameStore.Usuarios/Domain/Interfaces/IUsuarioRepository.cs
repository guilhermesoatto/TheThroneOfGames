using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameStore.Usuarios.Domain.Entities;

namespace GameStore.Usuarios.Domain.Interfaces
{
    public interface IUsuarioRepository
    {
        Task<IEnumerable<Usuario>> GetAllAsync();
        Task<Usuario?> GetByIdAsync(Guid id);
        Task AddAsync(Usuario usuario);
        Task UpdateAsync(Usuario usuario);
        Task DeleteAsync(Guid id);
        Task<Usuario?> GetByActivationTokenAsync(string activationToken);
        Task<Usuario?> GetByEmailAsync(string email);
    }
}
