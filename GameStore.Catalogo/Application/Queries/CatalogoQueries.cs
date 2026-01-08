using GameStore.Catalogo.Application.DTOs;
using GameStore.Catalogo.Application.Mappers;
using GameStore.Catalogo.Domain.Entities;
using GameStore.Catalogo.Domain.Interfaces;
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
        private readonly IJogoRepository _jogoRepository;

        public GetGameByIdQueryHandler(IJogoRepository jogoRepository)
        {
            _jogoRepository = jogoRepository;
        }

        public async Task<GameDTO?> HandleAsync(GetGameByIdQuery query)
        {
            try
            {
                var jogo = await _jogoRepository.GetByIdAsync(query.GameId);
                return jogo != null ? GameMapper.ToDTO(jogo) : null;
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
        private readonly IJogoRepository _jogoRepository;

        public GetGameByNameQueryHandler(IJogoRepository jogoRepository)
        {
            _jogoRepository = jogoRepository;
        }

        public async Task<GameDTO?> HandleAsync(GetGameByNameQuery query)
        {
            try
            {
                var jogos = await _jogoRepository.GetByNomeAsync(query.Name);
                var jogo = jogos.FirstOrDefault();
                return jogo != null ? GameMapper.ToDTO(jogo) : null;
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
        private readonly IJogoRepository _jogoRepository;

        public GetAllGamesQueryHandler(IJogoRepository jogoRepository)
        {
            _jogoRepository = jogoRepository;
        }

        public async Task<IEnumerable<GameDTO>> HandleAsync(GetAllGamesQuery query)
        {
            try
            {
                var jogos = await _jogoRepository.GetAllAsync();
                return GameMapper.ToDTOList(jogos);
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
        private readonly IJogoRepository _jogoRepository;

        public GetGamesByGenreQueryHandler(IJogoRepository jogoRepository)
        {
            _jogoRepository = jogoRepository;
        }

        public async Task<IEnumerable<GameDTO>> HandleAsync(GetGamesByGenreQuery query)
        {
            try
            {
                var jogos = await _jogoRepository.GetByGeneroAsync(query.Genre);
                return GameMapper.ToDTOList(jogos);
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
        private readonly IJogoRepository _jogoRepository;

        public GetAvailableGamesQueryHandler(IJogoRepository jogoRepository)
        {
            _jogoRepository = jogoRepository;
        }

        public async Task<IEnumerable<GameDTO>> HandleAsync(GetAvailableGamesQuery query)
        {
            try
            {
                var jogos = await _jogoRepository.GetDisponiveisAsync();
                return GameMapper.ToDTOList(jogos);
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
        private readonly IJogoRepository _jogoRepository;

        public GetGamesByPriceRangeQueryHandler(IJogoRepository jogoRepository)
        {
            _jogoRepository = jogoRepository;
        }

        public async Task<IEnumerable<GameDTO>> HandleAsync(GetGamesByPriceRangeQuery query)
        {
            try
            {
                var jogos = await _jogoRepository.GetByFaixaPrecoAsync(query.MinPrice, query.MaxPrice);
                return GameMapper.ToDTOList(jogos);
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
        private readonly IJogoRepository _jogoRepository;

        public SearchGamesQueryHandler(IJogoRepository jogoRepository)
        {
            _jogoRepository = jogoRepository;
        }

        public async Task<IEnumerable<GameDTO>> HandleAsync(SearchGamesQuery query)
        {
            try
            {
                // Buscar por nome e por gênero
                var jogosPorNome = await _jogoRepository.GetByNomeAsync(query.SearchTerm);
                var jogosPorGenero = await _jogoRepository.GetByGeneroAsync(query.SearchTerm);
                
                // Combinar e remover duplicados
                var jogos = jogosPorNome.Union(jogosPorGenero).Distinct();
                return GameMapper.ToDTOList(jogos);
            }
            catch
            {
                return Enumerable.Empty<GameDTO>();
            }
        }
    }
}
