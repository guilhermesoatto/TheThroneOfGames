using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheThroneOfGames.Domain.Entities;

namespace TheThroneOfGames.Application.Interface
{
    public interface IPromotionService : IBaseService<PromotionEntity>
    {
        // Additional promotion-specific methods can go here in future
    }
}
