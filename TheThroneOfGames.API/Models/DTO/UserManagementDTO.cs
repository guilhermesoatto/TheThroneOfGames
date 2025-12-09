using System.ComponentModel.DataAnnotations;

namespace TheThroneOfGames.API.Models.DTO;

public class UserListDTO
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Role { get; set; }
    public bool IsActive { get; set; }
}

public class UserRoleUpdateDTO
{
    [Required(ErrorMessage = "Role is required")]
    [RegularExpression("^(Admin|User)$", ErrorMessage = "Role must be either 'Admin' or 'User'")]
    public required string Role { get; set; }
}