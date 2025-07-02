namespace TheThroneOfGames.API.Models.DTO
{
    // DTO puro para transferência de dados de usuário
    public class UserDTO
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public string Nickname { get; set; }

        public UserDTO() { }
        public UserDTO(string name, string email, string password, string role, string nickname = null)
        {
            Name = name;
            Email = email;
            Password = password;
            Role = role;
            Nickname = nickname;
        }
    }
}
