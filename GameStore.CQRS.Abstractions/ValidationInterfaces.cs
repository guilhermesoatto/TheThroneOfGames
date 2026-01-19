namespace GameStore.CQRS.Abstractions
{
    /// <summary>
    /// Resultado da validação.
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Interface base para validadores.
    /// </summary>
    public interface IValidator<T>
    {
        ValidationResult Validate(T entity);
    }
}
