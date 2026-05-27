using FluentAssertions;
using NSubstitute;
using GameStore.Usuarios.Application.Commands;
using GameStore.Usuarios.Application.Handlers;
using GameStore.Usuarios.Domain.Entities;
using GameStore.Usuarios.Domain.Interfaces;
using GameStore.Common.Events;

namespace GameStore.Usuarios.Tests;

public class CommandHandlerTests
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IEventBus _eventBus;

    public CommandHandlerTests()
    {
        _usuarioRepository = Substitute.For<IUsuarioRepository>();
        _eventBus = Substitute.For<IEventBus>();
    }

    [Fact]
    public async Task ActivateUserCommandHandler_ValidToken_ShouldActivateUser()
    {
        var token = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        var user = new Usuario(userId, "Test User", "test@example.com", "hashed", "User", false, token);
        var command = new ActivateUserCommand(token);
        var handler = new ActivateUserCommandHandler(_usuarioRepository, _eventBus);

        _usuarioRepository.GetByActivationTokenAsync(token).Returns(user);

        var result = await handler.HandleAsync(command);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Usuário ativado com sucesso");
        result.EntityId.Should().Be(userId);
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ActivateUserCommandHandler_InvalidToken_ShouldReturnError()
    {
        var token = "invalid-token";
        var command = new ActivateUserCommand(token);
        var handler = new ActivateUserCommandHandler(_usuarioRepository, _eventBus);

        _usuarioRepository.GetByActivationTokenAsync(token).Returns((Usuario?)null);

        var result = await handler.HandleAsync(command);

        result.Success.Should().BeFalse();
        await _usuarioRepository.DidNotReceive().UpdateAsync(Arg.Any<Usuario>());
    }
}
