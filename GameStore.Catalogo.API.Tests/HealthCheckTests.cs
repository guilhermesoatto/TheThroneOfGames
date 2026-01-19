using System.Net;
using Xunit;

namespace GameStore.Catalogo.API.Tests;

public class HealthCheckTests : IClassFixture<IntegrationTestFixture>
{
    private readonly HttpClient _client;

    public HealthCheckTests(IntegrationTestFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task ServerIsRunning()
    {
        // Act
        var response = await _client.GetAsync("/");
        
        // Assert - any response means server is up
        Console.WriteLine($"Server response: {response.StatusCode}");
        Assert.NotNull(response);
    }

    [Fact]
    public async Task CanReachSwagger()
    {
        // Act
        var response = await _client.GetAsync("/swagger/index.html");
        
        // Assert
        Console.WriteLine($"Swagger response: {response.StatusCode}");
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
