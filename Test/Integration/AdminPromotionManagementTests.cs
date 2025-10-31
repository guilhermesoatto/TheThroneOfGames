using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TheThroneOfGames.API.Models.DTO;

namespace Test.Integration;

[TestFixture]
public class AdminPromotionManagementTests : IDisposable
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AdminPromotionManagementTests()
    {
        _factory = new CustomWebApplicationFactory<Program>();
        _client = _factory.CreateClient();

        // Clean up any existing email files
        var outboxPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Infrastructure", "Outbox"));
        if (Directory.Exists(outboxPath))
        {
            foreach (var file in Directory.GetFiles(outboxPath, "*.eml"))
            {
                File.Delete(file);
            }
        }
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
        Assert.IsNotNull(result);
        Assert.That(result.ContainsKey("token"), "Token not found in response");

        return result["token"];
    }

    private void SetAuthToken(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [Test]
    public async Task AdminCanCreateAndUpdatePromotion()
    {
        // Arrange - Get admin token
        var token = await GetAdminToken();
        SetAuthToken(token);

        var newPromotion = new PromotionDTO
        {
            Discount = 0.25m,
            ValidUntil = DateTime.UtcNow.AddDays(30)
        };

        // Act - Create promotion
        var createResponse = await _client.PostAsJsonAsync("/api/admin/promotion", newPromotion);

        // Assert - Creation successful
        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var createdPromotion = await createResponse.Content.ReadFromJsonAsync<PromotionDTO>();
        Assert.IsNotNull(createdPromotion?.Id);
        Assert.That(createdPromotion.Discount, Is.EqualTo(newPromotion.Discount));

        // Act - Update promotion
        var updatePromotion = new PromotionDTO
        {
            Discount = 0.5m,
            ValidUntil = DateTime.UtcNow.AddDays(60)
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/admin/promotion/{createdPromotion.Id}", updatePromotion);

        // Assert - Update successful
        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updatedPromotion = await updateResponse.Content.ReadFromJsonAsync<PromotionDTO>();
        Assert.That(updatedPromotion.Discount, Is.EqualTo(updatePromotion.Discount));
        Assert.That(updatedPromotion.ValidUntil.Date, Is.EqualTo(updatePromotion.ValidUntil.Date));

        // Act - Delete promotion
        var deleteResponse = await _client.DeleteAsync($"/api/admin/promotion/{createdPromotion.Id}");

        // Assert - Delete successful
        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        // Verify promotion is deleted
        var getResponse = await _client.GetAsync($"/api/admin/promotion/{createdPromotion.Id}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task NonAdminCannotAccessPromotionManagement()
    {
        // Arrange - Create and login as regular user
        var user = new UserDTO
        {
            Name = "Regular User",
            Email = "user@test.com",
            Password = "User@123!",
            Role = "User"
        };

        var preRegisterResponse = await _client.PostAsJsonAsync("/api/Usuario/pre-register", user);
        Assert.That(preRegisterResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Need to wait for the email file to be written
        await Task.Delay(100);

        // Get activation token from email
        var emailFiles = Directory.GetFiles(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Infrastructure", "Outbox")), "*.eml");
        Assert.That(emailFiles, Has.Length.EqualTo(1));
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
        Assert.IsNotNull(result);
        SetAuthToken(result["token"]);

        // Act & Assert - Try to access admin endpoints
        var createResponse = await _client.PostAsJsonAsync("/api/admin/promotion", new PromotionDTO
        {
            Discount = 0.25m,
            ValidUntil = DateTime.UtcNow.AddDays(30)
        });

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        var getResponse = await _client.GetAsync("/api/admin/promotion");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    public void Dispose()
    {
        // Clean up email files
        var outboxPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Infrastructure", "Outbox"));
        if (Directory.Exists(outboxPath))
        {
            foreach (var file in Directory.GetFiles(outboxPath, "*.eml"))
            {
                File.Delete(file);
            }
        }

        _client.Dispose();
        _factory.Dispose();
    }
}