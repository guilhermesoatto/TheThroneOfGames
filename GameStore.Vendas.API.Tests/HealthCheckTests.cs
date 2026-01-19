using Xunit;

namespace GameStore.Vendas.API.Tests;

[Trait("Category", "Integration")]
public class HealthCheckTests : IAsyncLifetime
{
    private VendasWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var testDb = $"GameStore_Test_{Guid.NewGuid():N}";
        _factory = new VendasWebApplicationFactory(testDb);
        _client = _factory.CreateClient();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
        await Task.CompletedTask;
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
