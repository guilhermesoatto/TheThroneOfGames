using System.ComponentModel.DataAnnotations;

namespace TheThroneOfGames.API.Models.DTO;

public class PromotionDTO
{
    public Guid? Id { get; set; }

    [Required]
    [Range(0, 1, ErrorMessage = "Discount must be between 0 and 1 (fraction)")]
    public decimal Discount { get; set; }

    [Required]
    public DateTime ValidUntil { get; set; }
}

public class PromotionListDTO
{
    public required Guid Id { get; set; }
    public decimal Discount { get; set; }
    public DateTime ValidUntil { get; set; }
}
