using FediverseHub.Core.Domain;
using FediverseHub.Core.Interfaces;

namespace FediverseHub.Core.Services;

public sealed class HashtagFollowService(
    ISettingsStore settingsStore,
    IEnumerable<IHashtagRemoteFollowClient> remoteClients) : IHashtagFollowService
{
    private readonly IReadOnlyList<IHashtagRemoteFollowClient> _remoteClients = remoteClients.ToArray();

    public async Task FollowHashtagsAsync(IEnumerable<string> hashtags, CancellationToken cancellationToken)
    {
        var normalized = HashtagNormalizer.NormalizeMany(hashtags);
        if (normalized.Count == 0)
        {
            return;
        }

        var settings = await settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        var merged = HashtagNormalizer.NormalizeMany(settings.FollowedHashtags.Concat(normalized));

        await settingsStore.SaveAsync(
            settings with { FollowedHashtags = merged },
            cancellationToken).ConfigureAwait(false);

        foreach (var client in _remoteClients)
        {
            try
            {
                if (!await client.SupportsHashtagFollowingAsync(cancellationToken).ConfigureAwait(false))
                {
                    continue;
                }

                foreach (var hashtag in normalized)
                {
                    await client.FollowHashtagAsync(hashtag, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                continue;
            }
        }
    }

    public async Task UnfollowHashtagAsync(string hashtag, CancellationToken cancellationToken)
    {
        var normalized = HashtagNormalizer.Normalize(hashtag);
        if (normalized.Length == 0)
        {
            return;
        }

        foreach (var client in _remoteClients)
        {
            if (await client.SupportsHashtagFollowingAsync(cancellationToken).ConfigureAwait(false))
            {
                await client.UnfollowHashtagAsync(normalized, cancellationToken).ConfigureAwait(false);
            }
        }

        var settings = await settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        var updated = settings.FollowedHashtags
            .Where(tag => !string.Equals(HashtagNormalizer.Normalize(tag), normalized, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        await settingsStore.SaveAsync(settings with { FollowedHashtags = updated }, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<string>> GetFollowedHashtagsAsync(CancellationToken cancellationToken)
    {
        var settings = await settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        return HashtagNormalizer.NormalizeMany(settings.FollowedHashtags);
    }
}
