using GameStore.Vendas.Application.DTOs;

namespace GameStore.Vendas.Application.Interfaces
{
    public interface IPedidoService
    {
        Task<PedidoDto?> ObterPedidoPorIdAsync(Guid id);
        Task<IEnumerable<PedidoDto>> ObterPedidosPorUsuarioAsync(Guid usuarioId);
        Task<PedidoDto> CriarPedidoAsync(Guid usuarioId);
        Task<bool> AdicionarItemAoPedidoAsync(Guid pedidoId, Guid jogoId, string nomeJogo, decimal preco);
        Task<bool> RemoverItemDoPedidoAsync(Guid pedidoId, Guid jogoId);
        Task<bool> FinalizarPedidoAsync(Guid pedidoId, string metodoPagamento);
        Task<bool> CancelarPedidoAsync(Guid pedidoId, string motivo);
    }
}