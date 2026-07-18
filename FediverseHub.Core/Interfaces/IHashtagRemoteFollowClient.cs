using FediverseHub.Core.Domain;

namespace FediverseHub.Core.Interfaces;

public interface IHashtagRemoteFollowClient
{
    FediverseSourceType SourceType { get; }

    Task<bool> SupportsHashtagFollowingAsync(CancellationToken cancellationToken);

    Task FollowHashtagAsync(string hashtag, CancellationToken cancellationToken);

    Task UnfollowHashtagAsync(string hashtag, CancellationToken cancellationToken);
}
