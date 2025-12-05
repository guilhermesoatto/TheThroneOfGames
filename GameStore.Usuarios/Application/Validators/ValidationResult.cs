namespace GameStore.Usuarios.Application.Validators
{
    /// <summary>
    /// Resultado da validação.
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationError> Errors { get; set; } = new();
    }
    
    public class ValidationError
    {
        public string PropertyName { get; set; } = null!;
        public string ErrorMessage { get; set; } = null!;
    }
}
