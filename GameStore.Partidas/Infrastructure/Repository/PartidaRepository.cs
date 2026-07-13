using GameStore.Partidas.Domain.Entities;
using GameStore.Partidas.Domain.Repositories;
using GameStore.Partidas.Domain.ValueObjects;
using GameStore.Partidas.Infrastructure.Persistence;
using GameStore.Partidas.Infrastructure.Persistence.Documents;
using MongoDB.Driver;

namespace GameStore.Partidas.Infrastructure.Repository;

public class PartidaRepository : IPartidaRepository
{
    private readonly PartidasMongoContext _context;

    public PartidaRepository(PartidasMongoContext context)
    {
        _context = context;
    }

    public async Task<Partida?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var document = await _context.Partidas.Find(p => p.Id == id).FirstOrDefaultAsync(ct);
        return document is null ? null : ToDomain(document);
    }

    public async Task AddAsync(Partida partida, CancellationToken ct = default)
    {
        await _context.Partidas.InsertOneAsync(ToDocument(partida), cancellationToken: ct);
    }

    public async Task UpdateAsync(Partida partida, CancellationToken ct = default)
    {
        await _context.Partidas.ReplaceOneAsync(p => p.Id == partida.Id, ToDocument(partida), cancellationToken: ct);
    }

    private static PartidaDocument ToDocument(Partida partida) => new()
    {
        Id = partida.Id,
        JogoId = partida.JogoId,
        DataHora = partida.DataHora,
        Status = partida.Status.ToString(),
        EquipeA = partida.EquipeA.Jogadores.ToList(),
        EquipeB = partida.EquipeB.Jogadores.ToList(),
        Confirmados = partida.Confirmados.ToList(),
        CriadoEmParaTtl = partida.DataHora,
    };

    private static Partida ToDomain(PartidaDocument document) => Partida.Reidratar(
        document.Id,
        document.JogoId,
        document.DataHora,
        Enum.Parse<StatusPartida>(document.Status),
        EquipeSlots.Reidratar(document.EquipeA),
        EquipeSlots.Reidratar(document.EquipeB),
        document.Confirmados);
}
