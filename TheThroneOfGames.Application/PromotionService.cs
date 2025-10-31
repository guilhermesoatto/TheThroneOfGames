using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheThroneOfGames.Application.Interface;
using TheThroneOfGames.Domain.Interfaces;
using TheThroneOfGames.Infrastructure.Entities;

namespace TheThroneOfGames.Application
{
    public class PromotionService : IPromotionService
    {
        private readonly IBaseRepository<Promotion> _promotionRepository;

        public PromotionService(IBaseRepository<Promotion> promotionRepository)
        {
            _promotionRepository = promotionRepository;
        }

        public async Task AddAsync(Promotion entity)
        {
            await _promotionRepository.AddAsync(entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _promotionRepository.DeleteAsync(id);
        }

        public async Task<IEnumerable<Promotion>> GetAllAsync()
        {
            return await _promotionRepository.GetAllAsync();
        }

        public async Task<Promotion?> GetByIdAsync(Guid id)
        {
            return await _promotionRepository.GetByIdAsync(id);
        }

        public async Task UpdateAsync(Promotion entity)
        {
            await _promotionRepository.UpdateAsync(entity);
        }
    }
}
