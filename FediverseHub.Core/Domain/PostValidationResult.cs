namespace FediverseHub.Core.Domain;

public sealed record PostValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static PostValidationResult Success { get; } = new(true, Array.Empty<string>());

    public static PostValidationResult Failure(params string[] errors) => new(false, errors);
}
