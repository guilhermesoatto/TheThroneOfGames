using System.ComponentModel.DataAnnotations;

namespace TheThroneOfGames.API.Models.DTO
{
    public class UserUpdateDTO
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;
    }
}