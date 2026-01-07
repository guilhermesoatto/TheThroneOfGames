using GameStore.Vendas.Domain.Repositories;
using GameStore.Vendas.Infrastructure.Persistence;
using GameStore.Vendas.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameStore.Vendas.Infrastructure.Extensions
{
    public static class VendasInfrastructureExtensions
    {
        public static IServiceCollection AddVendasInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // DbContext
            services.AddDbContext<VendasDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("VendasConnection") ??
                    configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions => sqlOptions.MigrationsAssembly("GameStore.Vendas")));

            // Repositories
            services.AddScoped<IPedidoRepository, PedidoRepository>();

            return services;
        }
    }
}