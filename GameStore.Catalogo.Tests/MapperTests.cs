using NUnit.Framework;
using GameStore.Catalogo.Application.DTOs;
using GameStore.Catalogo.Application.Mappers;
using GameStore.Catalogo.Domain.Entities;
using GameStore.Catalogo.Tests.Helpers;

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
            var gameId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow;
            var game = TestDataBuilder.CreateJogoWithId(
                id: gameId,
                nome: "Test Game",
                genero: "Action",
                preco: 59.99m,
                descricao: "A great action game",
                dataLancamento: createdAt,
                disponivel: true
            );

            // Act
            var result = GameMapper.ToDTO(game);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(game.Id));
            Assert.That(result.Name, Is.EqualTo(game.Nome));
            Assert.That(result.Genre, Is.EqualTo(game.Genero));
            Assert.That(result.Price, Is.EqualTo(game.Preco));
            Assert.That(result.Description, Is.EqualTo(game.Descricao));
            Assert.That(result.IsAvailable, Is.EqualTo(game.Disponivel));
            Assert.That(result.CreatedAt, Is.EqualTo(game.DataLancamento));
        }

        [Test]
        public void GameMapper_ToDTO_NullGame_ShouldThrowArgumentNullException()
        {
            // Arrange
            Jogo game = null!;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => GameMapper.ToDTO(game));
        }

        [Test]
        public void GameMapper_ToDTOList_ValidList_ShouldMapCorrectly()
        {
            // Arrange
            var games = new List<Jogo>
            {
                TestDataBuilder.CreateDefaultJogo(
                    nome: "Action Game",
                    genero: "Action",
                    preco: 59.99m,
                    estoque: 10
                ),
                TestDataBuilder.CreateJogoWithId(
                    Guid.NewGuid(),
                    nome: "RPG Game",
                    genero: "RPG",
                    preco: 49.99m,
                    estoque: 0,
                    disponivel: false
                )
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
            var games = new List<Jogo>();

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
            List<Jogo> games = null!;

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
            var gameId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow;
            var gameDto = new GameDTO
            {
                Id = gameId,
                Name = "Test Game",
                Genre = "Action",
                Price = 59.99m,
                Description = "A great action game",
                IsAvailable = true,
                CreatedAt = createdAt,
                UpdatedAt = null
            };

            // Act
            var result = GameMapper.FromDTO(gameDto);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(gameDto.Id));
            Assert.That(result.Nome, Is.EqualTo(gameDto.Name));
            Assert.That(result.Genero, Is.EqualTo(gameDto.Genre));
            Assert.That(result.Preco, Is.EqualTo(gameDto.Price));
            Assert.That(result.Descricao, Is.EqualTo(gameDto.Description));
            Assert.That(result.Disponivel, Is.EqualTo(gameDto.IsAvailable));
            Assert.That(result.DataLancamento, Is.EqualTo(gameDto.CreatedAt));
        }

        [Test]
        public void GameMapper_FromDTODefault_ShouldCreateValidGame()
        {
            // Arrange & Act
            var result = GameMapper.FromDTO(new GameDTO());

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(Guid.Empty));
        }

        #endregion
    }
}

