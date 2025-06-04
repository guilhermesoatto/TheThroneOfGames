using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheThroneOfGames.Infrastructure.Entities;

namespace TheThroneOfGames.Infrastructure.Repository.Interfaces
{
    public interface IUsuarioRepository
    {
        Task AddAsync(User user);
        Task<User> GetByActivationTokenAsync(string activationToken);
        Task UpdateAsync(User user);
    }
}
