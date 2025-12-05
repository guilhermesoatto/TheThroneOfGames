using NUnit.Framework;
using Moq;
using GameStore.Catalogo.Application.Commands;
using GameStore.Catalogo.Application.Handlers;
using GameStore.Catalogo.Application.Validators;
using GameStore.Catalogo.Application.DTOs;
using TheThroneOfGames.Domain.Interfaces;
using TheThroneOfGames.Domain.Events;
using TheThroneOfGames.Infrastructure.Entities;
using GameStore.CQRS.Abstractions;

namespace GameStore.Catalogo.Tests
{
    [TestFixture]
    public class CommandHandlerTests
    {
        private Mock<IGameRepository> _mockGameRepository = null!;
        private Mock<IEventBus> _mockEventBus = null!;

        [SetUp]
        public void Setup()
        {
            _mockGameRepository = new Mock<IGameRepository>();
            _mockEventBus = new Mock<IEventBus>();
        }

        #region CreateGameCommandHandler Tests

        [Test]
        public async Task CreateGameCommandHandler_ValidData_ShouldCreateGame()
        {
            // Arrange
            var command = new CreateGameCommand("Test Game", "Action", 59.99m, "Test Description");
            var handler = new CreateGameCommandHandler(_mockGameRepository.Object, _mockEventBus.Object);

            _mockGameRepository.Setup(r => r.GetByNameAsync("Test Game"))
                .ReturnsAsync((GameEntity?)null);
            _mockGameRepository.Setup(r => r.AddAsync(It.IsAny<GameEntity>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await handler.HandleAsync(command);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Is.EqualTo("Jogo criado com sucesso"));
            Assert.That(result.EntityId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(((GameDTO)result.Data!).Name, Is.EqualTo("Test Game"));

            _mockGameRepository.Verify(r => r.AddAsync(It.Is<GameEntity>(
                g => g.Name == "Test Game" && g.Genre == "Action" && g.Price == 59.99m)), Times.Once);
        }

        [Test]
        public async Task CreateGameCommandHandler_GameAlreadyExists_ShouldReturnError()
        {
            // Arrange
            var existingGame = new GameEntity
            {
                Id = Guid.NewGuid(),
                Name = "Test Game",
                Genre = "Action",
                Price = 49.99m,
                IsAvailable = true
            };

            var command = new CreateGameCommand("Test Game", "Action", 59.99m);
            var handler = new CreateGameCommandHandler(_mockGameRepository.Object, _mockEventBus.Object);

            _mockGameRepository.Setup(r => r.GetByNameAsync("Test Game"))
                .ReturnsAsync(existingGame);

            // Act
            var result = await handler.HandleAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Jogo já existe"));
            Assert.That(result.Errors, Contains.Item("Jogo 'Test Game' já está cadastrado"));

            _mockGameRepository.Verify(r => r.AddAsync(It.IsAny<GameEntity>()), Times.Never);
        }

        [Test]
        public async Task CreateGameCommandHandler_InvalidPrice_ShouldFailValidation()
        {
            // Arrange
            var command = new CreateGameCommand("Test Game", "Action", -10m);
            var handler = new CreateGameCommandHandler(_mockGameRepository.Object, _mockEventBus.Object);

            // Act
            var result = await handler.HandleAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Validação falhou"));
            Assert.That(result.Errors, Contains.Item("Preço deve ser maior que zero."));

            _mockGameRepository.Verify(r => r.GetByNameAsync(It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region UpdateGameCommandHandler Tests

        [Test]
        public async Task UpdateGameCommandHandler_ValidData_ShouldUpdateGame()
        {
            // Arrange
            var gameId = Guid.NewGuid();
            var existingGame = new GameEntity
            {
                Id = gameId,
                Name = "Old Name",
                Genre = "RPG",
                Price = 49.99m,
                IsAvailable = true
            };

            var command = new UpdateGameCommand(gameId, "New Name", "Action", 59.99m, "Updated Description");
            var handler = new UpdateGameCommandHandler(_mockGameRepository.Object, _mockEventBus.Object);

            _mockGameRepository.Setup(r => r.GetByIdAsync(gameId))
                .ReturnsAsync(existingGame);
            _mockGameRepository.Setup(r => r.GetByNameAsync("New Name"))
                .ReturnsAsync((GameEntity?)null);
            _mockGameRepository.Setup(r => r.UpdateAsync(It.IsAny<GameEntity>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await handler.HandleAsync(command);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Is.EqualTo("Jogo atualizado com sucesso"));
            Assert.That(result.EntityId, Is.EqualTo(gameId));
            Assert.That(existingGame.Name, Is.EqualTo("New Name"));
            Assert.That(existingGame.Genre, Is.EqualTo("Action"));
            Assert.That(existingGame.Price, Is.EqualTo(59.99m));
            Assert.That(existingGame.Description, Is.EqualTo("Updated Description"));

            _mockGameRepository.Verify(r => r.UpdateAsync(It.Is<GameEntity>(
                g => g.Name == "New Name" && g.Genre == "Action")), Times.Once);
        }

        [Test]
        public async Task UpdateGameCommandHandler_GameNotFound_ShouldReturnError()
        {
            // Arrange
            var gameId = Guid.NewGuid();
            var command = new UpdateGameCommand(gameId, "New Name", "Action", 59.99m);
            var handler = new UpdateGameCommandHandler(_mockGameRepository.Object, _mockEventBus.Object);

            _mockGameRepository.Setup(r => r.GetByIdAsync(gameId))
                .ReturnsAsync((GameEntity?)null);

            // Act
            var result = await handler.HandleAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Jogo não encontrado"));
            Assert.That(result.Errors, Contains.Item($"Jogo com ID {gameId} não encontrado"));

            _mockGameRepository.Verify(r => r.UpdateAsync(It.IsAny<GameEntity>()), Times.Never);
        }

        [Test]
        public async Task UpdateGameCommandHandler_NameAlreadyExists_ShouldReturnError()
        {
            // Arrange
            var gameId = Guid.NewGuid();
            var existingGame = new GameEntity { Id = gameId, Name = "Game 1", Genre = "RPG", Price = 49.99m };
            var otherGame = new GameEntity { Id = Guid.NewGuid(), Name = "Game 2", Genre = "Action", Price = 59.99m };

            var command = new UpdateGameCommand(gameId, "Game 2", "Action", 59.99m);
            var handler = new UpdateGameCommandHandler(_mockGameRepository.Object, _mockEventBus.Object);

            _mockGameRepository.Setup(r => r.GetByIdAsync(gameId))
                .ReturnsAsync(existingGame);
            _mockGameRepository.Setup(r => r.GetByNameAsync("Game 2"))
                .ReturnsAsync(otherGame);

            // Act
            var result = await handler.HandleAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Nome já está em uso"));
            Assert.That(result.Errors, Contains.Item("Jogo 'Game 2' já está cadastrado com outro ID"));

            _mockGameRepository.Verify(r => r.UpdateAsync(It.IsAny<GameEntity>()), Times.Never);
        }

        #endregion

        #region RemoveGameCommandHandler Tests

        [Test]
        public async Task RemoveGameCommandHandler_ValidGame_ShouldSoftDelete()
        {
            // Arrange
            var gameId = Guid.NewGuid();
            var existingGame = new GameEntity
            {
                Id = gameId,
                Name = "Test Game",
                Genre = "Action",
                Price = 59.99m,
                IsAvailable = true
            };

            var command = new RemoveGameCommand(gameId);
            var handler = new RemoveGameCommandHandler(_mockGameRepository.Object, _mockEventBus.Object);

            _mockGameRepository.Setup(r => r.GetByIdAsync(gameId))
                .ReturnsAsync(existingGame);
            _mockGameRepository.Setup(r => r.HasActivePurchasesAsync(gameId))
                .ReturnsAsync(false);
            _mockGameRepository.Setup(r => r.UpdateAsync(It.IsAny<GameEntity>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await handler.HandleAsync(command);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Is.EqualTo("Jogo removido com sucesso"));
            Assert.That(result.EntityId, Is.EqualTo(gameId));
            Assert.That(existingGame.IsAvailable, Is.False); // Soft delete

            _mockGameRepository.Verify(r => r.UpdateAsync(It.Is<GameEntity>(g => !g.IsAvailable)), Times.Once);
        }

        [Test]
        public async Task RemoveGameCommandHandler_GameNotFound_ShouldReturnError()
        {
            // Arrange
            var gameId = Guid.NewGuid();
            var command = new RemoveGameCommand(gameId);
            var handler = new RemoveGameCommandHandler(_mockGameRepository.Object, _mockEventBus.Object);

            _mockGameRepository.Setup(r => r.GetByIdAsync(gameId))
                .ReturnsAsync((GameEntity?)null);

            // Act
            var result = await handler.HandleAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Jogo não encontrado"));
            Assert.That(result.Errors, Contains.Item($"Jogo com ID {gameId} não encontrado"));

            _mockGameRepository.Verify(r => r.UpdateAsync(It.IsAny<GameEntity>()), Times.Never);
        }

        [Test]
        public async Task RemoveGameCommandHandler_HasActivePurchases_ShouldReturnError()
        {
            // Arrange
            var gameId = Guid.NewGuid();
            var existingGame = new GameEntity
            {
                Id = gameId,
                Name = "Test Game",
                Genre = "Action",
                Price = 59.99m,
                IsAvailable = true
            };

            var command = new RemoveGameCommand(gameId);
            var handler = new RemoveGameCommandHandler(_mockGameRepository.Object, _mockEventBus.Object);

            _mockGameRepository.Setup(r => r.GetByIdAsync(gameId))
                .ReturnsAsync(existingGame);
            _mockGameRepository.Setup(r => r.HasActivePurchasesAsync(gameId))
                .ReturnsAsync(true);

            // Act
            var result = await handler.HandleAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Jogo não pode ser removido"));
            Assert.That(result.Errors, Contains.Item("Jogo possui compras ativas e não pode ser removido"));

            _mockGameRepository.Verify(r => r.UpdateAsync(It.IsAny<GameEntity>()), Times.Never);
        }

        #endregion

        #region Command Validation Tests

        [Test]
        public void CatalogoValidators_CreateGameCommand_ValidData_ShouldPass()
        {
            // Arrange
            var command = new CreateGameCommand("Valid Game", "Action", 59.99m, "Valid Description");

            // Act
            var result = CatalogoValidators.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors.Count, Is.EqualTo(0));
        }

        [Test]
        public void CatalogoValidators_CreateGameCommand_EmptyName_ShouldFail()
        {
            // Arrange
            var command = new CreateGameCommand("", "Action", 59.99m);

            // Act
            var result = CatalogoValidators.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Nome do jogo é obrigatório."));
        }

        [Test]
        public void CatalogoValidators_CreateGameCommand_ZeroPrice_ShouldFail()
        {
            // Arrange
            var command = new CreateGameCommand("Valid Game", "Action", 0m);

            // Act
            var result = CatalogoValidators.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Preço deve ser maior que zero."));
        }

        [Test]
        public void CatalogoValidators_CreateGameCommand_ExcessivePrice_ShouldFail()
        {
            // Arrange
            var command = new CreateGameCommand("Valid Game", "Action", 15000m);

            // Act
            var result = CatalogoValidators.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Preço não pode exceder R$ 10.000,00."));
        }

        [Test]
        public void CatalogoValidators_UpdateGameCommand_EmptyGameId_ShouldFail()
        {
            // Arrange
            var command = new UpdateGameCommand(Guid.Empty, "Valid Game", "Action", 59.99m);

            // Act
            var result = CatalogoValidators.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("ID do jogo é obrigatório."));
        }

        [Test]
        public void CatalogoValidators_RemoveGameCommand_EmptyGameId_ShouldFail()
        {
            // Arrange
            var command = new RemoveGameCommand(Guid.Empty);

            // Act
            var result = CatalogoValidators.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("ID do jogo é obrigatório."));
        }

        #endregion
    }
}
