using System.Net;
using Xunit;

namespace TheThroneOfGames.E2E.Tests;

/// <summary>
/// Cobre o "job de integração" self-contained do CI (item 3.4): a aplicação compõe,
/// o container de DI resolve, o modelo EF valida e os endpoints de saúde/métricas
/// respondem — sem SQL Server, Prometheus ou Grafana.
/// </summary>
public sealed class HealthAndMetricsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthAndMetricsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Application_boots_and_public_info_responds()
    {
        var response = await _client.GetAsync("/api/usuario/public-info");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Prometheus_metrics_endpoint_is_exposed()
    {
        var response = await _client.GetAsync("/metrics");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("# HELP", body);
    }
}
