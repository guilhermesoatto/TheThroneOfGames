using GameStore.CQRS.Abstractions;

namespace GameStore.Vendas.Application.Commands
{
    /// <summary>
    /// Command para criar um novo pedido/purchase.
    /// </summary>
    public record CreatePurchaseCommand(
        Guid UserId,
        Guid GameId,
        decimal Price
    ) : ICommand
    {
        public Guid CommandId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Command para finalizar um pedido.
    /// </summary>
    public record FinalizePurchaseCommand(
        Guid PurchaseId,
        string PaymentMethod = "CreditCard"
    ) : ICommand
    {
        public Guid CommandId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Command para cancelar um pedido.
    /// </summary>
    public record CancelPurchaseCommand(
        Guid PurchaseId,
        string Reason
    ) : ICommand
    {
        public Guid CommandId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
    }
}
