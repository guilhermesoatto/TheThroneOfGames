namespace GameStore.CQRS.Abstractions
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
}
