using NUnit.Framework;
using Moq;
using GameStore.Catalogo.Application.Queries;
using GameStore.Catalogo.Application.Handlers;
using GameStore.Catalogo.Application.DTOs;
using GameStore.Catalogo.Domain.Interfaces;
using GameStore.Catalogo.Domain.Entities;
using GameStore.CQRS.Abstractions;

namespace GameStore.Catalogo.Tests{[TestFixture]public class QueryHandlerTests
    {
        private Jogo CreateTestJogo(string nome = "Test Game", decimal preco = 49.99m, string genero = "Strategy", int estoque = 100, bool? disponivel = null)
        {
            var jogo = new Jogo(
                nome: nome,
                descricao: "Descri��o de teste",
                preco: preco,
                genero: genero,
                desenvolvedora: "Test Developer",
                dataLancamento: DateTime.UtcNow,
                imagemUrl: "http://test.com/image.jpg",
                estoque: estoque
            );
            if (disponivel.HasValue && !disponivel.Value) { jogo.Indisponibilizar(); }
            return jogo;
        }

        private Mock<IJogoRepository> _mockJogoRepository = null!;

        [SetUp]
        public void Setup()
        {
            _mockJogoRepository = new Mock<IJogoRepository>();
        }

        #region GetGameByIdQueryHandler Tests

        [Test]
        public async Task GetGameByIdQueryHandler_ExistingGame_ShouldReturnDTO()
        {
            // Arrange
            var gameId = Guid.NewGuid();
            var game = CreateTestJogo("Test Game", 59.99m, "Action");

            var query = new GetGameByIdQuery(gameId);
            var handler = new GetGameByIdQueryHandler(_mockJogoRepository.Object);

            _mockJogoRepository.Setup(r => r.GetByIdAsync(gameId))
                .ReturnsAsync(game);

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("Test Game"));
            Assert.That(result.Price, Is.EqualTo(59.99m));
            Assert.That(result.IsAvailable, Is.True);

            _mockJogoRepository.Verify(r => r.GetByIdAsync(gameId), Times.Once);
        }

