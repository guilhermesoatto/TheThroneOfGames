using TheThroneOfGames.Application.Interface;
using TheThroneOfGames.Domain.Entities;
using TheThroneOfGames.Domain.Interfaces;

namespace TheThroneOfGames.Application;

public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _userRepository;

    public UsuarioService(IUsuarioRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<Usuario>> GetAllUsersAsync()
    {
        return await _userRepository.GetAllAsync();
    }

    public async Task<Usuario> GetUserByIdAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new ArgumentException($"User with ID {userId} not found.", nameof(userId));
        return user;
    }

    public async Task UpdateUserRoleAsync(Guid userId, string newRole)
    {
        var user = await GetUserByIdAsync(userId);
        user.UpdateRole(newRole);
        await _userRepository.UpdateAsync(user);
    }

    public async Task DisableUserAsync(Guid userId)
    {
        var user = await GetUserByIdAsync(userId);
        user.Disable();
        await _userRepository.UpdateAsync(user);
    }

    public async Task EnableUserAsync(Guid userId)
    {
        var user = await GetUserByIdAsync(userId);
        user.Enable();
        await _userRepository.UpdateAsync(user);
    }

    public async Task ActivateUserAsync(string activationToken)
    {
        var user = await _userRepository.GetByActivationTokenAsync(activationToken);
        if (user == null)
            throw new Exception("Token inválido ou expirado.");

        user.Activate();
        await _userRepository.UpdateAsync(user);
    }

    public async Task<string> PreRegisterUserAsync(string email, string name, string password)
    {
        // Validações básicas
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("E-mail é obrigatório.", nameof(email));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome é obrigatório.", nameof(name));

        if (!IsStrongPassword(password))
            throw new ArgumentException("Senha fraca. A senha deve ter no mínimo 8 caracteres, conter letras, números e caracteres especiais.");

        // Gerar hash de senha
        var passwordHash = HashPassword(password);

        // Gerar token de ativação
        var activationToken = Guid.NewGuid().ToString();

        var user = new Usuario(name, email, passwordHash, "User", activationToken);

        await _userRepository.AddAsync(user);

        return activationToken;
    }

    // Simple PBKDF2-based password hashing
    public static string HashPassword(string password)
    {
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        byte[] salt = new byte[16];
        rng.GetBytes(salt);

        using var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(password, salt, 100_000, System.Security.Cryptography.HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);

        var hashBytes = new byte[49]; // 16 salt + 32 hash + 1 version
        hashBytes[0] = 0x01; // version
        Buffer.BlockCopy(salt, 0, hashBytes, 1, 16);
        Buffer.BlockCopy(hash, 0, hashBytes, 17, 32);

        return Convert.ToBase64String(hashBytes);
    }

    public static bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        try
        {
            var hashBytes = Convert.FromBase64String(hashedPassword);
            if (hashBytes.Length != 49 || hashBytes[0] != 0x01) return false;
            var salt = new byte[16];
            Buffer.BlockCopy(hashBytes, 1, salt, 0, 16);
            var storedHash = new byte[32];
            Buffer.BlockCopy(hashBytes, 17, storedHash, 0, 32);

            using var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(providedPassword, salt, 100_000, System.Security.Cryptography.HashAlgorithmName.SHA256);
            var computed = pbkdf2.GetBytes(32);

            // Compare hash bytes in fixed time to prevent timing attacks
            return computed.SequenceEqual(storedHash);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsStrongPassword(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8) return false;
        bool hasLetter = false, hasDigit = false, hasSpecial = false;
        foreach (var c in password)
        {
            if (char.IsLetter(c)) hasLetter = true;
            else if (char.IsDigit(c)) hasDigit = true;
            else if (!char.IsWhiteSpace(c)) hasSpecial = true;
        }

        return hasLetter && hasDigit && hasSpecial;
    }
}
