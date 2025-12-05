using NUnit.Framework;
using GameStore.Catalogo.Application.DTOs;
using GameStore.Catalogo.Application.Mappers;
using TheThroneOfGames.Infrastructure.Entities;

namespace GameStore.Catalogo.Tests
{
    [TestFixture]
    public class MapperTests
    {
        #region GameMapper Tests

        [Test]
        public void GameMapper_ToDTO_ValidGame_ShouldMapCorrectly()
        {
            // Arrange
            var game = new GameEntity
            {
                Id = Guid.NewGuid(),
                Name = "Test Game",
                Genre = "Action",
                Price = 59.99m,
                Description = "A great action game",
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            // Act
            var result = GameMapper.ToDTO(game);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(game.Id));
            Assert.That(result.Name, Is.EqualTo(game.Name));
            Assert.That(result.Genre, Is.EqualTo(game.Genre));
            Assert.That(result.Price, Is.EqualTo(game.Price));
            Assert.That(result.Description, Is.EqualTo(game.Description));
            Assert.That(result.IsAvailable, Is.EqualTo(game.IsAvailable));
            Assert.That(result.CreatedAt, Is.EqualTo(game.CreatedAt));
            Assert.That(result.UpdatedAt, Is.EqualTo(game.UpdatedAt));
        }

        [Test]
        public void GameMapper_ToDTO_NullGame_ShouldThrowArgumentNullException()
        {
            // Arrange
            GameEntity game = null!;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => GameMapper.ToDTO(game));
        }

        [Test]
        public void GameMapper_ToDTOList_ValidList_ShouldMapCorrectly()
        {
            // Arrange
            var games = new List<GameEntity>
            {
                new GameEntity
                {
                    Id = Guid.NewGuid(),
                    Name = "Action Game",
                    Genre = "Action",
                    Price = 59.99m,
                    IsAvailable = true
                },
                new GameEntity
                {
                    Id = Guid.NewGuid(),
                    Name = "RPG Game",
                    Genre = "RPG",
                    Price = 49.99m,
                    IsAvailable = false
                }
            };

            // Act
            var result = GameMapper.ToDTOList(games);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(2));
            
            var firstDto = result.First();
            Assert.That(firstDto.Name, Is.EqualTo("Action Game"));
            Assert.That(firstDto.Genre, Is.EqualTo("Action"));
            Assert.That(firstDto.Price, Is.EqualTo(59.99m));
            Assert.That(firstDto.IsAvailable, Is.True);
            
            var secondDto = result.Last();
            Assert.That(secondDto.Name, Is.EqualTo("RPG Game"));
            Assert.That(secondDto.Genre, Is.EqualTo("RPG"));
            Assert.That(secondDto.Price, Is.EqualTo(49.99m));
            Assert.That(secondDto.IsAvailable, Is.False);
        }

        [Test]
        public void GameMapper_ToDTOList_EmptyList_ShouldReturnEmpty()
        {
            // Arrange
            var games = new List<GameEntity>();

            // Act
            var result = GameMapper.ToDTOList(games);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GameMapper_ToDTOList_NullList_ShouldReturnEmpty()
        {
            // Arrange
            List<GameEntity> games = null!;

            // Act
            var result = GameMapper.ToDTOList(games);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GameMapper_FromDTO_ValidDTO_ShouldMapCorrectly()
        {
            // Arrange
            var gameDto = new GameDTO
            {
                Id = Guid.NewGuid(),
                Name = "Test Game",
                Genre = "Action",
                Price = 59.99m,
                Description = "A great action game",
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            // Act
            var result = GameMapper.FromDTO(gameDto);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(gameDto.Id));
            Assert.That(result.Name, Is.EqualTo(gameDto.Name));
            Assert.That(result.Genre, Is.EqualTo(gameDto.Genre));
            Assert.That(result.Price, Is.EqualTo(gameDto.Price));
            Assert.That(result.Description, Is.EqualTo(gameDto.Description));
            Assert.That(result.IsAvailable, Is.EqualTo(gameDto.IsAvailable));
            Assert.That(result.CreatedAt, Is.EqualTo(gameDto.CreatedAt));
            Assert.That(result.UpdatedAt, Is.EqualTo(gameDto.UpdatedAt));
        }

        [Test]
        public void GameMapper_FromDTODefault_ShouldCreateValidGame()
        {
            // Arrange & Act
            var result = GameMapper.FromDTO(new GameDTO());

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(Guid.Empty));
            Assert.That(result.Name, Is.Null);
            Assert.That(result.Genre, Is.Null);
            Assert.That(result.Price, Is.EqualTo(0m));
            Assert.That(result.Description, Is.Null);
            Assert.That(result.IsAvailable, Is.False);
        }

        #endregion
    }
}
