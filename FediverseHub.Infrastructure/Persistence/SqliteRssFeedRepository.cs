using System.Text.Json;
using FediverseHub.Core.Domain;
using FediverseHub.Core.Interfaces;

namespace FediverseHub.Infrastructure.Persistence;

public sealed class SqliteRssFeedRepository(
    SqliteConnectionFactory connectionFactory,
    SqliteSchemaInitializer schemaInitializer) : IRssFeedRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<RssFeedDefinition>> GetFeedsAsync(CancellationToken cancellationToken)
    {
        await schemaInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Json FROM RssFeeds ORDER BY Id;";

        var feeds = new List<RssFeedDefinition>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var feed = JsonSerializer.Deserialize<RssFeedDefinition>(reader.GetString(0), JsonOptions);
            if (feed is not null)
            {
                feeds.Add(feed);
            }
        }

        return feeds;
    }

    public async Task UpsertFeedAsync(RssFeedDefinition feed, CancellationToken cancellationToken)
    {
        await schemaInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO RssFeeds (Id, Json, IsEnabled)
            VALUES ($id, $json, $isEnabled)
            ON CONFLICT(Id) DO UPDATE SET
                Json = excluded.Json,
                IsEnabled = excluded.IsEnabled;
            """;
        command.Parameters.AddWithValue("$id", feed.Id);
        command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(feed, JsonOptions));
        command.Parameters.AddWithValue("$isEnabled", feed.IsEnabled ? 1 : 0);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveFeedAsync(string id, CancellationToken cancellationToken)
    {
        await schemaInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM RssFeeds WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
