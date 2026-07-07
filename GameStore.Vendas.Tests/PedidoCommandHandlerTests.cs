using FluentAssertions;
using NSubstitute;
using GameStore.Vendas.Application.Commands;
using GameStore.Vendas.Application.Handlers;
using GameStore.Vendas.Domain.Entities;
using GameStore.Vendas.Domain.Repositories;
using GameStore.Vendas.Domain.EventSourcing;
using GameStore.Vendas.Domain.Shared;
using GameStore.Vendas.Domain.ValueObjects;

namespace GameStore.Vendas.Tests;

/// <summary>
/// Garante que cada transição de estado do Pedido registra o evento correspondente
/// no Event Store (esboço de Event Sourcing) além de persistir o snapshot.
/// </summary>
public class PedidoCommandHandlerTests
{
    private readonly IPedidoRepository _pedidoRepository;
    private readonly IEventStore _eventStore;

    public PedidoCommandHandlerTests()
    {
        _pedidoRepository = Substitute.For<IPedidoRepository>();
        _eventStore = Substitute.For<IEventStore>();
    }

    [Fact]
    public async Task CriarPedidoCommandHandler_Success_ShouldAppendPedidoCriadoEvent()
    {
        var handler = new CriarPedidoCommandHandler(_pedidoRepository, _eventStore);
        var command = new CriarPedidoCommand(Guid.NewGuid());

        var result = await handler.HandleAsync(command);

        result.Success.Should().BeTrue();
        await _pedidoRepository.Received(1).AddAsync(Arg.Any<Pedido>());
        await _eventStore.Received(1).AppendAsync(
            Arg.Is<DomainEvent>(e => e.EventType == "PedidoCriado"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FinalizarPedidoCommandHandler_Success_ShouldAppendPedidoFinalizadoEvent()
    {
        var pedido = new Pedido(Guid.NewGuid());
        pedido.AdicionarItem(Guid.NewGuid(), "Elden Ring", new Money(59.99m, "BRL"));
        _pedidoRepository.GetByIdAsync(pedido.Id).Returns(pedido);

        var handler = new FinalizarPedidoCommandHandler(_pedidoRepository, _eventStore);
        var command = new FinalizarPedidoCommand(pedido.Id, "CreditCard");

        var result = await handler.HandleAsync(command);

        result.Success.Should().BeTrue();
        await _eventStore.Received(1).AppendAsync(
            Arg.Is<DomainEvent>(e => e.EventType == "PedidoFinalizado" && e.AggregateId == pedido.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FinalizarPedidoCommandHandler_PedidoNaoEncontrado_ShouldNotAppendEvent()
    {
        _pedidoRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Pedido?)null);

        var handler = new FinalizarPedidoCommandHandler(_pedidoRepository, _eventStore);
        var command = new FinalizarPedidoCommand(Guid.NewGuid(), "CreditCard");

        var result = await handler.HandleAsync(command);

        result.Success.Should().BeFalse();
        await _eventStore.DidNotReceive().AppendAsync(Arg.Any<DomainEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelarPedidoCommandHandler_Success_ShouldAppendPedidoCanceladoEvent()
    {
        var pedido = new Pedido(Guid.NewGuid());
        pedido.AdicionarItem(Guid.NewGuid(), "Elden Ring", new Money(59.99m, "BRL"));
        _pedidoRepository.GetByIdAsync(pedido.Id).Returns(pedido);

        var handler = new CancelarPedidoCommandHandler(_pedidoRepository, _eventStore);
        var command = new CancelarPedidoCommand(pedido.Id, "Desistência do cliente");

        var result = await handler.HandleAsync(command);

        result.Success.Should().BeTrue();
        await _eventStore.Received(1).AppendAsync(
            Arg.Is<DomainEvent>(e => e.EventType == "PedidoCancelado" && e.AggregateId == pedido.Id),
            Arg.Any<CancellationToken>());
    }
}
