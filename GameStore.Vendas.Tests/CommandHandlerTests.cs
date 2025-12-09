using NUnit.Framework;
using Moq;
using GameStore.Vendas.Application.Commands;
using GameStore.Vendas.Application.Handlers;
using GameStore.Vendas.Application.Validators;
using GameStore.Vendas.Application.DTOs;
using GameStore.Vendas.Application.Mappers;
using TheThroneOfGames.Domain.Entities;
using TheThroneOfGames.Domain.Interfaces;
using TheThroneOfGames.Domain.Events;

namespace GameStore.Vendas.Tests
{
    [TestFixture]
    public class CommandHandlerTests
    {
        private Mock<IPurchaseRepository> _mockPurchaseRepository = null!;
        private Mock<IGameRepository> _mockGameRepository = null!;
        private Mock<IEventBus> _mockEventBus = null!;

        [SetUp]
        public void Setup()
        {
            _mockPurchaseRepository = new Mock<IPurchaseRepository>();
            _mockGameRepository = new Mock<IGameRepository>();
            _mockEventBus = new Mock<IEventBus>();
        }

        #region CreatePurchaseCommandHandler Tests

        [Test]
        public async Task CreatePurchaseCommandHandler_ValidData_ShouldCreatePurchaseAndPublishEvent()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var gameId = Guid.NewGuid();
            var price = 59.99m;
            var game = new GameEntity
            {
                Id = gameId,
                Name = "Test Game",
                Genre = "Action",
                Price = price,
                IsAvailable = true
            };

            var command = new CreatePurchaseCommand(userId, gameId, price);
            var handler = new CreatePurchaseCommandHandler(
                _mockPurchaseRepository.Object, 
                _mockGameRepository.Object, 
                _mockEventBus.Object);

            _mockGameRepository.Setup(r => r.GetByIdAsync(gameId))
                .ReturnsAsync(game);
            _mockPurchaseRepository.Setup(r => r.GetUserPurchaseAsync(userId, gameId))
                .ReturnsAsync((Purchase?)null);
            _mockPurchaseRepository.Setup(r => r.AddAsync(It.IsAny<Purchase>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await handler.HandleAsync(command);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Is.EqualTo("Compra criada com sucesso"));
            Assert.That(result.EntityId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result.Data, Is.Not.Null);

            _mockEventBus.Verify(e => e.PublishAsync(It.Is<GameCompradoEvent>(
                evt => evt.GameId == gameId && evt.UserId == userId && evt.Preco == price)), Times.Once);
        }

        [Test]
        public async Task CreatePurchaseCommandHandler_GameNotAvailable_ShouldReturnError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var gameId = Guid.NewGuid();
            var game = new GameEntity
            {
                Id = gameId,
                Name = "Test Game",
                Genre = "Action",
                Price = 59.99m,
                IsAvailable = false // Não disponível
            };

            var command = new CreatePurchaseCommand(userId, gameId, 59.99m);
            var handler = new CreatePurchaseCommandHandler(
                _mockPurchaseRepository.Object, 
                _mockGameRepository.Object, 
                _mockEventBus.Object);

            _mockGameRepository.Setup(r => r.GetByIdAsync(gameId))
                .ReturnsAsync(game);

            // Act
            var result = await handler.HandleAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Jogo não disponível"));
            Assert.That(result.Errors, Contains.Item("Jogo não está disponível para compra"));

            _mockEventBus.Verify(e => e.PublishAsync(It.IsAny<GameCompradoEvent>()), Times.Never);
        }

        [Test]
        public async Task CreatePurchaseCommandHandler_GameNotFound_ShouldReturnError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var gameId = Guid.NewGuid();
            var command = new CreatePurchaseCommand(userId, gameId, 59.99m);
            var handler = new CreatePurchaseCommandHandler(
                _mockPurchaseRepository.Object, 
                _mockGameRepository.Object, 
                _mockEventBus.Object);

            _mockGameRepository.Setup(r => r.GetByIdAsync(gameId))
                .ReturnsAsync((GameEntity?)null);

            // Act
            var result = await handler.HandleAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Jogo não encontrado"));
            Assert.That(result.Errors, Contains.Item($"Jogo com ID {gameId} não encontrado"));

