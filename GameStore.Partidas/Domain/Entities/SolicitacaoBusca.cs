using GameStore.Partidas.Domain.ValueObjects;

namespace GameStore.Partidas.Domain.Entities;

/// <summary>
/// Um jogador procurando partida para um jogo específico — a "fila de espera" do matchmaking.
/// Volta para Aguardando (uma nova instância) quando a Partida pareada é desfeita por
/// desistência do outro jogador — ver Partida.Desistir().
/// </summary>
public class SolicitacaoBusca
{
    public Guid Id { get; private set; }
    public Guid JogadorId { get; private set; }
    public Guid JogoId { get; private set; }
    public StatusSolicitacao Status { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public Guid? PartidaId { get; private set; }

    private SolicitacaoBusca() { } // serialização (Mongo driver / testes)

    public SolicitacaoBusca(Guid jogadorId, Guid jogoId)
    {
        if (jogadorId == Guid.Empty)
            throw new ArgumentException("JogadorId é obrigatório", nameof(jogadorId));
        if (jogoId == Guid.Empty)
            throw new ArgumentException("JogoId é obrigatório", nameof(jogoId));

        Id = Guid.NewGuid();
        JogadorId = jogadorId;
        JogoId = jogoId;
        Status = StatusSolicitacao.Aguardando;
        CriadoEm = DateTime.UtcNow;
        PartidaId = null;
    }

    public void Parear(Guid partidaId)
    {
        Status = StatusSolicitacao.Pareada;
        PartidaId = partidaId;
    }

    public void Cancelar()
    {
        Status = StatusSolicitacao.Cancelada;
    }

    /// <summary>Reconstrói uma instância a partir de dados persistidos — só para uso de Infrastructure (repositórios) na mesma assembly.</summary>
    internal static SolicitacaoBusca Reidratar(
        Guid id, Guid jogadorId, Guid jogoId, StatusSolicitacao status, DateTime criadoEm, Guid? partidaId)
    {
        return new SolicitacaoBusca
        {
            Id = id,
            JogadorId = jogadorId,
            JogoId = jogoId,
            Status = status,
            CriadoEm = criadoEm,
            PartidaId = partidaId,
        };
    }
}
