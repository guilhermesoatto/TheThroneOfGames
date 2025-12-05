namespace GameStore.Vendas.Application.DTOs
{
    /// <summary>
    /// Data Transfer Object para Purchase/Compra.
    /// Contém informações sobre uma transação de compra de jogo.
    /// </summary>
    public class PurchaseDTO
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public Guid GameId { get; set; }

        public decimal TotalPrice { get; set; }

        public DateTime PurchaseDate { get; set; }

        public string Status { get; set; } = "Completed";
        
        public string? PaymentMethod { get; set; }
        
        public string? CancellationReason { get; set; }
        
        public DateTime? CompletedAt { get; set; }
        
        public DateTime? CancelledAt { get; set; }
    }

    /// <summary>
    /// DTO para Pedido local do contexto Vendas.
    /// Representa uma ordem de compra com múltiplos itens.
    /// </summary>
    public class PedidoDTO
    {
        public int Id { get; set; }

        public int UsuarioId { get; set; }

        public decimal TotalPrice { get; set; }

        public DateTime DataPedido { get; set; }

        public string Status { get; set; } = "Pendente";
    }

    /// <summary>
    /// DTO para um item individual dentro de um Pedido.
    /// </summary>
    public class ItemPedidoDTO
    {
        public int Id { get; set; }

        public int PedidoId { get; set; }

        public int JogoId { get; set; }

        public decimal Preco { get; set; }

        public int Quantidade { get; set; }
    }
}
