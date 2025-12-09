using NUnit.Framework;
using GameStore.Catalogo.Application.DTOs;
using GameStore.Catalogo.Application.Mappers;
using TheThroneOfGames.Domain.Entities;
using System;

namespace GameStore.Catalogo.Tests
{
    public class GameMapperTests
    {
        [Test]
        public void GameMapper_ToDTO_Converts_GameEntity_To_GameDTO()
        {
            // Arrange
            var game = new GameEntity
            {
                Id = Guid.NewGuid(),
                Name = "The Throne of Games",
                Genre = "Strategy",
                Price = 49.99m
            };

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
        public void GameMapper_FromDTO_Converts_GameDTO_To_GameEntity()
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
            Assert.AreEqual("Elden Ring", game.Name);
            Assert.AreEqual("RPG", game.Genre);
            Assert.AreEqual(59.99m, game.Price);
        }

        [Test]
        public void GameMapper_ToDTO_Throws_With_Null_GameEntity()
        {
            // Act & Assert
            GameEntity? nullGame = null;
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
                new GameEntity { Id = Guid.NewGuid(), Name = "Game 1", Genre = "Action", Price = 39.99m },
                new GameEntity { Id = Guid.NewGuid(), Name = "Game 2", Genre = "Adventure", Price = 49.99m },
                new GameEntity { Id = Guid.NewGuid(), Name = "Game 3", Genre = "Puzzle", Price = 19.99m }
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
