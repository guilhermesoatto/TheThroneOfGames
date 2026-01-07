using GameStore.CQRS.Abstractions;

namespace GameStore.Vendas.Application.Commands
{
    /// <summary>
    /// Command para criar um novo pedido.
    /// </summary>
    public record CriarPedidoCommand(
        Guid UsuarioId
    ) : ICommand
    {
        public Guid CommandId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Command para adicionar um item ao pedido.
    /// </summary>
    public record AdicionarItemPedidoCommand(
        Guid PedidoId,
        Guid JogoId,
        string NomeJogo,
        decimal Preco
    ) : ICommand
    {
        public Guid CommandId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Command para remover um item do pedido.
    /// </summary>
    public record RemoverItemPedidoCommand(
        Guid PedidoId,
        Guid JogoId
    ) : ICommand
    {
        public Guid CommandId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Command para finalizar um pedido.
    /// </summary>
    public record FinalizarPedidoCommand(
        Guid PedidoId,
        string MetodoPagamento
    ) : ICommand
    {
        public Guid CommandId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Command para cancelar um pedido.
    /// </summary>
    public record CancelarPedidoCommand(
        Guid PedidoId,
        string Motivo
    ) : ICommand
    {
        public Guid CommandId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
    }
}