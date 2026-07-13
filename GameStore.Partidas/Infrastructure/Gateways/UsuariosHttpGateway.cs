using System.Net.Http.Json;
using System.Text.Json;
using GameStore.Partidas.Application.Ports;
using Microsoft.Extensions.Logging;

namespace GameStore.Partidas.Infrastructure.Gateways;

/// <summary>
/// Implementação concreta de IUsuariosGateway — primeira chamada HTTP síncrona entre bounded
/// contexts deste sistema (ver docs/ai/tasks/prd-partidas.json designDecisions.posseDoJogo).
/// Repassa o Bearer token de quem chamou Partidas, porque GET /api/usuario/possui-jogo/{jogoId}
/// identifica o jogador pelo próprio JWT (claim "sub") — Partidas não duplica essa lógica.
/// </summary>
public class UsuariosHttpGateway : IUsuariosGateway
{
    // A resposta de GET /api/usuario/possui-jogo/{jogoId} é serializada com o
    // JsonSerializerDefaults.Web do ASP.NET Core (camelCase: "possuiJogo") — o cliente precisa
    // do mesmo case-insensitivity, senão a propriedade fica sempre no default (false).
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ILogger<UsuariosHttpGateway> _logger;

    public UsuariosHttpGateway(HttpClient httpClient, ILogger<UsuariosHttpGateway> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> PossuiJogoAsync(Guid jogoId, string bearerToken, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/usuario/possui-jogo/{jogoId}");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "GameStore.Usuarios respondeu {StatusCode} para possui-jogo/{JogoId} — tratando como 'não possui'",
                response.StatusCode, jogoId);
            return false;
        }

        var body = await response.Content.ReadFromJsonAsync<PossuiJogoResponse>(JsonOptions, ct);
        return body?.PossuiJogo ?? false;
    }

    private sealed record PossuiJogoResponse(bool PossuiJogo);
}
