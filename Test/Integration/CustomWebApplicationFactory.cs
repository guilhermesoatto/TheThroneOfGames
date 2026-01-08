using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TheThroneOfGames.Infrastructure.Persistence;
using GameStore.Usuarios.Infrastructure.Persistence;
using GameStore.Catalogo.Infrastructure.Persistence;
using GameStore.Vendas.Infrastructure.Persistence;

namespace Test.Integration;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        
        // Não precisa configurar nada - vai usar SQL Server do container
        // Program.cs já configura tudo corretamente
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        
        // Executar migrations para garantir que banco está atualizado
        using (var scope = Services.CreateScope())
        {
            var scopedServices = scope.ServiceProvider;
            
            // Executar migrations para todos os contextos
            var dbMain = scopedServices.GetRequiredService<MainDbContext>();
            dbMain.Database.Migrate(); // Executa migrations pendentes
            
            var dbUsuarios = scopedServices.GetRequiredService<UsuariosDbContext>();
            dbUsuarios.Database.Migrate(); // Executa migrations dos bounded contexts
            
            var dbCatalogo = scopedServices.GetRequiredService<CatalogoDbContext>();
            dbCatalogo.Database.Migrate(); // Executa migrations dos bounded contexts
            
            var dbVendas = scopedServices.GetRequiredService<VendasDbContext>();
            dbVendas.Database.Migrate(); // Executa migrations dos bounded contexts
            
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
}