using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GameStore.Catalogo.Infrastructure.Persistence;
using GameStore.Usuarios.Infrastructure.Persistence;

namespace GameStore.Catalogo.API.Tests;

public class CatalogoWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        
        builder.ConfigureTestServices(services =>
        {
            // Remove all SQL Server DbContext related services
            var catalogoDescriptors = services.Where(d => 
                d.ServiceType.ToString().Contains("CatalogoDbContext")).ToList();
            foreach (var descriptor in catalogoDescriptors)
            {
                services.Remove(descriptor);
            }
            
            var usuariosDescriptors = services.Where(d => 
                d.ServiceType.ToString().Contains("UsuariosDbContext")).ToList();
            foreach (var descriptor in usuariosDescriptors)
            {
                services.Remove(descriptor);
            }
            
            // Add InMemory DbContexts for tests
            services.AddDbContext<CatalogoDbContext>(options =>
            {
                options.UseInMemoryDatabase("CatalogoTestDb");
            });
            
            services.AddDbContext<UsuariosDbContext>(options =>
            {
                options.UseInMemoryDatabase("UsuariosTestDb");
            });
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        
        // Seed data after application is built
        using var scope = Services.CreateScope();
        var usuariosDb = scope.ServiceProvider.GetRequiredService<UsuariosDbContext>();
        var catalogoDb = scope.ServiceProvider.GetRequiredService<CatalogoDbContext>();
        
        usuariosDb.Database.EnsureCreated();
        catalogoDb.Database.EnsureCreated();
        
        // Seed admin user for testing
        if (!usuariosDb.Usuarios.Any(u => u.Email == "admin@test.com" && u.Role == "Admin"))
        {
            var adminUser = new GameStore.Usuarios.Domain.Entities.Usuario(
                name: "Admin User",
                email: "admin@test.com",
                passwordHash: GameStore.Usuarios.Application.Services.UsuarioService.HashPassword("Admin@123!"),
                role: "Admin",
                activeToken: Guid.NewGuid().ToString()
            );
            adminUser.Activate();
            usuariosDb.Usuarios.Add(adminUser);
            usuariosDb.SaveChanges();
        }
    }
}
