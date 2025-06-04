using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheThroneOfGames.Application.DTO;
using TheThroneOfGames.Application.Interface;
using TheThroneOfGames.Infrastructure.Entities;

namespace TheThroneOfGames.Application
{
    //To Do
    //public class PromotionService : IPromotionService
    //{
    //    private readonly IPromotionRepository _promotionRepository;

    //    public PromotionService(IPromotionRepository promotionRepository)
    //    {
    //        _promotionRepository = promotionRepository;
    //    }

    //    public Task CreatePromotionAsync(PromotionDto dto)
    //    {
    //        if(dto.)
    //        var promotion = new PromotionEntity
    //        {
    //            Id = Guid.NewGuid(),
    //            Title = title,
    //            Description = description,
    //            Discount = discount,
    //            ValidUntil = validUntil,
    //            GameIds = gameIds
    //        };

    //        _promotionRepository.AddPromotion(promotion);

    //        return Task.CompletedTask;
    //    }

    //    public Task<IEnumerable<PromotionDto>> GetActivePromotionsAsync()
    //    {
    //        var promotions = _promotionRepository.GetAllPromotions()
    //            .Where(p => p.ValidUntil > DateTime.UtcNow);

    //        return Task.FromResult(promotions);
    //    }

    //    public Task<PromotionDto> GetPromotionByIdAsync(Guid promotionId)
    //    {
    //        var promotion = _promotionRepository.GetPromotionById(promotionId);
    //        if (promotion == null)
    //            throw new ArgumentException("Promoção não encontrada.");

    //        return Task.FromResult(promotion);
    //    }

    //    public Task DeletePromotionAsync(Guid promotionId)
    //    {
    //        var promotion = _promotionRepository.GetPromotionById(promotionId);
    //        if (promotion == null)
    //            throw new ArgumentException("Promoção não encontrada.");

    //        _promotionRepository.DeletePromotion(promotionId);

    //        return Task.CompletedTask;
    //    }
    //}
}
