using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheThroneOfGames.Domain.Interfaces;
using TheThroneOfGames.Infrastructure.Data;
using TheThroneOfGames.Infrastructure.Repository;

namespace TheThroneOfGames.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Configuração do DbContext (in-memory por padrão para testes/local)
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("TheThroneOfGamesDb"));

            // Registro dos Repositórios (usar nomes reais existentes)
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            services.AddScoped<IGameEntityRepository, GameEntityRepository>();
            services.AddScoped<IPromotionRepository, PromotionRepository>();
            // NOTE: Purchase repository may live in another project (GameStore.Vendas). Register it there when composing app.

            // Registro de Background Services (se aplicável)
            // services.AddHostedService<PromotionNotificationService>();

            return services;
        }
    }
}
