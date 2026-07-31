using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GameStore.Usuarios.Domain.Interfaces;
using GameStore.Usuarios.Application.Interfaces;
using GameStore.Usuarios.Application.Services;
using GameStore.Usuarios.Application.Commands;
using GameStore.Usuarios.Application.Handlers;
using GameStore.Usuarios.Infrastructure.Persistence;
using GameStore.Usuarios.Infrastructure.Repository;
using GameStore.CQRS.Abstractions;

namespace GameStore.Usuarios.Infrastructure.Extensions
{
    public static class UsuariosServiceCollectionExtensions
    {
        public static IServiceCollection AddUsuariosContext(this IServiceCollection services, string connectionString)
        {
            // Register DbContext - sempre usar SQL Server
            services.AddDbContext<UsuariosDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Register repositories
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            services.AddScoped<IInventarioRepository, InventarioRepository>();

            // Register application services
            // Explicitly register bounded context IUsuarioService with full namespace
            // This resolves to UsuarioController (uses GameStore.Usuarios namespace)
            services.AddScoped<GameStore.Usuarios.Application.Interfaces.IUsuarioService, UsuarioService>();
            services.AddScoped<AuthenticationService>();

            // CQRS command handlers — usados pelo Admin/UserManagementController
            // (GameStore.Usuarios.API), mesmo padrão de registro do Catálogo.
            services.AddScoped<ICommandHandler<CreateUserCommand>, CreateUserCommandHandler>();
            services.AddScoped<ICommandHandler<ChangeUserRoleCommand>, ChangeUserRoleCommandHandler>();

            return services;
        }
    }
}