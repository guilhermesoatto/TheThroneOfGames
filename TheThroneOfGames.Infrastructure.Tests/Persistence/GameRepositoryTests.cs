using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using TheThroneOfGames.Domain.Entities;
using TheThroneOfGames.Infrastructure.Persistence;
using TheThroneOfGames.Infrastructure.Repository;

namespace TheThroneOfGames.Infrastructure.Tests.Persistence
{
    [TestClass]
    public class GameRepositoryTests
    {
        private static MsSqlContainer _container = null!;

        [ClassInitialize]
        public static async Task ClassInitialize(TestContext _)
        {
            _container = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2019-latest")
                .Build();

            await _container.StartAsync();
        }

        [ClassCleanup]
        public static async Task ClassCleanup()
        {
            await _container.DisposeAsync();
        }

        private async Task<MainDbContext> CreateMigratedContextAsync()
        {
            var options = new DbContextOptionsBuilder<MainDbContext>()
                .UseSqlServer(_container.GetConnectionString())
                .Options;

            var context = new MainDbContext(options);
            await context.Database.MigrateAsync();
            return context;
        }

        [TestMethod]
        public void AppDbContext_TypeExists()
        {
            var type = Type.GetType("TheThroneOfGames.Infrastructure.Data.AppDbContext, TheThroneOfGames.Infrastructure");
            Assert.IsNotNull(type, "AppDbContext não encontrado no assembly Infrastructure (esperado: TheThroneOfGames.Infrastructure.Data.AppDbContext).");
        }

        [TestMethod]
        public async Task CanCreate_Real_DbContext_Against_MsSqlContainer()
        {
            await using var context = await CreateMigratedContextAsync();
            Assert.IsNotNull(context);
            Assert.IsTrue(await context.Database.CanConnectAsync());
        }

        [TestMethod]
        public async Task GameEntityRepository_AddAsync_Then_GetByIdAsync_PersistsToRealDatabase()
        {
            await using var context = await CreateMigratedContextAsync();
            var repository = new GameEntityRepository(context);

            var game = new GameEntity
            {
                Id = Guid.NewGuid(),
                Name = "Elden Ring",
                Genre = "RPG",
                Price = 199.90m,
                Description = "Souls-like open world",
                IsAvailable = true
            };

            await repository.AddAsync(game);

            await using var readContext = new MainDbContext(
                new DbContextOptionsBuilder<MainDbContext>().UseSqlServer(_container.GetConnectionString()).Options);
            var readRepository = new GameEntityRepository(readContext);

            var persisted = await readRepository.GetByIdAsync(game.Id);

            Assert.IsNotNull(persisted);
            Assert.AreEqual(game.Name, persisted!.Name);
            Assert.AreEqual(game.Genre, persisted.Genre);
            Assert.AreEqual(game.Price, persisted.Price);
        }

        [TestMethod]
        public async Task GameEntityRepository_UpdateAsync_PersistsChangesToRealDatabase()
        {
            await using var context = await CreateMigratedContextAsync();
            var repository = new GameEntityRepository(context);

            var game = new GameEntity
            {
                Id = Guid.NewGuid(),
                Name = "Dark Souls",
                Genre = "RPG",
                Price = 99.90m,
                IsAvailable = true
            };
            await repository.AddAsync(game);

            game.Price = 49.90m;
            game.IsAvailable = false;
            await repository.UpdateAsync(game);

            await using var readContext = new MainDbContext(
                new DbContextOptionsBuilder<MainDbContext>().UseSqlServer(_container.GetConnectionString()).Options);
            var readRepository = new GameEntityRepository(readContext);
            var persisted = await readRepository.GetByIdAsync(game.Id);

            Assert.IsNotNull(persisted);
            Assert.AreEqual(49.90m, persisted!.Price);
            Assert.IsFalse(persisted.IsAvailable);
        }

        [TestMethod]
        public async Task UsuarioRepository_AddAsync_Then_GetByEmailAsync_PersistsAggregateToRealDatabase()
        {
            await using var context = await CreateMigratedContextAsync();
            var repository = new UsuarioRepository(context);

            var usuario = new Usuario(
                name: "Geralt de Rivia",
                email: "geralt@throneofgames.test",
                passwordHash: "hashed-password",
                role: "Player",
                activeToken: Guid.NewGuid().ToString());

            await repository.AddAsync(usuario);

            await using var readContext = new MainDbContext(
                new DbContextOptionsBuilder<MainDbContext>().UseSqlServer(_container.GetConnectionString()).Options);
            var readRepository = new UsuarioRepository(readContext);

            var persisted = await readRepository.GetByEmailAsync("geralt@throneofgames.test");

            Assert.IsNotNull(persisted);
            Assert.AreEqual(usuario.Id, persisted!.Id);
            Assert.AreEqual(usuario.Name, persisted.Name);
            Assert.IsFalse(persisted.IsActive);
        }
    }
}
