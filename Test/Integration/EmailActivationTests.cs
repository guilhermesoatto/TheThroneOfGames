using System.Net;
using System.Net.Http.Json;
using TheThroneOfGames.API.Models.DTO;
using Microsoft.Extensions.DependencyInjection;
using TheThroneOfGames.Infrastructure.Persistence;
using System.IO;

namespace Test.Integration;

[TestFixture]
public class EmailActivationTests : IDisposable
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _outboxPath;

    public EmailActivationTests()
    {
        _factory = new CustomWebApplicationFactory<Program>();
        _client = _factory.CreateClient();

        // Set up the outbox path (same as EmailService)
        _outboxPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Infrastructure", "Outbox"));
        if (!Directory.Exists(_outboxPath))
        {
            Directory.CreateDirectory(_outboxPath);
        }
        else
        {
            // Clean up any existing test emails
            foreach (var file in Directory.GetFiles(_outboxPath, "*.eml"))
            {
                File.Delete(file);
            }
        }
    }

    [Test]
    public async Task PreRegister_ValidUser_SendsActivationEmail()
    {
        // Arrange
        var user = new UserDTO
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = "P@ssw0rd!",
            Role = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Usuario/pre-register", user);

        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Expected OK but got {response.StatusCode}. Response body: {body}");
        }

        // Wait briefly for the file to be written
        await Task.Delay(100);

        // Verify email file exists
        var emailFiles = Directory.GetFiles(_outboxPath, "*.eml");
        Assert.That(emailFiles, Has.Length.EqualTo(1), "An email file should have been created");

        // Read email content
        var emailContent = await File.ReadAllTextAsync(emailFiles[0]);

        // Verify email content
        Assert.That(emailContent, Contains.Substring($"To: {user.Email}"));
        Assert.That(emailContent, Contains.Substring("Subject: Ativação de conta - TheThroneOfGames"));
        Assert.That(emailContent, Contains.Substring("Olá Test User"));
        Assert.That(emailContent, Contains.Substring("/api/Usuario/activate?activationToken="));

        // Extract activation token from email
        var activationTokenStart = emailContent.IndexOf("activationToken=") + "activationToken=".Length;
        var activationTokenEnd = emailContent.IndexOf("\n", activationTokenStart);
        var activationToken = emailContent.Substring(activationTokenStart, activationTokenEnd - activationTokenStart).Trim();

        // Verify we can activate the account with the token
        var activationResponse = await _client.PostAsync($"/api/Usuario/activate?activationToken={activationToken}", null);
        Assert.That(activationResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Verify we can now login
        var loginResponse = await _client.PostAsJsonAsync("/api/Usuario/login", new LoginDTO
        {
            Email = user.Email,
            Password = user.Password
        });

        Assert.That(loginResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    var tokenDict = await loginResponse.Content.ReadFromJsonAsync<System.Collections.Generic.Dictionary<string, string>>();
    Assert.IsNotNull(tokenDict, "Login response JSON was null");
    Assert.IsTrue(tokenDict.TryGetValue("token", out var token) && !string.IsNullOrEmpty(token), "Token was not returned in login response");
    }

    [Test]
    public async Task PreRegister_InvalidPassword_DoesNotCreateEmailFile()
    {
        // Arrange
        var user = new UserDTO
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = "weak", // Too weak
            Role = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Usuario/pre-register", user);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        // Verify no email file was created
        var emailFiles = Directory.GetFiles(_outboxPath, "*.eml");
        Assert.That(emailFiles, Has.Length.Zero, "No email file should have been created");
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