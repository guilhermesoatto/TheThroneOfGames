using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheThroneOfGames.Domain.Auth
{
    public class UsuarioDomain
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Nickname { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public string ActiveToken { get; set; }
        public bool IsActive { get; set; }

        public void Activate() => IsActive = true;

        public bool IsValidToRegister()
        {
            ValidateEmail(Email);
            ValidatePassword(Password);
            return true;
        }

        private void ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                throw new ArgumentException("E-mail inválido.");
        }

        private void ValidatePassword(string password)
        {
            if (password.Length < 8 || !password.Any(char.IsDigit) || !password.Any(char.IsLetter) || !password.Any(char.IsSymbol))
                throw new ArgumentException("Senha deve ter pelo menos 8 caracteres, incluindo números, letras e caracteres especiais.");
        }
    }
}
