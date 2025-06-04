using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace TheThroneOfGames.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest loginRequest)
        {
            // Validação do usuário (exemplo simplificado)
            if (loginRequest.Email != "admin" || loginRequest.Password != "password")
                return Unauthorized("Credenciais inválidas.");

            // Claims do usuário
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "12345"), // ID do usuário
                new Claim(JwtRegisteredClaimNames.Name, loginRequest.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // ID único do token
                new Claim(ClaimTypes.Role, loginRequest.Email == "Admin" ? "Admin" : "User") // Papel do usuário
            };

            // Gerar o token
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("OTronoEhAChave"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "SeuEmissor",
                audience: "SuaAudiencia",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1), // Expiração do token
                signingCredentials: creds
            );

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiration = token.ValidTo
            });
        }
    }
}