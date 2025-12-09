using NUnit.Framework;
using Moq;
using GameStore.Catalogo.Application.Queries;
using GameStore.Catalogo.Application.Handlers;
using GameStore.Catalogo.Application.DTOs;
using TheThroneOfGames.Domain.Interfaces;
using TheThroneOfGames.Domain.Entities;
using GameStore.CQRS.Abstractions;

namespace GameStore.Catalogo.Tests
{
    [TestFixture]
    public class QueryHandlerTests
    {
        private Mock<IGameRepository> _mockGameRepository = null!;

        [SetUp]
        public void Setup()
        {
            _mockGameRepository = new Mock<IGameRepository>();
        }

        #region GetGameByIdQueryHandler Tests

        [Test]
        public async Task GetGameByIdQueryHandler_ExistingGame_ShouldReturnDTO()
        {
            // Arrange
            var gameId = Guid.NewGuid();
            var game = new GameEntity
            {
                Id = gameId,
                Name = "Test Game",
                Genre = "Action",
                Price = 59.99m,
                Description = "Test Description",
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            var query = new GetGameByIdQuery(gameId);
            var handler = new GetGameByIdQueryHandler(_mockGameRepository.Object);

            _mockGameRepository.Setup(r => r.GetByIdAsync(gameId))
                .ReturnsAsync(game);

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Id, Is.EqualTo(gameId));
            Assert.That(result.Name, Is.EqualTo("Test Game"));
            Assert.That(result.Price, Is.EqualTo(59.99m));
            Assert.That(result.IsAvailable, Is.True);

            _mockGameRepository.Verify(r => r.GetByIdAsync(gameId), Times.Once);
        }

        [Test]
        public async Task GetGameByIdQueryHandler_NonExistingGame_ShouldReturnNull()
        {
            // Arrange
            var gameId = Guid.NewGuid();
            var query = new GetGameByIdQuery(gameId);
            var handler = new GetGameByIdQueryHandler(_mockGameRepository.Object);

            _mockGameRepository.Setup(r => r.GetByIdAsync(gameId))
                .ReturnsAsync((GameEntity?)null);

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Null);
            _mockGameRepository.Verify(r => r.GetByIdAsync(gameId), Times.Once);
        }

        #endregion

        #region GetGameByNameQueryHandler Tests

        [Test]
        public async Task GetGameByNameQueryHandler_ExistingGame_ShouldReturnDTO()
        {
            // Arrange
            var gameName = "Test Game";
            var game = new GameEntity
            {
                Id = Guid.NewGuid(),
                Name = gameName,
                Genre = "Action",
                Price = 59.99m,
                IsAvailable = true
            };

            var query = new GetGameByNameQuery(gameName);
            var handler = new GetGameByNameQueryHandler(_mockGameRepository.Object);

            _mockGameRepository.Setup(r => r.GetByNameAsync(gameName))
                .ReturnsAsync(game);

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo(gameName));
            Assert.That(result.Price, Is.EqualTo(59.99m));

