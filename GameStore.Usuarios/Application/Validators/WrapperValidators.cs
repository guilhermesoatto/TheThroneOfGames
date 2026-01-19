using GameStore.Usuarios.Application.Commands;

namespace GameStore.Usuarios.Application.Validators
{
    // Lightweight wrapper validator classes so existing tests can instantiate
    // concrete validator types (they delegate to the static UsuarioValidators
    // implementation that contains the actual validation logic).

    public class CreateUserCommandValidator
    {
        public ValidationResult Validate(CreateUserCommand command)
            => UsuarioValidators.Validate(command);
    }

    public class UpdateUserProfileCommandValidator
    {
        public ValidationResult Validate(UpdateUserProfileCommand command)
            => UsuarioValidators.Validate(command);
    }

    public class ActivateUserCommandValidator
    {
        public ValidationResult Validate(ActivateUserCommand command)
            => UsuarioValidators.Validate(command);
    }

    public class ChangeUserRoleCommandValidator
    {
        public ValidationResult Validate(ChangeUserRoleCommand command)
            => UsuarioValidators.Validate(command);
    }
}
