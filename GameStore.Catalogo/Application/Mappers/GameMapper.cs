using GameStore.Catalogo.Application.DTOs;
using GameStore.Catalogo.Domain.Entities;

namespace GameStore.Catalogo.Application.Mappers
{
    /// <summary>
    /// Mapper para converter entre Jogo (bounded context) e GameDTO.
    /// Responsável por mapeamentos bidirecionais mantendo a integridade dos dados.
    /// </summary>
    public static class GameMapper
    {
        /// <summary>
        /// Converte um Jogo para GameDTO.
        /// </summary>
        public static GameDTO ToDTO(Jogo jogo)
        {
            if (jogo == null)
                throw new ArgumentNullException(nameof(jogo));

            return new GameDTO
            {
                Id = jogo.Id,
                Name = jogo.Nome,
                Genre = jogo.Genero,
                Price = jogo.Preco,
                Description = jogo.Descricao,
                IsAvailable = jogo.Disponivel,
                CreatedAt = jogo.DataLancamento, // Usando DataLancamento como CreatedAt
                UpdatedAt = jogo.DataLancamento
            };
        }

        /// <summary>
        /// Converte um GameDTO para Jogo.
        /// </summary>
        public static Jogo FromDTO(GameDTO dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return new Jogo(
                id: dto.Id,
                nome: dto.Name ?? "Sem nome",
                descricao: dto.Description ?? "Sem descrição",
                preco: dto.Price,
                genero: dto.Genre ?? "Indefinido",
                desenvolvedora: "Unknown", // Valor padrão
                dataLancamento: dto.CreatedAt,
                imagemUrl: "", // Valor padrão
                estoque: 100, // Valor padrão
                disponivel: dto.IsAvailable
            );
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
                Id = 0, // JogoDTO usa int, mas nossa entidade usa Guid - mapeamento temporário
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

            // Create a basic Jogo entity - this would need to be updated with full data
            return new Jogo(
                id: Guid.NewGuid(), // JogoDTO usa int, nossa entidade usa Guid
                nome: dto.Nome,
                descricao: "Descrição não disponível",
                preco: dto.Preco,
                genero: "Gênero não especificado",
                desenvolvedora: "Desenvolvedora não especificada",
                dataLancamento: DateTime.Now,
                imagemUrl: "",
                estoque: 0,
                disponivel: true
            );
        }

        /// <summary>
        /// Converte uma coleção de Jogos para uma coleção de GameDTOs.
        /// </summary>
        public static IEnumerable<GameDTO> ToDTOList(IEnumerable<Jogo>? jogos)
        {
            if (jogos == null)
                return Enumerable.Empty<GameDTO>();

            return jogos.Select(g => ToDTO(g) ?? new GameDTO()).ToList();
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