        [Test]
        public async Task GetGameByIdQueryHandler_NonExistingGame_ShouldReturnNull()
        {
            // Arrange
            var gameId = Guid.NewGuid();
            var query = new GetGameByIdQuery(gameId);
            var handler = new GetGameByIdQueryHandler(_mockJogoRepository.Object);

            _mockJogoRepository.Setup(r => r.GetByIdAsync(gameId))
                .ReturnsAsync((Jogo?)null);

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Null);
            _mockJogoRepository.Verify(r => r.GetByIdAsync(gameId), Times.Once);
        }

        #endregion

        #region GetGameByNameQueryHandler Tests

        [Test]
        public async Task GetGameByNameQueryHandler_ExistingGame_ShouldReturnDTO()
        {
            // Arrange
            var gameName = "Test Game";
            var game = CreateTestJogo(gameName, 59.99m, "Action");

            var query = new GetGameByNameQuery(gameName);
            var handler = new GetGameByNameQueryHandler(_mockJogoRepository.Object);

            _mockJogoRepository.Setup(r => r.GetByNomeAsync(gameName))
                .ReturnsAsync(new List<Jogo> { game });

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo(gameName));
            Assert.That(result.Price, Is.EqualTo(59.99m));

            _mockJogoRepository.Verify(r => r.GetByNomeAsync(gameName), Times.Once);
        }

        [Test]
        public async Task GetGameByNameQueryHandler_NonExistingGame_ShouldReturnNull()
        {
            // Arrange
            var gameName = "Non-existent Game";
            var query = new GetGameByNameQuery(gameName);
            var handler = new GetGameByNameQueryHandler(_mockJogoRepository.Object);

            _mockJogoRepository.Setup(r => r.GetByNomeAsync(gameName))
                .ReturnsAsync(new List<Jogo>());

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Null);
            _mockJogoRepository.Verify(r => r.GetByNomeAsync(gameName), Times.Once);
        }

        #endregion

        #region GetAllGamesQueryHandler Tests

        [Test]
        public async Task GetAllGamesQueryHandler_HasGames_ShouldReturnAll()
        {
            // Arrange
            var games = new List<Jogo>
            {
                CreateTestJogo("Game 1", 59.99m, "Action"),
                CreateTestJogo("Game 2", 49.99m, "RPG"),
                CreateTestJogo("Game 3", 39.99m, "Strategy")
            };

            var query = new GetAllGamesQuery();
            var handler = new GetAllGamesQueryHandler(_mockJogoRepository.Object);

            _mockJogoRepository.Setup(r => r.GetAllAsync())
                .ReturnsAsync(games);

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(3));
            Assert.That(result.Any(g => g.Name == "Game 1"), Is.True);
            Assert.That(result.Any(g => g.Name == "Game 2"), Is.True);
            Assert.That(result.Any(g => g.Name == "Game 3"), Is.True);

            _mockJogoRepository.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Test]
        public async Task GetAllGamesQueryHandler_NoGames_ShouldReturnEmpty()
        {
            // Arrange
            var query = new GetAllGamesQuery();
            var handler = new GetAllGamesQueryHandler(_mockJogoRepository.Object);

            _mockJogoRepository.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Jogo>());

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
            _mockJogoRepository.Verify(r => r.GetAllAsync(), Times.Once);
        }

        #endregion

        #region GetGamesByGenreQueryHandler Tests

        [Test]
        public async Task GetGamesByGenreQueryHandler_ExistingGenre_ShouldReturnGames()
        {
            // Arrange
            var genre = "Action";
            var games = new List<Jogo>
            {
                CreateTestJogo("Game 1", 59.99m, genre),
                CreateTestJogo("Game 2", 49.99m, genre)
            };

            var query = new GetGamesByGenreQuery(genre);
            var handler = new GetGamesByGenreQueryHandler(_mockJogoRepository.Object);

            _mockJogoRepository.Setup(r => r.GetByGeneroAsync(genre))
                .ReturnsAsync(games);

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(result.All(g => g.Genre == genre), Is.True);

            _mockJogoRepository.Verify(r => r.GetByGeneroAsync(genre), Times.Once);
        }

        [Test]
        public async Task GetGamesByGenreQueryHandler_NoGamesInGenre_ShouldReturnEmpty()
        {
            // Arrange
            var genre = "NonExistentGenre";
            var query = new GetGamesByGenreQuery(genre);
            var handler = new GetGamesByGenreQueryHandler(_mockJogoRepository.Object);

            _mockJogoRepository.Setup(r => r.GetByGeneroAsync(genre))
                .ReturnsAsync(new List<Jogo>());

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
            _mockJogoRepository.Verify(r => r.GetByGeneroAsync(genre), Times.Once);
        }

        #endregion

        #region GetAvailableGamesQueryHandler Tests

        [Test]
        public async Task GetAvailableGamesQueryHandler_HasAvailableGames_ShouldReturnAvailableGames()
        {
            // Arrange
            var availableGames = new List<Jogo>
            {
                CreateTestJogo("Game 1", 59.99m),
                CreateTestJogo("Game 2", 49.99m)
            };

            var query = new GetAvailableGamesQuery();
            var handler = new GetAvailableGamesQueryHandler(_mockJogoRepository.Object);

            _mockJogoRepository.Setup(r => r.GetDisponiveisAsync())
                .ReturnsAsync(availableGames);

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(result.All(g => g.IsAvailable), Is.True);

            _mockJogoRepository.Verify(r => r.GetDisponiveisAsync(), Times.Once);
        }

        [Test]
        public async Task GetAvailableGamesQueryHandler_NoAvailableGames_ShouldReturnEmpty()
        {
            // Arrange
            var query = new GetAvailableGamesQuery();
            var handler = new GetAvailableGamesQueryHandler(_mockJogoRepository.Object);

            _mockJogoRepository.Setup(r => r.GetDisponiveisAsync())
                .ReturnsAsync(new List<Jogo>());

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
            _mockJogoRepository.Verify(r => r.GetDisponiveisAsync(), Times.Once);
        }

        #endregion

        #region GetGamesByPriceRangeQueryHandler Tests

        [Test]
        public async Task GetGamesByPriceRangeQueryHandler_ValidRange_ShouldReturnGames()
        {
            // Arrange
            var minPrice = 20m;
            var maxPrice = 60m;
            var games = new List<Jogo>
            {
                CreateTestJogo("Game 1", 25m),
                CreateTestJogo("Game 2", 45m),
                CreateTestJogo("Game 3", 55m)
            };

            var query = new GetGamesByPriceRangeQuery(minPrice, maxPrice);
            var handler = new GetGamesByPriceRangeQueryHandler(_mockJogoRepository.Object);

            _mockJogoRepository.Setup(r => r.GetByFaixaPrecoAsync(minPrice, maxPrice))
                .ReturnsAsync(games);

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(3));
            Assert.That(result.All(g => g.Price >= minPrice && g.Price <= maxPrice), Is.True);

            _mockJogoRepository.Verify(r => r.GetByFaixaPrecoAsync(minPrice, maxPrice), Times.Once);
        }

        [Test]
        public async Task GetGamesByPriceRangeQueryHandler_NoGamesInRange_ShouldReturnEmpty()
        {
            // Arrange
            var minPrice = 100m;
            var maxPrice = 200m;
            var query = new GetGamesByPriceRangeQuery(minPrice, maxPrice);
            var handler = new GetGamesByPriceRangeQueryHandler(_mockJogoRepository.Object);

            _mockJogoRepository.Setup(r => r.GetByFaixaPrecoAsync(minPrice, maxPrice))
                .ReturnsAsync(new List<Jogo>());

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
            _mockJogoRepository.Verify(r => r.GetByFaixaPrecoAsync(minPrice, maxPrice), Times.Once);
        }

        #endregion


    }
}



