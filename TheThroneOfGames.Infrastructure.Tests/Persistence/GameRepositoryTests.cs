using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;

namespace TheThroneOfGames.Infrastructure.Tests.Persistence
{
    [TestClass]
    public class GameRepositoryTests
    {
        [TestMethod]
        public void AppDbContext_TypeExists()
        {
            var type = Type.GetType("TheThroneOfGames.Infrastructure.Data.AppDbContext, TheThroneOfGames.Infrastructure");
            Assert.IsNotNull(type, "AppDbContext não encontrado no assembly Infrastructure (esperado: TheThroneOfGames.Infrastructure.Data.AppDbContext).");
        }

        [TestMethod]
        public void CanCreate_InMemory_DbContext()
        {
            var options = new DbContextOptionsBuilder<TheThroneOfGames.Infrastructure.Data.AppDbContext>()
                .UseInMemoryDatabase("TestDb_" + Guid.NewGuid())
                .Options;

            using var context = new TheThroneOfGames.Infrastructure.Data.AppDbContext(options);
            Assert.IsNotNull(context);
        }
    }
}
