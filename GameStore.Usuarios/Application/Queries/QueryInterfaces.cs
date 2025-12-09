namespace GameStore.Usuarios.Application.Queries
{
    /// <summary>
    /// Interface base para Queries.
    /// Queries representam intenções de leitura do sistema.
    /// </summary>
    public interface IQuery<TResult>
    {
        // Queries podem ter parâmetros mas não alteram estado
    }

    /// <summary>
    /// Interface base para Query Handlers.
    /// </summary>
    public interface IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
    {
        Task<TResult> HandleAsync(TQuery query);
    }

    /// <summary>
    /// Interface base para Commands.
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
        Task<GameStore.CQRS.Abstractions.CommandResult> HandleAsync(TCommand command);
    }
}
