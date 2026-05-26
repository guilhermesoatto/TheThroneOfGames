using FluentAssertions;
using GameStore.Vendas.Domain.Entities;
using GameStore.Vendas.Domain.ValueObjects;

namespace GameStore.Vendas.Tests;

public class PedidoTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();

    [Fact]
    public void Pedido_Create_ShouldInitializeAsPendente()
    {
        // Act
        var pedido = new Pedido(UsuarioId);

        // Assert
        pedido.Id.Should().NotBe(Guid.Empty);
        pedido.UsuarioId.Should().Be(UsuarioId);
        pedido.Status.Should().Be("Pendente");
        pedido.Itens.Should().BeEmpty();
        pedido.ValorTotal.Amount.Should().Be(0);
    }

    [Fact]
    public void Pedido_AdicionarItem_ShouldAddItemAndRecalculateTotal()
    {
        // Arrange
        var pedido = new Pedido(UsuarioId);
        var jogoId = Guid.NewGuid();
        var preco = new Money(59.99m, "BRL");

        // Act
        pedido.AdicionarItem(jogoId, "Elden Ring", preco);

        // Assert
        pedido.Itens.Should().HaveCount(1);
        pedido.Itens.Should().ContainSingle(i => i.JogoId == jogoId);
        pedido.ValorTotal.Amount.Should().Be(59.99m);
    }

    [Fact]
    public void Pedido_AdicionarItemDuplicado_ShouldThrow()
    {
        // Arrange
        var pedido = new Pedido(UsuarioId);
        var jogoId = Guid.NewGuid();
        var preco = new Money(59.99m, "BRL");

        pedido.AdicionarItem(jogoId, "Elden Ring", preco);

        // Act
        var act = () => pedido.AdicionarItem(jogoId, "Elden Ring", preco);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*já foi adicionado*");
    }

    [Fact]
    public void Pedido_Finalizar_ShouldChangeStatusToFinalizado()
    {
        // Arrange
        var pedido = new Pedido(UsuarioId);
        pedido.AdicionarItem(Guid.NewGuid(), "Elden Ring", new Money(59.99m, "BRL"));

        // Act
        pedido.Finalizar("CreditCard");

        // Assert
        pedido.Status.Should().Be("Finalizado");
        pedido.MetodoPagamento.Should().Be("CreditCard");
        pedido.DataFinalizacao.Should().NotBeNull();
    }

    [Fact]
    public void Pedido_Finalizar_SemItens_ShouldThrow()
    {
        // Arrange
        var pedido = new Pedido(UsuarioId);

        // Act
        var act = () => pedido.Finalizar("CreditCard");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*sem itens*");
    }

    [Fact]
    public void Pedido_RemoverItem_ShouldRemoveAndRecalculateTotal()
    {
        // Arrange
        var pedido = new Pedido(UsuarioId);
        var jogoId = Guid.NewGuid();
        pedido.AdicionarItem(jogoId, "Elden Ring", new Money(59.99m, "BRL"));

        // Act
        pedido.RemoverItem(jogoId);

        // Assert
        pedido.Itens.Should().BeEmpty();
        pedido.ValorTotal.Amount.Should().Be(0);
    }
}


