using TheThroneOfGames.Domain.Entities;
using TheThroneOfGames.Domain.Interfaces;
using TheThroneOfGames.Infrastructure.Persistence;

namespace TheThroneOfGames.Infrastructure.Repository;

public class PromotionRepository : BaseRepository<PromotionEntity>, IPromotionRepository
{
    public PromotionRepository(MainDbContext context) : base(context)
    {
    }
}
