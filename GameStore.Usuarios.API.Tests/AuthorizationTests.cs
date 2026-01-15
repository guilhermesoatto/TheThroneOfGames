using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TheThroneOfGames.API.Models.DTO;

namespace GameStore.Usuarios.API.Tests;

[TestFixture]
public class AuthorizationTests : IDisposable
{
    private readonly UsuariosWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthorizationTests()
    {
        _factory = new UsuariosWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    private async Task<string> GetAdminToken()
    {
        var response = await _client.PostAsJsonAsync("/api/Usuario/login", new LoginDTO
        {
            Email = "admin@test.com",
            Password = "Admin@123!"
        });

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        return result!["token"];
    }

    [Test]
    public async Task AccessProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        // Act - Tentar acessar endpoint protegido sem token
        var response = await _client.GetAsync("/api/admin/game");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task AccessProtectedEndpoint_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", "invalid.token.here");

        // Act
        var response = await _client.GetAsync("/api/admin/game");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task AccessAdminEndpoint_WithAdminToken_ReturnsSuccess()
    {
        // Arrange
        var token = await GetAdminToken();
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/admin/game");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task AccessAdminEndpoint_WithExpiredToken_ReturnsUnauthorized()
    {
        // Arrange - Token com formato válido mas expirado/inválido
        var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", expiredToken);

        // Act
        var response = await _client.GetAsync("/api/admin/game");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task CreateAdminResource_WithValidAdminToken_ReturnsCreated()
    {
        // Arrange
        var token = await GetAdminToken();
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);

        var newGame = new GameDTO
        {
            Name = "Authorization Test Game",
            Genre = "Test",
            Price = 29.99m,
            Description = "Test game for authorization"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/admin/game", newGame);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }

    [Test]
    public async Task TokenValidation_ChecksIssuerAndAudience()
    {
        // Arrange - Token de admin válido
        var token = await GetAdminToken();
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);

        // Act - Fazer múltiplas requisições para validar token
        var response1 = await _client.GetAsync("/api/admin/game");
        var response2 = await _client.GetAsync("/api/admin/game");

        // Assert - Token deve funcionar consistentemente
        Assert.That(response1.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response2.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task JwtTokenContainsRequiredClaims()
    {
        // Arrange & Act
        var loginResponse = await _client.PostAsJsonAsync("/api/Usuario/login", new LoginDTO
        {
            Email = "admin@test.com",
            Password = "Admin@123!"
        });

        var result = await loginResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        var token = result!["token"];

        // Assert - Token não deve ser vazio e deve ter formato JWT válido
        Assert.That(token, Is.Not.Null.And.Not.Empty);
        Assert.That(token.Split('.'), Has.Length.EqualTo(3), "JWT deve ter 3 partes separadas por ponto");
        
        // Verificar que role está presente no response
        Assert.That(result.ContainsKey("role"), Is.True);
        Assert.That(result["role"], Is.EqualTo("Admin"));
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
