using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheThroneOfGames.Domain.Entities;

namespace TheThroneOfGames.Application.Interface
{
    public interface IUsuarioService
    {
        Task<string> PreRegisterUserAsync(string email, string name, string password);
        Task ActivateUserAsync(string activationToken);
        
        // Admin management methods
        Task<IEnumerable<Usuario>> GetAllUsersAsync();
        Task UpdateUserRoleAsync(Guid userId, string newRole);
        Task DisableUserAsync(Guid userId);
        Task EnableUserAsync(Guid userId);
        Task<Usuario> GetUserByIdAsync(Guid userId);
    }
}
