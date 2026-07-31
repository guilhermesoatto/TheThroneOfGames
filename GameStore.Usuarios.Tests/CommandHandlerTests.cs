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

    // Cobertura do gap fechado em docs/ai/tasks/fix-admin-user-management-gap.md — o
    // CreateUserCommandHandler criava usuários com passwordHash vazio (nunca conseguiriam
    // logar); o teste abaixo trava esse comportamento.
    [Fact]
    public async Task CreateUserCommandHandler_ValidCommand_ShouldHashPasswordAndStartInactive()
    {
        var command = new CreateUserCommand("Admin Created", "created@example.com", "Password@123", "User");
        var handler = new CreateUserCommandHandler(_usuarioRepository);

        _usuarioRepository.GetByEmailAsync(command.Email).Returns((Usuario?)null);

        Usuario? addedUser = null;
        await _usuarioRepository.AddAsync(Arg.Do<Usuario>(u => addedUser = u));

        var result = await handler.HandleAsync(command);

        result.Success.Should().BeTrue();
        addedUser.Should().NotBeNull();
        addedUser!.PasswordHash.Should().NotBeNullOrEmpty();
        addedUser.PasswordHash.Should().NotBe(command.Password);
        GameStore.Usuarios.Application.Services.UsuarioService.VerifyPassword(addedUser.PasswordHash, command.Password).Should().BeTrue();
        addedUser.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task CreateUserCommandHandler_DuplicateEmail_ShouldReturnError()
    {
        var existing = new Usuario(Guid.NewGuid(), "Existing", "dup@example.com", "hash", "User", true, "token");
        var command = new CreateUserCommand("New Name", "dup@example.com", "Password@123", "User");
        var handler = new CreateUserCommandHandler(_usuarioRepository);

        _usuarioRepository.GetByEmailAsync(command.Email).Returns(existing);

        var result = await handler.HandleAsync(command);

        result.Success.Should().BeFalse();
        await _usuarioRepository.DidNotReceive().AddAsync(Arg.Any<Usuario>());
    }

    [Fact]
    public async Task ChangeUserRoleCommandHandler_ExistingUser_ShouldUpdateRole()
    {
        var user = new Usuario(Guid.NewGuid(), "Test User", "role@example.com", "hash", "User", true, "token");
        var command = new ChangeUserRoleCommand("role@example.com", "Admin");
        var handler = new ChangeUserRoleCommandHandler(_usuarioRepository);

        _usuarioRepository.GetByEmailAsync(command.Email).Returns(user);

        var result = await handler.HandleAsync(command);

        result.Success.Should().BeTrue();
        user.Role.Should().Be("Admin");
        await _usuarioRepository.Received(1).UpdateAsync(user);
    }

    [Fact]
    public async Task ChangeUserRoleCommandHandler_UserNotFound_ShouldReturnError()
    {
        var command = new ChangeUserRoleCommand("missing@example.com", "Admin");
        var handler = new ChangeUserRoleCommandHandler(_usuarioRepository);

        _usuarioRepository.GetByEmailAsync(command.Email).Returns((Usuario?)null);

        var result = await handler.HandleAsync(command);

        result.Success.Should().BeFalse();
    }
}
