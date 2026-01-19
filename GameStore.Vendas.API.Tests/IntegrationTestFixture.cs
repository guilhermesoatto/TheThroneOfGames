using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using GameStore.Usuarios.Infrastructure.Persistence;
using GameStore.Catalogo.Infrastructure.Persistence;
using GameStore.Vendas.Infrastructure.Persistence;

namespace GameStore.Vendas.API.Tests;

public class IntegrationTestFixture : IAsyncLifetime
{
    private readonly string _testDatabaseName;
    public VendasWebApplicationFactory Factory { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public IntegrationTestFixture()
    {
        _testDatabaseName = $"GameStore_Test_{Guid.NewGuid():N}";
    }

    public async Task InitializeAsync()
    {
        Factory = new VendasWebApplicationFactory(_testDatabaseName);
        Client = Factory.CreateClient();

        using var scope = Factory.Services.CreateScope();
        var dbUsuarios = scope.ServiceProvider.GetRequiredService<UsuariosDbContext>();
        var dbCatalogo = scope.ServiceProvider.GetRequiredService<CatalogoDbContext>();
        var dbVendas = scope.ServiceProvider.GetRequiredService<VendasDbContext>();

        // Apply migrations to test database
        await dbUsuarios.Database.MigrateAsync();
        await dbCatalogo.Database.MigrateAsync();
        await dbVendas.Database.MigrateAsync();

        // Seed admin user
        await SeedAdminUserAsync(dbUsuarios);
    }

    public async Task DisposeAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var dbUsuarios = scope.ServiceProvider.GetRequiredService<UsuariosDbContext>();
        var dbCatalogo = scope.ServiceProvider.GetRequiredService<CatalogoDbContext>();
        var dbVendas = scope.ServiceProvider.GetRequiredService<VendasDbContext>();

        await dbUsuarios.Database.EnsureDeletedAsync();
        await dbCatalogo.Database.EnsureDeletedAsync();
        await dbVendas.Database.EnsureDeletedAsync();

        Client?.Dispose();
        Factory?.Dispose();
    }

    private async Task SeedAdminUserAsync(UsuariosDbContext dbUsuarios)
    {
        var adminExists = dbUsuarios.Usuarios.Any(u => u.Email == "admin@test.com" && u.Role == "Admin");
        if (adminExists) return;

        var adminUser = new GameStore.Usuarios.Domain.Entities.Usuario(
            name: "Admin User",
            email: "admin@test.com",
            passwordHash: GameStore.Usuarios.Application.Services.UsuarioService.HashPassword("Admin@123!"),
            role: "Admin",
            activeToken: Guid.NewGuid().ToString()
        );
        adminUser.Activate();

        dbUsuarios.Usuarios.Add(adminUser);
        await dbUsuarios.SaveChangesAsync();
    }

    public async Task<T> GetServiceAsync<T>() where T : notnull
    {
        using var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }
}

public class VendasIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public VendasIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    protected HttpClient Client => _fixture.Client;
}
