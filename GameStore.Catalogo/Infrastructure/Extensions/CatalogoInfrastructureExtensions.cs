using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Elastic.Clients.Elasticsearch;
using GameStore.Catalogo.Domain.Interfaces;
using GameStore.Catalogo.Application.Interfaces;
using GameStore.Catalogo.Application.Services;
using GameStore.Catalogo.Infrastructure.Persistence;
using GameStore.Catalogo.Infrastructure.Repository;
using GameStore.Catalogo.Infrastructure.Search;
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
        public static IServiceCollection AddCatalogoContext(
            this IServiceCollection services,
            string connectionString,
            string elasticsearchUri = "http://localhost:9200")
        {
            // Register DbContext - sempre usar SQL Server
            services.AddDbContext<CatalogoDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Register repositories
            services.AddScoped<IJogoRepository, JogoRepository>();

            // Register Elasticsearch client (busca avançada de jogos - fase3-T04)
            services.AddSingleton(new ElasticsearchClient(new Uri(elasticsearchUri)));
            services.AddScoped<IJogoSearchIndexer, ElasticsearchJogoIndexer>();

            // Register application services
            services.AddScoped<IJogoService, JogoService>();
            services.AddScoped<GameService>();

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