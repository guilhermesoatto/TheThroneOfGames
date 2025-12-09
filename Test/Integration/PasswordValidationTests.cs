using System.Net;
using System.Net.Http.Json;
using TheThroneOfGames.API.Models.DTO;

namespace Test.Integration;

[TestFixture]
public class PasswordValidationTests : IDisposable
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _outboxPath;

    public PasswordValidationTests()
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

    [TestCase("short", "Password must be at least 8 characters")]
    [TestCase("onlyletters", "Password must contain at least one number and one special character")]
    [TestCase("12345678", "Password must contain at least one letter and one special character")]
    [TestCase("!@#$%^&*", "Password must contain at least one letter and one number")]
    [TestCase("Password123", "Password must contain at least one special character")]
    [TestCase("Pass!@#$", "Password must contain at least one number")]
    [TestCase("1234!@#$", "Password must contain at least one letter")]
    public async Task PreRegister_WithInvalidPassword_ReturnsBadRequest(string password, string expectedError)
    {
        // Arrange
        var user = new UserDTO
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = password,
            Role = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Usuario/pre-register", user);
        var responseBody = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(responseBody, Contains.Substring(expectedError));

        // Verify no email file was created
        var emailFiles = Directory.GetFiles(_outboxPath, "*.eml");
        Assert.That(emailFiles, Has.Length.Zero, "No email file should have been created for invalid password");
    }

    [TestCase("P@ssw0rd")]
    [TestCase("Str0ng!Pass")]
    [TestCase("C0mpl3x!")]
    [TestCase("MyP@ss123")]
    [TestCase("!2Pass3$")]
    public async Task PreRegister_WithValidPassword_ReturnsOk(string password)
    {
        // Arrange
        var user = new UserDTO
        {
            Name = "Test User",
            Email = $"test_{Guid.NewGuid()}@example.com", // Unique email for each test
            Password = password,
            Role = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Usuario/pre-register", user);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), 
            $"Failed for password: {password}. Response: {await response.Content.ReadAsStringAsync()}");

        // Verify email file was created
        var emailFiles = Directory.GetFiles(_outboxPath, "*.eml");
        Assert.That(emailFiles, Has.Length.EqualTo(1), 
            $"Email file should have been created for valid password: {password}");
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