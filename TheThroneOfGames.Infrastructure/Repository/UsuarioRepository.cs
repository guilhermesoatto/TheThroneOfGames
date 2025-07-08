using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheThroneOfGames.Infrastructure.Entities;
using TheThroneOfGames.Infrastructure.Persistence;
using TheThroneOfGames.Infrastructure.Repository.Interfaces;

namespace TheThroneOfGames.Infrastructure.Repository
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly MainDbContext _context;

        public UsuarioRepository(MainDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(UserEntity user)
        {
            var userEntity = user;
            await _context.Users.AddAsync(userEntity);
            await _context.SaveChangesAsync();
        }

        public async Task<UserEntity> GetByActivationTokenAsync(string activationToken)
        {
            var userEntity = await _context.Users.FirstOrDefaultAsync(u => u.ActiveToken == activationToken);
            return userEntity;
        }

        public async Task<UserEntity?> GetByEmailAsync(string email)
        {
            var userEntity = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            return userEntity == null ? null : userEntity;
        }

        public async Task UpdateAsync(UserEntity user)
        {
            var userEntity = user;
            _context.Users.Update(userEntity);
            await _context.SaveChangesAsync();
        }
    }
}
