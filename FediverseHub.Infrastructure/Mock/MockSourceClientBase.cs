using FediverseHub.Core.Domain;
using FediverseHub.Core.Interfaces;
using FediverseHub.Core.Services;

namespace FediverseHub.Infrastructure.Mock;

public abstract class MockSourceClientBase :
    IFediverseSourceClient,
    IPostPublisher,
    IRegistrationClient,
    IHashtagRemoteFollowClient
{
    private readonly IReadOnlyList<UnifiedTimelineItem> _items;
    private readonly SourceCapabilitySet _capabilities;
    private readonly ComposePostValidator _validator = new();
    private readonly HashSet<string> _followedTags = new(StringComparer.OrdinalIgnoreCase);

    protected MockSourceClientBase(
        FediverseSourceType sourceType,
        string instance,
        SourceCapabilitySet capabilities,
        IReadOnlyList<UnifiedTimelineItem> items)
    {
        SourceType = sourceType;
        Instance = instance;
        _capabilities = capabilities;
        _items = items;
    }

    public FediverseSourceType SourceType { get; }

    protected string Instance { get; }

    public Task<AuthStatus> GetAuthStatusAsync(CancellationToken cancellationToken) =>
        Task.FromResult(AuthStatus.Demo(SourceType, $"demo@{new Uri(Instance).Host}"));

    public Task<SourceCapabilitySet> GetCapabilitiesAsync(CancellationToken cancellationToken) =>
        Task.FromResult(_capabilities);

    public Task<IReadOnlyList<UnifiedTimelineItem>> GetTimelineAsync(
        TimelineRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var page = ParsePageToken(request.PageToken);
        IEnumerable<UnifiedTimelineItem> items = CreatePage(_items, request.Limit, page);

        if (request.Since is not null)
        {
            items = items.Where(item => item.PublishedAt > request.Since);
        }

        return Task.FromResult<IReadOnlyList<UnifiedTimelineItem>>(
            items.OrderByDescending(static item => item.PublishedAt).Take(request.Limit).ToArray());
    }

    public Task<IReadOnlyList<UnifiedTimelineItem>> GetHashtagTimelineAsync(
        string hashtag,
        TimelineRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalized = HashtagNormalizer.Normalize(hashtag);
        var baseItems = _items
            .Where(item => item.Tags.Any(tag =>
                string.Equals(HashtagNormalizer.Normalize(tag), normalized, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        var page = ParsePageToken(request.PageToken);
        var items = CreatePage(baseItems, request.Limit, page)
            .OrderByDescending(static item => item.PublishedAt)
            .Take(request.Limit)
            .ToArray();

        return Task.FromResult<IReadOnlyList<UnifiedTimelineItem>>(items);
    }

    public Task<bool> SupportsHashtagFollowingAsync(CancellationToken cancellationToken) =>
        Task.FromResult(_capabilities.SupportsHashtagFollowing);

    public Task FollowHashtagAsync(string hashtag, CancellationToken cancellationToken)
    {
        if (_capabilities.SupportsHashtagFollowing)
        {
            _followedTags.Add(HashtagNormalizer.Normalize(hashtag));
        }

        return Task.CompletedTask;
    }

    public Task UnfollowHashtagAsync(string hashtag, CancellationToken cancellationToken)
    {
        _followedTags.Remove(HashtagNormalizer.Normalize(hashtag));
        return Task.CompletedTask;
    }

    public Task<PostValidationResult> ValidateAsync(
        ComposePostDraft draft,
        CancellationToken cancellationToken) =>
        Task.FromResult(_validator.Validate(draft, _capabilities));

    public Task<PostPublishResult> PublishAsync(
        ComposePostDraft draft,
        CancellationToken cancellationToken)
    {
        var validation = _validator.Validate(draft, _capabilities);
        if (!validation.IsValid)
        {
            return Task.FromResult(PostPublishResult.Failure(validation.Errors[0]));
        }

        var remoteId = $"{SourceType.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}";
        var url = $"{Instance.TrimEnd('/')}/mock/posts/{remoteId}";
        return Task.FromResult(PostPublishResult.Success(remoteId, url));
    }

    public Task<RegistrationResult> TryRegisterAsync(
        RegistrationAttempt attempt,
        CancellationToken cancellationToken)
    {
        if (!attempt.UserName.EndsWith(".peer", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(attempt.Email) ||
            string.IsNullOrWhiteSpace(attempt.Password))
        {
            return Task.FromResult(new RegistrationResult(
                SourceType,
                RegistrationState.Failed,
                Message: "Account name, email and password are required."));
        }

        var host = new Uri(Instance).Host;
        var result = new RegistrationResult(
            SourceType,
            RegistrationState.Created,
            Message: $"Created {attempt.UserName}@{host}",
            AccountHandle: $"{attempt.UserName}@{host}");
        return Task.FromResult(result);
    }

    protected static UnifiedTimelineItem Item(
        string id,
        FediverseSourceType sourceType,
        string instance,
        string author,
        string handle,
        string? title,
        string text,
        DateTimeOffset publishedAt,
        IReadOnlyList<string> tags,
        EngagementSummary engagement,
        string? thumbnail = null,
        string? video = null,
        IReadOnlyList<string>? media = null,
        string? external = null,
        bool interactive = true) =>
        new()
        {
            Id = id,
            SourceType = sourceType,
            SourceInstance = instance,
            AuthorName = author,
            AuthorHandle = handle,
            Title = title,
            Text = text,
            MediaUrls = media ?? Array.Empty<string>(),
            ThumbnailUrl = thumbnail,
            VideoUrl = video,
            ExternalUrl = external ?? $"{instance.TrimEnd('/')}/mock/{id}",
            PublishedAt = publishedAt,
            Tags = HashtagNormalizer.NormalizeMany(tags),
            Engagement = engagement,
            CanReply = interactive && sourceType != FediverseSourceType.Rss,
            CanLike = interactive && sourceType != FediverseSourceType.Rss,
            CanBoostOrShare = interactive && sourceType is not FediverseSourceType.Rss,
            CanOpenOriginal = true
        };

    private static IReadOnlyList<UnifiedTimelineItem> CreatePage(
        IReadOnlyList<UnifiedTimelineItem> baseItems,
        int limit,
        int page)
    {
        if (baseItems.Count == 0 || limit <= 0)
        {
            return Array.Empty<UnifiedTimelineItem>();
        }

        var items = new UnifiedTimelineItem[limit];
        for (var index = 0; index < limit; index++)
        {
            var globalIndex = page * limit + index;
            var template = baseItems[index % baseItems.Count];
            items[index] = template with
            {
                Id = $"{template.Id}:page-{page}:item-{index}",
                Text = page == 0
                    ? template.Text
                    : $"{template.Text} ({page + 1})",
                PublishedAt = template.PublishedAt.AddMinutes(-globalIndex * 7),
                ExternalUrl = template.ExternalUrl is null
                    ? null
                    : $"{template.ExternalUrl}?page={page}&item={index}"
            };
        }

        return items;
    }

    private static int ParsePageToken(string? pageToken) =>
        int.TryParse(pageToken, out var page) && page > 0 ? page : 0;
}
