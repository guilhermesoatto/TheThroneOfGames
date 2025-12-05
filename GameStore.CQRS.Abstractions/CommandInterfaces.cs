namespace GameStore.CQRS.Abstractions
{
    /// <summary>
    /// Interface base para Commands.
    /// Commands representam intenções de escrita/mutação do sistema.
    /// </summary>
    public interface ICommand
    {
        Guid CommandId { get; }
        DateTime CreatedAt { get; }
    }

    /// <summary>
    /// Interface base para Command Handlers.
    /// </summary>
    public interface ICommandHandler<TCommand> where TCommand : ICommand
    {
        Task<CommandResult> HandleAsync(TCommand command);
    }

    /// <summary>
    /// Resultado da execução de um Command.
    /// </summary>
    public class CommandResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Guid? EntityId { get; set; }
        public object? Data { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
