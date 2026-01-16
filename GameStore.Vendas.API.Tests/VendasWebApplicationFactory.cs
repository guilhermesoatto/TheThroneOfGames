using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GameStore.Usuarios.Infrastructure.Persistence;
using GameStore.Catalogo.Infrastructure.Persistence;
using GameStore.Vendas.Infrastructure.Persistence;

namespace GameStore.Vendas.API.Tests;

public class VendasWebApplicationFactory : WebApplicationFactory<global::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        
        builder.ConfigureServices(services =>
        {
            // Remove DbContext options
            var usuariosDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<UsuariosDbContext>));
            if (usuariosDescriptor != null) services.Remove(usuariosDescriptor);
            
            var catalogoDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<CatalogoDbContext>));
            if (catalogoDescriptor != null) services.Remove(catalogoDescriptor);
            
            var vendasDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<VendasDbContext>));
            if (vendasDescriptor != null) services.Remove(vendasDescriptor);
            
            // Remove any DbContext registrations
            var usuariosContext = services.FirstOrDefault(d => d.ServiceType == typeof(UsuariosDbContext));
            if (usuariosContext != null) services.Remove(usuariosContext);
            
            var catalogoContext = services.FirstOrDefault(d => d.ServiceType == typeof(CatalogoDbContext));
            if (catalogoContext != null) services.Remove(catalogoContext);
            
            var vendasContext = services.FirstOrDefault(d => d.ServiceType == typeof(VendasDbContext));
            if (vendasContext != null) services.Remove(vendasContext);
            
            // Add InMemory databases
            services.AddDbContext<UsuariosDbContext>(options => 
                options.UseInMemoryDatabase("TestDb_Usuarios"));
            services.AddDbContext<CatalogoDbContext>(options => 
                options.UseInMemoryDatabase("TestDb_Catalogo"));
            services.AddDbContext<VendasDbContext>(options => 
                options.UseInMemoryDatabase("TestDb_Vendas"));
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        
        using var scope = Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        
        // EnsureCreated for InMemory databases
        var dbUsuarios = scopedServices.GetRequiredService<UsuariosDbContext>();
        dbUsuarios.Database.EnsureCreated();
        
        var dbCatalogo = scopedServices.GetRequiredService<CatalogoDbContext>();
        dbCatalogo.Database.EnsureCreated();
        
        var dbVendas = scopedServices.GetRequiredService<VendasDbContext>();
        dbVendas.Database.EnsureCreated();
        
        // Limpar dados de testes anteriores
        dbUsuarios.Usuarios.RemoveRange(dbUsuarios.Usuarios);
        dbUsuarios.SaveChanges();
        
        dbCatalogo.Jogos.RemoveRange(dbCatalogo.Jogos);
        dbCatalogo.SaveChanges();
        
        // Vendas não precisa limpar pois depende dos outros contexts
        
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
