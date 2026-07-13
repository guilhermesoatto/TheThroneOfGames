using FluentAssertions;
using GameStore.Common.Events;
using GameStore.Partidas.Application.UseCases;
using GameStore.Partidas.Domain.Entities;
using GameStore.Partidas.Domain.Repositories;
using GameStore.Partidas.Domain.Shared;
using GameStore.Partidas.Domain.ValueObjects;
using NSubstitute;

namespace GameStore.Partidas.Tests.Application;

public class ConfirmarPartidaUseCaseTests
{
    private readonly IPartidaRepository _partidaRepository;
    private readonly IEventBus _eventBus;
    private readonly ConfirmarPartidaUseCase _useCase;

    public ConfirmarPartidaUseCaseTests()
    {
        _partidaRepository = Substitute.For<IPartidaRepository>();
        _eventBus = Substitute.For<IEventBus>();
        _useCase = new ConfirmarPartidaUseCase(_partidaRepository, _eventBus);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPartidaNotFound_ShouldReturnError()
    {
        var partidaId = Guid.NewGuid();
        _partidaRepository.GetByIdAsync(partidaId, Arg.Any<CancellationToken>()).Returns((Partida?)null);

        var result = await _useCase.ExecuteAsync(partidaId, Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<PartidaNaoEncontradaError>();
    }

    [Fact]
    public async Task ExecuteAsync_FirstPlayerConfirming_ShouldUpdate_ButNotPublish()
    {
        var (partida, _) = Partida.Formar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var jogadorA = partida.EquipeA.Jogadores[0];
        _partidaRepository.GetByIdAsync(partida.Id, Arg.Any<CancellationToken>()).Returns(partida);

        var result = await _useCase.ExecuteAsync(partida.Id, jogadorA);

        result.IsSuccess.Should().BeTrue();
        await _partidaRepository.Received(1).UpdateAsync(partida, Arg.Any<CancellationToken>());
        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<PartidaConfirmadaEvent>());
    }

    [Fact]
    public async Task ExecuteAsync_LastPlayerConfirming_ShouldPublishPartidaConfirmadaEvent()
    {
        var (partida, _) = Partida.Formar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var jogadorA = partida.EquipeA.Jogadores[0];
        var jogadorB = partida.EquipeB.Jogadores[0];
        partida.Confirmar(jogadorA);
        _partidaRepository.GetByIdAsync(partida.Id, Arg.Any<CancellationToken>()).Returns(partida);

        var result = await _useCase.ExecuteAsync(partida.Id, jogadorB);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(StatusPartida.Confirmada);
        await _eventBus.Received(1).PublishAsync(Arg.Any<PartidaConfirmadaEvent>());
    }
}
