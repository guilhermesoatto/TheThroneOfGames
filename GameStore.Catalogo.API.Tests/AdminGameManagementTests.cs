using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using GameStore.Catalogo.Application.Commands;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace GameStore.Catalogo.API.Tests;

/// <summary>
/// Gera o token JWT localmente (mesma chave de assinatura do appsettings.json do
/// GameStore.Catalogo.API) em vez de chamar POST /api/Usuario/login — essa rota pertence ao
/// GameStore.Usuarios.API, que não faz parte deste host de teste (isolamento de bounded context;
/// ver CLAUDE.md). Antes, este arquivo chamava um endpoint inexistente aqui e todo teste falhava
/// com 404 já na obtenção do token.
/// </summary>
[Trait("Category", "Integration")]
public class AdminGameManagementTests : IClassFixture<IntegrationTestFixture>
{
    private const string JwtKey = "your-super-secret-key-that-is-at-least-32-characters-long!";
    private readonly HttpClient _client;

    public AdminGameManagementTests(IntegrationTestFixture fixture)
    {
        _client = fixture.Client;
    }

    private static string CreateToken(string role)
    {
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(JwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Email, "test-user@test.com"),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private void SetAuthToken(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task AdminCanCreateAndUpdateGame()
    {
        // Arrange
        SetAuthToken(CreateToken("Admin"));

        var newGame = new GameDTO
        {
            Name = "Test Game",
            Genre = "Action",
            Price = 29.99m
        };

        // Act - Create game
        var createResponse = await _client.PostAsJsonAsync("/api/admin/game", newGame);

        // Assert - Creation successful
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createdGame = await createResponse.Content.ReadFromJsonAsync<GameDTO>();
        Assert.NotNull(createdGame);
        Assert.NotEqual(Guid.Empty, createdGame!.Id);
        Assert.Equal(newGame.Name, createdGame.Name);

        // Act - Update game (o corpo precisa ser um UpdateGameCommand — GameController.Update
        // rejeita com 400 se o "gameId" do corpo não bater com o :id da rota)
        var updateCommand = new UpdateGameCommand(
            GameId: createdGame.Id!.Value,
            Name: "Updated Game",
            Genre: "RPG",
            Price: 39.99m);

        var updateResponse = await _client.PutAsJsonAsync($"/api/admin/game/{createdGame.Id}", updateCommand);

        // Assert - Update successful
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedGame = await updateResponse.Content.ReadFromJsonAsync<GameDTO>();
        Assert.NotNull(updatedGame);
        Assert.Equal(updateCommand.Name, updatedGame!.Name);
        Assert.Equal(updateCommand.Genre, updatedGame.Genre);
        Assert.Equal(updateCommand.Price, updatedGame.Price);

        // Act - Delete game (soft delete - marca como indisponível)
        var deleteResponse = await _client.DeleteAsync($"/api/admin/game/{createdGame.Id}");

        // Assert - Delete successful
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify game still exists but is unavailable (soft delete)
        var getResponse = await _client.GetAsync($"/api/admin/game/{createdGame.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var deletedGame = await getResponse.Content.ReadFromJsonAsync<GameDTO>();
        Assert.NotNull(deletedGame);
        Assert.False(deletedGame!.IsAvailable);
    }

    [Fact]
    public async Task NonAdminCannotAccessGameManagement()
    {
        // Arrange - token com role "User" (não-admin)
        SetAuthToken(CreateToken("User"));

        // Act & Assert - Try to access admin endpoints
        var createResponse = await _client.PostAsJsonAsync("/api/admin/game", new GameDTO
        {
            Name = "Test Game",
            Genre = "Action",
            Price = 29.99m
        });

        Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);

        var getResponse = await _client.GetAsync("/api/admin/game");
        Assert.Equal(HttpStatusCode.Forbidden, getResponse.StatusCode);
    }
}
