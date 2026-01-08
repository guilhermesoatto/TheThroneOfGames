using NUnit.Framework;
using System;
using GameStore.Vendas.Domain.Entities;
using GameStore.Vendas.Domain.ValueObjects;
using GameStore.Vendas.Application.DTOs;
using GameStore.Vendas.Application.Mappers;

namespace GameStore.Vendas.Tests
{
    /// <summary>
    /// Testes de Unidade para Entidades de Domínio - Vendas
    /// </summary>
    [TestFixture]
    public class PedidoDomainTests
    {
        [Test]
        public void Pedido_Criar_DeveInicializarCorretamente()
        {
            // Arrange
            var usuarioId = Guid.NewGuid();
            var dataPedido = DateTime.UtcNow;

            // Act
            var pedido = new Pedido(usuarioId, dataPedido);

            // Assert
            Assert.That(pedido.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(pedido.UsuarioId, Is.EqualTo(usuarioId));
            Assert.That(pedido.Data, Is.EqualTo(dataPedido));
            Assert.That(pedido.ValorTotal, Is.EqualTo(0));
            Assert.That(pedido.Status, Is.EqualTo("Aguardando"));
            Assert.That(pedido.Itens, Is.Empty);
        }

        [Test]
        public void Pedido_AdicionarItem_DeveAtualizarValorTotal()
        {
            // Arrange
            var pedido = new Pedido(Guid.NewGuid(), DateTime.UtcNow);
            var item = new ItemPedido(Guid.NewGuid(), "God of War Ragnarök", Money.FromDecimal(249.90m), 1);

            // Act
            pedido.AdicionarItem(item);

            // Assert
            Assert.That(pedido.Itens.Count, Is.EqualTo(1));
            Assert.That(pedido.ValorTotal, Is.EqualTo(249.90m));
        }

        [Test]
        public void Pedido_AdicionarMultiplosItens_DeveCalcularValorTotalCorreto()
        {
            // Arrange
            var pedido = new Pedido(Guid.NewGuid(), DateTime.UtcNow);
            var item1 = new ItemPedido(Guid.NewGuid(), "The Last of Us Part II", Money.FromDecimal(149.90m), 1);
            var item2 = new ItemPedido(Guid.NewGuid(), "Horizon Forbidden West", Money.FromDecimal(199.90m), 2);
            var item3 = new ItemPedido(Guid.NewGuid(), "Spider-Man Miles Morales", Money.FromDecimal(129.90m), 1);

            // Act
            pedido.AdicionarItem(item1);
            pedido.AdicionarItem(item2);
            pedido.AdicionarItem(item3);

            // Assert
            Assert.That(pedido.Itens.Count, Is.EqualTo(3));
            // 149.90 + (199.90 * 2) + 129.90 = 679.60
            Assert.That(pedido.ValorTotal, Is.EqualTo(679.60m));
        }

        [Test]
        public void Pedido_Finalizar_DeveAlterarStatus()
        {
            // Arrange
            var pedido = new Pedido(Guid.NewGuid(), DateTime.UtcNow);
            var item = new ItemPedido(Guid.NewGuid(), "Elden Ring", Money.FromDecimal(199.90m), 1);
            pedido.AdicionarItem(item);

            // Act
            pedido.Finalizar();

            // Assert
            Assert.That(pedido.Status, Is.EqualTo("Finalizado"));
        }

        [Test]
        public void Pedido_Cancelar_DeveAlterarStatus()
        {
            // Arrange
            var pedido = new Pedido(Guid.NewGuid(), DateTime.UtcNow);

            // Act
            pedido.Cancelar();

            // Assert
            Assert.That(pedido.Status, Is.EqualTo("Cancelado"));
        }

        [Test]
        public void Pedido_RemoverItem_DeveAtualizarValorTotal()
        {
            // Arrange
            var pedido = new Pedido(Guid.NewGuid(), DateTime.UtcNow);
            var item1 = new ItemPedido(Guid.NewGuid(), "Jogo 1", Money.FromDecimal(100m), 1);
            var item2 = new ItemPedido(Guid.NewGuid(), "Jogo 2", Money.FromDecimal(50m), 1);
            pedido.AdicionarItem(item1);
            pedido.AdicionarItem(item2);

            // Act
            pedido.RemoverItem(item1.Id);

            // Assert
            Assert.That(pedido.Itens.Count, Is.EqualTo(1));
            Assert.That(pedido.ValorTotal, Is.EqualTo(50m));
        }
    }

    [TestFixture]
    public class ItemPedidoTests
    {
        [Test]
        public void ItemPedido_Criar_DeveInicializarCorretamente()
        {
            // Arrange
            var jogoId = Guid.NewGuid();
            var nomeJogo = "Cyberpunk 2077";
            var preco = Money.FromDecimal(179.90m);
            var quantidade = 2;

            // Act
            var item = new ItemPedido(jogoId, nomeJogo, preco, quantidade);

            // Assert
            Assert.That(item.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(item.JogoId, Is.EqualTo(jogoId));
            Assert.That(item.NomeJogo, Is.EqualTo(nomeJogo));
            Assert.That(item.PrecoUnitario.Amount, Is.EqualTo(179.90m));
            Assert.That(item.Quantidade, Is.EqualTo(2));
        }

        [Test]
        public void ItemPedido_CalcularSubtotal_DeveRetornarValorCorreto()
        {
            // Arrange
            var item = new ItemPedido(Guid.NewGuid(), "Jogo Teste", Money.FromDecimal(99.99m), 3);

            // Act
            var subtotal = item.CalcularSubtotal();

            // Assert
            Assert.That(subtotal, Is.EqualTo(299.97m));
        }

        [Test]
        public void ItemPedido_AlterarQuantidade_DeveAtualizar()
        {
            // Arrange
            var item = new ItemPedido(Guid.NewGuid(), "Jogo Teste", Money.FromDecimal(50m), 1);

            // Act
            item.AlterarQuantidade(5);

            // Assert
            Assert.That(item.Quantidade, Is.EqualTo(5));
        }
    }

