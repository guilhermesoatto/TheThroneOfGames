using GameStore.CQRS.Abstractions;

namespace GameStore.Catalogo.Application.Commands
{
    /// <summary>
    /// Command para criar um novo jogo.
    /// </summary>
    public record CreateGameCommand(
        string Name,
        string Genre,
        decimal Price,
        string Description = ""
    ) : ICommand
    {
        public Guid CommandId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Command para atualizar informações de um jogo.
    /// </summary>
    public record UpdateGameCommand(
        Guid GameId,
        string Name,
        string Genre,
        decimal Price,
        string Description = ""
    ) : ICommand
    {
        public Guid CommandId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Command para remover um jogo do catálogo.
    /// </summary>
    public record RemoveGameCommand(
        Guid GameId
    ) : ICommand
    {
        public Guid CommandId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
    }
}
