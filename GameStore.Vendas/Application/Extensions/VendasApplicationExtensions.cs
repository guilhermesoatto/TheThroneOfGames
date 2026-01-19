using GameStore.Vendas.Application.Interfaces;
using GameStore.Vendas.Application.Services;
using GameStore.Vendas.Application.Handlers;
using GameStore.Vendas.Application.Commands;
using GameStore.CQRS.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace GameStore.Vendas.Application.Extensions
{
    public static class VendasApplicationExtensions
    {
        public static IServiceCollection AddVendasApplication(this IServiceCollection services)
        {
            // Services
            services.AddScoped<IPedidoService, PedidoService>();

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