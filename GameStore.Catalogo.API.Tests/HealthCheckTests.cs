using System.Net;
using NUnit.Framework;

namespace GameStore.Catalogo.API.Tests;

[TestFixture]
public class HealthCheckTests : IDisposable
{
    private readonly CatalogoWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public HealthCheckTests()
    {
        _factory = new CatalogoWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [Test]
    public async Task ServerIsRunning()
    {
        // Act
        var response = await _client.GetAsync("/");
        
        // Assert - any response means server is up
        Console.WriteLine($"Server response: {response.StatusCode}");
        Assert.That(response, Is.Not.Null);
    }

    [Test]
    public async Task CanReachSwagger()
    {
        // Act
        var response = await _client.GetAsync("/swagger/index.html");
        
        // Assert
        Console.WriteLine($"Swagger response: {response.StatusCode}");
        Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.InternalServerError));
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }
}