            _mockEventBus.Verify(e => e.PublishAsync(It.IsAny<GameCompradoEvent>()), Times.Never);
        }

        [Test]
        public async Task CreatePurchaseCommandHandler_AlreadyPurchased_ShouldReturnError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var gameId = Guid.NewGuid();
            var existingPurchase = new Purchase
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                GameId = gameId,
                TotalPrice = 59.99m,
                Status = "Completed"
            };

            var game = new GameEntity
            {
                Id = gameId,
                Name = "Test Game",
                Genre = "Action",
                Price = 59.99m,
                IsAvailable = true
            };

            var command = new CreatePurchaseCommand(userId, gameId, 59.99m);
            var handler = new CreatePurchaseCommandHandler(
                _mockPurchaseRepository.Object, 
                _mockGameRepository.Object, 
                _mockEventBus.Object);

            _mockGameRepository.Setup(r => r.GetByIdAsync(gameId))
                .ReturnsAsync(game);
            _mockPurchaseRepository.Setup(r => r.GetUserPurchaseAsync(userId, gameId))
                .ReturnsAsync(existingPurchase);

            // Act
            var result = await handler.HandleAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Jogo já comprado"));
            Assert.That(result.Errors, Contains.Item("Usuário já possui este jogo"));

            _mockEventBus.Verify(e => e.PublishAsync(It.IsAny<GameCompradoEvent>()), Times.Never);
        }

        #endregion

        #region FinalizePurchaseCommandHandler Tests

        [Test]
        public async Task FinalizePurchaseCommandHandler_ValidPurchase_ShouldFinalizeAndPublishEvent()
        {
            // Arrange
            var purchaseId = Guid.NewGuid();
            var purchase = new Purchase
            {
                Id = purchaseId,
                UserId = Guid.NewGuid(),
                GameId = Guid.NewGuid(),
                TotalPrice = 59.99m,
                Status = "Pending"
            };

            var command = new FinalizePurchaseCommand(purchaseId, "CreditCard");
            var handler = new FinalizePurchaseCommandHandler(_mockPurchaseRepository.Object, _mockEventBus.Object);

            _mockPurchaseRepository.Setup(r => r.GetByIdAsync(purchaseId))
                .ReturnsAsync(purchase);
            _mockPurchaseRepository.Setup(r => r.UpdateAsync(It.IsAny<Purchase>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await handler.HandleAsync(command);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Is.EqualTo("Compra finalizada com sucesso"));
            Assert.That(result.EntityId, Is.EqualTo(purchaseId));
            Assert.That(purchase.Status, Is.EqualTo("Completed"));
            Assert.That(purchase.PaymentMethod, Is.EqualTo("CreditCard"));

            _mockEventBus.Verify(e => e.PublishAsync(It.Is<PedidoFinalizadoEvent>(
                evt => evt.PedidoId == purchaseId)), Times.Once);
        }

        [Test]
        public async Task FinalizePurchaseCommandHandler_PurchaseNotFound_ShouldReturnError()
        {
            // Arrange
            var purchaseId = Guid.NewGuid();
            var command = new FinalizePurchaseCommand(purchaseId);
            var handler = new FinalizePurchaseCommandHandler(_mockPurchaseRepository.Object, _mockEventBus.Object);

            _mockPurchaseRepository.Setup(r => r.GetByIdAsync(purchaseId))
                .ReturnsAsync((Purchase?)null);

            // Act
            var result = await handler.HandleAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Compra não encontrada"));
            Assert.That(result.Errors, Contains.Item($"Compra com ID {purchaseId} não encontrada"));

            _mockEventBus.Verify(e => e.PublishAsync(It.IsAny<PedidoFinalizadoEvent>()), Times.Never);
        }

        [Test]
        public async Task FinalizePurchaseCommandHandler_AlreadyCompleted_ShouldReturnError()
        {
            // Arrange
            var purchaseId = Guid.NewGuid();
            var purchase = new Purchase
            {
                Id = purchaseId,
                UserId = Guid.NewGuid(),
                GameId = Guid.NewGuid(),
                TotalPrice = 59.99m,
                Status = "Completed" // Já finalizada
            };

            var command = new FinalizePurchaseCommand(purchaseId);
            var handler = new FinalizePurchaseCommandHandler(_mockPurchaseRepository.Object, _mockEventBus.Object);

            _mockPurchaseRepository.Setup(r => r.GetByIdAsync(purchaseId))
                .ReturnsAsync(purchase);

            // Act
            var result = await handler.HandleAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Compra já processada"));
            Assert.That(result.Errors, Contains.Item($"Compra já está com status: {purchase.Status}"));

            _mockEventBus.Verify(e => e.PublishAsync(It.IsAny<PedidoFinalizadoEvent>()), Times.Never);
        }

        #endregion

        #region CancelPurchaseCommandHandler Tests

        [Test]
        public async Task CancelPurchaseCommandHandler_ValidPurchase_ShouldCancel()
        {
            // Arrange
            var purchaseId = Guid.NewGuid();
            var reason = "Customer request";
            var purchase = new Purchase
            {
                Id = purchaseId,
                UserId = Guid.NewGuid(),
                GameId = Guid.NewGuid(),
                TotalPrice = 59.99m,
                Status = "Pending"
            };

            var command = new CancelPurchaseCommand(purchaseId, reason);
            var handler = new CancelPurchaseCommandHandler(_mockPurchaseRepository.Object, _mockEventBus.Object);

            _mockPurchaseRepository.Setup(r => r.GetByIdAsync(purchaseId))
                .ReturnsAsync(purchase);
            _mockPurchaseRepository.Setup(r => r.UpdateAsync(It.IsAny<Purchase>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await handler.HandleAsync(command);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Is.EqualTo("Compra cancelada com sucesso"));
            Assert.That(result.EntityId, Is.EqualTo(purchaseId));
            Assert.That(purchase.Status, Is.EqualTo("Cancelled"));
            Assert.That(purchase.CancellationReason, Is.EqualTo(reason));

            _mockPurchaseRepository.Verify(r => r.UpdateAsync(It.Is<Purchase>(
                p => p.Status == "Cancelled" && p.CancellationReason == reason)), Times.Once);
        }

        [Test]
        public async Task CancelPurchaseCommandHandler_PurchaseNotFound_ShouldReturnError()
        {
            // Arrange
            var purchaseId = Guid.NewGuid();
            var command = new CancelPurchaseCommand(purchaseId, "Reason");
            var handler = new CancelPurchaseCommandHandler(_mockPurchaseRepository.Object, _mockEventBus.Object);

            _mockPurchaseRepository.Setup(r => r.GetByIdAsync(purchaseId))
                .ReturnsAsync((Purchase?)null);

            // Act
            var result = await handler.HandleAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Compra não encontrada"));
            Assert.That(result.Errors, Contains.Item($"Compra com ID {purchaseId} não encontrada"));

            _mockPurchaseRepository.Verify(r => r.UpdateAsync(It.IsAny<Purchase>()), Times.Never);
        }

        [Test]
        public async Task CancelPurchaseCommandHandler_AlreadyCompleted_ShouldReturnError()
        {
            // Arrange
            var purchaseId = Guid.NewGuid();
            var purchase = new Purchase
            {
                Id = purchaseId,
                UserId = Guid.NewGuid(),
                GameId = Guid.NewGuid(),
                TotalPrice = 59.99m,
                Status = "Completed"
            };

            var command = new CancelPurchaseCommand(purchaseId, "Reason");
            var handler = new CancelPurchaseCommandHandler(_mockPurchaseRepository.Object, _mockEventBus.Object);

            _mockPurchaseRepository.Setup(r => r.GetByIdAsync(purchaseId))
                .ReturnsAsync(purchase);

            // Act
            var result = await handler.HandleAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Compra já finalizada"));
            Assert.That(result.Errors, Contains.Item("Não é possível cancelar uma compra já finalizada"));

            _mockPurchaseRepository.Verify(r => r.UpdateAsync(It.IsAny<Purchase>()), Times.Never);
        }

        #endregion

        #region Command Validation Tests

        [Test]
        public void VendasValidators_CreatePurchaseCommand_ValidData_ShouldPass()
        {
            // Arrange
            var command = new CreatePurchaseCommand(Guid.NewGuid(), Guid.NewGuid(), 59.99m);

            // Act
            var result = VendasValidators.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors.Count, Is.EqualTo(0));
        }

        [Test]
        public void VendasValidators_CreatePurchaseCommand_EmptyUserId_ShouldFail()
        {
            // Arrange
            var command = new CreatePurchaseCommand(Guid.Empty, Guid.NewGuid(), 59.99m);

            // Act
            var result = VendasValidators.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("ID do usuário é obrigatório."));
        }

        [Test]
        public void VendasValidators_CreatePurchaseCommand_ZeroPrice_ShouldFail()
        {
            // Arrange
            var command = new CreatePurchaseCommand(Guid.NewGuid(), Guid.NewGuid(), 0m);

            // Act
            var result = VendasValidators.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Preço deve ser maior que zero."));
        }

        [Test]
        public void VendasValidators_FinalizePurchaseCommand_ValidPaymentMethod_ShouldPass()
        {
            // Arrange
            var command = new FinalizePurchaseCommand(Guid.NewGuid(), "CreditCard");

            // Act
            var result = VendasValidators.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors.Count, Is.EqualTo(0));
        }

        [Test]
        public void VendasValidators_FinalizePurchaseCommand_InvalidPaymentMethod_ShouldFail()
        {
            // Arrange
            var command = new FinalizePurchaseCommand(Guid.NewGuid(), "InvalidMethod");

            // Act
            var result = VendasValidators.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Método de pagamento inválido. Opções: CreditCard, DebitCard, PayPal, Pix."));
        }

        [Test]
        public void VendasValidators_CancelPurchaseCommand_EmptyReason_ShouldFail()
        {
            // Arrange
            var command = new CancelPurchaseCommand(Guid.NewGuid(), "");

            // Act
            var result = VendasValidators.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Motivo do cancelamento é obrigatório."));
        }

        [Test]
        public void VendasValidators_CancelPurchaseCommand_ShortReason_ShouldFail()
        {
            // Arrange
            var command = new CancelPurchaseCommand(Guid.NewGuid(), "abc");

            // Act
            var result = VendasValidators.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Motivo do cancelamento deve ter pelo menos 5 caracteres."));
        }

        #endregion
    }
}
