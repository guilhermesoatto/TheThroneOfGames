using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TheThroneOfGames.API.Models.DTO;

namespace GameStore.Catalogo.API.Tests;

[TestFixture]
public class AdminGameManagementTests : IDisposable
{
    private readonly CatalogoWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AdminGameManagementTests()
    {
        _factory = new CatalogoWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    private async Task<string> GetAdminToken()
    {
        var response = await _client.PostAsJsonAsync("/api/Usuario/login", new LoginDTO
        {
            Email = "admin@test.com",
            Password = "Admin@123!"
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Admin login failed");

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ContainsKey("token"), "Token not found in response");

        return result["token"];
    }

    private void SetAuthToken(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [Test]
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
        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var createdGame = await createResponse.Content.ReadFromJsonAsync<GameDTO>();
        Assert.That(createdGame, Is.Not.Null);
        Assert.That(createdGame!.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(createdGame.Name, Is.EqualTo(newGame.Name));

        // Act - Update game
        var updateGame = new GameDTO
        {
            Name = "Updated Game",
            Genre = "RPG",
            Price = 39.99m
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/admin/game/{createdGame.Id}", updateGame);

        // Assert - Update successful
        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updatedGame = await updateResponse.Content.ReadFromJsonAsync<GameDTO>();
        Assert.That(updatedGame.Name, Is.EqualTo(updateGame.Name));
        Assert.That(updatedGame.Genre, Is.EqualTo(updateGame.Genre));
        Assert.That(updatedGame.Price, Is.EqualTo(updateGame.Price));

        // Act - Delete game (soft delete - marca como indisponível)
        var deleteResponse = await _client.DeleteAsync($"/api/admin/game/{createdGame.Id}");

        // Assert - Delete successful
        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        // Verify game still exists but is unavailable (soft delete)
        var getResponse = await _client.GetAsync($"/api/admin/game/{createdGame.Id}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var deletedGame = await getResponse.Content.ReadFromJsonAsync<GameDTO>();
        Assert.That(deletedGame, Is.Not.Null);
        Assert.That(deletedGame!.IsAvailable, Is.False, "Jogo deveria estar indisponível após delete");
    }

    [Test]
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
        Assert.That(preRegisterResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Need to wait for the email file to be written
        await Task.Delay(100);

        // Get activation token from email
        var emailFiles = Directory.GetFiles(outboxPath, "*.eml");
        Assert.That(emailFiles, Has.Length.EqualTo(1), "Deveria ter exatamente 1 email de ativação");
        var emailContent = await File.ReadAllTextAsync(emailFiles[0]);

        var activationTokenStart = emailContent.IndexOf("activationToken=") + "activationToken=".Length;
        var activationTokenEnd = emailContent.IndexOf("\n", activationTokenStart);
        var activationToken = emailContent.Substring(activationTokenStart, activationTokenEnd - activationTokenStart).Trim();

        // Activate the account
        var activateResponse = await _client.PostAsync($"/api/Usuario/activate?activationToken={activationToken}", null);
        Assert.That(activateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var loginResponse = await _client.PostAsJsonAsync("/api/Usuario/login", new LoginDTO
        {
            Email = user.Email,
            Password = user.Password
        });

        var result = await loginResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.That(result, Is.Not.Null);
        SetAuthToken(result!["token"]);

        // Act & Assert - Try to access admin endpoints
        var createResponse = await _client.PostAsJsonAsync("/api/admin/game", new GameDTO
        {
            Name = "Test Game",
            Genre = "Action",
            Price = 29.99m
        });

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        var getResponse = await _client.GetAsync("/api/admin/game");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
