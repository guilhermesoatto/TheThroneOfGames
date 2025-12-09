using NUnit.Framework;
using Moq;
using GameStore.Vendas.Application.Queries;
using GameStore.Vendas.Application.Handlers;
using GameStore.Vendas.Application.DTOs;
using TheThroneOfGames.Domain.Interfaces;
using TheThroneOfGames.Domain.Entities;
using GameStore.CQRS.Abstractions;

namespace GameStore.Vendas.Tests
{
    [TestFixture]
    public class QueryHandlerTests
    {
        private Mock<IPurchaseRepository> _mockPurchaseRepository = null!;

        [SetUp]
        public void Setup()
        {
            _mockPurchaseRepository = new Mock<IPurchaseRepository>();
        }

        #region GetPurchaseByIdQueryHandler Tests

        [Test]
        public async Task GetPurchaseByIdQueryHandler_ExistingPurchase_ShouldReturnDTO()
        {
            // Arrange
            var purchaseId = Guid.NewGuid();
            var purchase = new Purchase
            {
                Id = purchaseId,
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

            var query = new GetPurchaseByIdQuery(purchaseId);
            var handler = new GetPurchaseByIdQueryHandler(_mockPurchaseRepository.Object);

            _mockPurchaseRepository.Setup(r => r.GetByIdAsync(purchaseId))
                .ReturnsAsync(purchase);

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Id, Is.EqualTo(purchaseId));
            Assert.That(result.TotalPrice, Is.EqualTo(59.99m));
            Assert.That(result.Status, Is.EqualTo("Completed"));

            _mockPurchaseRepository.Verify(r => r.GetByIdAsync(purchaseId), Times.Once);
        }

        [Test]
        public async Task GetPurchaseByIdQueryHandler_NonExistingPurchase_ShouldReturnNull()
        {
            // Arrange
            var purchaseId = Guid.NewGuid();
            var query = new GetPurchaseByIdQuery(purchaseId);
            var handler = new GetPurchaseByIdQueryHandler(_mockPurchaseRepository.Object);

            _mockPurchaseRepository.Setup(r => r.GetByIdAsync(purchaseId))
                .ReturnsAsync((Purchase?)null);

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Null);
            _mockPurchaseRepository.Verify(r => r.GetByIdAsync(purchaseId), Times.Once);
        }

        #endregion

        #region GetPurchasesByUserQueryHandler Tests

