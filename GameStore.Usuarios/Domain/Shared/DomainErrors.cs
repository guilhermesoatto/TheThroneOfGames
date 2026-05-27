namespace GameStore.Usuarios.Domain.Shared;

// Domain errors for the Usuarios bounded context
public sealed record EmailVazioError()
    : DomainError("EMAIL_VAZIO", "O email do usuário é obrigatório.");

public sealed record NomeVazioError()
    : DomainError("NOME_VAZIO", "O nome do usuário é obrigatório.");

public sealed record RoleVaziaError()
    : DomainError("ROLE_VAZIA", "A role do usuário não pode ser vazia.");

public sealed record UsuarioNaoEncontradoError(Guid Id)
    : DomainError("USUARIO_NAO_ENCONTRADO", $"Usuário com id '{Id}' não encontrado.");

public sealed record UsuarioJaExisteError(string Email)
    : DomainError("USUARIO_JA_EXISTE", $"Usuário com email '{Email}' já está cadastrado.");
