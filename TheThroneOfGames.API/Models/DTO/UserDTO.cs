
namespace TheThroneOfGames.API.Models.DTO
{
    public class UserDTO
    {
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        // Accept raw password from client; service will hash
        public string Password { get; set; } = null!;
        public string Role { get; set; } = null!;

        public UserDTO() { }
        public UserDTO(string name, string email, string password, string role)
        {
            Name = name;
            Email = email;
            Password = password;
            Role = role;
        }
    }
}
