using GameStore.CQRS.Abstractions;

namespace GameStore.Usuarios.Application.Commands
{
    /// <summary>
    /// Command para ativar um usuário.
    /// </summary>
    public record ActivateUserCommand(
        string ActivationToken
    ) : ICommand
    {
        public Guid CommandId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Command para atualizar o perfil de um usuário.
    /// </summary>
    public record UpdateUserProfileCommand(
        string ExistingEmail,
        string NewName,
        string NewEmail
    ) : ICommand
    {
        public Guid CommandId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Command para criar um novo usuário.
    /// </summary>
    public record CreateUserCommand(
        string Name,
        string Email,
        string Password,
        string Role = "User"
    ) : ICommand
    {
        public Guid CommandId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Command para alterar a role de um usuário.
    /// </summary>
    public record ChangeUserRoleCommand(
        string Email,
        string NewRole
    ) : ICommand
    {
        public Guid CommandId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
    }
}
