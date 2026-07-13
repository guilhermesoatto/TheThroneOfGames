namespace GameStore.Partidas.Domain.ValueObjects;

/// <summary>
/// Ciclo de vida de uma SolicitacaoBusca (um jogador procurando partida). Volta para
/// Aguardando (uma nova solicitação) quando a Partida pareada é desfeita por desistência —
/// ver Partida.Desistir().
/// </summary>
public enum StatusSolicitacao
{
    Aguardando = 0,
    Pareada = 1,
    Cancelada = 2,
}
