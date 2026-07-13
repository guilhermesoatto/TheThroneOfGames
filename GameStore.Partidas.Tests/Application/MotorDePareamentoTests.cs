using FluentAssertions;
using GameStore.Common.Events;
using GameStore.Partidas.Application.Services;
using GameStore.Partidas.Domain.Entities;
using GameStore.Partidas.Domain.Repositories;
using NSubstitute;

namespace GameStore.Partidas.Tests.Application;

public class MotorDePareamentoTests
{
    private readonly ISolicitacaoBuscaRepository _solicitacaoRepository;
    private readonly IPartidaRepository _partidaRepository;
    private readonly IEventBus _eventBus;
    private readonly MotorDePareamento _motor;

    public MotorDePareamentoTests()
    {
        _solicitacaoRepository = Substitute.For<ISolicitacaoBuscaRepository>();
        _partidaRepository = Substitute.For<IPartidaRepository>();
        _eventBus = Substitute.For<IEventBus>();
        _motor = new MotorDePareamento(_solicitacaoRepository, _partidaRepository, _eventBus);
    }

    [Fact]
    public async Task TentarParear_WhenNoOneWaiting_ShouldCreateSolicitacaoAguardando_AndReturnNoPartida()
    {
        var jogadorId = Guid.NewGuid();
        var jogoId = Guid.NewGuid();
        _solicitacaoRepository.BuscarOutroAguardandoAsync(jogoId, jogadorId, Arg.Any<CancellationToken>())
            .Returns((SolicitacaoBusca?)null);

        var (solicitacao, partida) = await _motor.TentarParearAsync(jogadorId, jogoId);

        partida.Should().BeNull();
        solicitacao.JogadorId.Should().Be(jogadorId);
        await _solicitacaoRepository.Received(1).AddAsync(Arg.Is<SolicitacaoBusca>(s => s.JogadorId == jogadorId), Arg.Any<CancellationToken>());
        await _partidaRepository.DidNotReceive().AddAsync(Arg.Any<Partida>(), Arg.Any<CancellationToken>());
        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<PartidaEncontradaEvent>());
    }

    [Fact]
    public async Task TentarParear_WhenSomeoneWaiting_ShouldFormPartida_AndPublishEvent()
    {
        var jogoId = Guid.NewGuid();
        var jogadorA = Guid.NewGuid();
        var jogadorB = Guid.NewGuid();
        var outraSolicitacao = new SolicitacaoBusca(jogadorA, jogoId);

        _solicitacaoRepository.BuscarOutroAguardandoAsync(jogoId, jogadorB, Arg.Any<CancellationToken>())
            .Returns(outraSolicitacao);

        var (minhaSolicitacao, partida) = await _motor.TentarParearAsync(jogadorB, jogoId);

        partida.Should().NotBeNull();
        partida!.EquipeA.Contem(jogadorA).Should().BeTrue();
        partida.EquipeB.Contem(jogadorB).Should().BeTrue();
        minhaSolicitacao.PartidaId.Should().Be(partida.Id);
        outraSolicitacao.PartidaId.Should().Be(partida.Id);

        await _partidaRepository.Received(1).AddAsync(Arg.Any<Partida>(), Arg.Any<CancellationToken>());
        await _solicitacaoRepository.Received(1).UpdateAsync(outraSolicitacao, Arg.Any<CancellationToken>());
        await _solicitacaoRepository.Received(1).AddAsync(minhaSolicitacao, Arg.Any<CancellationToken>());
        await _eventBus.Received(1).PublishAsync(Arg.Is<PartidaEncontradaEvent>(e =>
            e.JogadorAId == jogadorA && e.JogadorBId == jogadorB && e.JogoId == jogoId));
    }
}
