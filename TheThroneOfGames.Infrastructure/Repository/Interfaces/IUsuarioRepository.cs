using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheThroneOfGames.Domain.Entities;

namespace TheThroneOfGames.Domain.Repository.Interfaces
{
    public interface IUsuarioRepository
    {
        Task AddAsync(Usuario user);
        Task<Usuario> GetByActivationTokenAsync(string activationToken);
        Task UpdateAsync(Usuario user);
    }
}
