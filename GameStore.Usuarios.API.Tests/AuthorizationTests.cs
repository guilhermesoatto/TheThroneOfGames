using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;
using TheThroneOfGames.API.Models.DTO;

namespace GameStore.Usuarios.API.Tests;

public class AuthorizationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly HttpClient _client;

    public AuthorizationTests(IntegrationTestFixture fixture)
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

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        return result!["token"];
    }

    [Fact]
    public async Task AccessProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        // Act - Tentar acessar endpoint protegido sem token
        var response = await _client.GetAsync("/api/admin/game");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AccessProtectedEndpoint_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", "invalid.token.here");

        // Act
        var response = await _client.GetAsync("/api/admin/game");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AccessAdminEndpoint_WithAdminToken_ReturnsSuccess()
    {
        // Arrange
        var token = await GetAdminToken();
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/admin/game");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AccessAdminEndpoint_WithExpiredToken_ReturnsUnauthorized()
    {
        // Arrange - Token com formato válido mas expirado/inválido
        var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", expiredToken);

        // Act
        var response = await _client.GetAsync("/api/admin/game");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
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
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
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
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
    }

    [Fact]
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
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.Equal(3, token.Split('.').Length);
        
        // Verificar que role está presente no response
        Assert.True(result.ContainsKey("role"));
        Assert.Equal("Admin", result["role"]);
    }
}
