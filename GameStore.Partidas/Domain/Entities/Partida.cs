using GameStore.Common.Events;
using GameStore.Partidas.Domain.Shared;
using GameStore.Partidas.Domain.ValueObjects;

namespace GameStore.Partidas.Domain.Entities;

/// <summary>
/// Aggregate root do matchmaking. Identidade técnica é Id (Guid) — "jogo + equipeA + equipeB +
/// partida" do pedido original é modelado como o conjunto de atributos que descreve a partida
/// (JogoId + EquipeA + EquipeB), não uma chave composta literal (ver
/// docs/ai/tasks/prd-partidas.json designDecisions.identidade).
///
/// Trigger de match é 1v1 imediato: cada equipe nasce com exatamente 1 jogador (ver
/// designDecisions.triggerDeMatch e EquipeSlots — o teto de 5/time é estrutural, não usado
/// neste fluxo). Sem timeout automático: só volta pra Cancelada se Desistir() for chamado
/// explicitamente antes de todos confirmarem (designDecisions.confirmacao).
/// </summary>
public class Partida
{
    public Guid Id { get; private set; }
    public Guid JogoId { get; private set; }
    public DateTime DataHora { get; private set; }
    public StatusPartida Status { get; private set; }
    public EquipeSlots EquipeA { get; private set; } = null!;
    public EquipeSlots EquipeB { get; private set; } = null!;

    private readonly HashSet<Guid> _confirmados = new();
    public IReadOnlyCollection<Guid> Confirmados => _confirmados;

    private Partida() { } // serialização (Mongo driver / testes)

    private Partida(Guid jogoId, Guid jogadorAId, Guid jogadorBId)
    {
        Id = Guid.NewGuid();
        JogoId = jogoId;
        DataHora = DateTime.UtcNow;
        Status = StatusPartida.AguardandoConfirmacao;
        EquipeA = EquipeSlots.ComUmJogador(jogadorAId);
        EquipeB = EquipeSlots.ComUmJogador(jogadorBId);
    }

    /// <summary>Forma uma nova partida 1v1 a partir de dois jogadores já pareados pelo motor de matchmaking.</summary>
    public static (Partida Partida, PartidaEncontradaEvent Evento) Formar(Guid jogoId, Guid jogadorAId, Guid jogadorBId)
    {
        var partida = new Partida(jogoId, jogadorAId, jogadorBId);
        var evento = new PartidaEncontradaEvent(partida.Id, jogoId, jogadorAId, jogadorBId);
        return (partida, evento);
    }

    private bool PertenceAPartida(Guid jogadorId) => EquipeA.Contem(jogadorId) || EquipeB.Contem(jogadorId);

    private IReadOnlyList<Guid> TodosOsJogadores() =>
        EquipeA.Jogadores.Concat(EquipeB.Jogadores).ToList();

    /// <summary>
    /// Registra a confirmação de um jogador. Só produz PartidaConfirmadaEvent quando TODOS os
    /// jogadores já confirmaram — enquanto isso, retorna Success(null) (ainda aguardando).
    /// </summary>
    public Result<PartidaConfirmadaEvent?> Confirmar(Guid jogadorId)
    {
        if (Status != StatusPartida.AguardandoConfirmacao)
            return new PartidaNaoEstaAguardandoConfirmacaoError(Id, Status.ToString());

        if (!PertenceAPartida(jogadorId))
            return new JogadorNaoPertenceAPartidaError(jogadorId, Id);

        _confirmados.Add(jogadorId);

        var todos = TodosOsJogadores();
        if (todos.All(_confirmados.Contains))
        {
            Status = StatusPartida.Confirmada;
            return Result<PartidaConfirmadaEvent?>.Success(new PartidaConfirmadaEvent(Id, JogoId, todos));
        }

        return Result<PartidaConfirmadaEvent?>.Success(null);
    }

    /// <summary>
    /// Desfaz a partida por desistência explícita. Retorna o evento de cancelamento e o id do
    /// jogador que NÃO desistiu — quem chama este método (o use case) é responsável por criar
    /// uma nova SolicitacaoBusca(Aguardando) para ele e tentar um novo pareamento.
    /// </summary>
    public Result<(PartidaCanceladaEvent Evento, Guid JogadorRestanteId)> Desistir(Guid jogadorId)
    {
        if (Status != StatusPartida.AguardandoConfirmacao)
            return new PartidaNaoEstaAguardandoConfirmacaoError(Id, Status.ToString());

        if (!PertenceAPartida(jogadorId))
            return new JogadorNaoPertenceAPartidaError(jogadorId, Id);

        Status = StatusPartida.Cancelada;
        var jogadorRestanteId = TodosOsJogadores().First(id => id != jogadorId);
        var evento = new PartidaCanceladaEvent(Id, JogoId, jogadorId);

        return (evento, jogadorRestanteId);
    }

    /// <summary>Reconstrói uma instância a partir de dados persistidos — só para uso de Infrastructure (repositórios) na mesma assembly.</summary>
    internal static Partida Reidratar(
        Guid id, Guid jogoId, DateTime dataHora, StatusPartida status,
        EquipeSlots equipeA, EquipeSlots equipeB, IEnumerable<Guid> confirmados)
    {
        var partida = new Partida
        {
            Id = id,
            JogoId = jogoId,
            DataHora = dataHora,
            Status = status,
            EquipeA = equipeA,
            EquipeB = equipeB,
        };
        foreach (var jogadorId in confirmados)
            partida._confirmados.Add(jogadorId);

        return partida;
    }
}
