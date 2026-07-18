using FediverseHub.Core.Domain;
using FediverseHub.Core.Interfaces;

namespace FediverseHub.Core.Services;

public sealed class TimelineAggregator(
    IEnumerable<IFediverseSourceClient> sourceClients,
    ITimelineRepository? timelineRepository = null) : ITimelineAggregator
{
    private readonly IReadOnlyList<IFediverseSourceClient> _sourceClients = sourceClients.ToArray();

    public async Task<IReadOnlyList<UnifiedTimelineItem>> GetUnifiedTimelineAsync(
        TimelineRequest request,
        CancellationToken cancellationToken)
    {
        var selectedSources = request.Sources;
        var clients = _sourceClients
            .Where(client => selectedSources is null || selectedSources.Contains(client.SourceType))
            .ToArray();

        if (clients.Length == 0)
        {
            return Array.Empty<UnifiedTimelineItem>();
        }

        var tasks = CreateFetchTasks(clients, request, cancellationToken);
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        var items = results.SelectMany(static result => result).ToArray();

        if (items.Length == 0 && timelineRepository is not null)
        {
            return await timelineRepository.GetCachedTimelineAsync(request, cancellationToken)
                .ConfigureAwait(false);
        }

        var uniqueItems = items
            .DistinctBy(static item => $"{item.SourceType}:{item.Id}")
            .ToArray();
        var ordered = SelectItems(uniqueItems, request, clients)
            .Take(request.Limit)
            .ToArray();

        if (timelineRepository is not null)
        {
            await timelineRepository.SaveTimelineItemsAsync(ordered, cancellationToken)
                .ConfigureAwait(false);
        }

        return ordered;
    }

    private static IEnumerable<UnifiedTimelineItem> SelectItems(
        IReadOnlyList<UnifiedTimelineItem> items,
        TimelineRequest request,
        IReadOnlyList<IFediverseSourceClient> clients)
    {
        if (request.Sources is not null || clients.Select(static client => client.SourceType).Distinct().Count() <= 1)
        {
            return OrderItems(items, request);
        }

        return InterleaveSources(items, request, clients);
    }

    private static IReadOnlyList<Task<IReadOnlyList<UnifiedTimelineItem>>> CreateFetchTasks(
        IEnumerable<IFediverseSourceClient> clients,
        TimelineRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedHashtags = HashtagNormalizer.NormalizeMany(request.InterestHashtags);
        var tasks = new List<Task<IReadOnlyList<UnifiedTimelineItem>>>();

        foreach (var client in clients)
        {
            tasks.Add(FetchTimelineSafelyAsync(client, request, cancellationToken));

            foreach (var hashtag in normalizedHashtags)
            {
                tasks.Add(FetchHashtagTimelineSafelyAsync(client, hashtag, request, cancellationToken));
            }
        }

        return tasks;
    }

    private static async Task<IReadOnlyList<UnifiedTimelineItem>> FetchTimelineSafelyAsync(
        IFediverseSourceClient client,
        TimelineRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await client.GetTimelineAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return Array.Empty<UnifiedTimelineItem>();
        }
    }

    private static async Task<IReadOnlyList<UnifiedTimelineItem>> FetchHashtagTimelineSafelyAsync(
        IFediverseSourceClient client,
        string hashtag,
        TimelineRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await client.GetHashtagTimelineAsync(hashtag, request, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return Array.Empty<UnifiedTimelineItem>();
        }
    }

    private static IOrderedEnumerable<UnifiedTimelineItem> OrderItems(
        IEnumerable<UnifiedTimelineItem> items,
        TimelineRequest request)
    {
        if (!request.PreferInterestRelevance || request.InterestHashtags.Count == 0)
        {
            return items.OrderByDescending(static item => item.PublishedAt);
        }

        var interests = new HashSet<string>(
            request.InterestHashtags.Select(HashtagNormalizer.Normalize),
            StringComparer.OrdinalIgnoreCase);

        return items
            .OrderByDescending(item => item.Tags.Count(tag => interests.Contains(HashtagNormalizer.Normalize(tag))))
            .ThenByDescending(static item => item.PublishedAt);
    }

    private static IEnumerable<UnifiedTimelineItem> InterleaveSources(
        IReadOnlyList<UnifiedTimelineItem> items,
        TimelineRequest request,
        IReadOnlyList<IFediverseSourceClient> clients)
    {
        var sourceOrder = clients
            .Select(static client => client.SourceType)
            .Distinct()
            .OrderByDescending(source => items
                .Where(item => item.SourceType == source)
                .Select(static item => item.PublishedAt)
                .DefaultIfEmpty(DateTimeOffset.MinValue)
                .Max())
            .ToArray();
        var buckets = sourceOrder
            .Select(source => new Queue<UnifiedTimelineItem>(
                OrderItems(items.Where(item => item.SourceType == source), request)))
            .ToArray();

        while (buckets.Any(static bucket => bucket.Count > 0))
        {
            foreach (var bucket in buckets)
            {
                if (bucket.TryDequeue(out var item))
                {
                    yield return item;
                }
            }
        }
    }
}
