using GameStore.Partidas.Domain.Entities;

namespace GameStore.Partidas.Domain.Repositories;

public interface ISolicitacaoBuscaRepository
{
    Task<SolicitacaoBusca?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Solicitação em Aguardando do próprio jogador para este jogo, se já existir (evita duplicar).</summary>
    Task<SolicitacaoBusca?> GetAguardandoDoJogadorAsync(Guid jogadorId, Guid jogoId, CancellationToken ct = default);

    /// <summary>
    /// Outra solicitação em Aguardando para o mesmo jogo, de um jogador DIFERENTE do informado —
    /// é a query que dá match no motor de pareamento 1v1 (a primeira encontrada "ganha").
    /// </summary>
    Task<SolicitacaoBusca?> BuscarOutroAguardandoAsync(Guid jogoId, Guid jogadorIdSolicitante, CancellationToken ct = default);

    Task AddAsync(SolicitacaoBusca solicitacao, CancellationToken ct = default);
    Task UpdateAsync(SolicitacaoBusca solicitacao, CancellationToken ct = default);
}