    [TestFixture]
    public class MoneyValueObjectTests
    {
        [Test]
        public void Money_FromDecimal_DeveCriarCorretamente()
        {
            // Arrange & Act
            var money = Money.FromDecimal(99.90m);

            // Assert
            Assert.That(money.Amount, Is.EqualTo(99.90m));
        }

        [Test]
        public void Money_Adicao_DeveCalcularCorreto()
        {
            // Arrange
            var money1 = Money.FromDecimal(100.00m);
            var money2 = Money.FromDecimal(50.50m);

            // Act
            var resultado = money1 + money2;

            // Assert
            Assert.That(resultado.Amount, Is.EqualTo(150.50m));
        }

        [Test]
        public void Money_Subtracao_DeveCalcularCorreto()
        {
            // Arrange
            var money1 = Money.FromDecimal(200.00m);
            var money2 = Money.FromDecimal(75.50m);

            // Act
            var resultado = money1 - money2;

            // Assert
            Assert.That(resultado.Amount, Is.EqualTo(124.50m));
        }

        [Test]
        public void Money_Multiplicacao_DeveCalcularCorreto()
        {
            // Arrange
            var money = Money.FromDecimal(49.90m);

            // Act
            var resultado = money * 3;

            // Assert
            Assert.That(resultado.Amount, Is.EqualTo(149.70m));
        }

        [Test]
        public void Money_Comparacao_DeveRetornarCorreto()
        {
            // Arrange
            var money1 = Money.FromDecimal(100m);
            var money2 = Money.FromDecimal(200m);
            var money3 = Money.FromDecimal(100m);

            // Act & Assert
            Assert.That(money1 < money2, Is.True);
            Assert.That(money2 > money1, Is.True);
            Assert.That(money1 == money3, Is.True);
        }

        [Test]
        public void Money_Zero_DeveRetornarZero()
        {
            // Act
            var zero = Money.Zero;

            // Assert
            Assert.That(zero.Amount, Is.EqualTo(0));
        }
    }

    [TestFixture]
    public class PedidoMapperTests
    {
        [Test]
        public void PedidoMapper_ToDto_DeveConverterCorretamente()
        {
            // Arrange
            var usuarioId = Guid.NewGuid();
            var pedido = new Pedido(usuarioId, DateTime.UtcNow);
            var item = new ItemPedido(Guid.NewGuid(), "Test Game", Money.FromDecimal(99.90m), 2);
            pedido.AdicionarItem(item);
            pedido.Finalizar();

            // Act
            var dto = PedidoMapper.ToDto(pedido);

            // Assert
            Assert.That(dto, Is.Not.Null);
            Assert.That(dto.Id, Is.EqualTo(pedido.Id));
            Assert.That(dto.UsuarioId, Is.EqualTo(usuarioId));
            Assert.That(dto.ValorTotal, Is.EqualTo(pedido.ValorTotal));
            Assert.That(dto.Status, Is.EqualTo("Finalizado"));
            Assert.That(dto.Itens.Count, Is.EqualTo(1));
        }

        [Test]
        public void PedidoMapper_ToDto_DeveTratarPedidoVazio()
        {
            // Arrange
            var pedido = new Pedido(Guid.NewGuid(), DateTime.UtcNow);

            // Act
            var dto = PedidoMapper.ToDto(pedido);

            // Assert
            Assert.That(dto, Is.Not.Null);
            Assert.That(dto.ValorTotal, Is.EqualTo(0));
            Assert.That(dto.Status, Is.EqualTo("Aguardando"));
            Assert.That(dto.Itens, Is.Empty);
        }
    }

    [TestFixture]
    public class PedidoDtoTests
    {
        [Test]
        public void PedidoDto_DeveSerInstanciavel()
        {
            // Act
            var dto = new PedidoDto();

            // Assert
            Assert.That(dto, Is.Not.Null);
        }

        [Test]
        public void ItemPedidoDto_DeveManterDados()
        {
            // Arrange
            var jogoId = Guid.NewGuid();

            // Act
            var dto = new ItemPedidoDto
            {
                JogoId = jogoId,
                NomeJogo = "Final Fantasy XVI",
                PrecoUnitario = 299.90m,
                Quantidade = 1
            };

            // Assert
            Assert.That(dto.JogoId, Is.EqualTo(jogoId));
            Assert.That(dto.NomeJogo, Is.EqualTo("Final Fantasy XVI"));
            Assert.That(dto.PrecoUnitario, Is.EqualTo(299.90m));
            Assert.That(dto.Quantidade, Is.EqualTo(1));
        }
    }
}
