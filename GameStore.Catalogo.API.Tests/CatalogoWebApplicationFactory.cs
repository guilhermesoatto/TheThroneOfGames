using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
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
        
        // Não configurar nada - usar SQL Server do container configurado no Program.cs
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        
        // Executar migrations para garantir que banco está atualizado
        using var scope = Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        
        // Executar migrations apenas dos bounded contexts
        var dbUsuarios = scopedServices.GetRequiredService<UsuariosDbContext>();
        dbUsuarios.Database.Migrate(); // Executa migrations dos bounded contexts
        
        var dbCatalogo = scopedServices.GetRequiredService<CatalogoDbContext>();
        dbCatalogo.Database.Migrate(); // Executa migrations dos bounded contexts
        
        // Limpar dados de testes anteriores
        dbUsuarios.Usuarios.RemoveRange(dbUsuarios.Usuarios);
        dbUsuarios.SaveChanges();
        
        dbCatalogo.Jogos.RemoveRange(dbCatalogo.Jogos);
        dbCatalogo.SaveChanges();
        
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
