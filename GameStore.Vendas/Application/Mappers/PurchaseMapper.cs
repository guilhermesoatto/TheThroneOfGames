using GameStore.Vendas.Application.DTOs;
using GameStore.Vendas.Domain.Entities;

namespace GameStore.Vendas.Application.Mappers
{
    /// <summary>
    /// Mapper para converter entre entidades do domínio de vendas e DTOs.
    /// Responsável por mapeamentos bidirecionais mantendo a integridade dos dados.
    /// </summary>
    public static class PurchaseMapper
    {
        /// <summary>
        /// Converte um Pedido para PedidoDTO.
        /// </summary>
        public static PedidoDto ToPedidoDTO(Pedido pedido)
        {
            if (pedido == null)
                throw new ArgumentNullException(nameof(pedido));

            return new PedidoDto
            {
                Id = pedido.Id,
                UsuarioId = pedido.UsuarioId,
                DataCriacao = pedido.DataCriacao,
                Status = pedido.Status,
                ValorTotal = pedido.ValorTotal,
                MetodoPagamento = pedido.MetodoPagamento,
                DataFinalizacao = pedido.DataFinalizacao,
                DataCancelamento = pedido.DataCancelamento,
                MotivoCancelamento = pedido.MotivoCancelamento,
                Itens = pedido.Itens.Select(ToItemPedidoDto).ToList()
            };
        }

        /// <summary>
        /// Converte um ItemPedido para ItemPedidoDto.
        /// </summary>
        public static ItemPedidoDto ToItemPedidoDto(ItemPedido item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            return new ItemPedidoDto
            {
                Id = item.Id,
                JogoId = item.JogoId,
                NomeJogo = item.NomeJogo,
                Preco = item.Preco,
                DataAdicao = item.DataAdicao
            };
        }

        /// <summary>
        /// Converte uma coleção de Pedidos para uma coleção de PedidoDTOs.
        /// </summary>
        public static IEnumerable<PedidoDto> ToPedidoDTOList(IEnumerable<Pedido> pedidos)
        {
            if (pedidos == null)
                throw new ArgumentNullException(nameof(pedidos));

            return pedidos.Select(ToPedidoDTO).ToList();
        }
    }
}
