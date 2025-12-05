using GameStore.Catalogo.Application.DTOs;
using GameStore.Catalogo.Domain.Entities;
using TheThroneOfGames.Infrastructure.Entities;

namespace GameStore.Catalogo.Application.Mappers
{
    /// <summary>
    /// Mapper para converter entre GameEntity (monolith) e GameDTO.
    /// Também mapeia para JogoDTO (entidade local) quando necessário.
    /// Responsável por mapeamentos bidirecionais mantendo a integridade dos dados.
    /// </summary>
    public static class GameMapper
    {
        /// <summary>
        /// Converte um GameEntity para GameDTO.
        /// </summary>
        public static GameDTO ToDTO(GameEntity game)
        {
            if (game == null)
                throw new ArgumentNullException(nameof(game));

            return new GameDTO
            {
                Id = game.Id,
                Name = game.Name,
                Genre = game.Genre,
                Price = game.Price,
                Description = game.Description,
                IsAvailable = game.IsAvailable,
                CreatedAt = game.CreatedAt,
                UpdatedAt = game.UpdatedAt
            };
        }

        /// <summary>
        /// Converte um GameDTO para GameEntity.
        /// </summary>
        public static GameEntity FromDTO(GameDTO dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return new GameEntity
            {
                Id = dto.Id,
                Name = dto.Name,
                Genre = dto.Genre,
                Price = dto.Price,
                Description = dto.Description,
                IsAvailable = dto.IsAvailable,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt
            };
        }

        /// <summary>
        /// Converte um Jogo (entidade local) para JogoDTO.
        /// </summary>
        public static JogoDTO ToJogoDTO(Jogo jogo)
        {
            if (jogo == null)
                throw new ArgumentNullException(nameof(jogo));

            return new JogoDTO
            {
                Id = jogo.Id,
                Nome = jogo.Nome,
                Preco = jogo.Preco
            };
        }

        /// <summary>
        /// Converte um JogoDTO para Jogo (entidade local).
        /// </summary>
        public static Jogo FromJogoDTO(JogoDTO dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return new Jogo
            {
                Id = dto.Id,
                Nome = dto.Nome,
                Preco = dto.Preco
            };
        }

        /// <summary>
        /// Converte uma coleção de GameEntities para uma coleção de GameDTOs.
        /// </summary>
        public static IEnumerable<GameDTO> ToDTOList(IEnumerable<GameEntity>? games)
        {
            if (games == null)
                return Enumerable.Empty<GameDTO>();

            return games.Select(g => ToDTO(g) ?? new GameDTO()).ToList();
        }

        /// <summary>
        /// Converte uma coleção de Jogos para uma coleção de JogoDTOs.
        /// </summary>
        public static IEnumerable<JogoDTO> ToJogoDTOList(IEnumerable<Jogo> jogos)
        {
            if (jogos == null)
                throw new ArgumentNullException(nameof(jogos));

            return jogos.Select(ToJogoDTO).ToList();
        }
    }
}
