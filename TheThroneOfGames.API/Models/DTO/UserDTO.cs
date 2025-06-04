using TheThroneOfGames.Domain.Entities;

namespace TheThroneOfGames.API.Models.DTO
{
    public class UserDTO : Usuario
    {
        public UserDTO(string name, string email, string passwordHash, string role) : base(name, email, passwordHash, role)
        {
        }
    }
}
