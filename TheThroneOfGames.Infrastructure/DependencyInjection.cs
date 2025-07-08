using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheThroneOfGames.Infrastructure.Email;
using TheThroneOfGames.Infrastructure.Persistence;
using TheThroneOfGames.Infrastructure.Repository;
using TheThroneOfGames.Infrastructure.Repository.Interfaces;

namespace TheThroneOfGames.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Configuração do MongoDbContext
            services.AddSingleton(sp =>
                new MongoDbContext(
                    configuration.GetConnectionString("MongoDbConnection") ?? throw new InvalidOperationException("MongoDb connection string is not configured"),
                    configuration["MongoDbDatabaseName"] ?? throw new InvalidOperationException("MongoDb database name is not configured")
                ));

            // Registro dos Repositórios
            services.AddScoped<IUsuarioRepository, MongoUsuarioRepository>();
            // Adicione outros repositórios conforme necessário

            // Registro dos Serviços de E-mail
            services.Configure<SmtpSettings>(options =>
                configuration.GetSection("SmtpSettings").Bind(options));
            services.AddTransient<IEmailService, EmailService>();

            return services;
        }
    }
}
