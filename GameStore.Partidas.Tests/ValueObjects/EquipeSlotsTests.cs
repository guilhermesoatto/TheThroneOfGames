using FluentAssertions;
using GameStore.Partidas.Domain.Shared;
using GameStore.Partidas.Domain.ValueObjects;

namespace GameStore.Partidas.Tests.ValueObjects;

public class EquipeSlotsTests
{
    [Fact]
    public void Vazia_ShouldHaveNoJogadores()
    {
        var equipe = EquipeSlots.Vazia();

        equipe.Jogadores.Should().BeEmpty();
    }

    [Fact]
    public void ComUmJogador_ShouldContainExactlyOnePlayer()
    {
        var jogadorId = Guid.NewGuid();

        var equipe = EquipeSlots.ComUmJogador(jogadorId);

        equipe.Jogadores.Should().ContainSingle().Which.Should().Be(jogadorId);
        equipe.Contem(jogadorId).Should().BeTrue();
    }

    [Fact]
    public void Adicionar_ShouldReturnNewInstance_WithoutMutatingOriginal()
    {
        var original = EquipeSlots.Vazia();
        var jogadorId = Guid.NewGuid();

        var result = original.Adicionar(jogadorId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Contem(jogadorId).Should().BeTrue();
        original.Jogadores.Should().BeEmpty(); // imutabilidade — original não muda
    }

    [Fact]
    public void Adicionar_WhenAlreadyAtLimit_ShouldReturnEquipeCheiaError()
    {
        var equipe = EquipeSlots.Vazia();
        for (var i = 0; i < EquipeSlots.LimiteMaximo; i++)
        {
            equipe = equipe.Adicionar(Guid.NewGuid()).Value;
        }

        var result = equipe.Adicionar(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<EquipeCheiaError>();
    }

    [Fact]
    public void Adicionar_SameJogadorTwice_ShouldBeIdempotent()
    {
        var jogadorId = Guid.NewGuid();
        var equipe = EquipeSlots.ComUmJogador(jogadorId);

        var result = equipe.Adicionar(jogadorId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Jogadores.Should().ContainSingle();
    }
}
