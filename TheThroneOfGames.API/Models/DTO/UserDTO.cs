using TheThroneOfGames.Domain;
using TheThroneOfGames.Domain.Auth;

namespace TheThroneOfGames.API.Models.DTO
{
    public class UserDTO : UsuarioDomain
    {
        public UserDTO(string name, string email, string passwordHash, string role)
        {
            Name = name;
            Email = email;
            Password = passwordHash;
            Role = role;
        }
    }
}
