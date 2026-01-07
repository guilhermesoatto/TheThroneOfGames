using System;

namespace GameStore.Usuarios.Domain.Entities
{
    public class Usuario
    {
        // Parameterless constructor required by EF Core for materialization
        protected Usuario() { }

        public Guid Id { get; private set; }
        public string Name { get; private set; } = null!;
        public string Email { get; private set; } = null!;
        public string PasswordHash { get; private set; } = null!;
        public string Role { get; private set; } = null!;
        public bool IsActive { get; private set; }
        public string ActiveToken { get; set; } = null!;

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

        public void UpdateRole(string newRole)
        {
            if (string.IsNullOrWhiteSpace(newRole))
                throw new ArgumentException("Role cannot be empty", nameof(newRole));
            Role = newRole;
        }

        public void ChangeRole(string newRole)
        {
            if (string.IsNullOrWhiteSpace(newRole))
                throw new ArgumentException("Role cannot be empty", nameof(newRole));
            Role = newRole;
        }

        public void UpdateProfile(string newName, string newEmail)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("Name is required", nameof(newName));

            if (string.IsNullOrWhiteSpace(newEmail))
                throw new ArgumentException("Email is required", nameof(newEmail));

            Name = newName;
            Email = newEmail;
        }

        public void Disable()
        {
            IsActive = false;
        }

        public void Enable()
        {
            IsActive = true;
        }
    }
}