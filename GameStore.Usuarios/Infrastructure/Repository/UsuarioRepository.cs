using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheThroneOfGames.Domain.Entities;
using TheThroneOfGames.Domain.Interfaces;
using TheThroneOfGames.Infrastructure.Persistence;
using TheThroneOfGames.Infrastructure.Repository;

namespace GameStore.Usuarios.Infrastructure.Repository
{
    using Microsoft.EntityFrameworkCore;

        public class UsuarioRepository : BaseRepository<TheThroneOfGames.Domain.Entities.Usuario>, IUsuarioRepository
    {
        private readonly MainDbContext _context;

        public UsuarioRepository(MainDbContext context) : base(context)
        {
            _context = context;
        }

            public async Task<TheThroneOfGames.Domain.Entities.Usuario?> GetByActivationTokenAsync(string activationToken)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.ActiveToken == activationToken);
        }

            public async Task<TheThroneOfGames.Domain.Entities.Usuario?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}
