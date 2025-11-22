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
    // Backward-compatible overload: if role is omitted, defaults to 'User'
    Task<string> PreRegisterUserAsync(string email, string name, string password);
    Task<string> PreRegisterUserAsync(string email, string name, string password, string role);
        Task ActivateUserAsync(string activationToken);
        
        // Admin management methods
        Task<IEnumerable<Usuario>> GetAllUsersAsync();
        Task UpdateUserRoleAsync(Guid userId, string newRole);
        Task DisableUserAsync(Guid userId);
        Task EnableUserAsync(Guid userId);
        Task<Usuario> GetUserByIdAsync(Guid userId);
        
            // Profile update for the authenticated user (or admin updating another user)
            Task UpdateUserProfileAsync(string existingEmail, string newName, string newEmail);
    }
}
