using NUnit.Framework;
using GameStore.Vendas.Application.DTOs;
using GameStore.Vendas.Application.Mappers;
using TheThroneOfGames.Infrastructure.Entities;

namespace GameStore.Vendas.Tests
{
    [TestFixture]
    public class MapperTests
    {
        #region PurchaseMapper Tests

        [Test]
        public void PurchaseMapper_ToPurchaseDTO_ValidPurchase_ShouldMapCorrectly()
        {
            // Arrange
            var purchase = new Purchase
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                GameId = Guid.NewGuid(),
                TotalPrice = 59.99m,
                PurchaseDate = DateTime.UtcNow,
                Status = "Completed",
                PaymentMethod = "CreditCard",
                CancellationReason = null,
                CompletedAt = DateTime.UtcNow,
                CancelledAt = null
            };

            // Act
            var result = PurchaseMapper.ToPurchaseDTO(purchase);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(purchase.Id));
            Assert.That(result.UserId, Is.EqualTo(purchase.UserId));
            Assert.That(result.GameId, Is.EqualTo(purchase.GameId));
            Assert.That(result.TotalPrice, Is.EqualTo(purchase.TotalPrice));
            Assert.That(result.PurchaseDate, Is.EqualTo(purchase.PurchaseDate));
            Assert.That(result.Status, Is.EqualTo(purchase.Status));
            Assert.That(result.PaymentMethod, Is.EqualTo(purchase.PaymentMethod));
            Assert.That(result.CancellationReason, Is.EqualTo(purchase.CancellationReason));
            Assert.That(result.CompletedAt, Is.EqualTo(purchase.CompletedAt));
            Assert.That(result.CancelledAt, Is.EqualTo(purchase.CancelledAt));
        }

        [Test]
        public void PurchaseMapper_ToPurchaseDTO_NullPurchase_ShouldReturnNull()
        {
            // Arrange
            Purchase purchase = null!;

            // Act
            var result = PurchaseMapper.ToPurchaseDTO(purchase);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void PurchaseMapper_ToPurchaseDTOList_ValidList_ShouldMapCorrectly()
        {
            // Arrange
            var purchases = new List<Purchase>
            {
                new Purchase
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    GameId = Guid.NewGuid(),
                    TotalPrice = 59.99m,
                    Status = "Completed",
                    PaymentMethod = "CreditCard"
                },
                new Purchase
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    GameId = Guid.NewGuid(),
                    TotalPrice = 29.99m,
                    Status = "Pending",
                    PaymentMethod = "PayPal"
                }
            };

            // Act
            var result = PurchaseMapper.ToPurchaseDTOList(purchases);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(2));
            
            var firstDto = result.First();
            Assert.That(firstDto.TotalPrice, Is.EqualTo(59.99m));
            Assert.That(firstDto.Status, Is.EqualTo("Completed"));
            Assert.That(firstDto.PaymentMethod, Is.EqualTo("CreditCard"));
            
            var secondDto = result.Last();
            Assert.That(secondDto.TotalPrice, Is.EqualTo(29.99m));
            Assert.That(secondDto.Status, Is.EqualTo("Pending"));
            Assert.That(secondDto.PaymentMethod, Is.EqualTo("PayPal"));
        }

        [Test]
        public void PurchaseMapper_ToPurchaseDTOList_EmptyList_ShouldReturnEmpty()
        {
            // Arrange
            var purchases = new List<Purchase>();

            // Act
            var result = PurchaseMapper.ToPurchaseDTOList(purchases);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void PurchaseMapper_ToPurchaseDTOList_NullList_ShouldReturnEmpty()
        {
            // Arrange
            List<Purchase> purchases = null!;

            // Act
            var result = PurchaseMapper.ToPurchaseDTOList(purchases);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void PurchaseMapper_FromDTO_ValidDTO_ShouldMapCorrectly()
        {
            // Arrange
            var purchaseDto = new PurchaseDTO
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                GameId = Guid.NewGuid(),
                TotalPrice = 59.99m,
                PurchaseDate = DateTime.UtcNow,
                Status = "Completed",
                PaymentMethod = "CreditCard",
                CancellationReason = null,
                CompletedAt = DateTime.UtcNow,
                CancelledAt = null
            };

            // Act
            var result = PurchaseMapper.FromDTO(purchaseDto);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(purchaseDto.Id));
            Assert.That(result.UserId, Is.EqualTo(purchaseDto.UserId));
            Assert.That(result.GameId, Is.EqualTo(purchaseDto.GameId));
            Assert.That(result.TotalPrice, Is.EqualTo(purchaseDto.TotalPrice));
            Assert.That(result.PurchaseDate, Is.EqualTo(purchaseDto.PurchaseDate));
            Assert.That(result.Status, Is.EqualTo(purchaseDto.Status));
            Assert.That(result.PaymentMethod, Is.EqualTo(purchaseDto.PaymentMethod));
            Assert.That(result.CancellationReason, Is.EqualTo(purchaseDto.CancellationReason));
            Assert.That(result.CompletedAt, Is.EqualTo(purchaseDto.CompletedAt));
            Assert.That(result.CancelledAt, Is.EqualTo(purchaseDto.CancelledAt));
        }

        [Test]
        public void PurchaseMapper_FromDTODefault_ShouldCreateValidPurchase()
        {
            // Arrange & Act
            var result = PurchaseMapper.FromDTO();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(Guid.Empty));
            Assert.That(result.UserId, Is.EqualTo(Guid.Empty));
            Assert.That(result.GameId, Is.EqualTo(Guid.Empty));
            Assert.That(result.TotalPrice, Is.EqualTo(0m));
            Assert.That(result.Status, Is.Null);
            Assert.That(result.PaymentMethod, Is.Null);
        }

        #endregion
    }
}