        [Test]
        public async Task GetPurchasesByUserQueryHandler_ExistingUser_ShouldReturnPurchases()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var purchases = new List<Purchase>
            {
                new Purchase
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    GameId = Guid.NewGuid(),
                    TotalPrice = 59.99m,
                    PurchaseDate = DateTime.UtcNow,
                    Status = "Completed"
                },
                new Purchase
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    GameId = Guid.NewGuid(),
                    TotalPrice = 29.99m,
                    PurchaseDate = DateTime.UtcNow.AddDays(-1),
                    Status = "Pending"
                }
            };

            var query = new GetPurchasesByUserQuery(userId);
            var handler = new GetPurchasesByUserQueryHandler(_mockPurchaseRepository.Object);

            _mockPurchaseRepository.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(purchases);

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(result.All(p => p.UserId == userId), Is.True);

            _mockPurchaseRepository.Verify(r => r.GetByUserIdAsync(userId), Times.Once);
        }

        [Test]
        public async Task GetPurchasesByUserQueryHandler_NoPurchases_ShouldReturnEmpty()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = new GetPurchasesByUserQuery(userId);
            var handler = new GetPurchasesByUserQueryHandler(_mockPurchaseRepository.Object);

            _mockPurchaseRepository.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(new List<Purchase>());

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
            _mockPurchaseRepository.Verify(r => r.GetByUserIdAsync(userId), Times.Once);
        }

        #endregion

        #region GetAllPurchasesQueryHandler Tests

        [Test]
        public async Task GetAllPurchasesQueryHandler_HasPurchases_ShouldReturnAll()
        {
            // Arrange
            var purchases = new List<Purchase>
            {
                new Purchase { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), GameId = Guid.NewGuid(), TotalPrice = 59.99m },
                new Purchase { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), GameId = Guid.NewGuid(), TotalPrice = 29.99m },
                new Purchase { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), GameId = Guid.NewGuid(), TotalPrice = 99.99m }
            };

            var query = new GetAllPurchasesQuery();
            var handler = new GetAllPurchasesQueryHandler(_mockPurchaseRepository.Object);

            _mockPurchaseRepository.Setup(r => r.GetAllAsync())
                .ReturnsAsync(purchases);

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(3));

            _mockPurchaseRepository.Verify(r => r.GetAllAsync(), Times.Once);
        }

        #endregion

        #region GetPurchasesByStatusQueryHandler Tests

        [Test]
        public async Task GetPurchasesByStatusQueryHandler_ExistingStatus_ShouldReturnPurchases()
        {
            // Arrange
            var status = "Completed";
            var purchases = new List<Purchase>
            {
                new Purchase { Id = Guid.NewGuid(), Status = status, TotalPrice = 59.99m },
                new Purchase { Id = Guid.NewGuid(), Status = status, TotalPrice = 29.99m }
            };

            var query = new GetPurchasesByStatusQuery(status);
            var handler = new GetPurchasesByStatusQueryHandler(_mockPurchaseRepository.Object);

            _mockPurchaseRepository.Setup(r => r.GetByStatusAsync(status))
                .ReturnsAsync(purchases);

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(result.All(p => p.Status == status), Is.True);

            _mockPurchaseRepository.Verify(r => r.GetByStatusAsync(status), Times.Once);
        }

        #endregion

        #region GetPurchasesByDateRangeQueryHandler Tests

        [Test]
        public async Task GetPurchasesByDateRangeQueryHandler_ValidRange_ShouldReturnPurchases()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;
            var purchases = new List<Purchase>
            {
                new Purchase { Id = Guid.NewGuid(), PurchaseDate = startDate.AddDays(1), TotalPrice = 59.99m },
                new Purchase { Id = Guid.NewGuid(), PurchaseDate = endDate.AddDays(-1), TotalPrice = 29.99m }
            };

            var query = new GetPurchasesByDateRangeQuery(startDate, endDate);
            var handler = new GetPurchasesByDateRangeQueryHandler(_mockPurchaseRepository.Object);

            _mockPurchaseRepository.Setup(r => r.GetByDateRangeAsync(startDate, endDate))
                .ReturnsAsync(purchases);

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(result.All(p => p.PurchaseDate >= startDate && p.PurchaseDate <= endDate), Is.True);

            _mockPurchaseRepository.Verify(r => r.GetByDateRangeAsync(startDate, endDate), Times.Once);
        }

        #endregion

        #region GetPurchasesByGameQueryHandler Tests

        [Test]
        public async Task GetPurchasesByGameQueryHandler_ExistingGame_ShouldReturnPurchases()
        {
            // Arrange
            var gameId = Guid.NewGuid();
            var purchases = new List<Purchase>
            {
                new Purchase { Id = Guid.NewGuid(), GameId = gameId, TotalPrice = 59.99m },
                new Purchase { Id = Guid.NewGuid(), GameId = gameId, TotalPrice = 59.99m }
            };

            var query = new GetPurchasesByGameQuery(gameId);
            var handler = new GetPurchasesByGameQueryHandler(_mockPurchaseRepository.Object);

            _mockPurchaseRepository.Setup(r => r.GetByGameIdAsync(gameId))
                .ReturnsAsync(purchases);

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(result.All(p => p.GameId == gameId), Is.True);

            _mockPurchaseRepository.Verify(r => r.GetByGameIdAsync(gameId), Times.Once);
        }

        #endregion

        #region GetSalesStatsQueryHandler Tests

        [Test]
        public async Task GetSalesStatsQueryHandler_AllPurchases_ShouldReturnCorrectStats()
        {
            // Arrange
            var purchases = new List<Purchase>
            {
                new Purchase { Id = Guid.NewGuid(), TotalPrice = 59.99m, Status = "Completed" },
                new Purchase { Id = Guid.NewGuid(), TotalPrice = 29.99m, Status = "Pending" },
                new Purchase { Id = Guid.NewGuid(), TotalPrice = 99.99m, Status = "Cancelled" }
            };

            var query = new GetSalesStatsQuery();
            var handler = new GetSalesStatsQueryHandler(_mockPurchaseRepository.Object);

            _mockPurchaseRepository.Setup(r => r.GetAllAsync())
                .ReturnsAsync(purchases);

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TotalPurchases, Is.EqualTo(3));
            Assert.That(result.TotalRevenue, Is.EqualTo(189.97m));
            Assert.That(result.AveragePurchaseValue, Is.EqualTo(63.323333333333333m));
            Assert.That(result.CompletedPurchases, Is.EqualTo(1));
            Assert.That(result.PendingPurchases, Is.EqualTo(1));
            Assert.That(result.CancelledPurchases, Is.EqualTo(1));
            Assert.That(result.PurchasesByStatus.Count, Is.EqualTo(3));
            Assert.That(result.RevenueByGame.Count, Is.EqualTo(3));

            _mockPurchaseRepository.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Test]
        public async Task GetSalesStatsQueryHandler_WithDateRange_ShouldFilterCorrectly()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-5);
            var endDate = DateTime.UtcNow;
            var purchases = new List<Purchase>
            {
                new Purchase { Id = Guid.NewGuid(), TotalPrice = 59.99m, PurchaseDate = startDate.AddDays(1), Status = "Completed" },
                new Purchase { Id = Guid.NewGuid(), TotalPrice = 29.99m, PurchaseDate = startDate.AddDays(-1), Status = "Pending" }, // Fora do range
                new Purchase { Id = Guid.NewGuid(), TotalPrice = 99.99m, PurchaseDate = endDate.AddDays(-1), Status = "Completed" }
            };

            var query = new GetSalesStatsQuery(startDate, endDate);
            var handler = new GetSalesStatsQueryHandler(_mockPurchaseRepository.Object);

            _mockPurchaseRepository.Setup(r => r.GetAllAsync())
                .ReturnsAsync(purchases);

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TotalPurchases, Is.EqualTo(2)); // Apenas 2 dentro do range
            Assert.That(result.TotalRevenue, Is.EqualTo(159.98m));
            Assert.That(result.CompletedPurchases, Is.EqualTo(2));
            Assert.That(result.PendingPurchases, Is.EqualTo(0));
            Assert.That(result.CancelledPurchases, Is.EqualTo(0));
            Assert.That(result.PurchasesByStatus.Count, Is.EqualTo(1)); // Apenas "Completed"
            Assert.That(result.RevenueByGame.Count, Is.EqualTo(2));

            _mockPurchaseRepository.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Test]
        public async Task GetSalesStatsQueryHandler_NoPurchases_ShouldReturnZeroStats()
        {
            // Arrange
            var query = new GetSalesStatsQuery();
            var handler = new GetSalesStatsQueryHandler(_mockPurchaseRepository.Object);

            _mockPurchaseRepository.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Purchase>());

            // Act
            var result = await handler.HandleAsync(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TotalPurchases, Is.EqualTo(0));
            Assert.That(result.TotalRevenue, Is.EqualTo(0m));
            Assert.That(result.AveragePurchaseValue, Is.EqualTo(0m));
            Assert.That(result.CompletedPurchases, Is.EqualTo(0));
            Assert.That(result.PendingPurchases, Is.EqualTo(0));
            Assert.That(result.CancelledPurchases, Is.EqualTo(0));
            Assert.That(result.PurchasesByStatus, Is.Empty);
            Assert.That(result.RevenueByGame, Is.Empty);

            _mockPurchaseRepository.Verify(r => r.GetAllAsync(), Times.Once);
        }

        #endregion
    }
}
