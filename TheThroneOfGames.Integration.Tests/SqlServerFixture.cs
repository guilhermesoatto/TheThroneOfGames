using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TheThroneOfGames.Infrastructure.Persistence;
using Xunit;

namespace TheThroneOfGames.Integration.Tests;

/// <summary>
/// Cria um banco SQL Server descartável para a suíte de integração e aplica as migrations
/// reais do EF Core nele. A connection string vem de INTEGRATION_DB_CONNECTION (definida pelo
/// service container no CI); localmente usa o SQL Server do docker-compose.
/// O banco é derrubado ao final.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private const string DefaultServer =
        "Server=localhost,1433;User Id=sa;Password=Your_strong_Pass123;TrustServerCertificate=true;Encrypt=false";

    public string ConnectionString { get; private set; } = string.Empty;
    private string _databaseName = string.Empty;
    private string _masterConnectionString = string.Empty;

    public MainDbContext NewContext() =>
        new(new DbContextOptionsBuilder<MainDbContext>().UseSqlServer(ConnectionString).Options);

    public async Task InitializeAsync()
    {
        var raw = Environment.GetEnvironmentVariable("INTEGRATION_DB_CONNECTION") ?? DefaultServer;
        _databaseName = $"fcg_it_{Guid.NewGuid():N}";

        var master = new SqlConnectionStringBuilder(raw) { InitialCatalog = "master" };
        _masterConnectionString = master.ConnectionString;

        var db = new SqlConnectionStringBuilder(raw) { InitialCatalog = _databaseName };
        ConnectionString = db.ConnectionString;

        await ExecuteOnMasterAsync($"CREATE DATABASE [{_databaseName}];");

        await using var ctx = NewContext();
        await ctx.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        try
        {
            await ExecuteOnMasterAsync(
                $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}];");
        }
        catch
        {
            // Ambiente efêmero (container do CI) — ignorar falha de limpeza.
        }
    }

    private async Task ExecuteOnMasterAsync(string sql)
    {
        await using var conn = new SqlConnection(_masterConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "sqlserver";
}
