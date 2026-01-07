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

        public async Task<Pedido?> GetByIdAsync(Guid id)
        {
            return await _context.Pedidos
                .Include(p => p.Itens)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Pedido>> GetByUsuarioIdAsync(Guid usuarioId)
        {
            return await _context.Pedidos
                .Include(p => p.Itens)
                .Where(p => p.UsuarioId == usuarioId)
                .OrderByDescending(p => p.DataCriacao)
                .ToListAsync();
        }

        public async Task<IEnumerable<Pedido>> GetPedidosPendentesAsync()
        {
            return await _context.Pedidos
                .Include(p => p.Itens)
                .Where(p => p.Status == "Pendente")
                .OrderBy(p => p.DataCriacao)
                .ToListAsync();
        }

        public async Task AddAsync(Pedido pedido)
        {
            await _context.Pedidos.AddAsync(pedido);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Pedido pedido)
        {
            _context.Pedidos.Update(pedido);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var pedido = await GetByIdAsync(id);
            if (pedido != null)
            {
                _context.Pedidos.Remove(pedido);
                await _context.SaveChangesAsync();
            }
        }
    }
}