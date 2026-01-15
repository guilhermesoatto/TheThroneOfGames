using NUnit.Framework;

namespace GameStore.Vendas.API.Tests;

[TestFixture]
public class HealthCheckTests
{
    private VendasWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void Setup()
    {
        _factory = new VendasWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [Test]
    public async Task ServerIsRunning()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        Assert.That(response, Is.Not.Null);
    }

    [Test]
    public async Task CanReachSwagger()
    {
        // Act
        var response = await _client.GetAsync("/swagger");

        // Assert
        Console.WriteLine($"Swagger response: {response.StatusCode}");
        Assert.That(response, Is.Not.Null);
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
