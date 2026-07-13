using GameStore.Partidas.Domain.Entities;
using GameStore.Partidas.Domain.Repositories;
using GameStore.Partidas.Domain.Shared;

namespace GameStore.Partidas.Application.UseCases;

public sealed record ConsultarStatusResultado(SolicitacaoBusca Solicitacao, Partida? Partida);

/// <summary>Consulta de status para o cliente fazer polling — usado também pelo script de validação (partidas-T08).</summary>
public class ConsultarStatusUseCase
{
    private readonly ISolicitacaoBuscaRepository _solicitacaoRepository;
    private readonly IPartidaRepository _partidaRepository;

    public ConsultarStatusUseCase(
        ISolicitacaoBuscaRepository solicitacaoRepository,
        IPartidaRepository partidaRepository)
    {
        _solicitacaoRepository = solicitacaoRepository;
        _partidaRepository = partidaRepository;
    }

    public async Task<Result<ConsultarStatusResultado>> ExecuteAsync(Guid solicitacaoId, CancellationToken ct = default)
    {
        var solicitacao = await _solicitacaoRepository.GetByIdAsync(solicitacaoId, ct);
        if (solicitacao is null)
            return new SolicitacaoNaoEncontradaError(solicitacaoId);

        var partida = solicitacao.PartidaId is Guid partidaId
            ? await _partidaRepository.GetByIdAsync(partidaId, ct)
            : null;

        return new ConsultarStatusResultado(solicitacao, partida);
    }
}
