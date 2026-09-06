namespace TheThroneOfGames.Application.Tests.Services;

/// <summary>
/// Cobre o hashing PBKDF2 de <see cref="UsuarioService.HashPassword"/> /
/// <see cref="UsuarioService.VerifyPassword"/> — funções puras, sem infraestrutura.
/// </summary>
[TestClass]
public class PasswordHashingTests
{
    [TestMethod]
    public void HashPassword_ShouldProduceVerifiableHash()
    {
        var hash = UsuarioService.HashPassword("Str0ng!Pass");

        Assert.IsTrue(UsuarioService.VerifyPassword(hash, "Str0ng!Pass"));
    }

    [TestMethod]
    public void HashPassword_ShouldBeSaltedPerCall()
    {
        var a = UsuarioService.HashPassword("Str0ng!Pass");
        var b = UsuarioService.HashPassword("Str0ng!Pass");

        Assert.AreNotEqual(a, b, "salt aleatório => hashes diferentes para a mesma senha");
        Assert.IsTrue(UsuarioService.VerifyPassword(a, "Str0ng!Pass"));
        Assert.IsTrue(UsuarioService.VerifyPassword(b, "Str0ng!Pass"));
    }

    [TestMethod]
    public void VerifyPassword_WithWrongPassword_ShouldReturnFalse()
    {
        var hash = UsuarioService.HashPassword("Str0ng!Pass");

        Assert.IsFalse(UsuarioService.VerifyPassword(hash, "wrong-password"));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("not-base64!!")]
    [DataRow("YWJj")] // "abc" — base64 válido mas com tamanho errado
    public void VerifyPassword_WithMalformedHash_ShouldReturnFalseWithoutThrowing(string malformed)
    {
        Assert.IsFalse(UsuarioService.VerifyPassword(malformed, "whatever"));
    }
}
