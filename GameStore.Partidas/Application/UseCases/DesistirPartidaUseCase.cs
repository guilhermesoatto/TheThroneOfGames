using GameStore.Common.Events;
using GameStore.Partidas.Application.Services;
using GameStore.Partidas.Domain.Entities;
using GameStore.Partidas.Domain.Repositories;
using GameStore.Partidas.Domain.Shared;

namespace GameStore.Partidas.Application.UseCases;

/// <summary>
/// Um jogador desiste antes da confirmação mútua. Desfaz a partida, publica o evento de
/// cancelamento, e devolve IMEDIATAMENTE o outro jogador à fila — tentando formar uma partida
/// nova na hora (ver docs/ai/tasks/prd-partidas.json designDecisions.desistencia).
/// </summary>
public class DesistirPartidaUseCase
{
    private readonly IPartidaRepository _partidaRepository;
    private readonly IEventBus _eventBus;
    private readonly MotorDePareamento _motorDePareamento;

    public DesistirPartidaUseCase(
        IPartidaRepository partidaRepository,
        IEventBus eventBus,
        MotorDePareamento motorDePareamento)
    {
        _partidaRepository = partidaRepository;
        _eventBus = eventBus;
        _motorDePareamento = motorDePareamento;
    }

    public async Task<Result<Partida>> ExecuteAsync(Guid partidaId, Guid jogadorId, CancellationToken ct = default)
    {
        var partida = await _partidaRepository.GetByIdAsync(partidaId, ct);
        if (partida is null)
            return new PartidaNaoEncontradaError(partidaId);

        var result = partida.Desistir(jogadorId);
        if (result.IsFailure)
            return result.Error;

        await _partidaRepository.UpdateAsync(partida, ct);
        await _eventBus.PublishAsync(result.Value.Evento);

        await _motorDePareamento.TentarParearAsync(result.Value.JogadorRestanteId, partida.JogoId, ct);

        return partida;
    }
}
