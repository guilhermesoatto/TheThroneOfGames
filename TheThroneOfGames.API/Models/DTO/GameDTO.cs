using System.ComponentModel.DataAnnotations;

namespace TheThroneOfGames.API.Models.DTO;

public class GameDTO
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")]
    public required string Name { get; set; }

    [Required(ErrorMessage = "Genre is required")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Genre must be between 1 and 50 characters")]
    public required string Genre { get; set; }

    [Required(ErrorMessage = "Price is required")]
    [Range(0, 1000, ErrorMessage = "Price must be between $0 and $1000")]
    public decimal Price { get; set; }
}

public class GameListDTO
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Genre { get; set; }
    public decimal Price { get; set; }
}