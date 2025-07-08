using MongoDB.Driver;
using System.Threading.Tasks;
using TheThroneOfGames.Infrastructure.Entities;
using TheThroneOfGames.Infrastructure.Persistence;
using TheThroneOfGames.Infrastructure.Repository.Interfaces;

namespace TheThroneOfGames.Infrastructure.Repository
{
    public class MongoUsuarioRepository : IUsuarioRepository
    {
        private readonly IMongoCollection<UserEntity> _users;

        public MongoUsuarioRepository(MongoDbContext context)
        {
            _users = context.Users;
        }

        public async Task AddAsync(UserEntity user)
        {
            await _users.InsertOneAsync(user);
        }

        public async Task<UserEntity> GetByActivationTokenAsync(string activationToken)
        {
            var filter = Builders<UserEntity>.Filter.Eq(u => u.ActiveToken, activationToken);
            return await _users.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<UserEntity?> GetByEmailAsync(string email)
        {
            var filter = Builders<UserEntity>.Filter.Eq(u => u.Email, email);
            return await _users.Find(filter).FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(UserEntity user)
        {
            var filter = Builders<UserEntity>.Filter.Eq(u => u.Id, user.Id);
            await _users.ReplaceOneAsync(filter, user);
        }
    }
}
