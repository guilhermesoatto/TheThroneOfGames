using NUnit.Framework;
using GameStore.Catalogo.Application.DTOs;
using GameStore.Catalogo.Application.Mappers;
using GameStore.Catalogo.Domain.Entities;
using System;
using System.Linq;

namespace GameStore.Catalogo.Tests
{
    public class GameMapperTests
    {
        private Jogo CreateTestJogo(string nome = "Test Game", decimal preco = 49.99m, string genero = "Strategy")
        {
            return new Jogo(
                nome: nome,
                descricao: "Descrição de teste",
                preco: preco,
                genero: genero,
                desenvolvedora: "Test Developer",
                dataLancamento: DateTime.UtcNow,
                imagemUrl: "http://test.com/image.jpg",
                estoque: 100
            );
        }

        [Test]
        public void GameMapper_ToDTO_Converts_Jogo_To_GameDTO()
        {
            // Arrange
            var game = CreateTestJogo("The Throne of Games", 49.99m, "Strategy");

            // Act
            var dto = GameMapper.ToDTO(game);

            // Assert
            Assert.AreEqual(game.Id, dto.Id);
            Assert.AreEqual("The Throne of Games", dto.Name);
            Assert.AreEqual("Strategy", dto.Genre);
            Assert.AreEqual(49.99m, dto.Price);
            Assert.IsTrue(dto.IsAvailable);
        }

        [Test]
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
            Assert.AreEqual(gameId, game.Id);
            Assert.AreEqual("Elden Ring", game.Nome);
            Assert.AreEqual("RPG", game.Genero);
            Assert.AreEqual(59.99m, game.Preco);
        }

        [Test]
        public void GameMapper_ToDTO_Throws_With_Null_Jogo()
        {
            // Act & Assert
            Jogo? nullGame = null;
            Assert.Throws<ArgumentNullException>(() => GameMapper.ToDTO(nullGame!));
        }

        [Test]
        public void GameMapper_FromDTO_Throws_With_Null_DTO()
        {
            // Act & Assert
            GameDTO? nullDto = null;
            Assert.Throws<ArgumentNullException>(() => GameMapper.FromDTO(nullDto!));
        }

        [Test]
        public void GameMapper_ToDTOList_Converts_Multiple_Games()
        {
            // Arrange
            var games = new[]
            {
                CreateTestJogo("Game 1", 39.99m, "Action"),
                CreateTestJogo("Game 2", 49.99m, "Adventure"),
                CreateTestJogo("Game 3", 19.99m, "Puzzle")
            };

            // Act
            var dtos = GameMapper.ToDTOList(games).ToList();

            // Assert
            Assert.AreEqual(3, dtos.Count);
            Assert.AreEqual("Game 1", dtos[0].Name);
            Assert.AreEqual("Game 3", dtos[2].Name);
            Assert.AreEqual(3, dtos.Count(g => g.IsAvailable));
        }
    }
}

