using NUnit.Framework;
using GameStore.Vendas.Application.DTOs;
using GameStore.Vendas.Application.Mappers;
using TheThroneOfGames.Infrastructure.Entities;
using System;

namespace GameStore.Vendas.Tests
{
    public class PurchaseMapperTests
    {
        [Test]
        public void PurchaseMapper_ToDTO_Converts_Purchase_To_PurchaseDTO()
        {
            // Arrange
            var purchase = new Purchase
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                GameId = Guid.NewGuid(),
                PurchaseDate = DateTime.UtcNow
            };

            // Act
            var dto = PurchaseMapper.ToDTO(purchase);

            // Assert
            Assert.AreEqual(purchase.Id, dto.Id);
            Assert.AreEqual(purchase.UserId, dto.UserId);
            Assert.AreEqual(purchase.GameId, dto.GameId);
            Assert.AreEqual(purchase.PurchaseDate, dto.PurchaseDate);
            Assert.AreEqual("Completed", dto.Status);
        }

        [Test]
        public void PurchaseMapper_FromDTO_Converts_PurchaseDTO_To_Purchase()
        {
            // Arrange
            var purchaseId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var gameId = Guid.NewGuid();
            var purchaseDate = DateTime.UtcNow;

            var dto = new PurchaseDTO
            {
                Id = purchaseId,
                UserId = userId,
                GameId = gameId,
                TotalPrice = 49.99m,
                PurchaseDate = purchaseDate,
                Status = "Completed"
            };

            // Act
            var purchase = PurchaseMapper.FromDTO(dto);

            // Assert
            Assert.AreEqual(purchaseId, purchase.Id);
            Assert.AreEqual(userId, purchase.UserId);
            Assert.AreEqual(gameId, purchase.GameId);
            Assert.AreEqual(purchaseDate, purchase.PurchaseDate);
        }

        [Test]
        public void PurchaseMapper_ToDTO_Throws_With_Null_Purchase()
        {
            // Act & Assert
            Purchase? nullPurchase = null;
            Assert.Throws<ArgumentNullException>(() => PurchaseMapper.ToDTO(nullPurchase!));
        }

        [Test]
        public void PurchaseMapper_FromDTO_Throws_With_Null_DTO()
        {
            // Act & Assert
            PurchaseDTO? nullDto = null;
            Assert.Throws<ArgumentNullException>(() => PurchaseMapper.FromDTO(nullDto!));
        }

        [Test]
        public void PurchaseMapper_ToDTOList_Converts_Multiple_Purchases()
        {
            // Arrange
            var purchases = new[]
            {
                new Purchase { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), GameId = Guid.NewGuid(), PurchaseDate = DateTime.UtcNow },
                new Purchase { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), GameId = Guid.NewGuid(), PurchaseDate = DateTime.UtcNow },
                new Purchase { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), GameId = Guid.NewGuid(), PurchaseDate = DateTime.UtcNow }
            };

            // Act
            var dtos = PurchaseMapper.ToDTOList(purchases).ToList();

            // Assert
            Assert.AreEqual(3, dtos.Count);
            Assert.IsTrue(dtos.All(p => p.Status == "Completed"));
        }
    }
}
