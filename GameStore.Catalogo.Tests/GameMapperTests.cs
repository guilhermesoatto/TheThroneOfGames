using FluentAssertions;
using GameStore.Catalogo.Application.DTOs;
using GameStore.Catalogo.Application.Mappers;
using GameStore.Catalogo.Domain.Entities;

namespace GameStore.Catalogo.Tests;

public class GameMapperTests
{
    private static Jogo CreateTestJogo(string nome = "Test Game", decimal preco = 49.99m, string genero = "Strategy")
        => new(nome, "Descrição de teste", preco, genero,
            "Test Developer", DateTime.UtcNow, "http://test.com/image.jpg", 100);

    [Fact]
    public void GameMapper_ToDTO_Converts_Jogo_To_GameDTO()
    {
        // Arrange
        var game = CreateTestJogo("The Throne of Games", 49.99m, "Strategy");

        // Act
        var dto = GameMapper.ToDTO(game);

        // Assert
        dto.Id.Should().Be(game.Id);
        dto.Name.Should().Be("The Throne of Games");
        dto.Genre.Should().Be("Strategy");
        dto.Price.Should().Be(49.99m);
        dto.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void GameMapper_FromDTO_Converts_GameDTO_To_Jogo()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var dto = new GameDTO
        {
            Id = gameId,
            Name = "Elden Ring",
            Genre = "RPG",
            Price = 59.99m,
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var game = GameMapper.FromDTO(dto);

        // Assert
        game.Id.Should().Be(gameId);
        game.Nome.Should().Be("Elden Ring");
        game.Genero.Should().Be("RPG");
        game.Preco.Should().Be(59.99m);
    }

    [Fact]
    public void GameMapper_ToDTO_Throws_With_Null_Jogo()
    {
        // Act
        var act = () => GameMapper.ToDTO(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GameMapper_FromDTO_Throws_With_Null_DTO()
    {
        // Act
        var act = () => GameMapper.FromDTO(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

}
