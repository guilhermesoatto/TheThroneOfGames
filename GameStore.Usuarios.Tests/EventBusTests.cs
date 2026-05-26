using FluentAssertions;
using GameStore.Common.Events;
using GameStore.Common.Messaging;

namespace GameStore.Usuarios.Tests;

public class EventBusTests
{
    private readonly IEventBus _eventBus = new SimpleEventBus();

    [Fact]
    public async Task EventBus_PublishAsync_Calls_RegisteredHandlers()
    {
        bool handlerCalled = false;
        var handler = new TestEventHandler(() => handlerCalled = true);
        var ev = new UsuarioAtivadoEvent(Guid.NewGuid(), "test@example.com", "Test User");

        _eventBus.Subscribe(handler);
        await _eventBus.PublishAsync(ev);

        handlerCalled.Should().BeTrue();
    }

    [Fact]
    public async Task EventBus_PublishAsync_Calls_Multiple_Handlers()
    {
        var calls = new List<string>();
        _eventBus.Subscribe(new TestEventHandler(() => calls.Add("H1")));
        _eventBus.Subscribe(new TestEventHandler(() => calls.Add("H2")));
        var ev = new UsuarioAtivadoEvent(Guid.NewGuid(), "test@example.com", "Test");

        await _eventBus.PublishAsync(ev);

        calls.Should().HaveCount(2).And.Contain("H1").And.Contain("H2");
    }

    [Fact]
    public void EventBus_GetHandlerCount_Returns_Correct_Count()
    {
        _eventBus.Subscribe(new TestEventHandler(() => { }));
        _eventBus.Subscribe(new TestEventHandler(() => { }));

        _eventBus.GetHandlerCount<UsuarioAtivadoEvent>().Should().Be(2);
    }

    [Fact]
    public async Task EventBus_Unsubscribe_Removes_Handler()
    {
        bool called = false;
        var handler = new TestEventHandler(() => called = true);
        var ev = new UsuarioAtivadoEvent(Guid.NewGuid(), "test@example.com", "Test");

        _eventBus.Subscribe(handler);
        _eventBus.Unsubscribe(handler);
        await _eventBus.PublishAsync(ev);

        called.Should().BeFalse();
    }
}

internal sealed class TestEventHandler : IEventHandler<UsuarioAtivadoEvent>
{
    private readonly Action _onHandle;
    public TestEventHandler(Action onHandle) => _onHandle = onHandle;
    public Task HandleAsync(UsuarioAtivadoEvent domainEvent) { _onHandle(); return Task.CompletedTask; }
}
