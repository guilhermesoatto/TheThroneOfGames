using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using GameStore.Catalogo.Infrastructure.Persistence;
using GameStore.Usuarios.Infrastructure.Persistence;

namespace GameStore.Catalogo.API.Tests;

public class CatalogoWebApplicationFactory : WebApplicationFactory<GameStore.Catalogo.API.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        
        builder.ConfigureServices(services =>
        {
            // Remove all DbContext registrations
            services.RemoveAll(typeof(DbContextOptions<CatalogoDbContext>));
            services.RemoveAll(typeof(CatalogoDbContext));
            
            services.RemoveAll(typeof(DbContextOptions<UsuariosDbContext>));
            services.RemoveAll(typeof(UsuariosDbContext));
            
            // Add InMemory DbContexts for tests
            services.AddDbContext<CatalogoDbContext>(options =>
            {
                options.UseInMemoryDatabase("CatalogoTestDb");
            });
            
            services.AddDbContext<UsuariosDbContext>(options =>
            {
                options.UseInMemoryDatabase("UsuariosTestDb");
            });
            
            // Build service provider and seed data
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            
            var catalogoDb = scopedServices.GetRequiredService<CatalogoDbContext>();
            var usuariosDb = scopedServices.GetRequiredService<UsuariosDbContext>();
            
            catalogoDb.Database.EnsureCreated();
            usuariosDb.Database.EnsureCreated();
            
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
        });
    }
}
