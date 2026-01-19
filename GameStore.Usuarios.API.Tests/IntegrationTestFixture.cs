using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using GameStore.Usuarios.Infrastructure.Persistence;
using GameStore.Catalogo.Infrastructure.Persistence;

namespace GameStore.Usuarios.API.Tests;

public class IntegrationTestFixture : IAsyncLifetime
{
    private readonly string _testDatabaseName;
    public UsuariosWebApplicationFactory Factory { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public IntegrationTestFixture()
    {
        _testDatabaseName = $"GameStore_Test_{Guid.NewGuid():N}";
    }

    public async Task InitializeAsync()
    {
        Factory = new UsuariosWebApplicationFactory(_testDatabaseName);
        Client = Factory.CreateClient();

        using var scope = Factory.Services.CreateScope();
        var dbUsuarios = scope.ServiceProvider.GetRequiredService<UsuariosDbContext>();
        var dbCatalogo = scope.ServiceProvider.GetRequiredService<CatalogoDbContext>();

        // Apply migrations to test database
        await dbUsuarios.Database.MigrateAsync();
        await dbCatalogo.Database.MigrateAsync();

        // Seed admin user
        await SeedAdminUserAsync(dbUsuarios);
    }

    public async Task DisposeAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var dbUsuarios = scope.ServiceProvider.GetRequiredService<UsuariosDbContext>();
        var dbCatalogo = scope.ServiceProvider.GetRequiredService<CatalogoDbContext>();

        await dbUsuarios.Database.EnsureDeletedAsync();
        await dbCatalogo.Database.EnsureDeletedAsync();

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

public class UsuariosIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public UsuariosIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    protected HttpClient Client => _fixture.Client;
}
