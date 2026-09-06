using Microsoft.Extensions.DependencyInjection;
using TheThroneOfGames.Application.Interface;
using TheThroneOfGames.Domain.Entities;
using TheThroneOfGames.Domain.Interfaces;
using TheThroneOfGames.Infrastructure.Repository;

namespace TheThroneOfGames.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register application services (Fase 2 - Monolito)
        services.AddScoped<IGameService, GameService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IPromotionService, PromotionService>();

        // Register repositories
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IBaseRepository<GameEntity>, BaseRepository<GameEntity>>();
        services.AddScoped<IBaseRepository<PurchaseEntity>, BaseRepository<PurchaseEntity>>();
        services.AddScoped<IBaseRepository<PromotionEntity>, BaseRepository<PromotionEntity>>();
        services.AddScoped<IPromotionRepository, PromotionRepository>();

        return services;
    }
}
