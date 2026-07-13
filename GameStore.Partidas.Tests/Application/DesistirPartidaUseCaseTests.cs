using FluentAssertions;
using GameStore.Common.Events;
using GameStore.Partidas.Application.Services;
using GameStore.Partidas.Application.UseCases;
using GameStore.Partidas.Domain.Entities;
using GameStore.Partidas.Domain.Repositories;
using GameStore.Partidas.Domain.Shared;
using GameStore.Partidas.Domain.ValueObjects;
using NSubstitute;

namespace GameStore.Partidas.Tests.Application;

public class DesistirPartidaUseCaseTests
{
    private readonly IPartidaRepository _partidaRepository;
    private readonly ISolicitacaoBuscaRepository _solicitacaoRepository;
    private readonly IEventBus _eventBus;
    private readonly DesistirPartidaUseCase _useCase;

    public DesistirPartidaUseCaseTests()
    {
        _partidaRepository = Substitute.For<IPartidaRepository>();
        _solicitacaoRepository = Substitute.For<ISolicitacaoBuscaRepository>();
        _eventBus = Substitute.For<IEventBus>();
        var motor = new MotorDePareamento(_solicitacaoRepository, _partidaRepository, _eventBus);
        _useCase = new DesistirPartidaUseCase(_partidaRepository, _eventBus, motor);
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
    public async Task ExecuteAsync_ShouldCancelPartida_PublishEvent_AndRequeueRemainingPlayer()
    {
        var jogoId = Guid.NewGuid();
        var (partida, _) = Partida.Formar(jogoId, Guid.NewGuid(), Guid.NewGuid());
        var jogadorQueDesiste = partida.EquipeA.Jogadores[0];
        var jogadorRestante = partida.EquipeB.Jogadores[0];
        _partidaRepository.GetByIdAsync(partida.Id, Arg.Any<CancellationToken>()).Returns(partida);
        _solicitacaoRepository.BuscarOutroAguardandoAsync(jogoId, jogadorRestante, Arg.Any<CancellationToken>())
            .Returns((SolicitacaoBusca?)null);

        var result = await _useCase.ExecuteAsync(partida.Id, jogadorQueDesiste);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(StatusPartida.Cancelada);
        await _eventBus.Received(1).PublishAsync(Arg.Is<PartidaCanceladaEvent>(e => e.JogadorQueDesistiuId == jogadorQueDesiste));

        // o jogador restante foi devolvido para uma nova SolicitacaoBusca(Aguardando)
        await _solicitacaoRepository.Received(1).AddAsync(
            Arg.Is<SolicitacaoBusca>(s => s.JogadorId == jogadorRestante && s.Status == StatusSolicitacao.Aguardando),
            Arg.Any<CancellationToken>());
    }
}
