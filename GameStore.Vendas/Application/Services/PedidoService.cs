using GameStore.Vendas.Application.Interfaces;
using GameStore.Vendas.Application.DTOs;
using GameStore.Vendas.Application.Mappers;
using GameStore.Vendas.Domain.Entities;
using GameStore.Vendas.Domain.Repositories;
using GameStore.Vendas.Domain.ValueObjects;

namespace GameStore.Vendas.Application.Services
{
    public class PedidoService : IPedidoService
    {
        private readonly IPedidoRepository _pedidoRepository;

        public PedidoService(IPedidoRepository pedidoRepository)
        {
            _pedidoRepository = pedidoRepository ?? throw new ArgumentNullException(nameof(pedidoRepository));
        }

        public async Task<PedidoDto?> ObterPedidoPorIdAsync(Guid id)
        {
            var pedido = await _pedidoRepository.GetByIdAsync(id);
            return pedido != null ? PedidoMapper.ToPedidoDto(pedido) : null;
        }

        public async Task<IEnumerable<PedidoDto>> ObterPedidosPorUsuarioAsync(Guid usuarioId)
        {
            var pedidos = await _pedidoRepository.GetByUsuarioIdAsync(usuarioId);
            return pedidos.Select(PedidoMapper.ToPedidoDto);
        }

        public async Task<PedidoDto> CriarPedidoAsync(Guid usuarioId)
        {
            var pedido = new Pedido(usuarioId);
            await _pedidoRepository.AddAsync(pedido);
            return PedidoMapper.ToPedidoDto(pedido);
        }

        public async Task<bool> AdicionarItemAoPedidoAsync(Guid pedidoId, Guid jogoId, string nomeJogo, decimal preco)
        {
            var pedido = await _pedidoRepository.GetByIdAsync(pedidoId);
            if (pedido == null)
                return false;

            try
            {
                pedido.AdicionarItem(jogoId, nomeJogo, new Money(preco));
                await _pedidoRepository.UpdateAsync(pedido);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RemoverItemDoPedidoAsync(Guid pedidoId, Guid jogoId)
        {
            var pedido = await _pedidoRepository.GetByIdAsync(pedidoId);
            if (pedido == null)
                return false;

            try
            {
                pedido.RemoverItem(jogoId);
                await _pedidoRepository.UpdateAsync(pedido);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> FinalizarPedidoAsync(Guid pedidoId, string metodoPagamento)
        {
            var pedido = await _pedidoRepository.GetByIdAsync(pedidoId);
            if (pedido == null)
                return false;

            try
            {
                pedido.Finalizar(metodoPagamento);
                await _pedidoRepository.UpdateAsync(pedido);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CancelarPedidoAsync(Guid pedidoId, string motivo)
        {
            var pedido = await _pedidoRepository.GetByIdAsync(pedidoId);
            if (pedido == null)
                return false;

            try
            {
                pedido.Cancelar(motivo);
                await _pedidoRepository.UpdateAsync(pedido);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
