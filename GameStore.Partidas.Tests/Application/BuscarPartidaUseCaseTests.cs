using FluentAssertions;
using GameStore.Common.Events;
using GameStore.Partidas.Application.Ports;
using GameStore.Partidas.Application.Services;
using GameStore.Partidas.Application.UseCases;
using GameStore.Partidas.Domain.Entities;
using GameStore.Partidas.Domain.Repositories;
using GameStore.Partidas.Domain.Shared;
using NSubstitute;

namespace GameStore.Partidas.Tests.Application;

public class BuscarPartidaUseCaseTests
{
    private readonly IUsuariosGateway _usuariosGateway;
    private readonly ISolicitacaoBuscaRepository _solicitacaoRepository;
    private readonly BuscarPartidaUseCase _useCase;

    public BuscarPartidaUseCaseTests()
    {
        _usuariosGateway = Substitute.For<IUsuariosGateway>();
        _solicitacaoRepository = Substitute.For<ISolicitacaoBuscaRepository>();
        var partidaRepository = Substitute.For<IPartidaRepository>();
        var eventBus = Substitute.For<IEventBus>();
        var motor = new MotorDePareamento(_solicitacaoRepository, partidaRepository, eventBus);

        _useCase = new BuscarPartidaUseCase(_usuariosGateway, _solicitacaoRepository, motor);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPlayerDoesNotOwnGame_ShouldReturnError_WithoutTouchingRepository()
    {
        var jogadorId = Guid.NewGuid();
        var jogoId = Guid.NewGuid();
        _usuariosGateway.PossuiJogoAsync(jogoId, "token", Arg.Any<CancellationToken>()).Returns(false);

        var result = await _useCase.ExecuteAsync(jogadorId, jogoId, "token");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<JogadorNaoPossuiJogoError>();
        await _solicitacaoRepository.DidNotReceive().AddAsync(Arg.Any<SolicitacaoBusca>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenPlayerAlreadyHasPendingRequest_ShouldReturnError()
    {
        var jogadorId = Guid.NewGuid();
        var jogoId = Guid.NewGuid();
        _usuariosGateway.PossuiJogoAsync(jogoId, "token", Arg.Any<CancellationToken>()).Returns(true);
        _solicitacaoRepository.GetAguardandoDoJogadorAsync(jogadorId, jogoId, Arg.Any<CancellationToken>())
            .Returns(new SolicitacaoBusca(jogadorId, jogoId));

        var result = await _useCase.ExecuteAsync(jogadorId, jogoId, "token");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<SolicitacaoJaExisteError>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenEligibleAndNoOneWaiting_ShouldReturnSolicitacao_WithoutPartida()
    {
        var jogadorId = Guid.NewGuid();
        var jogoId = Guid.NewGuid();
        _usuariosGateway.PossuiJogoAsync(jogoId, "token", Arg.Any<CancellationToken>()).Returns(true);
        _solicitacaoRepository.GetAguardandoDoJogadorAsync(jogadorId, jogoId, Arg.Any<CancellationToken>())
            .Returns((SolicitacaoBusca?)null);
        _solicitacaoRepository.BuscarOutroAguardandoAsync(jogoId, jogadorId, Arg.Any<CancellationToken>())
            .Returns((SolicitacaoBusca?)null);

        var result = await _useCase.ExecuteAsync(jogadorId, jogoId, "token");

        result.IsSuccess.Should().BeTrue();
        result.Value.Partida.Should().BeNull();
        result.Value.Solicitacao.JogadorId.Should().Be(jogadorId);
    }
}
