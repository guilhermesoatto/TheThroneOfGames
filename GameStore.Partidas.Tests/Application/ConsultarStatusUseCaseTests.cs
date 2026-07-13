using FluentAssertions;
using GameStore.Partidas.Application.UseCases;
using GameStore.Partidas.Domain.Entities;
using GameStore.Partidas.Domain.Repositories;
using GameStore.Partidas.Domain.Shared;
using NSubstitute;

namespace GameStore.Partidas.Tests.Application;

public class ConsultarStatusUseCaseTests
{
    private readonly ISolicitacaoBuscaRepository _solicitacaoRepository;
    private readonly IPartidaRepository _partidaRepository;
    private readonly ConsultarStatusUseCase _useCase;

    public ConsultarStatusUseCaseTests()
    {
        _solicitacaoRepository = Substitute.For<ISolicitacaoBuscaRepository>();
        _partidaRepository = Substitute.For<IPartidaRepository>();
        _useCase = new ConsultarStatusUseCase(_solicitacaoRepository, _partidaRepository);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSolicitacaoNotFound_ShouldReturnError()
    {
        var id = Guid.NewGuid();
        _solicitacaoRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((SolicitacaoBusca?)null);

        var result = await _useCase.ExecuteAsync(id);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<SolicitacaoNaoEncontradaError>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenStillWaiting_ShouldReturnSolicitacao_WithoutPartida()
    {
        var solicitacao = new SolicitacaoBusca(Guid.NewGuid(), Guid.NewGuid());
        _solicitacaoRepository.GetByIdAsync(solicitacao.Id, Arg.Any<CancellationToken>()).Returns(solicitacao);

        var result = await _useCase.ExecuteAsync(solicitacao.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Partida.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WhenPaired_ShouldReturnSolicitacaoAndPartida()
    {
        var (partida, _) = Partida.Formar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var solicitacao = new SolicitacaoBusca(partida.EquipeA.Jogadores[0], partida.JogoId);
        solicitacao.Parear(partida.Id);

        _solicitacaoRepository.GetByIdAsync(solicitacao.Id, Arg.Any<CancellationToken>()).Returns(solicitacao);
        _partidaRepository.GetByIdAsync(partida.Id, Arg.Any<CancellationToken>()).Returns(partida);

        var result = await _useCase.ExecuteAsync(solicitacao.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Partida.Should().Be(partida);
    }
}
