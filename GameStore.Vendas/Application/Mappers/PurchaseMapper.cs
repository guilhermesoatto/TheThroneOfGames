using GameStore.Vendas.Application.DTOs;
using GameStore.Vendas.Domain.Entities;
using TheThroneOfGames.Domain.Entities;

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
        public static PurchaseDTO ToPurchaseDTO(PurchaseEntity purchase)
        {
            if (purchase == null)
                throw new ArgumentNullException(nameof(purchase));

            return new PurchaseDTO
            {
                Id = purchase.Id,
                UserId = purchase.UserId,
                GameId = purchase.GameId,
                TotalPrice = purchase.TotalPrice,
                PurchaseDate = purchase.PurchaseDate,
                Status = purchase.Status,
                PaymentMethod = purchase.PaymentMethod,
                CancellationReason = purchase.CancellationReason,
                CompletedAt = purchase.CompletedAt,
                CancelledAt = purchase.CancelledAt
            };
        }

        /// <summary>
        /// Converte um PurchaseEntity para PurchaseDTO (método legacy).
        /// </summary>
        public static PurchaseDTO ToDTO(PurchaseEntity purchase) => ToPurchaseDTO(purchase);

        /// <summary>
        /// Converte um PurchaseDTO para PurchaseEntity.
        /// </summary>
        public static PurchaseEntity FromDTO(PurchaseDTO dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return new PurchaseEntity
            {
                Id = dto.Id,
                UserId = dto.UserId,
                GameId = dto.GameId,
                TotalPrice = dto.TotalPrice,
                PurchaseDate = dto.PurchaseDate,
                Status = dto.Status ?? "Pending",
                PaymentMethod = dto.PaymentMethod,
                CancellationReason = dto.CancellationReason,
                CompletedAt = dto.CompletedAt,
                CancelledAt = dto.CancelledAt
            };
        }

        /// <summary>
        /// Converte uma coleção de PurchaseEntity para uma coleção de PurchaseDTOs.
        /// </summary>
        public static IEnumerable<PurchaseDTO> ToPurchaseDTOList(IEnumerable<PurchaseEntity> purchases)
        {
            if (purchases == null)
                throw new ArgumentNullException(nameof(purchases));

            return purchases.Select(ToPurchaseDTO).ToList();
        }

        /// <summary>
        /// Converte uma coleção de PurchaseEntity para uma coleção de PurchaseDTOs (método legacy).
        /// </summary>
        public static IEnumerable<PurchaseDTO> ToDTOList(IEnumerable<PurchaseEntity> purchases) => ToPurchaseDTOList(purchases);

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
