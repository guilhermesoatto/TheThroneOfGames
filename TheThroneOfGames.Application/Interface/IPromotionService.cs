using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheThroneOfGames.Application.DTO;

namespace TheThroneOfGames.Application.Interface
{
    public interface IPromotionService
    {
        Task CreatePromotionAsync(string title, string description, decimal discount, DateTime validUntil, List<Guid> gameIds);
        Task<PromotionDto> GetPromotionByIdAsync(Guid promotionId);
        Task ApplyPromotionAsync(Guid userId, Guid promotionId);
        Task<IEnumerable<PromotionDto>> GetUserAppliedPromotionsAsync(Guid userId);
    }
}
