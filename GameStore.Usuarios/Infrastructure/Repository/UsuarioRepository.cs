using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GameStore.Usuarios.Domain.Entities;
using GameStore.Usuarios.Domain.Interfaces;
using GameStore.Usuarios.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Usuarios.Infrastructure.Repository
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly UsuariosDbContext _context;

        public UsuarioRepository(UsuariosDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Usuario>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.Usuarios.ToListAsync(ct);
        }

        public async Task<Usuario?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.Usuarios.FindAsync(new object[] { id }, ct);
        }

        public async Task AddAsync(Usuario usuario, CancellationToken ct = default)
        {
            await _context.Usuarios.AddAsync(usuario, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(Usuario usuario, CancellationToken ct = default)
        {
            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var usuario = await GetByIdAsync(id, ct);
            if (usuario != null)
            {
                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync(ct);
            }
        }

        public async Task<Usuario?> GetByActivationTokenAsync(string activationToken, CancellationToken ct = default)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.ActiveToken == activationToken, ct);
        }

        public async Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct = default)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email, ct);
        }
    }
}
