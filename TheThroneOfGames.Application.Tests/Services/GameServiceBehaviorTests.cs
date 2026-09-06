using Moq;
using TheThroneOfGames.Domain.Entities;
using TheThroneOfGames.Domain.Events;
using TheThroneOfGames.Domain.Interfaces;

namespace TheThroneOfGames.Application.Tests.Services;

[TestClass]
public class GameServiceBehaviorTests
{
    private readonly Mock<IBaseRepository<GameEntity>> _games = new();
    private readonly Mock<IBaseRepository<PurchaseEntity>> _purchases = new();
    private readonly Mock<IEventBus> _bus = new();

    private GameService CreateSut() => new(_games.Object, _purchases.Object, _bus.Object);

    private static GameEntity Game(string name = "Elden Ring", decimal price = 199.90m) =>
        new() { Id = Guid.NewGuid(), Name = name, Genre = "RPG", Price = price };

    [TestMethod]
    public async Task Crud_ShouldDelegateToRepository()
    {
        var sut = CreateSut();
        var game = Game();

        await sut.AddAsync(game);
        await sut.UpdateAsync(game);
        await sut.DeleteAsync(game.Id);
        await sut.GetByIdAsync(game.Id);
        await sut.GetAllAsync();

        _games.Verify(r => r.AddAsync(game), Times.Once);
        _games.Verify(r => r.UpdateAsync(game), Times.Once);
        _games.Verify(r => r.DeleteAsync(game.Id), Times.Once);
        _games.Verify(r => r.GetByIdAsync(game.Id), Times.Once);
        _games.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [TestMethod]
    public async Task BuyGame_WhenGameNotFound_ShouldThrow_AndNotPublish()
    {
        _games.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((GameEntity?)null);
        var sut = CreateSut();

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => sut.BuyGame(Guid.NewGuid(), Guid.NewGuid()));

        _purchases.Verify(r => r.AddAsync(It.IsAny<PurchaseEntity>()), Times.Never);
        _bus.Verify(b => b.PublishAsync(It.IsAny<GameCompradoEvent>()), Times.Never);
    }

    [TestMethod]
    public async Task BuyGame_Valid_ShouldRecordPurchaseWithGamePrice_AndPublishEvent()
    {
        var game = Game(price: 149.50m);
        var userId = Guid.NewGuid();
        PurchaseEntity? recorded = null;
        _games.Setup(r => r.GetByIdAsync(game.Id)).ReturnsAsync(game);
        _purchases.Setup(r => r.AddAsync(It.IsAny<PurchaseEntity>()))
                  .Callback<PurchaseEntity>(p => recorded = p).Returns(Task.CompletedTask);
        var sut = CreateSut();

        await sut.BuyGame(game.Id, userId);

        Assert.IsNotNull(recorded);
        Assert.AreEqual(game.Id, recorded!.GameId);
        Assert.AreEqual(userId, recorded.UserId);
        Assert.AreEqual(149.50m, recorded.TotalPrice);
        _bus.Verify(b => b.PublishAsync(It.Is<GameCompradoEvent>(e =>
            e.GameId == game.Id && e.UserId == userId && e.Preco == 149.50m)), Times.Once);
    }

    [TestMethod]
    public async Task GetOwnedGames_ShouldReturnOnlyGamesPurchasedByUser()
    {
        var userId = Guid.NewGuid();
        var owned = Game("Owned");
        var other = Game("Other");
        _purchases.Setup(r => r.GetAllAsync()).ReturnsAsync(new[]
        {
            new PurchaseEntity { GameId = owned.Id, UserId = userId },
            new PurchaseEntity { GameId = other.Id, UserId = Guid.NewGuid() }
        });
        _games.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { owned, other });
        var sut = CreateSut();

        var result = await sut.GetOwnedGames(userId);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(owned.Id, result[0].Id);
    }

    [TestMethod]
    public async Task GetAvailableGames_ShouldExcludeOwnedGames()
    {
        var userId = Guid.NewGuid();
        var owned = Game("Owned");
        var free = Game("Available");
        _purchases.Setup(r => r.GetAllAsync()).ReturnsAsync(new[]
        {
            new PurchaseEntity { GameId = owned.Id, UserId = userId }
        });
        _games.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { owned, free });
        var sut = CreateSut();

        var result = await sut.GetAvailableGames(userId);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(free.Id, result[0].Id);
    }

    [TestMethod]
    public async Task GetAllGames_ShouldMaterializeRepositoryResult()
    {
        _games.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { Game("A"), Game("B") });
        var sut = CreateSut();

        var result = await sut.GetAllGames();

        Assert.AreEqual(2, result.Count);
    }
}
