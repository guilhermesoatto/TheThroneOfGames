using GameStore.Vendas.Domain.Repositories;
using GameStore.Vendas.Infrastructure.Persistence;
using GameStore.Vendas.Infrastructure.Repository;
using GameStore.Vendas.Application.Commands;
using GameStore.Vendas.Application.Handlers;
using GameStore.CQRS.Abstractions;
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
            // DbContext - sempre usar PostgreSQL
            services.AddDbContext<VendasDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("VendasConnection") ??
                    configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions => npgsqlOptions.MigrationsAssembly("GameStore.Vendas")));

            // Repositories
            services.AddScoped<IPedidoRepository, PedidoRepository>();

            // Command Handlers
            services.AddScoped<ICommandHandler<CriarPedidoCommand>, CriarPedidoCommandHandler>();
            services.AddScoped<ICommandHandler<AdicionarItemPedidoCommand>, AdicionarItemPedidoCommandHandler>();
            services.AddScoped<ICommandHandler<RemoverItemPedidoCommand>, RemoverItemPedidoCommandHandler>();
            services.AddScoped<ICommandHandler<FinalizarPedidoCommand>, FinalizarPedidoCommandHandler>();
            services.AddScoped<ICommandHandler<CancelarPedidoCommand>, CancelarPedidoCommandHandler>();

            return services;
        }
    }
}