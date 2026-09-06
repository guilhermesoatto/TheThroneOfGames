using TheThroneOfGames.Domain.Entities;
using TheThroneOfGames.Infrastructure.Repository;
using Xunit;

namespace TheThroneOfGames.Integration.Tests;

/// <summary>
/// Round-trips de persistência contra SQL Server real — pega o que o provider InMemory
/// não pega: dialeto SQL, precisão de coluna decimal, tradução de query.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class RepositorySqlServerTests(SqlServerFixture fixture)
{
    [Fact]
    public async Task Game_AddThenGetById_PersistsWithExactDecimalPrecision()
    {
        var game = new GameEntity
        {
            Id = Guid.NewGuid(),
            Name = "Elden Ring",
            Genre = "RPG",
            Price = 199.99m,
            Description = "Souls-like open world",
            IsAvailable = true
        };

        await using (var write = fixture.NewContext())
        {
            await new GameEntityRepository(write).AddAsync(game);
        }

        await using var read = fixture.NewContext();
        var persisted = await new GameEntityRepository(read).GetByIdAsync(game.Id);

        Assert.NotNull(persisted);
        Assert.Equal("Elden Ring", persisted!.Name);
        Assert.Equal(199.99m, persisted.Price); // precisão exata só é garantida em banco relacional
        Assert.True(persisted.IsAvailable);
    }

    [Fact]
    public async Task Game_Update_PersistsChanges()
    {
        var game = new GameEntity { Id = Guid.NewGuid(), Name = "Dark Souls", Genre = "RPG", Price = 99.90m, IsAvailable = true };

        await using (var write = fixture.NewContext())
        {
            var repo = new GameEntityRepository(write);
            await repo.AddAsync(game);
            game.Price = 49.90m;
            game.IsAvailable = false;
            await repo.UpdateAsync(game);
        }

        await using var read = fixture.NewContext();
        var persisted = await new GameEntityRepository(read).GetByIdAsync(game.Id);

        Assert.NotNull(persisted);
        Assert.Equal(49.90m, persisted!.Price);
        Assert.False(persisted.IsAvailable);
    }

    [Fact]
    public async Task Usuario_AddThenQueries_PersistAggregate()
    {
        var email = $"geralt-{Guid.NewGuid():N}@throneofgames.test";
        var token = Guid.NewGuid().ToString();
        var usuario = new Usuario("Geralt de Rivia", email, "hashed-password", "Player", token);

        await using (var write = fixture.NewContext())
        {
            await new UsuarioRepository(write).AddAsync(usuario);
        }

        await using var read = fixture.NewContext();
        var repo = new UsuarioRepository(read);

        var byEmail = await repo.GetByEmailAsync(email);
        var byToken = await repo.GetByActivationTokenAsync(token);

        Assert.NotNull(byEmail);
        Assert.Equal(usuario.Id, byEmail!.Id);
        Assert.False(byEmail.IsActive);
        Assert.NotNull(byToken);
        Assert.Equal(usuario.Id, byToken!.Id);
    }
}
