using GameStore.Catalogo.Domain.Entities;

namespace GameStore.Catalogo.Infrastructure.Search;

/// <summary>
/// Porta para indexação e busca avançada de jogos via Elasticsearch (Fase 3 — fase3-T04).
/// </summary>
public interface IJogoSearchIndexer
{
    Task IndexAsync(Jogo jogo, CancellationToken ct = default);

    Task DeleteAsync(Guid jogoId, CancellationToken ct = default);

    Task<IReadOnlyList<JogoSearchDocument>> SearchAsync(string termo, CancellationToken ct = default);
}
