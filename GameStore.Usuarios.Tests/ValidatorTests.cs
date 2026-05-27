using FluentAssertions;
using GameStore.Usuarios.Application.Commands;
using GameStore.Usuarios.Application.Validators;

namespace GameStore.Usuarios.Tests;

public class ValidatorTests
{
    [Fact]
    public void CreateUserCommandValidator_ValidCommand_ShouldPass()
    {
        var validator = new CreateUserCommandValidator();
        var command = new CreateUserCommand("John Doe", "john.doe@example.com", "StrongPassword123!", "User");

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void CreateUserCommandValidator_EmptyName_ShouldFail()
    {
        var validator = new CreateUserCommandValidator();
        var command = new CreateUserCommand("", "john.doe@example.com", "StrongPassword123!", "User");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void CreateUserCommandValidator_InvalidEmail_ShouldFail()
    {
        var validator = new CreateUserCommandValidator();
        var command = new CreateUserCommand("John", "not-an-email", "StrongPassword123!", "User");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }
}
