using FediverseHub.Core.Domain;

namespace FediverseHub.Core.Interfaces;

public interface IRegistrationClient
{
    FediverseSourceType SourceType { get; }

    Task<RegistrationResult> TryRegisterAsync(
        RegistrationAttempt attempt,
        CancellationToken cancellationToken);
}
