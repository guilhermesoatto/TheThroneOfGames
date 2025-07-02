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
        Task AddAsync(UserEntity user);
        Task<UserEntity> GetByActivationTokenAsync(string activationToken);
        Task UpdateAsync(UserEntity user);
    }
}
