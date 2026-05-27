namespace GameStore.Catalogo.Domain.Shared;

public abstract record DomainError(string Code, string Message);

public readonly struct Result<T>
{
    private readonly T? _value;
    private readonly DomainError? _error;

    private Result(T value)
    {
        _value = value;
        _error = null;
        IsSuccess = true;
    }

    private Result(DomainError error)
    {
        _value = default;
        _error = error;
        IsSuccess = false;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failed Result.");

    public DomainError Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot access Error on a successful Result.");

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(DomainError error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(DomainError error) => Failure(error);

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<DomainError, TOut> onFailure)
        => IsSuccess ? onSuccess(_value!) : onFailure(_error!);
}
