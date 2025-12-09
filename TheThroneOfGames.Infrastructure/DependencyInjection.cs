using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using TheThroneOfGames.Infrastructure.Data;
using TheThroneOfGames.Infrastructure.Persistence;
using TheThroneOfGames.Domain.Interfaces;
using TheThroneOfGames.Infrastructure.Repository;

namespace TheThroneOfGames.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Configuração do DbContext
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))); // Ou UseSqlite

            // Registro dos Repositórios
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IGameRepository, GameRepository>();
            services.AddScoped<IPurchaseRepository, PurchaseRepository>();
            // ... registrar outros repositórios

            // Registro dos Serviços de E-mail
            services.Configure<SmtpSettings>(configuration.GetSection("SmtpSettings"));
            services.AddTransient<IEmailService, EmailService>(); // IEmailService deve ser uma interface no Domain

            // Registro de Background Services (se aplicável)
            // services.AddHostedService<PromotionNotificationService>();

            return services;
        }
    }
}
