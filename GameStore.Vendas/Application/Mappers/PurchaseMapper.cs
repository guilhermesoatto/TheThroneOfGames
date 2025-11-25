using GameStore.Vendas.Application.DTOs;
using GameStore.Vendas.Domain.Entities;
using TheThroneOfGames.Infrastructure.Entities;

namespace GameStore.Vendas.Application.Mappers
{
    /// <summary>
    /// Mapper para converter entre Purchase (monolith) e PurchaseDTO.
    /// Também mapeia para PedidoDTO (entidade local) quando necessário.
    /// Responsável por mapeamentos bidirecionais mantendo a integridade dos dados.
    /// </summary>
    public static class PurchaseMapper
    {
        /// <summary>
        /// Converte um Purchase para PurchaseDTO.
        /// </summary>
        public static PurchaseDTO ToDTO(Purchase purchase)
        {
            if (purchase == null)
                throw new ArgumentNullException(nameof(purchase));

            return new PurchaseDTO
            {
                Id = purchase.Id,
                UserId = purchase.UserId,
                GameId = purchase.GameId,
                TotalPrice = 0, // Será ajustado quando Purchase tiver TotalPrice
                PurchaseDate = purchase.PurchaseDate,
                Status = "Completed"
            };
        }

        /// <summary>
        /// Converte um PurchaseDTO para Purchase.
        /// </summary>
        public static Purchase FromDTO(PurchaseDTO dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return new Purchase
            {
                Id = dto.Id,
                UserId = dto.UserId,
                GameId = dto.GameId,
                PurchaseDate = dto.PurchaseDate
            };
        }

        /// <summary>
        /// Converte um Pedido (entidade local) para PedidoDTO.
        /// </summary>
        public static PedidoDTO ToPedidoDTO(Pedido pedido)
        {
            if (pedido == null)
                throw new ArgumentNullException(nameof(pedido));

            return new PedidoDTO
            {
                Id = pedido.Id,
                UsuarioId = pedido.UsuarioId,
                TotalPrice = 0,
                DataPedido = DateTime.UtcNow,
                Status = "Pendente"
            };
        }

        /// <summary>
        /// Converte um PedidoDTO para Pedido (entidade local).
        /// </summary>
        public static Pedido FromPedidoDTO(PedidoDTO dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return new Pedido
            {
                Id = dto.Id,
                UsuarioId = dto.UsuarioId
            };
        }

        /// <summary>
        /// Converte uma coleção de Purchases para uma coleção de PurchaseDTOs.
        /// </summary>
        public static IEnumerable<PurchaseDTO> ToDTOList(IEnumerable<Purchase> purchases)
        {
            if (purchases == null)
                throw new ArgumentNullException(nameof(purchases));

            return purchases.Select(ToDTO).ToList();
        }

        /// <summary>
        /// Converte uma coleção de Pedidos para uma coleção de PedidoDTOs.
        /// </summary>
        public static IEnumerable<PedidoDTO> ToPedidoDTOList(IEnumerable<Pedido> pedidos)
        {
            if (pedidos == null)
                throw new ArgumentNullException(nameof(pedidos));

            return pedidos.Select(ToPedidoDTO).ToList();
        }
    }
}
