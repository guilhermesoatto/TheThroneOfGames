using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheThroneOfGames.Domain.Entities;

namespace TheThroneOfGames.Infrastructure.Persistence
{
    public class MongoDbContext
    {
        //private readonly IMongoDatabase _database;

        //public MongoDbContext(string connectionString, string databaseName)
        //{
        //    var client = new MongoClient(connectionString);
        //    _database = client.GetDatabase(databaseName);
        //}

        //public IMongoCollection<PromotionEntity> Promotions => _database.GetCollection<PromotionEntity>("Promotions");
        //public IMongoCollection<PurchaseEntity> Purchases => _database.GetCollection<PurchaseEntity>("Purchases");
    }
}
