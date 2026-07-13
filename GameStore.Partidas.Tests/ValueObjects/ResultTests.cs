using FluentAssertions;
using GameStore.Partidas.Domain.Shared;

namespace GameStore.Partidas.Tests.ValueObjects;

public class ResultTests
{
    [Fact]
    public void Success_ShouldExposeValue_AndNotThrow()
    {
        var result = Result<int>.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Failure_ShouldExposeError_AndThrowOnValueAccess()
    {
        var error = new PartidaNaoEncontradaError(Guid.NewGuid());

        var result = Result<int>.Failure(error);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
        var act = () => result.Value;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ImplicitOperators_ShouldConvertValueAndError()
    {
        Result<int> fromValue = 10;
        Result<int> fromError = new PartidaNaoEncontradaError(Guid.NewGuid());

        fromValue.IsSuccess.Should().BeTrue();
        fromError.IsFailure.Should().BeTrue();
    }
}
