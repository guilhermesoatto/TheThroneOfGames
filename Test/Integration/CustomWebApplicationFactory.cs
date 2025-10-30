using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TheThroneOfGames.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using System.Data.Common;

namespace Test.Integration;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    private DbConnection? _connection;
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registrations
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<MainDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(MainDbContext));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Remove any existing database provider configurations
            var dbOptionsDependencies = services.Where(s => 
                s.ServiceType.Namespace.StartsWith("Microsoft.EntityFrameworkCore") && 
                s.ServiceType.Name.Contains("DbContext")).ToList();
            foreach (var dependency in dbOptionsDependencies)
            {
                services.Remove(dependency);
            }

            // Create and open a single in-memory SQLite connection that will be shared
            // across the application lifetime so the database schema persists.
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // Add SQLite database provider using the open connection
            services.AddDbContext<MainDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // Build the service provider and ensure DB is created using the same connection
            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<MainDbContext>();
                db.Database.EnsureCreated();
            }
        });

        // Use test environment
        builder.UseEnvironment("Test");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            try
            {
                _connection?.Close();
                _connection?.Dispose();
            }
            catch { }
        }
    }
}