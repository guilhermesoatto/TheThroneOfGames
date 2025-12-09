using NUnit.Framework;
using Microsoft.Extensions.Configuration;
using TheThroneOfGames.API.Services;
using TheThroneOfGames.Domain.Interfaces;
using Moq;
using TheThroneOfGames.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;

namespace Test.Integration;

[TestFixture]
public class AuthenticationTests
{
    private Mock<IConfiguration> _configurationMock;
    private Mock<IUsuarioRepository> _userRepositoryMock;
    private AuthenticationService _authService;
    private readonly string _validPassword = "P@ssw0rd!";
    private readonly string _validEmail = "test@test.com";

    [SetUp]
    public void Setup()
    {
        _configurationMock = new Mock<IConfiguration>();
        _userRepositoryMock = new Mock<IUsuarioRepository>();

        var jwtSection = new Mock<IConfigurationSection>();
        jwtSection.Setup(x => x["Key"]).Returns("SUA_CHAVE_SECRETA_AQUI_1234567890");
        jwtSection.Setup(x => x["Issuer"]).Returns("TheThroneOfGamesAPI");
        jwtSection.Setup(x => x["Audience"]).Returns("TheThroneOfGamesAPIUsers");

        _configurationMock.Setup(x => x.GetSection("Jwt")).Returns(jwtSection.Object);

        _authService = new AuthenticationService(_userRepositoryMock.Object, _configurationMock.Object);
    }

    [Test]
    public async Task AuthenticateAsync_ValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var hashedPassword = TheThroneOfGames.Application.UsuarioService.HashPassword(_validPassword);
        var user = new Usuario("Test User", _validEmail, hashedPassword, "User", "token");
        user.Activate(); // Ensure user is active

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(_validEmail))
            .ReturnsAsync(user);

    // Act
    var token = await _authService.AuthenticateAsync(_validEmail, _validPassword);

        // Assert
        Assert.That(token, Is.Not.Null);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

            Assert.Multiple(() =>
            {
                var emailClaim = jwtToken.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.Email);
                var roleClaim = jwtToken.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.Role);

                Assert.That(emailClaim.Value, Is.EqualTo(_validEmail));
                Assert.That(roleClaim.Value, Is.EqualTo("User"));
                Assert.That(jwtToken.Issuer, Is.EqualTo("TheThroneOfGamesAPI"));
                Assert.That(jwtToken.ValidTo, Is.GreaterThan(DateTime.UtcNow));
            });
    }

    [Test]
    public async Task AuthenticateAsync_InactiveUser_ShouldReturnNull()
    {
        // Arrange
        var hashedPassword = TheThroneOfGames.Application.UsuarioService.HashPassword(_validPassword);
        var user = new Usuario("Test User", _validEmail, hashedPassword, "User", "token");
        // Note: user.Activate() not called, so IsActive remains false

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(_validEmail))
            .ReturnsAsync(user);

        // Act
        var token = await _authService.AuthenticateAsync(_validEmail, _validPassword);

        // Assert
        Assert.That(token, Is.Null);
    }

    [Test]
    public async Task AuthenticateAsync_InvalidPassword_ShouldReturnNull()
    {
        // Arrange
        var hashedPassword = TheThroneOfGames.Application.UsuarioService.HashPassword(_validPassword);
        var user = new Usuario("Test User", _validEmail, hashedPassword, "User", "token");
        user.Activate();

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(_validEmail))
            .ReturnsAsync(user);

        // Act
        var token = await _authService.AuthenticateAsync(_validEmail, "wrongpassword");

        // Assert
        Assert.That(token, Is.Null);
    }

    [Test]
    public async Task AuthenticateAsync_NonexistentUser_ShouldReturnNull()
    {
        // Arrange
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(_validEmail))
            .ReturnsAsync((Usuario)null);

        // Act
        var token = await _authService.AuthenticateAsync(_validEmail, _validPassword);

        // Assert
        Assert.That(token, Is.Null);
    }
}