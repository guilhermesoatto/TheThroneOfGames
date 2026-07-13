using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace GameStore.Partidas.API.Tests;

/// <summary>
/// Prova, contra um MongoDB real (Testcontainers, via PartidaApiTestFixture) e a API HTTP de
/// verdade, que o fluxo de matchmaking 1v1 funciona ponta a ponta: buscar -> parear -> confirmar
/// -> confirmada, e que desistência devolve o outro jogador à fila. IUsuariosGateway é um dublê
/// aqui (ver StubUsuariosGateway) — a integração HTTP real com Usuarios é validada pelo script
/// de aceite (partidas-T08) contra o ambiente completo.
/// </summary>
[Trait("Category", "Integration")]
public class PartidaFluxoCompletoTests : IClassFixture<PartidaApiTestFixture>
{
    private readonly PartidaApiTestFixture _fixture;

    public PartidaFluxoCompletoTests(PartidaApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    private HttpClient ClientComo(Guid jogadorId)
    {
        var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestTokenFactory.CriarToken(jogadorId));
        return client;
    }

    [Fact]
    public async Task Buscar_WhenAlone_ShouldStayAguardando()
    {
        var jogadorId = Guid.NewGuid();
        var jogoId = Guid.NewGuid();
        using var client = ClientComo(jogadorId);

        var response = await client.PostAsJsonAsync("/api/partida/buscar", new { jogoId });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().Should().Be("Aguardando");
        body.GetProperty("partidaId").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Buscar_WhenSomeoneElseWaiting_ShouldFormMatchImmediately()
    {
        var jogoId = Guid.NewGuid();
        var jogadorA = Guid.NewGuid();
        var jogadorB = Guid.NewGuid();

        using var clientA = ClientComo(jogadorA);
        var responseA = await clientA.PostAsJsonAsync("/api/partida/buscar", new { jogoId });
        responseA.EnsureSuccessStatusCode();

        using var clientB = ClientComo(jogadorB);
        var responseB = await clientB.PostAsJsonAsync("/api/partida/buscar", new { jogoId });
        responseB.EnsureSuccessStatusCode();

        var bodyB = await responseB.Content.ReadFromJsonAsync<JsonElement>();
        bodyB.GetProperty("status").GetString().Should().Be("Pareada");
        bodyB.GetProperty("partidaId").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task FluxoCompleto_BothConfirm_ShouldStartMatch()
    {
        var jogoId = Guid.NewGuid();
        var jogadorA = Guid.NewGuid();
        var jogadorB = Guid.NewGuid();

        using var clientA = ClientComo(jogadorA);
        await clientA.PostAsJsonAsync("/api/partida/buscar", new { jogoId });

        using var clientB = ClientComo(jogadorB);
        var responseB = await clientB.PostAsJsonAsync("/api/partida/buscar", new { jogoId });
        var bodyB = await responseB.Content.ReadFromJsonAsync<JsonElement>();
        var partidaId = bodyB.GetProperty("partidaId").GetGuid();

        var confirmA = await clientA.PostAsync($"/api/partida/{partidaId}/confirmar", null);
        var confirmABody = await confirmA.Content.ReadFromJsonAsync<JsonElement>();
        confirmABody.GetProperty("status").GetString().Should().Be("AguardandoConfirmacao");

        var confirmB = await clientB.PostAsync($"/api/partida/{partidaId}/confirmar", null);
        var confirmBBody = await confirmB.Content.ReadFromJsonAsync<JsonElement>();
        confirmBBody.GetProperty("status").GetString().Should().Be("Confirmada");
    }

    [Fact]
    public async Task Desistir_ShouldRequeueRemainingPlayer_WhoStaysAguardando()
    {
        var jogoId = Guid.NewGuid();
        var jogadorA = Guid.NewGuid();
        var jogadorB = Guid.NewGuid();

        using var clientA = ClientComo(jogadorA);
        await clientA.PostAsJsonAsync("/api/partida/buscar", new { jogoId });

        using var clientB = ClientComo(jogadorB);
        var responseB = await clientB.PostAsJsonAsync("/api/partida/buscar", new { jogoId });
        var bodyB = await responseB.Content.ReadFromJsonAsync<JsonElement>();
        var partidaId = bodyB.GetProperty("partidaId").GetGuid();

        var desistir = await clientA.PostAsync($"/api/partida/{partidaId}/desistir", null);
        var desistirBody = await desistir.Content.ReadFromJsonAsync<JsonElement>();
        desistirBody.GetProperty("status").GetString().Should().Be("Cancelada");

        // jogadorB deve ter sido devolvido para uma nova SolicitacaoBusca(Aguardando) — busca de
        // novo (idempotência: se ainda estivesse "Pareada" na antiga, isso teria dado erro).
        var buscarDeNovo = await clientB.PostAsJsonAsync("/api/partida/buscar", new { jogoId });
        buscarDeNovo.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest); // já tem uma Aguardando (a nova, criada pelo Desistir)
    }
}
