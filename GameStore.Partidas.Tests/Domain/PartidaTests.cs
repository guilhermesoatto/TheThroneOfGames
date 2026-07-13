using FluentAssertions;
using GameStore.Partidas.Domain.Entities;
using GameStore.Partidas.Domain.Shared;
using GameStore.Partidas.Domain.ValueObjects;

namespace GameStore.Partidas.Tests.Domain;

public class PartidaTests
{
    private static (Partida Partida, Guid JogoId, Guid JogadorA, Guid JogadorB) NovaPartida()
    {
        var jogoId = Guid.NewGuid();
        var jogadorA = Guid.NewGuid();
        var jogadorB = Guid.NewGuid();
        var (partida, _) = Partida.Formar(jogoId, jogadorA, jogadorB);
        return (partida, jogoId, jogadorA, jogadorB);
    }

    [Fact]
    public void Formar_ShouldCreateMatch_AguardandoConfirmacao_With1v1Teams()
    {
        var (partida, jogoId, jogadorA, jogadorB) = NovaPartida();

        partida.Status.Should().Be(StatusPartida.AguardandoConfirmacao);
        partida.JogoId.Should().Be(jogoId);
        partida.EquipeA.Jogadores.Should().ContainSingle().Which.Should().Be(jogadorA);
        partida.EquipeB.Jogadores.Should().ContainSingle().Which.Should().Be(jogadorB);
    }

    [Fact]
    public void Formar_ShouldReturnPartidaEncontradaEvent()
    {
        var jogoId = Guid.NewGuid();
        var jogadorA = Guid.NewGuid();
        var jogadorB = Guid.NewGuid();

        var (partida, evento) = Partida.Formar(jogoId, jogadorA, jogadorB);

        evento.PartidaId.Should().Be(partida.Id);
        evento.JogoId.Should().Be(jogoId);
        evento.JogadorAId.Should().Be(jogadorA);
        evento.JogadorBId.Should().Be(jogadorB);
    }

    [Fact]
    public void Confirmar_FirstPlayerOnly_ShouldStayAguardando_AndReturnNullEvent()
    {
        var (partida, _, jogadorA, _) = NovaPartida();

        var result = partida.Confirmar(jogadorA);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
        partida.Status.Should().Be(StatusPartida.AguardandoConfirmacao);
    }

    [Fact]
    public void Confirmar_BothPlayers_ShouldConfirmMatch_AndReturnEvent()
    {
        var (partida, jogoId, jogadorA, jogadorB) = NovaPartida();

        partida.Confirmar(jogadorA);
        var result = partida.Confirmar(jogadorB);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.PartidaId.Should().Be(partida.Id);
        result.Value.JogoId.Should().Be(jogoId);
        result.Value.JogadoresIds.Should().BeEquivalentTo(new[] { jogadorA, jogadorB });
        partida.Status.Should().Be(StatusPartida.Confirmada);
    }

    [Fact]
    public void Confirmar_PlayerNotInMatch_ShouldReturnError()
    {
        var (partida, _, _, _) = NovaPartida();
        var estranho = Guid.NewGuid();

        var result = partida.Confirmar(estranho);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<JogadorNaoPertenceAPartidaError>();
    }

    [Fact]
    public void Confirmar_AfterAlreadyConfirmed_ShouldReturnError()
    {
        var (partida, _, jogadorA, jogadorB) = NovaPartida();
        partida.Confirmar(jogadorA);
        partida.Confirmar(jogadorB); // partida já Confirmada

        var result = partida.Confirmar(jogadorA);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<PartidaNaoEstaAguardandoConfirmacaoError>();
    }

    [Fact]
    public void Desistir_ShouldCancelMatch_AndReturnRemainingPlayer()
    {
        var (partida, jogoId, jogadorA, jogadorB) = NovaPartida();

        var result = partida.Desistir(jogadorA);

        result.IsSuccess.Should().BeTrue();
        partida.Status.Should().Be(StatusPartida.Cancelada);
        result.Value.Evento.JogadorQueDesistiuId.Should().Be(jogadorA);
        result.Value.Evento.JogoId.Should().Be(jogoId);
        result.Value.JogadorRestanteId.Should().Be(jogadorB);
    }

    [Fact]
    public void Desistir_PlayerNotInMatch_ShouldReturnError()
    {
        var (partida, _, _, _) = NovaPartida();
        var estranho = Guid.NewGuid();

        var result = partida.Desistir(estranho);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<JogadorNaoPertenceAPartidaError>();
    }

    [Fact]
    public void Desistir_AfterAlreadyCancelled_ShouldReturnError()
    {
        var (partida, _, jogadorA, _) = NovaPartida();
        partida.Desistir(jogadorA);

        var result = partida.Desistir(jogadorA);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<PartidaNaoEstaAguardandoConfirmacaoError>();
    }
}
