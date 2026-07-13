using GameStore.Partidas.Domain.Entities;
using GameStore.Partidas.Domain.Repositories;
using GameStore.Partidas.Domain.ValueObjects;
using GameStore.Partidas.Infrastructure.Persistence;
using GameStore.Partidas.Infrastructure.Persistence.Documents;
using MongoDB.Driver;

namespace GameStore.Partidas.Infrastructure.Repository;

public class SolicitacaoBuscaRepository : ISolicitacaoBuscaRepository
{
    private readonly PartidasMongoContext _context;

    public SolicitacaoBuscaRepository(PartidasMongoContext context)
    {
        _context = context;
    }

    public async Task<SolicitacaoBusca?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var document = await _context.SolicitacoesBusca.Find(s => s.Id == id).FirstOrDefaultAsync(ct);
        return document is null ? null : ToDomain(document);
    }

    public async Task<SolicitacaoBusca?> GetAguardandoDoJogadorAsync(Guid jogadorId, Guid jogoId, CancellationToken ct = default)
    {
        var document = await _context.SolicitacoesBusca
            .Find(s => s.JogadorId == jogadorId && s.JogoId == jogoId && s.Status == StatusSolicitacao.Aguardando.ToString())
            .FirstOrDefaultAsync(ct);
        return document is null ? null : ToDomain(document);
    }

    public async Task<SolicitacaoBusca?> BuscarOutroAguardandoAsync(Guid jogoId, Guid jogadorIdSolicitante, CancellationToken ct = default)
    {
        var document = await _context.SolicitacoesBusca
            .Find(s => s.JogoId == jogoId
                       && s.JogadorId != jogadorIdSolicitante
                       && s.Status == StatusSolicitacao.Aguardando.ToString())
            .SortBy(s => s.CriadoEm) // o mais antigo esperando ganha — evita starvation
            .FirstOrDefaultAsync(ct);
        return document is null ? null : ToDomain(document);
    }

    public async Task AddAsync(SolicitacaoBusca solicitacao, CancellationToken ct = default)
    {
        await _context.SolicitacoesBusca.InsertOneAsync(ToDocument(solicitacao), cancellationToken: ct);
    }

    public async Task UpdateAsync(SolicitacaoBusca solicitacao, CancellationToken ct = default)
    {
        await _context.SolicitacoesBusca.ReplaceOneAsync(s => s.Id == solicitacao.Id, ToDocument(solicitacao), cancellationToken: ct);
    }

    private static SolicitacaoBuscaDocument ToDocument(SolicitacaoBusca solicitacao) => new()
    {
        Id = solicitacao.Id,
        JogadorId = solicitacao.JogadorId,
        JogoId = solicitacao.JogoId,
        Status = solicitacao.Status.ToString(),
        CriadoEm = solicitacao.CriadoEm,
        PartidaId = solicitacao.PartidaId,
        CriadoEmParaTtl = solicitacao.CriadoEm,
    };

    private static SolicitacaoBusca ToDomain(SolicitacaoBuscaDocument document) => SolicitacaoBusca.Reidratar(
        document.Id,
        document.JogadorId,
        document.JogoId,
        Enum.Parse<StatusSolicitacao>(document.Status),
        document.CriadoEm,
        document.PartidaId);
}
