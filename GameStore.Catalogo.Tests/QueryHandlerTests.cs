using FluentAssertions;
using NSubstitute;
using GameStore.Catalogo.Application.Queries;
using GameStore.Catalogo.Application.Handlers;
using GameStore.Catalogo.Domain.Interfaces;
using GameStore.Catalogo.Domain.Entities;
using GameStore.Catalogo.Infrastructure.Search;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameStore.Catalogo.Tests;

public class QueryHandlerTests
{
    private readonly IJogoRepository _jogoRepository;
    private readonly IJogoSearchIndexer _searchIndexer;

    public QueryHandlerTests()
    {
        _jogoRepository = Substitute.For<IJogoRepository>();
        _searchIndexer = Substitute.For<IJogoSearchIndexer>();
    }

    private static Jogo CreateTestJogo(string nome = "Test Game", decimal preco = 49.99m,
        string genero = "Strategy", int estoque = 100, bool disponivel = true)
    {
        var jogo = new Jogo(nome, "Descrição de teste", preco, genero,
            "Test Developer", DateTime.UtcNow, "http://test.com/image.jpg", estoque);
        if (!disponivel) jogo.Indisponibilizar();
        return jogo;
    }

    #region GetGameByIdQueryHandler

    [Fact]
    public async Task GetGameByIdQueryHandler_ExistingGame_ShouldReturnDTO()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = CreateTestJogo("Test Game", 59.99m, "Action");
        var handler = new GetGameByIdQueryHandler(_jogoRepository);

        _jogoRepository.GetByIdAsync(gameId).Returns(game);

        // Act
        var result = await handler.HandleAsync(new GetGameByIdQuery(gameId));

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Game");
        result.Price.Should().Be(59.99m);
        result.IsAvailable.Should().BeTrue();

        await _jogoRepository.Received(1).GetByIdAsync(gameId);
    }

    [Fact]
    public async Task GetGameByIdQueryHandler_NonExistingGame_ShouldReturnNull()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var handler = new GetGameByIdQueryHandler(_jogoRepository);

        _jogoRepository.GetByIdAsync(gameId).Returns((Jogo?)null);

        // Act
        var result = await handler.HandleAsync(new GetGameByIdQuery(gameId));

        // Assert
        result.Should().BeNull();
        await _jogoRepository.Received(1).GetByIdAsync(gameId);
    }

    #endregion

    #region GetGameByNameQueryHandler

    [Fact]
    public async Task GetGameByNameQueryHandler_ExistingGame_ShouldReturnDTO()
    {
        // Arrange
        var game = CreateTestJogo("Test Game", 59.99m, "Action");
        var handler = new GetGameByNameQueryHandler(_jogoRepository);

        _jogoRepository.GetByNomeAsync("Test Game").Returns(new List<Jogo> { game });

        // Act
        var result = await handler.HandleAsync(new GetGameByNameQuery("Test Game"));

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Game");
        result.Price.Should().Be(59.99m);

        await _jogoRepository.Received(1).GetByNomeAsync("Test Game");
    }

    [Fact]
    public async Task GetGameByNameQueryHandler_NonExistingGame_ShouldReturnNull()
    {
        // Arrange
        var handler = new GetGameByNameQueryHandler(_jogoRepository);

        _jogoRepository.GetByNomeAsync("Non-existent Game").Returns(new List<Jogo>());

        // Act
        var result = await handler.HandleAsync(new GetGameByNameQuery("Non-existent Game"));

        // Assert
        result.Should().BeNull();
        await _jogoRepository.Received(1).GetByNomeAsync("Non-existent Game");
    }

    #endregion

    #region GetAllGamesQueryHandler

    [Fact]
    public async Task GetAllGamesQueryHandler_HasGames_ShouldReturnAll()
    {
        // Arrange
        var games = new List<Jogo>
        {
            CreateTestJogo("Game 1", 59.99m, "Action"),
            CreateTestJogo("Game 2", 49.99m, "RPG"),
            CreateTestJogo("Game 3", 39.99m, "Strategy")
        };
        var handler = new GetAllGamesQueryHandler(_jogoRepository);

        _jogoRepository.GetAllAsync().Returns(games);

        // Act
        var result = await handler.HandleAsync(new GetAllGamesQuery());

        // Assert
        result.Should().HaveCount(3);
        result.Should().ContainSingle(g => g.Name == "Game 1");
        result.Should().ContainSingle(g => g.Name == "Game 2");
        result.Should().ContainSingle(g => g.Name == "Game 3");

        await _jogoRepository.Received(1).GetAllAsync();
    }

    [Fact]
    public async Task GetAllGamesQueryHandler_NoGames_ShouldReturnEmpty()
    {
        // Arrange
        var handler = new GetAllGamesQueryHandler(_jogoRepository);

        _jogoRepository.GetAllAsync().Returns(new List<Jogo>());

        // Act
        var result = await handler.HandleAsync(new GetAllGamesQuery());

        // Assert
        result.Should().BeEmpty();
        await _jogoRepository.Received(1).GetAllAsync();
    }

    #endregion

    #region SearchGamesQueryHandler

    [Fact]
    public async Task SearchGamesQueryHandler_ElasticsearchAvailable_ReturnsIndexedResults()
    {
        // Arrange
        var handler = new SearchGamesQueryHandler(_jogoRepository, _searchIndexer, NullLogger<SearchGamesQueryHandler>.Instance);
        var documento = new JogoSearchDocument
        {
            Id = Guid.NewGuid(),
            Nome = "Elden Ring",
            Genero = "RPG",
            Preco = 199.90m,
            Descricao = "Um RPG de mundo aberto",
            Disponivel = true
        };
        _searchIndexer.SearchAsync("elden").Returns(new List<JogoSearchDocument> { documento });

        // Act
        var result = await handler.HandleAsync(new SearchGamesQuery("elden"));

        // Assert
        result.Should().ContainSingle(g => g.Name == "Elden Ring" && g.Genre == "RPG" && g.Price == 199.90m);
        await _jogoRepository.DidNotReceive().GetByNomeAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task SearchGamesQueryHandler_ElasticsearchUnavailable_FallsBackToDatabaseSearch()
    {
        // Arrange
        var handler = new SearchGamesQueryHandler(_jogoRepository, _searchIndexer, NullLogger<SearchGamesQueryHandler>.Instance);
        var jogo = CreateTestJogo("Dark Souls", 99.90m, "RPG");

        _searchIndexer.SearchAsync("dark").Returns<Task<IReadOnlyList<JogoSearchDocument>>>(_ =>
            throw new InvalidOperationException("Elasticsearch indisponível"));
        _jogoRepository.GetByNomeAsync("dark").Returns(new List<Jogo> { jogo });
        _jogoRepository.GetByGeneroAsync("dark").Returns(new List<Jogo>());

        // Act
        var result = await handler.HandleAsync(new SearchGamesQuery("dark"));

        // Assert
        result.Should().ContainSingle(g => g.Name == "Dark Souls");
    }

    #endregion
}

