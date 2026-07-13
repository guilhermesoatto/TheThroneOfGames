using System;
using System.Threading;
using System.Threading.Tasks;
using GameStore.Usuarios.Domain.Entities;
using GameStore.Usuarios.Domain.Interfaces;
using GameStore.Usuarios.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Usuarios.Infrastructure.Repository
{
    public class InventarioRepository : IInventarioRepository
    {
        private readonly UsuariosDbContext _context;

        public InventarioRepository(UsuariosDbContext context)
        {
            _context = context;
        }

        public async Task AdicionarSeNaoExistirAsync(ItemInventario item, CancellationToken ct = default)
        {
            var jaExiste = await _context.ItensInventario
                .AnyAsync(i => i.UsuarioId == item.UsuarioId && i.JogoId == item.JogoId, ct);
            if (jaExiste)
                return;

            await _context.ItensInventario.AddAsync(item, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task<bool> PossuiJogoAsync(Guid usuarioId, Guid jogoId, CancellationToken ct = default)
        {
            return await _context.ItensInventario
                .AnyAsync(i => i.UsuarioId == usuarioId && i.JogoId == jogoId, ct);
        }
    }
}
