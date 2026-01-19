using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GameStore.Vendas.Infrastructure.Persistence;

public class VendasDbContextFactory : IDesignTimeDbContextFactory<VendasDbContext>
{
    public VendasDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<VendasDbContext>();
        
        // Use connection string from args or default to local PostgreSQL
        var connectionString = args.Length > 0 
            ? args[0] 
            : "Host=localhost;Port=5432;Database=GameStore_Test;Username=sa;Password=YourSecurePassword123!";
        
        optionsBuilder.UseNpgsql(connectionString);
        
        return new VendasDbContext(optionsBuilder.Options);
    }
}
