namespace TowerFluffy.Domain.Match;

public readonly record struct DomainResult<T>(T? Value, DomainError? Error)
{
    public bool IsSuccess => Error is null;

    public static DomainResult<T> Success(T value) => new(Value: value, Error: null);

    public static DomainResult<T> Failure(string code, string message)
        => new(Value: default, Error: new DomainError(code, message));
}
