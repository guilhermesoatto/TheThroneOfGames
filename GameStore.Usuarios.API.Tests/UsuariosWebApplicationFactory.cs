using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GameStore.Usuarios.Infrastructure.Persistence;
using GameStore.Catalogo.Infrastructure.Persistence;

namespace GameStore.Usuarios.API.Tests;

public class UsuariosWebApplicationFactory : WebApplicationFactory<global::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        
        builder.ConfigureServices(services =>
        {
            var usuariosDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<UsuariosDbContext>));
            if (usuariosDescriptor != null) services.Remove(usuariosDescriptor);
            
            var catalogoDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<CatalogoDbContext>));
            if (catalogoDescriptor != null) services.Remove(catalogoDescriptor);
            
            services.AddDbContext<UsuariosDbContext>(options => options.UseInMemoryDatabase("TestDb_Usuarios"));
            services.AddDbContext<CatalogoDbContext>(options => options.UseInMemoryDatabase("TestDb_Catalogo"));
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        
        using var scope = Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        
        var dbUsuarios = scopedServices.GetRequiredService<UsuariosDbContext>();
        dbUsuarios.Database.EnsureCreated();
        
        var dbCatalogo = scopedServices.GetRequiredService<CatalogoDbContext>();
        dbCatalogo.Database.EnsureCreated();
        
        // Seed admin user for testing in UsuariosDbContext (bounded context)
        if (!dbUsuarios.Usuarios.Any(u => u.Email == "admin@test.com" && u.Role == "Admin"))
        {
            var adminUser = new GameStore.Usuarios.Domain.Entities.Usuario(
                name: "Admin User",
                email: "admin@test.com",
                passwordHash: GameStore.Usuarios.Application.Services.UsuarioService.HashPassword("Admin@123!"),
                role: "Admin",
                activeToken: Guid.NewGuid().ToString()
            );
            adminUser.Activate();
            dbUsuarios.Usuarios.Add(adminUser);
            dbUsuarios.SaveChanges();
        }
    }
}
