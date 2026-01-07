using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using GameStore.Usuarios.Domain.Interfaces;
using GameStore.Usuarios.Domain.Entities;

namespace GameStore.Usuarios.Application.Services;

public class AuthenticationService
{
    private readonly IUsuarioRepository _userRepository;
    private readonly IConfiguration _configuration;

    public AuthenticationService(IUsuarioRepository userRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
    }

    public async Task<string?> AuthenticateAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null) return null;

        if (!UsuarioService.VerifyPassword(user.PasswordHash, password)) return null;
        if (!user.IsActive) return null;

        var jwtSettings = _configuration.GetSection("Jwt");
        var keyStr = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT 'Key' is not configured.");
        var issuer = jwtSettings["Issuer"] ?? string.Empty;
        var audience = jwtSettings["Audience"] ?? string.Empty;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            // Standard and framework-friendly claims
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            // Also add lightweight JWT claim names expected by integration tests (lowercase)
            new Claim("email", user.Email),
            new Claim("name", user.Name),
            new Claim("role", user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}