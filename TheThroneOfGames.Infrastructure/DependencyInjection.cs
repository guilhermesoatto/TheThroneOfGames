using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using TheThroneOfGames.Infrastructure.Data;
using TheThroneOfGames.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using TheThroneOfGames.Domain.Interfaces;
using TheThroneOfGames.Domain.Entities;
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
