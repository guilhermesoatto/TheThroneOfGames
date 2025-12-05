using NUnit.Framework;
using GameStore.Vendas.Application.Commands;
using GameStore.Vendas.Application.Validators;
using GameStore.CQRS.Abstractions;

namespace GameStore.Vendas.Tests
{
    [TestFixture]
    public class ValidatorTests
    {
        #region CreatePurchaseCommandValidator Tests

        [Test]
        public void CreatePurchaseCommandValidator_ValidCommand_ShouldPass()
        {
            // Arrange
            var validator = new CreatePurchaseCommandValidator();
            var command = new CreatePurchaseCommand
            {
                UserId = Guid.NewGuid(),
                GameId = Guid.NewGuid(),
                TotalPrice = 59.99m,
                PaymentMethod = "CreditCard"
            };

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void CreatePurchaseCommandValidator_EmptyUserId_ShouldFail()
        {
            // Arrange
            var validator = new CreatePurchaseCommandValidator();
            var command = new CreatePurchaseCommand
            {
                UserId = Guid.Empty,
                GameId = Guid.NewGuid(),
                TotalPrice = 59.99m,
                PaymentMethod = "CreditCard"
            };

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.PropertyName == "UserId"), Is.True);
        }

        [Test]
        public void CreatePurchaseCommandValidator_EmptyGameId_ShouldFail()
        {
            // Arrange
            var validator = new CreatePurchaseCommandValidator();
            var command = new CreatePurchaseCommand
            {
                UserId = Guid.NewGuid(),
                GameId = Guid.Empty,
                TotalPrice = 59.99m,
                PaymentMethod = "CreditCard"
            };

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.PropertyName == "GameId"), Is.True);
        }

        [Test]
        public void CreatePurchaseCommandValidator_ZeroPrice_ShouldFail()
        {
            // Arrange
            var validator = new CreatePurchaseCommandValidator();
            var command = new CreatePurchaseCommand
            {
                UserId = Guid.NewGuid(),
                GameId = Guid.NewGuid(),
                TotalPrice = 0,
                PaymentMethod = "CreditCard"
            };

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.PropertyName == "TotalPrice"), Is.True);
        }

        [Test]
        public void CreatePurchaseCommandValidator_NegativePrice_ShouldFail()
        {
            // Arrange
            var validator = new CreatePurchaseCommandValidator();
            var command = new CreatePurchaseCommand
            {
                UserId = Guid.NewGuid(),
                GameId = Guid.NewGuid(),
                TotalPrice = -10m,
                PaymentMethod = "CreditCard"
            };

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.PropertyName == "TotalPrice"), Is.True);
        }

        [Test]
        public void CreatePurchaseCommandValidator_EmptyPaymentMethod_ShouldFail()
        {
            // Arrange
            var validator = new CreatePurchaseCommandValidator();
            var command = new CreatePurchaseCommand
            {
                UserId = Guid.NewGuid(),
                GameId = Guid.NewGuid(),
                TotalPrice = 59.99m,
                PaymentMethod = ""
            };

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.PropertyName == "PaymentMethod"), Is.True);
        }

        #endregion

        #region FinalizePurchaseCommandValidator Tests

        [Test]
        public void FinalizePurchaseCommandValidator_ValidCommand_ShouldPass()
        {
            // Arrange
            var validator = new FinalizePurchaseCommandValidator();
            var command = new FinalizePurchaseCommand
            {
                PurchaseId = Guid.NewGuid()
            };

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void FinalizePurchaseCommandValidator_EmptyPurchaseId_ShouldFail()
        {
            // Arrange
            var validator = new FinalizePurchaseCommandValidator();
            var command = new FinalizePurchaseCommand
            {
                PurchaseId = Guid.Empty
            };

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.PropertyName == "PurchaseId"), Is.True);
        }

        #endregion

        #region CancelPurchaseCommandValidator Tests

        [Test]
        public void CancelPurchaseCommandValidator_ValidCommand_ShouldPass()
        {
            // Arrange
            var validator = new CancelPurchaseCommandValidator();
            var command = new CancelPurchaseCommand
            {
                PurchaseId = Guid.NewGuid(),
                CancellationReason = "User requested cancellation"
            };

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void CancelPurchaseCommandValidator_EmptyPurchaseId_ShouldFail()
        {
            // Arrange
            var validator = new CancelPurchaseCommandValidator();
            var command = new CancelPurchaseCommand
            {
                PurchaseId = Guid.Empty,
                CancellationReason = "User requested cancellation"
            };

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.PropertyName == "PurchaseId"), Is.True);
        }

        [Test]
        public void CancelPurchaseCommandValidator_EmptyCancellationReason_ShouldFail()
        {
            // Arrange
            var validator = new CancelPurchaseCommandValidator();
            var command = new CancelPurchaseCommand
            {
                PurchaseId = Guid.NewGuid(),
                CancellationReason = ""
            };

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.PropertyName == "CancellationReason"), Is.True);
        }

        [Test]
        public void CancelPurchaseCommandValidator_TooShortCancellationReason_ShouldFail()
        {
            // Arrange
            var validator = new CancelPurchaseCommandValidator();
            var command = new CancelPurchaseCommand
            {
                PurchaseId = Guid.NewGuid(),
                CancellationReason = "No" // Muito curto
            };

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.PropertyName == "CancellationReason"), Is.True);
        }

        #endregion
    }
}
