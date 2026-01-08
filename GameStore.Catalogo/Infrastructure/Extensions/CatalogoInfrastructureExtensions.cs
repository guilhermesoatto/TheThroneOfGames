using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GameStore.Catalogo.Domain.Interfaces;
using GameStore.Catalogo.Application.Interfaces;
using GameStore.Catalogo.Application.Services;
using GameStore.Catalogo.Infrastructure.Persistence;
using GameStore.Catalogo.Infrastructure.Repository;
using GameStore.Catalogo.Application.Commands;
using GameStore.Catalogo.Application.Queries;
using GameStore.Catalogo.Application.Handlers;
using GameStore.Catalogo.Application.DTOs;
using GameStore.CQRS.Abstractions;
using GameStore.Common.Events;
using GameStore.Common.Messaging;

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

            // Register CQRS Handlers
            services.AddScoped<ICommandHandler<CreateGameCommand>, CreateGameCommandHandler>();
            services.AddScoped<ICommandHandler<UpdateGameCommand>, UpdateGameCommandHandler>();
            services.AddScoped<ICommandHandler<RemoveGameCommand>, RemoveGameCommandHandler>();
            
            services.AddScoped<IQueryHandler<GetAllGamesQuery, IEnumerable<GameDTO>>, GetAllGamesQueryHandler>();
            services.AddScoped<IQueryHandler<GetGameByIdQuery, GameDTO?>, GetGameByIdQueryHandler>();
            services.AddScoped<IQueryHandler<GetGameByNameQuery, GameDTO?>, GetGameByNameQueryHandler>();
            services.AddScoped<IQueryHandler<GetGamesByGenreQuery, IEnumerable<GameDTO>>, GetGamesByGenreQueryHandler>();
            services.AddScoped<IQueryHandler<GetAvailableGamesQuery, IEnumerable<GameDTO>>, GetAvailableGamesQueryHandler>();
            services.AddScoped<IQueryHandler<GetGamesByPriceRangeQuery, IEnumerable<GameDTO>>, GetGamesByPriceRangeQueryHandler>();
            services.AddScoped<IQueryHandler<SearchGamesQuery, IEnumerable<GameDTO>>, SearchGamesQueryHandler>();

            // Register EventBus if not already registered
            services.AddSingleton<IEventBus, SimpleEventBus>();

            return services;
        }
    }
}