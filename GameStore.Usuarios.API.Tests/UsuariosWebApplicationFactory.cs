using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GameStore.Usuarios.Infrastructure.Persistence;
using GameStore.Catalogo.Infrastructure.Persistence;

namespace GameStore.Usuarios.API.Tests;

public class UsuariosWebApplicationFactory : WebApplicationFactory<global::Program>
{
    private readonly string _testDatabaseName;

    public UsuariosWebApplicationFactory(string testDatabaseName)
    {
        _testDatabaseName = testDatabaseName;
    }

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
            
            // Remove any DbContext registrations
            var usuariosContext = services.FirstOrDefault(d => d.ServiceType == typeof(UsuariosDbContext));
            if (usuariosContext != null) services.Remove(usuariosContext);
            
            var catalogoContext = services.FirstOrDefault(d => d.ServiceType == typeof(CatalogoDbContext));
            if (catalogoContext != null) services.Remove(catalogoContext);
            
            // Add PostgreSQL databases with test-specific database names
            var connectionString = $"Host=localhost;Port=5432;Database={_testDatabaseName};Username=sa;Password=YourSecurePassword123!";
            services.AddDbContext<UsuariosDbContext>(options => 
                options.UseNpgsql(connectionString));
            services.AddDbContext<CatalogoDbContext>(options => 
                options.UseNpgsql(connectionString));
        });
    }
}
