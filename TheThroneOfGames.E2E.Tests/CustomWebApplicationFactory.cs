using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TheThroneOfGames.Infrastructure.Persistence;

namespace TheThroneOfGames.E2E.Tests;

/// <summary>
/// Sobe a API real (Program.cs) em processo, trocando o provider de persistência do
/// SQL Server para EF Core InMemory. Nenhum slave service (SQL Server, Prometheus, Grafana,
/// SMTP) é necessário — os testes E2E exercitam apenas os contratos HTTP do monólito.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Nome fixo => o estado do banco InMemory é compartilhado entre as requisições da
    // mesma instância da factory (necessário para a jornada registrar -> ativar -> logar).
    private readonly string _databaseName = $"e2e-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        // Config mínima para os `?? throw` de Program.cs não dispararem antes do swap de serviços.
        builder.UseSetting("ConnectionStrings:DefaultConnection", "InMemory-e2e");
        builder.UseSetting("Jwt:Key", "e2e-super-secret-signing-key-with-at-least-32-chars");
        builder.UseSetting("Jwt:Issuer", "TheThroneOfGamesAPI");
        builder.UseSetting("Jwt:Audience", "TheThroneOfGamesAPIUsers");

        builder.ConfigureTestServices(services =>
        {
            // Remove o registro do DbContext relacional (SQL Server) feito em Program.cs.
            var descriptorsToRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<MainDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(MainDbContext) ||
                (d.ServiceType.IsGenericType &&
                 d.ServiceType.GetGenericTypeDefinition().Name.StartsWith("IDbContextOptionsConfiguration", StringComparison.Ordinal)))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<MainDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }
}
