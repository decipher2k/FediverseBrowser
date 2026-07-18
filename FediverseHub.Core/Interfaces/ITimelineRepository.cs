using FediverseHub.Core.Domain;

namespace FediverseHub.Core.Interfaces;

public interface ITimelineRepository
{
    Task SaveTimelineItemsAsync(
        IEnumerable<UnifiedTimelineItem> items,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<UnifiedTimelineItem>> GetCachedTimelineAsync(
        TimelineRequest request,
        CancellationToken cancellationToken);

    Task ClearAsync(CancellationToken cancellationToken);
}
