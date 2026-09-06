using TheThroneOfGames.Domain.Entities;
using TheThroneOfGames.Domain.Interfaces;
using TheThroneOfGames.Infrastructure.Persistence;

namespace TheThroneOfGames.Infrastructure.Repository;

public class GameEntityRepository : BaseRepository<GameEntity>, IGameEntityRepository
{
    public GameEntityRepository(MainDbContext context) : base(context)
    {
    }
}
