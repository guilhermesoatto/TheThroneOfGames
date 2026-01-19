using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;
using TheThroneOfGames.API.Models.DTO;

namespace GameStore.Catalogo.API.Tests;

public class AdminGameManagementTests : IClassFixture<IntegrationTestFixture>
{
    private readonly HttpClient _client;

    public AdminGameManagementTests(IntegrationTestFixture fixture)
    {
        _client = fixture.Client;
    }

    private async Task<string> GetAdminToken()
    {
        var response = await _client.PostAsJsonAsync("/api/Usuario/login", new LoginDTO
        {
            Email = "admin@test.com",
            Password = "Admin@123!"
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(result);
        Assert.True(result.ContainsKey("token"), "Token not found in response");

        return result["token"];
    }

    private void SetAuthToken(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task AdminCanCreateAndUpdateGame()
    {
        // Arrange - Get admin token
        var token = await GetAdminToken();
        SetAuthToken(token);

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

        // Act - Update game
        var updateGame = new GameDTO
        {
            Name = "Updated Game",
            Genre = "RPG",
            Price = 39.99m
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/admin/game/{createdGame.Id}", updateGame);

        // Assert - Update successful
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedGame = await updateResponse.Content.ReadFromJsonAsync<GameDTO>();
        Assert.NotNull(updatedGame);
        Assert.Equal(updateGame.Name, updatedGame!.Name);
        Assert.Equal(updateGame.Genre, updatedGame.Genre);
        Assert.Equal(updateGame.Price, updatedGame.Price);

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
        // Limpar emails antigos antes do teste
        var outboxPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Infrastructure", "Outbox"));
        if (Directory.Exists(outboxPath))
        {
            foreach (var file in Directory.GetFiles(outboxPath, "*.eml"))
            {
                File.Delete(file);
            }
        }

        // Arrange - Create and login as regular user
        var user = new UserDTO
        {
            Name = "Regular User",
            Email = "user@test.com",
            Password = "User@123!",
            Role = "User"
        };

        var preRegisterResponse = await _client.PostAsJsonAsync("/api/Usuario/register", user);
        Assert.Equal(HttpStatusCode.OK, preRegisterResponse.StatusCode);

        // Need to wait for the email file to be written
        await Task.Delay(100);

        // Get activation token from email
        var emailFiles = Directory.GetFiles(outboxPath, "*.eml");
        Assert.Single(emailFiles);
        var emailContent = await File.ReadAllTextAsync(emailFiles[0]);

        var activationTokenStart = emailContent.IndexOf("activationToken=") + "activationToken=".Length;
        var activationTokenEnd = emailContent.IndexOf("\n", activationTokenStart);
        var activationToken = emailContent.Substring(activationTokenStart, activationTokenEnd - activationTokenStart).Trim();

        // Activate the account
        var activateResponse = await _client.PostAsync($"/api/Usuario/activate?activationToken={activationToken}", null);
        Assert.Equal(HttpStatusCode.OK, activateResponse.StatusCode);

        var loginResponse = await _client.PostAsJsonAsync("/api/Usuario/login", new LoginDTO
        {
            Email = user.Email,
            Password = user.Password
        });

        var result = await loginResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(result);
        SetAuthToken(result!["token"]);

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
