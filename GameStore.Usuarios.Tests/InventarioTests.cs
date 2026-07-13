using FluentAssertions;
using GameStore.Usuarios.Domain.Entities;
using GameStore.Usuarios.Infrastructure.Persistence;
using GameStore.Usuarios.Infrastructure.Repository;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Usuarios.Tests;

/// <summary>
/// Cobre o Inventário (partidas-T00, ver docs/ai/tasks/prd-partidas.json) — fonte da verdade
/// de "o jogador possui este jogo?" consultada pelo bounded context de Partidas antes de
/// liberar a busca por partida. Consumers RabbitMQ (GameCompradoEventConsumer) não são
/// unit-testáveis diretamente neste codebase (conectam a um broker real no construtor, mesmo
/// padrão dos outros consumers) — a persistência a partir do evento é validada ao vivo pelo
/// script de aceite (tools/validate-partidas.js), aqui testamos o repositório isoladamente.
/// </summary>
public class InventarioTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly UsuariosDbContext _context;
    private readonly InventarioRepository _repository;

    public InventarioTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<UsuariosDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new UsuariosDbContext(options);
        _context.Database.EnsureCreated();
        _repository = new InventarioRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task PossuiJogoAsync_WhenNotInInventario_ShouldReturnFalse()
    {
        var possui = await _repository.PossuiJogoAsync(Guid.NewGuid(), Guid.NewGuid());

        possui.Should().BeFalse();
    }

    [Fact]
    public async Task AdicionarSeNaoExistir_ThenPossuiJogo_ShouldReturnTrue()
    {
        var usuarioId = Guid.NewGuid();
        var jogoId = Guid.NewGuid();

        await _repository.AdicionarSeNaoExistirAsync(new ItemInventario(usuarioId, jogoId, "Elden Ring"));
        var possui = await _repository.PossuiJogoAsync(usuarioId, jogoId);

        possui.Should().BeTrue();
    }

    [Fact]
    public async Task AdicionarSeNaoExistir_CalledTwiceForSameGame_ShouldNotDuplicate()
    {
        var usuarioId = Guid.NewGuid();
        var jogoId = Guid.NewGuid();

        await _repository.AdicionarSeNaoExistirAsync(new ItemInventario(usuarioId, jogoId, "Elden Ring"));
        await _repository.AdicionarSeNaoExistirAsync(new ItemInventario(usuarioId, jogoId, "Elden Ring"));

        var total = await _context.ItensInventario.CountAsync(i => i.UsuarioId == usuarioId && i.JogoId == jogoId);
        total.Should().Be(1);
    }

    [Fact]
    public async Task PossuiJogoAsync_ForDifferentPlayer_ShouldReturnFalse()
    {
        var jogoId = Guid.NewGuid();
        await _repository.AdicionarSeNaoExistirAsync(new ItemInventario(Guid.NewGuid(), jogoId, "Elden Ring"));

        var possui = await _repository.PossuiJogoAsync(Guid.NewGuid(), jogoId);

        possui.Should().BeFalse();
    }
}
