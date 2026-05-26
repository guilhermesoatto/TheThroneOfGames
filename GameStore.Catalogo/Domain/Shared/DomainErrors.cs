namespace GameStore.Catalogo.Domain.Shared;

// Domain errors for the Catalogo bounded context
public sealed record JogoNomeVazioError()
    : DomainError("JOGO_NOME_VAZIO", "O nome do jogo é obrigatório.");

public sealed record JogoPrecoNegativoError()
    : DomainError("JOGO_PRECO_NEGATIVO", "O preço do jogo não pode ser negativo.");

public sealed record JogoEstoqueNegativoError()
    : DomainError("JOGO_ESTOQUE_NEGATIVO", "O estoque do jogo não pode ser negativo.");

public sealed record JogoNaoEncontradoError(Guid Id)
    : DomainError("JOGO_NAO_ENCONTRADO", $"Jogo com id '{Id}' não encontrado.");

public sealed record JogoJaExisteError(string Nome)
    : DomainError("JOGO_JA_EXISTE", $"Jogo '{Nome}' já está cadastrado.");
