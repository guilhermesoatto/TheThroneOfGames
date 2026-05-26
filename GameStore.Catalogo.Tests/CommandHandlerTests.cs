using FluentAssertions;
using NSubstitute;
using GameStore.Catalogo.Application.Commands;
using GameStore.Catalogo.Application.Handlers;
using GameStore.Catalogo.Application.DTOs;
using GameStore.Catalogo.Domain.Interfaces;
using GameStore.Catalogo.Domain.Entities;
using GameStore.Common.Events;
using GameStore.Catalogo.Tests.Helpers;

namespace GameStore.Catalogo.Tests;

public class CommandHandlerTests
{
    private readonly IJogoRepository _jogoRepository;
    private readonly IEventBus _eventBus;

    public CommandHandlerTests()
    {
        _jogoRepository = Substitute.For<IJogoRepository>();
        _eventBus = Substitute.For<IEventBus>();
    }

    private static Jogo CreateTestJogo(string nome = "Test Game", decimal preco = 59.99m, string genero = "Action")
        => new(
            nome: nome,
            descricao: "Descrição de teste",
            preco: preco,
            genero: genero,
            desenvolvedora: "Test Developer",
            dataLancamento: DateTime.UtcNow,
            imagemUrl: "http://test.com/image.jpg",
            estoque: 100);

    #region CreateGameCommandHandler

    [Fact]
    public async Task CreateGameCommandHandler_ValidData_ShouldCreateGame()
    {
        // Arrange
        var command = new CreateGameCommand("Test Game", "Action", 59.99m, "Test Description");
        var handler = new CreateGameCommandHandler(_jogoRepository, _eventBus);

        _jogoRepository.GetByNomeAsync("Test Game").Returns(new List<Jogo>());

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Jogo criado com sucesso");
        result.EntityId.Should().NotBe(Guid.Empty);
        result.Data.Should().NotBeNull();
        ((GameDTO)result.Data!).Name.Should().Be("Test Game");

        await _jogoRepository.Received(1).AddAsync(
            Arg.Is<Jogo>(j => j.Nome == "Test Game" && j.Genero == "Action" && j.Preco == 59.99m));
    }

    [Fact]
    public async Task CreateGameCommandHandler_GameAlreadyExists_ShouldReturnError()
    {
        // Arrange
        var existingGame = CreateTestJogo("Test Game");
        var command = new CreateGameCommand("Test Game", "Action", 59.99m);
        var handler = new CreateGameCommandHandler(_jogoRepository, _eventBus);

        _jogoRepository.GetByNomeAsync("Test Game").Returns(new List<Jogo> { existingGame });

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Jogo já existe");
        result.Errors.Should().Contain("Jogo 'Test Game' já está cadastrado");

        await _jogoRepository.DidNotReceive().AddAsync(Arg.Any<Jogo>());
    }

    [Fact]
    public async Task CreateGameCommandHandler_InvalidPrice_ShouldFailValidation()
    {
        // Arrange
        var command = new CreateGameCommand("Test Game", "Action", -10m);
        var handler = new CreateGameCommandHandler(_jogoRepository, _eventBus);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Validação falhou");
        result.Errors.Should().Contain("Preço deve ser maior que zero.");

        await _jogoRepository.DidNotReceive().GetByNomeAsync(Arg.Any<string>());
    }

    #endregion

    #region UpdateGameCommandHandler

    [Fact]
    public async Task UpdateGameCommandHandler_ValidData_ShouldUpdateGame()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var existingGame = TestDataBuilder.CreateJogoWithId(gameId);
        var command = new UpdateGameCommand(gameId, "New Name", "Action", 59.99m, "Updated Description");
        var handler = new UpdateGameCommandHandler(_jogoRepository, _eventBus);

        _jogoRepository.GetByIdAsync(gameId).Returns(existingGame);
        _jogoRepository.GetByNomeAsync("New Name").Returns(new List<Jogo>());

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Jogo atualizado com sucesso");
        result.EntityId.Should().Be(gameId);
        existingGame.Nome.Should().Be("New Name");
        existingGame.Genero.Should().Be("Action");
        existingGame.Preco.Should().Be(59.99m);
        existingGame.Descricao.Should().Be("Updated Description");

        await _jogoRepository.Received(1).UpdateAsync(Arg.Is<Jogo>(g => g.Nome == "New Name"));
    }

    [Fact]
    public async Task UpdateGameCommandHandler_GameNotFound_ShouldReturnError()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var command = new UpdateGameCommand(gameId, "New Name", "Action", 59.99m);
        var handler = new UpdateGameCommandHandler(_jogoRepository, _eventBus);

        _jogoRepository.GetByIdAsync(gameId).Returns((Jogo?)null);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Jogo não encontrado");
        result.Errors.Should().Contain($"Jogo com ID {gameId} não encontrado");

        await _jogoRepository.DidNotReceive().UpdateAsync(Arg.Any<Jogo>());
    }

    #endregion
}


