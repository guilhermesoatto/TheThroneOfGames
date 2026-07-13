using GameStore.Common.Events;
using GameStore.Partidas.Domain.Entities;
using GameStore.Partidas.Domain.Repositories;
using GameStore.Partidas.Domain.Shared;

namespace GameStore.Partidas.Application.UseCases;

public class ConfirmarPartidaUseCase
{
    private readonly IPartidaRepository _partidaRepository;
    private readonly IEventBus _eventBus;

    public ConfirmarPartidaUseCase(IPartidaRepository partidaRepository, IEventBus eventBus)
    {
        _partidaRepository = partidaRepository;
        _eventBus = eventBus;
    }

    public async Task<Result<Partida>> ExecuteAsync(Guid partidaId, Guid jogadorId, CancellationToken ct = default)
    {
        var partida = await _partidaRepository.GetByIdAsync(partidaId, ct);
        if (partida is null)
            return new PartidaNaoEncontradaError(partidaId);

        var result = partida.Confirmar(jogadorId);
        if (result.IsFailure)
            return result.Error;

        await _partidaRepository.UpdateAsync(partida, ct);

        // Só publica quando TODOS confirmaram — result.Value vem null enquanto ainda falta alguém.
        if (result.Value is not null)
            await _eventBus.PublishAsync(result.Value);

        return partida;
    }
}
