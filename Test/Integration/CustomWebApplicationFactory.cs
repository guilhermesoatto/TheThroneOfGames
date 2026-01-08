using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TheThroneOfGames.Infrastructure.Persistence;
using GameStore.Usuarios.Infrastructure.Persistence;
using GameStore.Catalogo.Infrastructure.Persistence;
using GameStore.Vendas.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using System.Data.Common;

namespace Test.Integration;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    private DbConnection? _connectionMain;
    private DbConnection? _connectionUsuarios;
    private DbConnection? _connectionCatalogo;
    private DbConnection? _connectionVendas;
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove all existing DbContext registrations
            RemoveDbContext<MainDbContext>(services);
            RemoveDbContext<UsuariosDbContext>(services);
            RemoveDbContext<CatalogoDbContext>(services);
            RemoveDbContext<VendasDbContext>(services);

            // Create in-memory SQLite connections for each context
            _connectionMain = new SqliteConnection("DataSource=:memory:");
            _connectionMain.Open();
            
            _connectionUsuarios = new SqliteConnection("DataSource=:memory:");
            _connectionUsuarios.Open();
            
            _connectionCatalogo = new SqliteConnection("DataSource=:memory:");
            _connectionCatalogo.Open();
            
            _connectionVendas = new SqliteConnection("DataSource=:memory:");
            _connectionVendas.Open();

            // Add all DbContexts with SQLite in-memory provider
            services.AddDbContext<MainDbContext>(options =>
            {
                options.UseSqlite(_connectionMain);
            });
            
            services.AddDbContext<UsuariosDbContext>(options =>
            {
                options.UseSqlite(_connectionUsuarios);
            });
            
            services.AddDbContext<CatalogoDbContext>(options =>
            {
                options.UseSqlite(_connectionCatalogo);
            });
            
            services.AddDbContext<VendasDbContext>(options =>
            {
                options.UseSqlite(_connectionVendas);
            });

            // Build the service provider and ensure all DBs are created
            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                
                // Create all database schemas
                var dbMain = scopedServices.GetRequiredService<MainDbContext>();
                dbMain.Database.EnsureCreated();
                
                var dbUsuarios = scopedServices.GetRequiredService<UsuariosDbContext>();
                dbUsuarios.Database.EnsureCreated();
                
                var dbCatalogo = scopedServices.GetRequiredService<CatalogoDbContext>();
                dbCatalogo.Database.EnsureCreated();
                
                var dbVendas = scopedServices.GetRequiredService<VendasDbContext>();
                dbVendas.Database.EnsureCreated();
                
                // Seed admin user for testing in MainDbContext
                if (!dbMain.Users.Any(u => u.Role == "Admin"))
                {
                    var adminUser = new TheThroneOfGames.Domain.Entities.Usuario(
                        name: "Admin User",
                        email: "admin@test.com",
                        passwordHash: TheThroneOfGames.Application.UsuarioService.HashPassword("Admin@123!"),
                        role: "Admin",
                        activeToken: Guid.NewGuid().ToString()
                    );
                    adminUser.Activate();
                    dbMain.Users.Add(adminUser);
                    dbMain.SaveChanges();
                }
            }
        });

        builder.UseEnvironment("Test");
    }

    private void RemoveDbContext<TContext>(IServiceCollection services) where TContext : DbContext
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<TContext>));
        if (descriptor != null) services.Remove(descriptor);

        var contextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(TContext));
        if (contextDescriptor != null) services.Remove(contextDescriptor);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            try
            {
                _connectionMain?.Close();
                _connectionMain?.Dispose();
                _connectionUsuarios?.Close();
                _connectionUsuarios?.Dispose();
                _connectionCatalogo?.Close();
                _connectionCatalogo?.Dispose();
                _connectionVendas?.Close();
                _connectionVendas?.Dispose();
            }
            catch { }
        }
    }
}