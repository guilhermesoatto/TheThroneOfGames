using GameStore.Common.Events;
using GameStore.Partidas.Domain.Entities;
using GameStore.Partidas.Domain.Repositories;

namespace GameStore.Partidas.Application.Services;

/// <summary>
/// Tenta parear um jogador com outro que já esteja esperando o mesmo jogo (1v1 imediato).
/// Reaproveitado por BuscarPartidaUseCase (primeira busca) e DesistirPartidaUseCase (o jogador
/// que não desistiu volta pra fila e o motor tenta formar uma partida nova na hora — ver
/// docs/ai/tasks/prd-partidas.json designDecisions.desistencia).
/// </summary>
public class MotorDePareamento
{
    private readonly ISolicitacaoBuscaRepository _solicitacaoRepository;
    private readonly IPartidaRepository _partidaRepository;
    private readonly IEventBus _eventBus;

    public MotorDePareamento(
        ISolicitacaoBuscaRepository solicitacaoRepository,
        IPartidaRepository partidaRepository,
        IEventBus eventBus)
    {
        _solicitacaoRepository = solicitacaoRepository;
        _partidaRepository = partidaRepository;
        _eventBus = eventBus;
    }

    public async Task<(SolicitacaoBusca Solicitacao, Partida? Partida)> TentarParearAsync(
        Guid jogadorId, Guid jogoId, CancellationToken ct = default)
    {
        var outra = await _solicitacaoRepository.BuscarOutroAguardandoAsync(jogoId, jogadorId, ct);
        var minha = new SolicitacaoBusca(jogadorId, jogoId);

        if (outra is null)
        {
            await _solicitacaoRepository.AddAsync(minha, ct);
            return (minha, null);
        }

        var (partida, evento) = Partida.Formar(jogoId, outra.JogadorId, jogadorId);
        outra.Parear(partida.Id);
        minha.Parear(partida.Id);

        await _partidaRepository.AddAsync(partida, ct);
        await _solicitacaoRepository.UpdateAsync(outra, ct);
        await _solicitacaoRepository.AddAsync(minha, ct);
        await _eventBus.PublishAsync(evento);

        return (minha, partida);
    }
}
