namespace GameStore.Catalogo.API.Tests;

/// <summary>
/// DTOs locais para os testes de integração deste bounded context.
/// Substituem os DTOs do monolito legado (TheThroneOfGames.API.Models.DTO) para eliminar
/// qualquer dependência de código do antigo monolito nestes testes.
/// </summary>
public class UserDTO
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string Role { get; set; }
}

public class LoginDTO
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}

public class GameDTO
{
    public Guid? Id { get; set; }
    public required string Name { get; set; }
    public required string Genre { get; set; }
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsAvailable { get; set; } = true;
}
