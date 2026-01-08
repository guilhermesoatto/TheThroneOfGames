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
            
            // Seed admin user for testing in MainDbContext (apenas se não existir)
            if (!dbMain.Users.Any(u => u.Email == "admin@test.com" && u.Role == "Admin"))
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
    }
}