namespace GameStore.Partidas.Domain.Shared;

// Domain errors for the Partidas bounded context (matchmaking)
public sealed record JogadorNaoPossuiJogoError(Guid JogadorId, Guid JogoId)
    : DomainError("JOGADOR_NAO_POSSUI_JOGO", $"Jogador '{JogadorId}' não possui o jogo '{JogoId}' — compre o jogo antes de buscar partida.");

public sealed record SolicitacaoJaExisteError(Guid JogadorId, Guid JogoId)
    : DomainError("SOLICITACAO_JA_EXISTE", $"Jogador '{JogadorId}' já está aguardando uma partida para o jogo '{JogoId}'.");

public sealed record PartidaNaoEncontradaError(Guid PartidaId)
    : DomainError("PARTIDA_NAO_ENCONTRADA", $"Partida com id '{PartidaId}' não encontrada.");

public sealed record SolicitacaoNaoEncontradaError(Guid SolicitacaoId)
    : DomainError("SOLICITACAO_NAO_ENCONTRADA", $"Solicitação de busca com id '{SolicitacaoId}' não encontrada.");

public sealed record PartidaNaoEstaAguardandoConfirmacaoError(Guid PartidaId, string Status)
    : DomainError("PARTIDA_NAO_AGUARDA_CONFIRMACAO", $"Partida '{PartidaId}' não pode ser confirmada/desfeita — está no status '{Status}'.");

public sealed record JogadorNaoPertenceAPartidaError(Guid JogadorId, Guid PartidaId)
    : DomainError("JOGADOR_NAO_PERTENCE_A_PARTIDA", $"Jogador '{JogadorId}' não faz parte da partida '{PartidaId}'.");

public sealed record EquipeCheiaError(int LimiteMaximo)
    : DomainError("EQUIPE_CHEIA", $"A equipe já atingiu o limite máximo de {LimiteMaximo} jogadores.");
