using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace GameStore.Usuarios.API.Tests;

[Trait("Category", "Integration")]
public class AuthenticationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly HttpClient _client;

    public AuthenticationTests(IntegrationTestFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
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
        var response = await _client.PostAsJsonAsync("/api/Usuario/pre-register", newUser);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(result);
        Assert.True(result.ContainsKey("message"));
    }

    [Fact]
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
        var response = await _client.PostAsJsonAsync("/api/Usuario/pre-register", newUser);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
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
        var firstResponse = await _client.PostAsJsonAsync("/api/Usuario/pre-register", user);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        // Act - Tentar registrar com mesmo email
        var secondResponse = await _client.PostAsJsonAsync("/api/Usuario/pre-register", user);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);
    }

    [Fact]
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

        var registerResponse = await _client.PostAsJsonAsync("/api/Usuario/pre-register", user);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        // O envio de e-mail foi removido nesta fase (microsserviço independente — ver comentário
        // em UsuarioController.PreRegisterUser); o token de ativação é retornado diretamente no
        // corpo da resposta do pré-registro para uso pelo fluxo assíncrono de notificação (Serverless).
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var activationToken = registerResult!["activationToken"].ToString();

        // Act - Ativar conta (UsuarioController expõe apenas [HttpPost("activate")])
        var activateResponse = await _client.PostAsync($"/api/Usuario/activate?activationToken={Uri.EscapeDataString(activationToken)}", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, activateResponse.StatusCode);
    }

    [Fact]
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
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(result);
        Assert.True(result.ContainsKey("token"));
        Assert.True(result.ContainsKey("role"));
        Assert.Equal("Admin", result["role"]);
        Assert.NotEmpty(result["token"]);
    }

    [Fact]
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
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
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

        var registerResponse = await _client.PostAsJsonAsync("/api/Usuario/pre-register", user);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginRequest = new LoginDTO
        {
            Email = "inactive@example.com",
            Password = "Test@123!"
        };

        // Act - Tentar fazer login sem ativar
        var response = await _client.PostAsJsonAsync("/api/Usuario/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
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
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
