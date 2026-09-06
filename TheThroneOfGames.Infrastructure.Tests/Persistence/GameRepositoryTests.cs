using Microsoft.EntityFrameworkCore;
using TheThroneOfGames.Domain.Entities;
using TheThroneOfGames.Infrastructure.Persistence;
using TheThroneOfGames.Infrastructure.Repository;

namespace TheThroneOfGames.Infrastructure.Tests.Persistence
{
    /// <summary>
    /// Testes de persistência do monólito. Antes usavam Testcontainers + SQL Server real;
    /// foram convertidos para o provider EF Core InMemory para rodar no CI sem Docker nem
    /// slave services (ver docs/reports/alinhamento-rubrica-fase2.md, item 3.4).
    /// </summary>
    [TestClass]
    public class GameRepositoryTests
    {
        private static MainDbContext NewContext() =>
            new MainDbContext(
                new DbContextOptionsBuilder<MainDbContext>()
                    .UseInMemoryDatabase($"tests-{Guid.NewGuid()}")
                    .Options);

        [TestMethod]
        public void AppDbContext_TypeExists()
        {
            var type = Type.GetType("TheThroneOfGames.Infrastructure.Data.AppDbContext, TheThroneOfGames.Infrastructure");
            Assert.IsNotNull(type, "AppDbContext não encontrado no assembly Infrastructure (esperado: TheThroneOfGames.Infrastructure.Data.AppDbContext).");
        }

        [TestMethod]
        public async Task CanCreate_DbContext_AndConnect()
        {
            await using var context = NewContext();
            context.Database.EnsureCreated();
            Assert.IsNotNull(context);
            Assert.IsTrue(await context.Database.CanConnectAsync());
        }

        [TestMethod]
        public async Task GameEntityRepository_AddAsync_Then_GetByIdAsync_PersistsToDatabase()
        {
            var dbName = $"tests-{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<MainDbContext>().UseInMemoryDatabase(dbName).Options;

            var game = new GameEntity
            {
                Id = Guid.NewGuid(),
                Name = "Elden Ring",
                Genre = "RPG",
                Price = 199.90m,
                Description = "Souls-like open world",
                IsAvailable = true
            };

            await using (var writeContext = new MainDbContext(options))
            {
                await new GameEntityRepository(writeContext).AddAsync(game);
            }

            await using var readContext = new MainDbContext(options);
            var persisted = await new GameEntityRepository(readContext).GetByIdAsync(game.Id);

            Assert.IsNotNull(persisted);
            Assert.AreEqual(game.Name, persisted!.Name);
            Assert.AreEqual(game.Genre, persisted.Genre);
            Assert.AreEqual(game.Price, persisted.Price);
        }

        [TestMethod]
        public async Task GameEntityRepository_UpdateAsync_PersistsChangesToDatabase()
        {
            var dbName = $"tests-{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<MainDbContext>().UseInMemoryDatabase(dbName).Options;

            var game = new GameEntity
            {
                Id = Guid.NewGuid(),
                Name = "Dark Souls",
                Genre = "RPG",
                Price = 99.90m,
                IsAvailable = true
            };

            await using (var writeContext = new MainDbContext(options))
            {
                var repository = new GameEntityRepository(writeContext);
                await repository.AddAsync(game);

                game.Price = 49.90m;
                game.IsAvailable = false;
                await repository.UpdateAsync(game);
            }

            await using var readContext = new MainDbContext(options);
            var persisted = await new GameEntityRepository(readContext).GetByIdAsync(game.Id);

            Assert.IsNotNull(persisted);
            Assert.AreEqual(49.90m, persisted!.Price);
            Assert.IsFalse(persisted.IsAvailable);
        }

        [TestMethod]
        public async Task UsuarioRepository_AddAsync_Then_GetByEmailAsync_PersistsAggregateToDatabase()
        {
            var dbName = $"tests-{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<MainDbContext>().UseInMemoryDatabase(dbName).Options;

            var usuario = new Usuario(
                name: "Geralt de Rivia",
                email: "geralt@throneofgames.test",
                passwordHash: "hashed-password",
                role: "Player",
                activeToken: Guid.NewGuid().ToString());

            await using (var writeContext = new MainDbContext(options))
            {
                await new UsuarioRepository(writeContext).AddAsync(usuario);
            }

            await using var readContext = new MainDbContext(options);
            var persisted = await new UsuarioRepository(readContext).GetByEmailAsync("geralt@throneofgames.test");

            Assert.IsNotNull(persisted);
            Assert.AreEqual(usuario.Id, persisted!.Id);
            Assert.AreEqual(usuario.Name, persisted.Name);
            Assert.IsFalse(persisted.IsActive);
        }
    }
}
