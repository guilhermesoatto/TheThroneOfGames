using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using GameStore.Common.Messaging;
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
            // Remove o hosted service que inicia o consumer de PedidoFinalizadoEvent (Program.cs
            // registra EventConsumerService condicionalmente a EventBus:UseRabbitMq=true, valor fixo
            // no appsettings.json base). Sem isso, o TestServer tentaria conectar a um RabbitMQ real
            // na inicialização, o que não existe neste ambiente de teste. IEventConsumer/
            // PedidoFinalizadoEventConsumer só são resolvidos (e só então abrem conexão) de dentro do
            // construtor de EventConsumerService, então removê-lo é suficiente — não precisa remover
            // o registro de IEventConsumer em si.
            var eventConsumerHostedService = services.SingleOrDefault(
                d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(EventConsumerService));
            if (eventConsumerHostedService != null) services.Remove(eventConsumerHostedService);

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
