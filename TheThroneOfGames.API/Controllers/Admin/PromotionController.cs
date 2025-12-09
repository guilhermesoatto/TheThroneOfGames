using Microsoft.AspNetCore.Mvc;
using TheThroneOfGames.API.Controllers.Base;
using TheThroneOfGames.API.Models.DTO;
using TheThroneOfGames.Application.Interface;
using TheThroneOfGames.Domain.Entities;

namespace TheThroneOfGames.API.Controllers.Admin;

public class PromotionController : AdminControllerBase
{
    private readonly IPromotionService _promotionService;

    public PromotionController(IPromotionService promotionService)
    {
        _promotionService = promotionService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<PromotionListDTO>), 200)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var promotions = await _promotionService.GetAllAsync();
            var dtos = promotions.Select(p => new PromotionListDTO
            {
                Id = p.Id,
                Discount = p.Discount,
                ValidUntil = p.ValidUntil
            }).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PromotionDTO), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var p = await _promotionService.GetByIdAsync(id);
            if (p == null) return NotFoundById<PromotionEntity>(id);

            var dto = new PromotionDTO
            {
                Id = p.Id,
                Discount = p.Discount,
                ValidUntil = p.ValidUntil
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(PromotionDTO), 201)]
    public async Task<IActionResult> Create([FromBody] PromotionDTO dto)
    {
        try
        {
            var p = new PromotionEntity
            {
                Discount = dto.Discount,
                ValidUntil = dto.ValidUntil
            };

            await _promotionService.AddAsync(p);

            dto.Id = p.Id;
            return CreatedAtAction(nameof(GetById), new { id = p.Id }, dto);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PromotionDTO), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] PromotionDTO dto)
    {
        try
        {
            var existing = await _promotionService.GetByIdAsync(id);
            if (existing == null) return NotFoundById<PromotionEntity>(id);

            existing.Discount = dto.Discount;
            existing.ValidUntil = dto.ValidUntil;

            await _promotionService.UpdateAsync(existing);

            dto.Id = id;
            return Ok(dto);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var existing = await _promotionService.GetByIdAsync(id);
            if (existing == null) return NotFoundById<PromotionEntity>(id);

            await _promotionService.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }
}
