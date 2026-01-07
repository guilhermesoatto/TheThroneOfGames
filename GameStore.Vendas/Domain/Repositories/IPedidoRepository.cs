using GameStore.Vendas.Domain.Entities;

namespace GameStore.Vendas.Domain.Repositories
{
    public interface IPedidoRepository
    {
        Task<Pedido?> GetByIdAsync(Guid id);
        Task<IEnumerable<Pedido>> GetByUsuarioIdAsync(Guid usuarioId);
        Task<IEnumerable<Pedido>> GetPedidosPendentesAsync();
        Task AddAsync(Pedido pedido);
        Task UpdateAsync(Pedido pedido);
        Task DeleteAsync(Guid id);
    }
}