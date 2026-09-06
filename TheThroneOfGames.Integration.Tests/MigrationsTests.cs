using Microsoft.EntityFrameworkCore;
using Xunit;

namespace TheThroneOfGames.Integration.Tests;

[Collection(SqlServerCollection.Name)]
public sealed class MigrationsTests(SqlServerFixture fixture)
{
    [Fact]
    public async Task Database_is_reachable()
    {
        await using var ctx = fixture.NewContext();
        Assert.True(await ctx.Database.CanConnectAsync());
    }

    [Fact]
    public async Task All_migrations_are_applied_with_none_pending()
    {
        await using var ctx = fixture.NewContext();

        var pending = await ctx.Database.GetPendingMigrationsAsync();
        Assert.Empty(pending);

        var applied = (await ctx.Database.GetAppliedMigrationsAsync()).ToList();
        var defined = ctx.Database.GetMigrations().ToList();
        Assert.Equal(defined, applied);
    }
}
