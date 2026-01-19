namespace GameStore.Usuarios.Application.Handlers
{
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
