namespace FediverseHub.Core.Interfaces;

public interface IHashtagFollowService
{
    Task FollowHashtagsAsync(IEnumerable<string> hashtags, CancellationToken cancellationToken);

    Task UnfollowHashtagAsync(string hashtag, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetFollowedHashtagsAsync(CancellationToken cancellationToken);
}
