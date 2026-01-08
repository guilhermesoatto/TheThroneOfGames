using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TheThroneOfGames.API.Models.DTO;

namespace Test.Integration;

[TestFixture]
public class AuthorizationTests : IDisposable
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _outboxPath;

    private string _userToken;
    private string _adminToken;

    public AuthorizationTests()
    {
        _factory = new CustomWebApplicationFactory<Program>();
        _client = _factory.CreateClient();

        // Set up the outbox path
        _outboxPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Infrastructure", "Outbox"));
        if (!Directory.Exists(_outboxPath))
        {
            Directory.CreateDirectory(_outboxPath);
        }
    }

    [SetUp]
    public async Task SetUp()
    {
        // Clean up any existing test emails
        foreach (var file in Directory.GetFiles(_outboxPath, "*.eml"))
        {
            File.Delete(file);
        }

        // Generate unique emails per test run to avoid conflicts
        var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
        var testUserEmail = $"testuser{uniqueId}@example.com";
        var testAdminEmail = $"testadmin{uniqueId}@example.com";

        // Create and activate test users with unique emails
        await CreateAndActivateUser(testUserEmail, "TestUser", "User");
        await CreateAndActivateUser(testAdminEmail, "TestAdmin", "Admin");

        // Get tokens for both users
        _userToken = await GetUserToken(testUserEmail, "Test@123!");
        _adminToken = await GetUserToken(testAdminEmail, "Test@123!");
    }

    private async Task CreateAndActivateUser(string email, string name, string role)
    {
        var user = new UserDTO
        {
            Name = name,
            Email = email,
            Password = "Test@123!",
            Role = role
        };

        var preRegisterResponse = await _client.PostAsJsonAsync("/api/Usuario/pre-register", user);
        Assert.That(preRegisterResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        await Task.Delay(100); // Wait for email

        var emailFiles = Directory.GetFiles(_outboxPath, "*.eml");
        var emailContent = await File.ReadAllTextAsync(emailFiles[0]);
        var activationTokenStart = emailContent.IndexOf("activationToken=") + "activationToken=".Length;
        var activationTokenEnd = emailContent.IndexOf("\n", activationTokenStart);
        var activationToken = emailContent.Substring(activationTokenStart, activationTokenEnd - activationTokenStart).Trim();

        await _client.PostAsync($"/api/Usuario/activate?activationToken={activationToken}", null);

        // Clean up email
        File.Delete(emailFiles[0]);
    }

    private async Task<string> GetUserToken(string email, string password)
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/Usuario/login", new LoginDTO
        {
            Email = email,
            Password = password
        });

        var result = await loginResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.IsNotNull(result);
        return result["token"];
    }

    [Test]
    public async Task AdminEndpoint_WithAdminToken_ReturnsOk()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        // Act
        var response = await _client.GetAsync("/api/admin/game");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task AdminEndpoint_WithUserToken_ReturnsForbidden()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _userToken);

        // Act
        var response = await _client.GetAsync("/api/admin/game");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task UserEndpoint_WithUserToken_ReturnsOk()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _userToken);

        // Act
        var response = await _client.GetAsync("/api/game");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task UserEndpoint_WithAdminToken_ReturnsOk()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        // Act
        var response = await _client.GetAsync("/api/game");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task PublicEndpoint_WithoutToken_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/Usuario/public-info");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task ModifyOwnProfile_AllowedForOwnUser()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _userToken);
        var updateDto = new UserUpdateDTO
        {
            Name = "Updated Test User",
            Email = "testuser@example.com" // Same email as original user
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/Usuario/profile", updateDto);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task ModifyOtherProfile_ForbiddenForNonAdmin()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _userToken);
        var updateDto = new UserUpdateDTO
        {
            Name = "Try to update admin",
            Email = "testadmin@example.com" // Try to update admin's profile
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/Usuario/profile", updateDto);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    public void Dispose()
    {
        // Clean up email files
        if (Directory.Exists(_outboxPath))
        {
            foreach (var file in Directory.GetFiles(_outboxPath, "*.eml"))
            {
                File.Delete(file);
            }
        }

        _client.Dispose();
        _factory.Dispose();
    }
}