using System.Text;
using FediverseHub.Core.Domain;
using FediverseHub.Infrastructure.Rss;

namespace FediverseHub.Tests;

public sealed class RssParserTests
{
    [Fact]
    public async Task Syndication_parser_maps_items_to_unified_timeline_items()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <rss version="2.0">
              <channel>
                <title>Example Feed</title>
                <link>https://example.com</link>
                <description>Example</description>
                <item>
                  <guid>item-1</guid>
                  <title>Fediverse article</title>
                  <link>https://example.com/fediverse</link>
                  <description>RSS content</description>
                  <category>fediverse</category>
                  <pubDate>Mon, 15 Jun 2026 10:00:00 GMT</pubDate>
                </item>
              </channel>
            </rss>
            """;

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var parser = new SyndicationRssFeedParser();

        var items = await parser.ParseAsync(
            new RssFeedDefinition
            {
                Id = "example",
                Title = "Example Feed",
                Url = "https://example.com/feed.xml"
            },
            stream,
            CancellationToken.None);

        var item = Assert.Single(items);
        Assert.Equal(FediverseSourceType.Rss, item.SourceType);
        Assert.Equal("Fediverse article", item.Title);
        Assert.Equal("https://example.com/fediverse", item.ExternalUrl);
        Assert.Contains("#fediverse", item.Tags);
        Assert.False(item.CanReply);
    }
}
