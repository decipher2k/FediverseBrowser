using FediverseHub.Core.Domain;

namespace FediverseHub.Core.Interfaces;

public interface IRssFeedParser
{
    Task<IReadOnlyList<UnifiedTimelineItem>> ParseAsync(
        RssFeedDefinition feed,
        Stream stream,
        CancellationToken cancellationToken);
}
