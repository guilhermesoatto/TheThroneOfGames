using FluentAssertions;
using GameStore.Catalogo.Application.DTOs;
using GameStore.Catalogo.Application.Mappers;
using GameStore.Catalogo.Domain.Entities;
using GameStore.Catalogo.Tests.Helpers;

namespace GameStore.Catalogo.Tests;

public class MapperTests
{
    [Fact]
    public void GameMapper_ToDTO_ValidGame_ShouldMapCorrectly()
    {
        var gameId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var game = TestDataBuilder.CreateJogoWithId(
            id: gameId, nome: "Test Game", genero: "Action",
            preco: 59.99m, descricao: "A great action game",
            dataLancamento: createdAt, disponivel: true);

        var result = GameMapper.ToDTO(game);

        result.Should().NotBeNull();
        result.Id.Should().Be(game.Id);
        result.Name.Should().Be(game.Nome);
        result.Genre.Should().Be(game.Genero);
        result.Price.Should().Be(game.Preco);
        result.IsAvailable.Should().Be(game.Disponivel);
    }

    [Fact]
    public void GameMapper_ToDTO_Null_ShouldThrow()
    {
        var act = () => GameMapper.ToDTO(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GameMapper_ToDTOList_ShouldMapAll()
    {
        var games = new List<Jogo>
        {
            TestDataBuilder.CreateDefaultJogo(nome: "Game 1", genero: "Action", preco: 59.99m),
            TestDataBuilder.CreateDefaultJogo(nome: "Game 2", genero: "RPG", preco: 49.99m)
        };

        var dtos = GameMapper.ToDTOList(games).ToList();

        dtos.Should().HaveCount(2);
        dtos[0].Name.Should().Be("Game 1");
        dtos[1].Genre.Should().Be("RPG");
    }
}
