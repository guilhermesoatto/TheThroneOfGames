using FluentAssertions;
using GameStore.Catalogo.Application.EventHandlers;
using GameStore.Common.Events;

namespace GameStore.Catalogo.Tests;

public class EventHandlerTests
{
    [Fact]
    public async Task UsuarioAtivadoEventHandler_ValidEvent_ShouldNotThrow()
    {
        var ev = new UsuarioAtivadoEvent(Guid.NewGuid(), "test@example.com", "Test User");
        var handler = new UsuarioAtivadoEventHandler();

        var act = async () => await handler.HandleAsync(ev);

        await act.Should().NotThrowAsync();
    }
}
