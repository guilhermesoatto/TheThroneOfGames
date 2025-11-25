using TheThroneOfGames.Domain.Interfaces;
using TheThroneOfGames.Infrastructure.Persistence;
using TheThroneOfGames.Infrastructure.Entities;
using TheThroneOfGames.Infrastructure.Repository;

namespace GameStore.Vendas.Infrastructure.Repository;

public class PurchaseRepository : BaseRepository<Purchase>, IBaseRepository<Purchase>
{
    public PurchaseRepository(MainDbContext context) : base(context)
    {
    }
}
