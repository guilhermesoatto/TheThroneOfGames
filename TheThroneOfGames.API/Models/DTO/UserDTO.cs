
namespace TheThroneOfGames.API.Models.DTO
{
    public class UserDTO
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }

        public UserDTO() { }
        public UserDTO(string name, string email, string passwordHash, string role)
        {
            Name = name;
            Email = email;
            PasswordHash = passwordHash;
            Role = role;
        }
    }
}
