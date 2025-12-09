using GameStore.Catalogo.Application.DTOs;
using GameStore.Catalogo.Application.Mappers;
using TheThroneOfGames.Domain.Entities;
using TheThroneOfGames.Domain.Interfaces;
using GameStore.CQRS.Abstractions;

namespace GameStore.Catalogo.Application.Queries
{
    /// <summary>
    /// Query para obter jogo por ID.
    /// </summary>
    public record GetGameByIdQuery(Guid GameId) : IQuery<GameDTO?>;

    /// <summary>
    /// Query para obter jogo por nome.
    /// </summary>
    public record GetGameByNameQuery(string Name) : IQuery<GameDTO?>;

    /// <summary>
    /// Query para obter todos os jogos.
    /// </summary>
    public record GetAllGamesQuery() : IQuery<IEnumerable<GameDTO>>;

    /// <summary>
    /// Query para obter jogos por gênero.
    /// </summary>
    public record GetGamesByGenreQuery(string Genre) : IQuery<IEnumerable<GameDTO>>;

    /// <summary>
    /// Query para obter jogos disponíveis.
    /// </summary>
    public record GetAvailableGamesQuery() : IQuery<IEnumerable<GameDTO>>;

    /// <summary>
    /// Query para obter jogos por faixa de preço.
    /// </summary>
    public record GetGamesByPriceRangeQuery(decimal MinPrice, decimal MaxPrice) : IQuery<IEnumerable<GameDTO>>;

    /// <summary>
    /// Query para buscar jogos (por nome ou gênero).
    /// </summary>
    public record SearchGamesQuery(string SearchTerm) : IQuery<IEnumerable<GameDTO>>;

    /// <summary>
    /// Handler para GetGameByIdQuery.
    /// </summary>
    public class GetGameByIdQueryHandler : IQueryHandler<GetGameByIdQuery, GameDTO?>
    {
        private readonly IGameRepository _gameRepository;

        public GetGameByIdQueryHandler(IGameRepository gameRepository)
        {
            _gameRepository = gameRepository;
        }

        public async Task<GameDTO?> HandleAsync(GetGameByIdQuery query)
        {
            try
            {
                var game = await _gameRepository.GetByIdAsync(query.GameId);
                return game != null ? GameMapper.ToDTO(game) : null;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Handler para GetGameByNameQuery.
    /// </summary>
    public class GetGameByNameQueryHandler : IQueryHandler<GetGameByNameQuery, GameDTO?>
    {
        private readonly IGameRepository _gameRepository;

        public GetGameByNameQueryHandler(IGameRepository gameRepository)
        {
            _gameRepository = gameRepository;
        }

        public async Task<GameDTO?> HandleAsync(GetGameByNameQuery query)
        {
            try
            {
                var game = await _gameRepository.GetByNameAsync(query.Name);
                return game != null ? GameMapper.ToDTO(game) : null;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Handler para GetAllGamesQuery.
    /// </summary>
    public class GetAllGamesQueryHandler : IQueryHandler<GetAllGamesQuery, IEnumerable<GameDTO>>
    {
        private readonly IGameRepository _gameRepository;

        public GetAllGamesQueryHandler(IGameRepository gameRepository)
        {
            _gameRepository = gameRepository;
        }

        public async Task<IEnumerable<GameDTO>> HandleAsync(GetAllGamesQuery query)
        {
            try
            {
                var games = await _gameRepository.GetAllAsync();
                return GameMapper.ToDTOList(games);
            }
            catch
            {
                return Enumerable.Empty<GameDTO>();
            }
        }
    }

    /// <summary>
    /// Handler para GetGamesByGenreQuery.
    /// </summary>
    public class GetGamesByGenreQueryHandler : IQueryHandler<GetGamesByGenreQuery, IEnumerable<GameDTO>>
    {
        private readonly IGameRepository _gameRepository;

        public GetGamesByGenreQueryHandler(IGameRepository gameRepository)
        {
            _gameRepository = gameRepository;
        }

        public async Task<IEnumerable<GameDTO>> HandleAsync(GetGamesByGenreQuery query)
        {
            try
            {
                var games = await _gameRepository.GetByGenreAsync(query.Genre);
                return GameMapper.ToDTOList(games);
            }
            catch
            {
                return Enumerable.Empty<GameDTO>();
            }
        }
    }

    /// <summary>
    /// Handler para GetAvailableGamesQuery.
    /// </summary>
    public class GetAvailableGamesQueryHandler : IQueryHandler<GetAvailableGamesQuery, IEnumerable<GameDTO>>
    {
        private readonly IGameRepository _gameRepository;

        public GetAvailableGamesQueryHandler(IGameRepository gameRepository)
        {
            _gameRepository = gameRepository;
        }

        public async Task<IEnumerable<GameDTO>> HandleAsync(GetAvailableGamesQuery query)
        {
            try
            {
                var games = await _gameRepository.GetAvailableGamesAsync();
                return GameMapper.ToDTOList(games);
            }
            catch
            {
                return Enumerable.Empty<GameDTO>();
            }
        }
    }

    /// <summary>
    /// Handler para GetGamesByPriceRangeQuery.
    /// </summary>
    public class GetGamesByPriceRangeQueryHandler : IQueryHandler<GetGamesByPriceRangeQuery, IEnumerable<GameDTO>>
    {
        private readonly IGameRepository _gameRepository;

        public GetGamesByPriceRangeQueryHandler(IGameRepository gameRepository)
        {
            _gameRepository = gameRepository;
        }

        public async Task<IEnumerable<GameDTO>> HandleAsync(GetGamesByPriceRangeQuery query)
        {
            try
            {
                var games = await _gameRepository.GetByPriceRangeAsync(query.MinPrice, query.MaxPrice);
                return GameMapper.ToDTOList(games);
            }
            catch
            {
                return Enumerable.Empty<GameDTO>();
            }
        }
    }

    /// <summary>
    /// Handler para SearchGamesQuery.
    /// </summary>
    public class SearchGamesQueryHandler : IQueryHandler<SearchGamesQuery, IEnumerable<GameDTO>>
    {
        private readonly IGameRepository _gameRepository;

        public SearchGamesQueryHandler(IGameRepository gameRepository)
        {
            _gameRepository = gameRepository;
        }

        public async Task<IEnumerable<GameDTO>> HandleAsync(SearchGamesQuery query)
        {
            try
            {
                var games = await _gameRepository.SearchGamesAsync(query.SearchTerm);
                return GameMapper.ToDTOList(games);
            }
            catch
            {
                return Enumerable.Empty<GameDTO>();
            }
        }
    }
}
