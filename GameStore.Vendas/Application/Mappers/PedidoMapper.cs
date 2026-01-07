using GameStore.Vendas.Domain.Entities;
using GameStore.Vendas.Application.DTOs;

namespace GameStore.Vendas.Application.Mappers
{
    public static class PedidoMapper
    {
        public static PedidoDto ToPedidoDto(Pedido pedido)
        {
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

        public static ItemPedidoDto ToItemPedidoDto(ItemPedido item)
        {
            return new ItemPedidoDto
            {
                Id = item.Id,
                JogoId = item.JogoId,
                NomeJogo = item.NomeJogo,
                Preco = item.Preco,
                DataAdicao = item.DataAdicao
            };
        }
    }
}