using TheThroneOfGames.Domain.Interfaces;
using TheThroneOfGames.Infrastructure.Persistence;
using TheThroneOfGames.Domain.Entities;
using TheThroneOfGames.Infrastructure.Repository;

namespace GameStore.Vendas.Infrastructure.Repository;

public class PurchaseRepository : BaseRepository<PurchaseEntity>, IBaseRepository<PurchaseEntity>
{
    public PurchaseRepository(MainDbContext context) : base(context)
    {
    }
}
