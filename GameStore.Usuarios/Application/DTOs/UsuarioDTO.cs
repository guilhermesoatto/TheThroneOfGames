namespace GameStore.Usuarios.Application.DTOs
{
    /// <summary>
    /// Data Transfer Object para Usuario.
    /// Contém apenas os campos necessários para transferência entre camadas.
    /// NÃO inclui senhas ou tokens sensíveis.
    /// </summary>
    public class UsuarioDTO
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string Role { get; set; } = null!;

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
