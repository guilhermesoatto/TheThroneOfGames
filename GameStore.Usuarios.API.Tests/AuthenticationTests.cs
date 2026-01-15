using System.Net;
using System.Net.Http.Json;
using TheThroneOfGames.API.Models.DTO;

namespace GameStore.Usuarios.API.Tests;

[TestFixture]
public class AuthenticationTests : IDisposable
{
    private readonly UsuariosWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly string _outboxPath;

    public AuthenticationTests()
    {
        _factory = new UsuariosWebApplicationFactory();
        _client = _factory.CreateClient();
        _outboxPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Infrastructure", "Outbox"));
    }

    [SetUp]
    public void Setup()
    {
        // Limpar emails antes de cada teste
        if (Directory.Exists(_outboxPath))
        {
            foreach (var file in Directory.GetFiles(_outboxPath, "*.eml"))
            {
                File.Delete(file);
            }
        }
    }

    [Test]
    public async Task UserRegistration_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var newUser = new UserDTO
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = "Test@123!",
            Role = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Usuario/register", newUser);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ContainsKey("message"), Is.True);
    }

    [Test]
    public async Task UserRegistration_WithInvalidPassword_ReturnsBadRequest()
    {
        // Arrange - senha sem caractere especial
        var newUser = new UserDTO
        {
            Name = "Test User",
            Email = "test2@example.com",
            Password = "Test123", // Senha inválida
            Role = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Usuario/register", newUser);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task UserRegistration_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var user = new UserDTO
        {
            Name = "First User",
            Email = "duplicate@example.com",
            Password = "Test@123!",
            Role = "User"
        };

        // Act - Registrar primeiro usuário
        var firstResponse = await _client.PostAsJsonAsync("/api/Usuario/register", user);
        Assert.That(firstResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Act - Tentar registrar com mesmo email
        var secondResponse = await _client.PostAsJsonAsync("/api/Usuario/register", user);

        // Assert
        Assert.That(secondResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task UserActivation_WithValidToken_ReturnsSuccess()
    {
        // Arrange - Registrar usuário
        var user = new UserDTO
        {
            Name = "User To Activate",
            Email = "activate@example.com",
            Password = "Test@123!",
            Role = "User"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/Usuario/register", user);
        Assert.That(registerResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Aguardar email ser escrito
        await Task.Delay(100);

        // Obter token de ativação do email
        var emailFiles = Directory.GetFiles(_outboxPath, "*.eml");
        Assert.That(emailFiles, Has.Length.EqualTo(1), "Deveria ter exatamente 1 email");
        
        var emailContent = await File.ReadAllTextAsync(emailFiles[0]);
        var tokenStart = emailContent.IndexOf("activationToken=") + "activationToken=".Length;
        var tokenEnd = emailContent.IndexOf("\n", tokenStart);
        var activationToken = emailContent.Substring(tokenStart, tokenEnd - tokenStart).Trim();

        // Act - Ativar conta
        var activateResponse = await _client.GetAsync($"/api/Usuario/activate?activationToken={Uri.EscapeDataString(activationToken)}");

        // Assert
        Assert.That(activateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Login_WithValidCredentials_ReturnsTokenAndRole()
    {
        // Arrange - Admin já existe no banco (seeded)
        var loginRequest = new LoginDTO
        {
            Email = "admin@test.com",
            Password = "Admin@123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Usuario/login", loginRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ContainsKey("token"), Is.True);
        Assert.That(result.ContainsKey("role"), Is.True);
        Assert.That(result["role"], Is.EqualTo("Admin"));
        Assert.That(result["token"], Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginDTO
        {
            Email = "admin@test.com",
            Password = "WrongPassword@123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Usuario/login", loginRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Login_WithInactiveUser_ReturnsUnauthorized()
    {
        // Arrange - Registrar usuário mas não ativar
        var user = new UserDTO
        {
            Name = "Inactive User",
            Email = "inactive@example.com",
            Password = "Test@123!",
            Role = "User"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/Usuario/register", user);
        Assert.That(registerResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var loginRequest = new LoginDTO
        {
            Email = "inactive@example.com",
            Password = "Test@123!"
        };

        // Act - Tentar fazer login sem ativar
        var response = await _client.PostAsJsonAsync("/api/Usuario/login", loginRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Login_WithNonexistentUser_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginDTO
        {
            Email = "nonexistent@example.com",
            Password = "Test@123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Usuario/login", loginRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
