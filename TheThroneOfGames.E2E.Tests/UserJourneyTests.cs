using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace TheThroneOfGames.E2E.Tests;

/// <summary>
/// Jornada ponta-a-ponta do monólito, espelhando os fluxos de demonstração
/// (commit 571f26a) e o roteiro do vídeo de entrega (tools/record-delivery.js):
/// registrar -> ativar -> logar -> criar jogo (admin) -> listar.
/// Asserções em xUnit nativo (sem FluentAssertions — POC não depende de lib paga).
/// </summary>
public sealed class UserJourneyTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UserJourneyTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Admin_registers_activates_logs_in_and_manages_a_game()
    {
        var email = $"admin-{Guid.NewGuid():N}@throneofgames.test";
        const string password = "Str0ng!Pass";

        // 1. Pré-registro -> devolve o token de ativação (nenhum outro canal o entrega nesta fase).
        var preRegister = await _client.PostAsJsonAsync("/api/usuario/pre-register", new
        {
            name = "Admin Referee",
            email,
            password,
            role = "Admin"
        });
        Assert.Equal(HttpStatusCode.OK, preRegister.StatusCode);
        var activationToken = (await preRegister.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("activationToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(activationToken));

        // 2. Ativação da conta via token (query string).
        var activate = await _client.PostAsync(
            $"/api/usuario/activate?activationToken={Uri.EscapeDataString(activationToken!)}", content: null);
        Assert.Equal(HttpStatusCode.OK, activate.StatusCode);

        // 3. Login -> JWT com role Admin.
        var login = await _client.PostAsJsonAsync("/api/usuario/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var loginBody = await login.Content.ReadFromJsonAsync<JsonElement>();
        var jwt = loginBody.GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(jwt));
        Assert.Equal("Admin", loginBody.GetProperty("role").GetString());

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        // 4. Criação de jogo no painel admin -> 201 Created.
        var createGame = await _client.PostAsJsonAsync("/api/admin/game", new
        {
            name = "Elden Ring",
            genre = "RPG",
            price = 199.90m,
            description = "Souls-like open world",
            isAvailable = true
        });
        Assert.Equal(HttpStatusCode.Created, createGame.StatusCode);
        var createdId = (await createGame.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, createdId);

        // 5. Listagem admin contém o jogo recém-criado.
        var adminList = await _client.GetFromJsonAsync<JsonElement>("/api/admin/game");
        Assert.Contains("Elden Ring", adminList.EnumerateArray().Select(g => g.GetProperty("name").GetString()));

        // 6. Catálogo acessível a usuário autenticado.
        var catalog = await _client.GetAsync("/api/game");
        Assert.Equal(HttpStatusCode.OK, catalog.StatusCode);
    }

    [Fact]
    public async Task Admin_endpoints_reject_anonymous_callers()
    {
        // _client deste teste não tem header Authorization (instância nova por método de teste).
        var response = await _client.GetAsync("/api/admin/game");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_wrong_password_is_rejected()
    {
        var email = $"user-{Guid.NewGuid():N}@throneofgames.test";
        const string password = "Str0ng!Pass";

        var preRegister = await _client.PostAsJsonAsync("/api/usuario/pre-register", new
        {
            name = "Casual Player",
            email,
            password,
            role = "User"
        });
        Assert.Equal(HttpStatusCode.OK, preRegister.StatusCode);
        var token = (await preRegister.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("activationToken").GetString();
        await _client.PostAsync($"/api/usuario/activate?activationToken={Uri.EscapeDataString(token!)}", content: null);

        var login = await _client.PostAsJsonAsync("/api/usuario/login", new { email, password = "wrong-password" });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }
}
