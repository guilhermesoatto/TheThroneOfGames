using System.Threading;
using GameStore.Vendas.Domain.Entities;
using GameStore.Vendas.Domain.Repositories;
using GameStore.Vendas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Vendas.Infrastructure.Repository
{
    public class PedidoRepository : IPedidoRepository
    {
        private readonly VendasDbContext _context;

        public PedidoRepository(VendasDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Pedido?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.Pedidos
                .Include(p => p.Itens)
                .FirstOrDefaultAsync(p => p.Id == id, ct);
        }

        public async Task<IEnumerable<Pedido>> GetByUsuarioIdAsync(Guid usuarioId, CancellationToken ct = default)
        {
            return await _context.Pedidos
                .Include(p => p.Itens)
                .Where(p => p.UsuarioId == usuarioId)
                .OrderByDescending(p => p.DataCriacao)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<Pedido>> GetPedidosPendentesAsync(CancellationToken ct = default)
        {
            return await _context.Pedidos
                .Include(p => p.Itens)
                .Where(p => p.Status == "Pendente")
                .OrderBy(p => p.DataCriacao)
                .ToListAsync(ct);
        }

        public async Task AddAsync(Pedido pedido, CancellationToken ct = default)
        {
            await _context.Pedidos.AddAsync(pedido, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(Pedido pedido, CancellationToken ct = default)
        {
            _context.Pedidos.Update(pedido);
            await _context.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var pedido = await GetByIdAsync(id, ct);
            if (pedido != null)
            {
                _context.Pedidos.Remove(pedido);
                await _context.SaveChangesAsync(ct);
            }
        }
    }
}