using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TheThroneOfGames.API.Models.DTO;

namespace Test.Integration;

[TestFixture]
public class JwtTokenTests : IDisposable
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _outboxPath;

    public JwtTokenTests()
    {
        _factory = new CustomWebApplicationFactory<Program>();
        _client = _factory.CreateClient();

        // Set up the outbox path (same as EmailService)
        _outboxPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Infrastructure", "Outbox"));
        if (!Directory.Exists(_outboxPath))
        {
            Directory.CreateDirectory(_outboxPath);
        }
    }

    [SetUp]
    public void SetUp()
    {
        // Clean up any existing test emails before each test
        foreach (var file in Directory.GetFiles(_outboxPath, "*.eml"))
        {
            File.Delete(file);
        }
    }

    [Test]
    public async Task JwtToken_ContainsExpectedClaims()
    {
        // Arrange - Create and activate a test user
        var user = new UserDTO
        {
            Name = "Test User",
            Email = "testjwt@example.com",
            Password = "Test@123!",
            Role = "User"
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

        // Act - Login and get token
        var loginResponse = await _client.PostAsJsonAsync("/api/Usuario/login", new LoginDTO
        {
            Email = user.Email,
            Password = user.Password
        });

        var result = await loginResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.IsNotNull(result);
        var token = result["token"];

        // Parse the JWT token
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadToken(token) as JwtSecurityToken;

        // Assert - Verify claims
        Assert.IsNotNull(jsonToken);
        Assert.That(jsonToken.Claims.First(c => c.Type == "email")?.Value, Is.EqualTo(user.Email));
        Assert.That(jsonToken.Claims.First(c => c.Type == "name")?.Value, Is.EqualTo(user.Name));
        Assert.That(jsonToken.Claims.First(c => c.Type == "role")?.Value, Is.EqualTo(user.Role));
        
        // Verify expiration is set in the future
        Assert.That(jsonToken.ValidTo, Is.GreaterThan(DateTime.UtcNow));
        Assert.That(jsonToken.ValidFrom, Is.LessThan(DateTime.UtcNow));
    }

    [Test]
    public async Task ExpiredToken_ReturnsUnauthorized()
    {
        // Arrange - Get an expired token (manipulate the token to be expired)
        var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9." + 
            "eyJzdWIiOiJ0ZXN0QGV4YW1wbGUuY29tIiwibmFtZSI6IlRlc3QgVXNlciIsInJvbGUiOiJVc2VyIiwiZXhwIjoxNTE2MjM5MDIyfQ." +
            "8oF3DgcEYuGF5Xt5FP6CHC7_ZM5FYHqXhJcwg3FR0P0";

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        // Act - Try to access a protected endpoint
        var response = await _client.GetAsync("/api/admin/game");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task InvalidToken_ReturnsUnauthorized()
    {
        // Arrange - Set an invalid token
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.token.here");

        // Act - Try to access a protected endpoint
        var response = await _client.GetAsync("/api/admin/game");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task MissingToken_ReturnsUnauthorized()
    {
        // Act - Try to access a protected endpoint without a token
        var response = await _client.GetAsync("/api/admin/game");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
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