using FluentAssertions;
using GameStore.Partidas.Domain.Entities;
using GameStore.Partidas.Domain.ValueObjects;

namespace GameStore.Partidas.Tests.Domain;

public class SolicitacaoBuscaTests
{
    [Fact]
    public void Constructor_ShouldStartAsAguardando_WithoutPartida()
    {
        var jogadorId = Guid.NewGuid();
        var jogoId = Guid.NewGuid();

        var solicitacao = new SolicitacaoBusca(jogadorId, jogoId);

        solicitacao.Status.Should().Be(StatusSolicitacao.Aguardando);
        solicitacao.JogadorId.Should().Be(jogadorId);
        solicitacao.JogoId.Should().Be(jogoId);
        solicitacao.PartidaId.Should().BeNull();
    }

    [Fact]
    public void Parear_ShouldSetStatusPareada_AndPartidaId()
    {
        var solicitacao = new SolicitacaoBusca(Guid.NewGuid(), Guid.NewGuid());
        var partidaId = Guid.NewGuid();

        solicitacao.Parear(partidaId);

        solicitacao.Status.Should().Be(StatusSolicitacao.Pareada);
        solicitacao.PartidaId.Should().Be(partidaId);
    }

    [Fact]
    public void Cancelar_ShouldSetStatusCancelada()
    {
        var solicitacao = new SolicitacaoBusca(Guid.NewGuid(), Guid.NewGuid());

        solicitacao.Cancelar();

        solicitacao.Status.Should().Be(StatusSolicitacao.Cancelada);
    }

    [Fact]
    public void Constructor_WithEmptyJogadorId_ShouldThrow()
    {
        var act = () => new SolicitacaoBusca(Guid.Empty, Guid.NewGuid());

        act.Should().Throw<ArgumentException>();
    }
}