            _mockGameRepository.Verify(r => r.GetByNameAsync(gameName), Times.Once);
        }

        [Test]
        public async Task GetGameByNameQueryHandler_NonExistingGame_ShouldReturnNull()
        {
            // Arrange
            var gameName = "Non-existent Game";
            var query = new GetGameByNameQuery(gameName);
            var handler = new GetGameByNameQueryHandler(_mockGameRepository.Object);

            _mockGameRepository.Setup(r => r.GetByNameAsync(gameName))
                .ReturnsAsync((GameEntity?)null);

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Null);
            _mockGameRepository.Verify(r => r.GetByNameAsync(gameName), Times.Once);
        }

        #endregion

        #region GetAllGamesQueryHandler Tests

        [Test]
        public async Task GetAllGamesQueryHandler_HasGames_ShouldReturnAll()
        {
            // Arrange
            var games = new List<GameEntity>
            {
                new GameEntity { Id = Guid.NewGuid(), Name = "Game 1", Genre = "Action", Price = 59.99m },
                new GameEntity { Id = Guid.NewGuid(), Name = "Game 2", Genre = "RPG", Price = 49.99m },
                new GameEntity { Id = Guid.NewGuid(), Name = "Game 3", Genre = "Strategy", Price = 39.99m }
            };

            var query = new GetAllGamesQuery();
            var handler = new GetAllGamesQueryHandler(_mockGameRepository.Object);

            _mockGameRepository.Setup(r => r.GetAllAsync())
                .ReturnsAsync(games);

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(3));
            Assert.That(result.Any(g => g.Name == "Game 1"), Is.True);
            Assert.That(result.Any(g => g.Name == "Game 2"), Is.True);
            Assert.That(result.Any(g => g.Name == "Game 3"), Is.True);

            _mockGameRepository.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Test]
        public async Task GetAllGamesQueryHandler_NoGames_ShouldReturnEmpty()
        {
            // Arrange
            var query = new GetAllGamesQuery();
            var handler = new GetAllGamesQueryHandler(_mockGameRepository.Object);

            _mockGameRepository.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<GameEntity>());

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
            _mockGameRepository.Verify(r => r.GetAllAsync(), Times.Once);
        }

        #endregion

        #region GetGamesByGenreQueryHandler Tests

        [Test]
        public async Task GetGamesByGenreQueryHandler_ExistingGenre_ShouldReturnGames()
        {
            // Arrange
            var genre = "Action";
            var games = new List<GameEntity>
            {
                new GameEntity { Id = Guid.NewGuid(), Name = "Game 1", Genre = genre, Price = 59.99m },
                new GameEntity { Id = Guid.NewGuid(), Name = "Game 2", Genre = genre, Price = 49.99m }
            };

            var query = new GetGamesByGenreQuery(genre);
            var handler = new GetGamesByGenreQueryHandler(_mockGameRepository.Object);

            _mockGameRepository.Setup(r => r.GetByGenreAsync(genre))
                .ReturnsAsync(games);

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(result.All(g => g.Genre == genre), Is.True);

            _mockGameRepository.Verify(r => r.GetByGenreAsync(genre), Times.Once);
        }

        [Test]
        public async Task GetGamesByGenreQueryHandler_NoGamesInGenre_ShouldReturnEmpty()
        {
            // Arrange
            var genre = "NonExistentGenre";
            var query = new GetGamesByGenreQuery(genre);
            var handler = new GetGamesByGenreQueryHandler(_mockGameRepository.Object);

            _mockGameRepository.Setup(r => r.GetByGenreAsync(genre))
                .ReturnsAsync(new List<GameEntity>());

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
            _mockGameRepository.Verify(r => r.GetByGenreAsync(genre), Times.Once);
        }

        #endregion

        #region GetAvailableGamesQueryHandler Tests

        [Test]
        public async Task GetAvailableGamesQueryHandler_HasAvailableGames_ShouldReturnAvailableGames()
        {
            // Arrange
            var availableGames = new List<GameEntity>
            {
                new GameEntity { Id = Guid.NewGuid(), Name = "Game 1", IsAvailable = true, Price = 59.99m },
                new GameEntity { Id = Guid.NewGuid(), Name = "Game 2", IsAvailable = true, Price = 49.99m }
            };

            var query = new GetAvailableGamesQuery();
            var handler = new GetAvailableGamesQueryHandler(_mockGameRepository.Object);

            _mockGameRepository.Setup(r => r.GetAvailableGamesAsync())
                .ReturnsAsync(availableGames);

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(result.All(g => g.IsAvailable), Is.True);

            _mockGameRepository.Verify(r => r.GetAvailableGamesAsync(), Times.Once);
        }

        [Test]
        public async Task GetAvailableGamesQueryHandler_NoAvailableGames_ShouldReturnEmpty()
        {
            // Arrange
            var query = new GetAvailableGamesQuery();
            var handler = new GetAvailableGamesQueryHandler(_mockGameRepository.Object);

            _mockGameRepository.Setup(r => r.GetAvailableGamesAsync())
                .ReturnsAsync(new List<GameEntity>());

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
            _mockGameRepository.Verify(r => r.GetAvailableGamesAsync(), Times.Once);
        }

        #endregion

        #region GetGamesByPriceRangeQueryHandler Tests

        [Test]
        public async Task GetGamesByPriceRangeQueryHandler_ValidRange_ShouldReturnGames()
        {
            // Arrange
            var minPrice = 20m;
            var maxPrice = 60m;
            var games = new List<GameEntity>
            {
                new GameEntity { Id = Guid.NewGuid(), Name = "Game 1", Price = 25m },
                new GameEntity { Id = Guid.NewGuid(), Name = "Game 2", Price = 45m },
                new GameEntity { Id = Guid.NewGuid(), Name = "Game 3", Price = 55m }
            };

            var query = new GetGamesByPriceRangeQuery(minPrice, maxPrice);
            var handler = new GetGamesByPriceRangeQueryHandler(_mockGameRepository.Object);

            _mockGameRepository.Setup(r => r.GetByPriceRangeAsync(minPrice, maxPrice))
                .ReturnsAsync(games);

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(3));
            Assert.That(result.All(g => g.Price >= minPrice && g.Price <= maxPrice), Is.True);

            _mockGameRepository.Verify(r => r.GetByPriceRangeAsync(minPrice, maxPrice), Times.Once);
        }

        [Test]
        public async Task GetGamesByPriceRangeQueryHandler_NoGamesInRange_ShouldReturnEmpty()
        {
            // Arrange
            var minPrice = 100m;
            var maxPrice = 200m;
            var query = new GetGamesByPriceRangeQuery(minPrice, maxPrice);
            var handler = new GetGamesByPriceRangeQueryHandler(_mockGameRepository.Object);

            _mockGameRepository.Setup(r => r.GetByPriceRangeAsync(minPrice, maxPrice))
                .ReturnsAsync(new List<GameEntity>());

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
            _mockGameRepository.Verify(r => r.GetByPriceRangeAsync(minPrice, maxPrice), Times.Once);
        }

        #endregion

        #region SearchGamesQueryHandler Tests

        [Test]
        public async Task SearchGamesQueryHandler_ValidSearchTerm_ShouldReturnGames()
        {
            // Arrange
            var searchTerm = "Action";
            var games = new List<GameEntity>
            {
                new GameEntity { Id = Guid.NewGuid(), Name = "Action Game 1", Genre = "Action" },
                new GameEntity { Id = Guid.NewGuid(), Name = "Super Action", Genre = "Adventure" }
            };

            var query = new SearchGamesQuery(searchTerm);
            var handler = new SearchGamesQueryHandler(_mockGameRepository.Object);

            _mockGameRepository.Setup(r => r.SearchGamesAsync(searchTerm))
                .ReturnsAsync(games);

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(result.Any(g => g.Name.Contains(searchTerm) || g.Genre.Contains(searchTerm)), Is.True);

            _mockGameRepository.Verify(r => r.SearchGamesAsync(searchTerm), Times.Once);
        }

        [Test]
        public async Task SearchGamesQueryHandler_NoResults_ShouldReturnEmpty()
        {
            // Arrange
            var searchTerm = "NonExistentGame";
            var query = new SearchGamesQuery(searchTerm);
            var handler = new SearchGamesQueryHandler(_mockGameRepository.Object);

            _mockGameRepository.Setup(r => r.SearchGamesAsync(searchTerm))
                .ReturnsAsync(new List<GameEntity>());

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
            _mockGameRepository.Verify(r => r.SearchGamesAsync(searchTerm), Times.Once);
        }

        #endregion
    }
}
