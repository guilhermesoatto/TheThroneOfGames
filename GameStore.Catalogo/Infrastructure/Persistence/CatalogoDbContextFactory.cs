using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GameStore.Catalogo.Infrastructure.Persistence;

public class CatalogoDbContextFactory : IDesignTimeDbContextFactory<CatalogoDbContext>
{
    public CatalogoDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CatalogoDbContext>();
        
        // Use connection string from args or default to local PostgreSQL
        var connectionString = args.Length > 0 
            ? args[0] 
            : "Host=localhost;Port=5432;Database=GameStore_Test;Username=sa;Password=YourSecurePassword123!";
        
        optionsBuilder.UseNpgsql(connectionString);
        
        return new CatalogoDbContext(optionsBuilder.Options);
    }
}
