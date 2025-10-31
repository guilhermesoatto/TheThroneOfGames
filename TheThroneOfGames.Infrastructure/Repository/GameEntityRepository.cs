using TheThroneOfGames.Domain.Interfaces;
using TheThroneOfGames.Infrastructure.Persistence;
using TheThroneOfGames.Infrastructure.Entities;

namespace TheThroneOfGames.Infrastructure.Repository;

public class GameEntityRepository : BaseRepository<GameEntity>, IGameEntityRepository
{
    public GameEntityRepository(MainDbContext context) : base(context)
    {
    }
}