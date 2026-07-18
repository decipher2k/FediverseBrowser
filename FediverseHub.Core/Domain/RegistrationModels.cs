namespace FediverseHub.Core.Domain;

public sealed record RegistrationAttempt(
    FediverseSourceType SourceType,
    string UserName,
    string Email,
    string? Password);

public sealed record RegistrationResult(
    FediverseSourceType SourceType,
    RegistrationState State,
    string? ExternalRegistrationUrl = null,
    string? Message = null,
    string? AccountHandle = null);

public enum RegistrationState
{
    Created,
    RequiresEmailVerification,
    RequiresCaptcha,
    Closed,
    ExternalFallback,
    Failed
}
