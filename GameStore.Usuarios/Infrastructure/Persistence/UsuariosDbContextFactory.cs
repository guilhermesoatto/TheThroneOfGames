using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GameStore.Usuarios.Infrastructure.Persistence;

public class UsuariosDbContextFactory : IDesignTimeDbContextFactory<UsuariosDbContext>
{
    public UsuariosDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UsuariosDbContext>();
        
        // Use connection string from args or default to local PostgreSQL
        var connectionString = args.Length > 0 
            ? args[0] 
            : "Host=localhost;Port=5432;Database=GameStore_Test;Username=sa;Password=YourSecurePassword123!";
        
        optionsBuilder.UseNpgsql(connectionString);
        
        return new UsuariosDbContext(optionsBuilder.Options);
    }
}
