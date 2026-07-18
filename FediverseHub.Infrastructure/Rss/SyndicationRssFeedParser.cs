using System.ServiceModel.Syndication;
using System.Xml;
using FediverseHub.Core.Domain;
using FediverseHub.Core.Interfaces;

namespace FediverseHub.Infrastructure.Rss;

public sealed class SyndicationRssFeedParser : IRssFeedParser
{
    public Task<IReadOnlyList<UnifiedTimelineItem>> ParseAsync(
        RssFeedDefinition feed,
        Stream stream,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var reader = XmlReader.Create(stream, new XmlReaderSettings { Async = false });
        var syndicationFeed = SyndicationFeed.Load(reader);

        var items = syndicationFeed.Items.Select(item =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var link = item.Links.FirstOrDefault()?.Uri?.ToString() ?? feed.Url;
            var thumbnail = item.Links.FirstOrDefault(linkItem =>
                linkItem.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true)?.Uri?.ToString();

            return new UnifiedTimelineItem
            {
                Id = $"rss:{feed.Id}:{item.Id ?? link}",
                SourceType = FediverseSourceType.Rss,
                SourceInstance = feed.Title,
                AuthorName = item.Authors.FirstOrDefault()?.Name ?? syndicationFeed.Title?.Text ?? feed.Title,
                AuthorHandle = feed.Url,
                Title = item.Title?.Text,
                Text = item.Summary?.Text,
                ThumbnailUrl = thumbnail,
                ExternalUrl = link,
                PublishedAt = item.PublishDate == DateTimeOffset.MinValue
                    ? DateTimeOffset.UtcNow
                    : item.PublishDate,
                Tags = item.Categories.Select(category => "#" + category.Name).ToArray(),
                CanOpenOriginal = true
            };
        }).ToArray();

        return Task.FromResult<IReadOnlyList<UnifiedTimelineItem>>(items);
    }
}
