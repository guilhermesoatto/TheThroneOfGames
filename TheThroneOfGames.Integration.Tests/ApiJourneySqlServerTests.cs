using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TheThroneOfGames.Integration.Tests;

/// <summary>
/// Jornada HTTP completa contra a API real + SQL Server real (sem troca de provider):
/// Program.cs roda `Database.Migrate()` no banco de teste e todo o fluxo passa por SQL.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class ApiJourneySqlServerTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiJourneySqlServerTests(SqlServerFixture fixture)
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");
            builder.UseSetting("ConnectionStrings:DefaultConnection", fixture.ConnectionString);
            builder.UseSetting("Jwt:Key", "integration-super-secret-signing-key-32-chars-min");
            builder.UseSetting("Jwt:Issuer", "TheThroneOfGamesAPI");
            builder.UseSetting("Jwt:Audience", "TheThroneOfGamesAPIUsers");
        });
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task Register_activate_login_create_game_over_real_sql_server()
    {
        var email = $"admin-{Guid.NewGuid():N}@throneofgames.test";
        const string password = "Str0ng!Pass";

        var preRegister = await _client.PostAsJsonAsync("/api/usuario/pre-register",
            new { name = "Admin IT", email, password, role = "Admin" });
        Assert.Equal(HttpStatusCode.OK, preRegister.StatusCode);
        var token = (await preRegister.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("activationToken").GetString();

        var activate = await _client.PostAsync(
            $"/api/usuario/activate?activationToken={Uri.EscapeDataString(token!)}", content: null);
        Assert.Equal(HttpStatusCode.OK, activate.StatusCode);

        var login = await _client.PostAsJsonAsync("/api/usuario/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var jwt = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var createGame = await _client.PostAsJsonAsync("/api/admin/game",
            new { name = $"Game {Guid.NewGuid():N}", genre = "RPG", price = 149.90m, isAvailable = true });
        Assert.Equal(HttpStatusCode.Created, createGame.StatusCode);

        var list = await _client.GetFromJsonAsync<JsonElement>("/api/admin/game");
        Assert.True(list.GetArrayLength() >= 1);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
