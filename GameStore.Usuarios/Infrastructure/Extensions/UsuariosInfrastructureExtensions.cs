using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GameStore.Usuarios.Domain.Interfaces;
using GameStore.Usuarios.Application.Interfaces;
using GameStore.Usuarios.Application.Services;
using GameStore.Usuarios.Infrastructure.Persistence;
using GameStore.Usuarios.Infrastructure.Repository;

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

            // Register application services
            // Explicitly register bounded context IUsuarioService with full namespace
            // This resolves to UsuarioController (uses GameStore.Usuarios namespace)
            services.AddScoped<GameStore.Usuarios.Application.Interfaces.IUsuarioService, UsuarioService>();
            services.AddScoped<AuthenticationService>();

            return services;
        }
    }
}