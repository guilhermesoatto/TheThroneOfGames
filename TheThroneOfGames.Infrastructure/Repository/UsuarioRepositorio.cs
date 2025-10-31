using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheThroneOfGames.Domain.Entities;
using TheThroneOfGames.Domain.Interfaces;
using TheThroneOfGames.Infrastructure.Persistence;

namespace TheThroneOfGames.Infrastructure.Repository
{
    using Microsoft.EntityFrameworkCore;

    public class UsuarioRepository : BaseRepository<Usuario>, IUsuarioRepository
    {
        private readonly MainDbContext _context;

        public UsuarioRepository(MainDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Usuario?> GetByActivationTokenAsync(string activationToken)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.ActiveToken == activationToken);
        }

        public async Task<Usuario?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}
