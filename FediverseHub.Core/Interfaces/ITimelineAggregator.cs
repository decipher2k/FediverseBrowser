using FediverseHub.Core.Domain;

namespace FediverseHub.Core.Interfaces;

public interface ITimelineAggregator
{
    Task<IReadOnlyList<UnifiedTimelineItem>> GetUnifiedTimelineAsync(
        TimelineRequest request,
        CancellationToken cancellationToken);
}
