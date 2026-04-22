namespace TowerFluffy.Application.Game;

public readonly record struct CommandResult(bool IsSuccess, string? ErrorMessage)
{
    public static CommandResult Success() => new(true, ErrorMessage: null);

    public static CommandResult Failure(string message) => new(false, message);
}
