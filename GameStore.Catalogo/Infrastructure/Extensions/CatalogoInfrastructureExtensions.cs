using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GameStore.Catalogo.Domain.Interfaces;
using GameStore.Catalogo.Application.Interfaces;
using GameStore.Catalogo.Application.Services;
using GameStore.Catalogo.Infrastructure.Persistence;
using GameStore.Catalogo.Infrastructure.Repository;

namespace GameStore.Catalogo.Infrastructure.Extensions
{
    public static class CatalogoServiceCollectionExtensions
    {
        public static IServiceCollection AddCatalogoContext(this IServiceCollection services, string connectionString)
        {
            // Register DbContext - sempre usar SQL Server
            services.AddDbContext<CatalogoDbContext>(options =>
                options.UseSqlServer(connectionString));

            // Register repositories
            services.AddScoped<IJogoRepository, JogoRepository>();

            // Register application services
            services.AddScoped<IJogoService, JogoService>();

            return services;
        }
    }
}