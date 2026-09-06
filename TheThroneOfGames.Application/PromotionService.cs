using TheThroneOfGames.Application.Interface;
using TheThroneOfGames.Domain.Entities;
using TheThroneOfGames.Domain.Interfaces;

namespace TheThroneOfGames.Application
{
    public class PromotionService : IPromotionService
    {
        private readonly IBaseRepository<PromotionEntity> _promotionRepository;

        public PromotionService(IBaseRepository<PromotionEntity> promotionRepository)
        {
            _promotionRepository = promotionRepository;
        }

        public async Task AddAsync(PromotionEntity entity)
        {
            await _promotionRepository.AddAsync(entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _promotionRepository.DeleteAsync(id);
        }

        public async Task<IEnumerable<PromotionEntity>> GetAllAsync()
        {
            return await _promotionRepository.GetAllAsync();
        }

        public async Task<PromotionEntity?> GetByIdAsync(Guid id)
        {
            return await _promotionRepository.GetByIdAsync(id);
        }

        public async Task UpdateAsync(PromotionEntity entity)
        {
            await _promotionRepository.UpdateAsync(entity);
        }
    }
}
