using FediverseHub.Core.Domain;

namespace FediverseHub.Core.Interfaces;

public interface IRssFeedRepository
{
    Task<IReadOnlyList<RssFeedDefinition>> GetFeedsAsync(CancellationToken cancellationToken);

    Task UpsertFeedAsync(RssFeedDefinition feed, CancellationToken cancellationToken);

    Task RemoveFeedAsync(string id, CancellationToken cancellationToken);
}
