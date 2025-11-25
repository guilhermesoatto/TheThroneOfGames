using TheThroneOfGames.Domain.Interfaces;
using TheThroneOfGames.Infrastructure.Persistence;
using TheThroneOfGames.Infrastructure.Entities;
using TheThroneOfGames.Infrastructure.Repository;

namespace GameStore.Catalogo.Infrastructure.Repository;

public class GameEntityRepository : BaseRepository<GameEntity>, IGameEntityRepository
{
    public GameEntityRepository(MainDbContext context) : base(context)
    {
    }
}
