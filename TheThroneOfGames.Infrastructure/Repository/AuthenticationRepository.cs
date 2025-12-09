using TheThroneOfGames.Domain.Entities;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using TheThroneOfGames.Domain.Entities;

namespace TheThroneOfGames.Infrastructure.Repository
{
    public class AuthenticationRepository 
    {
        private static readonly ConcurrentDictionary<string, Usuario> _users = new();

        public Task<Usuario?> GetByEmailAsync(string email)
        {
            _users.TryGetValue(email, out var user);
            return Task.FromResult(user);
        }
    }
}
