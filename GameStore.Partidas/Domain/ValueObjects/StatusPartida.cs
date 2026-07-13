namespace GameStore.Partidas.Domain.ValueObjects;

/// <summary>
/// Ciclo de vida de uma Partida. Sem estado de "timeout" — a transição para Cancelada só
/// acontece por desistência explícita de um jogador (ver docs/ai/tasks/prd-partidas.json
/// designDecisions.confirmacao), nunca por expiração automática de tempo.
/// </summary>
public enum StatusPartida
{
    AguardandoConfirmacao = 0,
    Confirmada = 1,
    Cancelada = 2,
}
