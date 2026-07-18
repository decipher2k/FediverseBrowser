using FediverseHub.Core.Domain;

namespace FediverseHub.Core.Interfaces;

public interface IFediverseSourceClient
{
    FediverseSourceType SourceType { get; }

    Task<AuthStatus> GetAuthStatusAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<UnifiedTimelineItem>> GetTimelineAsync(
        TimelineRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<UnifiedTimelineItem>> GetHashtagTimelineAsync(
        string hashtag,
        TimelineRequest request,
        CancellationToken cancellationToken);

    Task<SourceCapabilitySet> GetCapabilitiesAsync(CancellationToken cancellationToken);
}
