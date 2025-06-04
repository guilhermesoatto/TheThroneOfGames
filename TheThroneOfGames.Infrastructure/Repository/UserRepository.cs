using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheThroneOfGames.Domain.Repository.Interfaces;
using TheThroneOfGames.Infrastructure.Entities;
using TheThroneOfGames.Infrastructure.Persistence;



namespace TheThroneOfGames.Domain.Repository
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly MainDbContext _context;

        public UsuarioRepository(MainDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Usuario user)
        {
            var userEntity = .ToEntity(user);
            await _context.Users.AddAsync(userEntity);
            await _context.SaveChangesAsync();
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            var userEntity = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            return userEntity == null ? null : UserMapper.ToDomain(userEntity);
        }

        public async Task UpdateAsync(User user)
        {
            var userEntity = UserMapper.ToEntity(user);
            _context.Users.Update(userEntity);
            await _context.SaveChangesAsync();
        }
    }
}
