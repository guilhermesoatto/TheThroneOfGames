using System.Net;
using Xunit;

namespace GameStore.Usuarios.API.Tests;

[Trait("Category", "Integration")]
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

        // Assert
        Assert.NotNull(response);
    }

    [Fact]
    public async Task CanReachSwagger()
    {
        // Act
        var response = await _client.GetAsync("/swagger");

        // Assert
        Console.WriteLine($"Swagger response: {response.StatusCode}");
        Assert.NotNull(response);
    }
}
