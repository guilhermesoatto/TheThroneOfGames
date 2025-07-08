using MongoDB.Driver;
using TheThroneOfGames.Infrastructure.Entities;

namespace TheThroneOfGames.Infrastructure.Persistence
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        public IMongoCollection<UserEntity> Users => _database.GetCollection<UserEntity>("Users");
    }
}
