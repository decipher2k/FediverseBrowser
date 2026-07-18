using FediverseHub.Core.Domain;

namespace FediverseHub.Core.Interfaces;

public interface IPostPublisher
{
    FediverseSourceType SourceType { get; }

    Task<PostValidationResult> ValidateAsync(
        ComposePostDraft draft,
        CancellationToken cancellationToken);

    Task<PostPublishResult> PublishAsync(
        ComposePostDraft draft,
        CancellationToken cancellationToken);
}
