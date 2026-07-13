namespace GameStore.Partidas.Application.Ports;

/// <summary>
/// Porta de saída para o bounded context Usuários — primeira chamada síncrona entre
/// bounded contexts deste sistema (ver docs/ai/tasks/prd-partidas.json
/// designDecisions.posseDoJogo). A implementação concreta (Infrastructure) chama
/// GET /api/usuario/possui-jogo/{jogoId} via HttpClient, repassando o Bearer token de quem
/// fez a requisição original a Partidas (Usuários identifica o jogador pelo próprio JWT).
/// </summary>
public interface IUsuariosGateway
{
    Task<bool> PossuiJogoAsync(Guid jogoId, string bearerToken, CancellationToken ct = default);
}
