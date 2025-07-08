using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheThroneOfGames.Domain.Entities
{
    public class Usuario
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public string Email { get; private set; }
        public string PasswordHash { get; private set; }
        public string Role { get; private set; }
        public bool IsActive { get; private set; }
        public string ActiveToken { get; set; }
        public Usuario(string name, string email, string passwordHash, string role, string activeToken)
        {
            Id = Guid.NewGuid();
            Name = name;
            Email = email;
            PasswordHash = passwordHash;
            Role = role;
            IsActive = false;
            ActiveToken = activeToken;
        }

        public Usuario(Guid id, string name, string email, string passwordHash, string role, bool isActive, string activeToken)
        {
            Id = id;
            Name = name;
            Email = email;
            PasswordHash = passwordHash;
            Role = role;
            IsActive = isActive;
            ActiveToken = activeToken;
        }

        public void Activate() => IsActive = true;
    }
}
