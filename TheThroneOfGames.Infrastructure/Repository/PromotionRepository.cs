using TheThroneOfGames.Domain.Interfaces;
using TheThroneOfGames.Infrastructure.Persistence;
using TheThroneOfGames.Domain.Entities;

namespace TheThroneOfGames.Infrastructure.Repository;

public class PromotionRepository : BaseRepository<PromotionEntity>, IPromotionRepository
{
    public PromotionRepository(MainDbContext context) : base(context)
    {
    }
}
