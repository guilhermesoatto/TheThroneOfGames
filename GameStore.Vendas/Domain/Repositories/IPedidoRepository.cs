using System.Threading;
using GameStore.Vendas.Domain.Entities;

namespace GameStore.Vendas.Domain.Repositories;

public interface IPedidoRepository
{
    Task<Pedido?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Pedido>> GetByUsuarioIdAsync(Guid usuarioId, CancellationToken ct = default);
    Task<IEnumerable<Pedido>> GetPedidosPendentesAsync(CancellationToken ct = default);
    Task AddAsync(Pedido pedido, CancellationToken ct = default);
    Task UpdateAsync(Pedido pedido, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}