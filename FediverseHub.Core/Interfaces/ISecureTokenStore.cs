using FediverseHub.Core.Domain;

namespace FediverseHub.Core.Interfaces;

public interface ISecureTokenStore
{
    Task SaveTokenAsync(
        FediverseSourceType sourceType,
        string accountId,
        string accessToken,
        CancellationToken cancellationToken);

    Task<string?> GetTokenAsync(
        FediverseSourceType sourceType,
        string accountId,
        CancellationToken cancellationToken);

    Task DeleteTokenAsync(
        FediverseSourceType sourceType,
        string accountId,
        CancellationToken cancellationToken);

    Task DeleteAllAsync(CancellationToken cancellationToken);
}
