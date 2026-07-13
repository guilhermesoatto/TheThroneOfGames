using GameStore.Partidas.Domain.Shared;

namespace GameStore.Partidas.Domain.ValueObjects;

/// <summary>
/// Os jogadores de um lado (Equipe A ou B) de uma Partida. Imutável — cada operação retorna
/// uma nova instância. Teto estrutural de 5 jogadores (ver docs/ai/tasks/prd-partidas.json
/// designDecisions.equipe): o fluxo atual (1v1 imediato) nunca chega a usar mais que 1 slot,
/// o teto existe para uma evolução futura (times maiores) sem quebrar o modelo.
/// </summary>
public sealed record EquipeSlots
{
    public const int LimiteMaximo = 5;

    private readonly IReadOnlyList<Guid> _jogadores;

    public IReadOnlyList<Guid> Jogadores => _jogadores;

    private EquipeSlots(IReadOnlyList<Guid> jogadores)
    {
        _jogadores = jogadores;
    }

    public static EquipeSlots Vazia() => new(Array.Empty<Guid>());

    public static EquipeSlots ComUmJogador(Guid jogadorId) => new(new[] { jogadorId });

    /// <summary>Reconstrói uma instância a partir de dados persistidos — só para uso de Infrastructure (repositórios) na mesma assembly.</summary>
    internal static EquipeSlots Reidratar(IEnumerable<Guid> jogadores) => new(jogadores.ToList());

    public bool Contem(Guid jogadorId) => _jogadores.Contains(jogadorId);

    public Result<EquipeSlots> Adicionar(Guid jogadorId)
    {
        if (_jogadores.Count >= LimiteMaximo)
            return new EquipeCheiaError(LimiteMaximo);

        if (Contem(jogadorId))
            return this;

        var novaLista = new List<Guid>(_jogadores) { jogadorId };
        return new EquipeSlots(novaLista);
    }
}
