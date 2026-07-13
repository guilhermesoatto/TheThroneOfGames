using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace GameStore.Partidas.API.Tests;

/// <summary>
/// GameStore.Partidas não tem endpoint de login (identidade mora em GameStore.Usuarios) —
/// para testar os endpoints [Authorize] em isolamento, assina um JWT com a MESMA chave
/// configurada em appsettings.json (interoperabilidade entre serviços), sem precisar subir
/// Usuarios de verdade. O script de validação ponta a ponta (partidas-T08) usa login real.
/// </summary>
public static class JwtTestTokenFactory
{
    private const string Key = "your-super-secret-key-that-is-at-least-32-characters-long!";

    public static string CriarToken(Guid jogadorId, string role = "User")
    {
        var claims = new[]
        {
            new Claim("sub", jogadorId.ToString()),
            new Claim(ClaimTypes.Role, role),
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Key)), SecurityAlgorithms.HmacSha256Signature);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
