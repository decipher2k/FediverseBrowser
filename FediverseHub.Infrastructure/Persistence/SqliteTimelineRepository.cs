using System.Text.Json;
using FediverseHub.Core.Domain;
using FediverseHub.Core.Interfaces;
using Microsoft.Data.Sqlite;

namespace FediverseHub.Infrastructure.Persistence;

public sealed class SqliteTimelineRepository(
    SqliteConnectionFactory connectionFactory,
    SqliteSchemaInitializer schemaInitializer) : ITimelineRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task SaveTimelineItemsAsync(
        IEnumerable<UnifiedTimelineItem> items,
        CancellationToken cancellationToken)
    {
        await schemaInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        foreach (var item in items)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO TimelineItems (Id, SourceType, SourceInstance, PublishedAt, Json)
                VALUES ($id, $sourceType, $sourceInstance, $publishedAt, $json)
                ON CONFLICT(Id, SourceType) DO UPDATE SET
                    SourceInstance = excluded.SourceInstance,
                    PublishedAt = excluded.PublishedAt,
                    Json = excluded.Json;
                """;
            command.Parameters.AddWithValue("$id", item.Id);
            command.Parameters.AddWithValue("$sourceType", (int)item.SourceType);
            command.Parameters.AddWithValue("$sourceInstance", item.SourceInstance);
            command.Parameters.AddWithValue("$publishedAt", item.PublishedAt.UtcDateTime.ToString("O"));
            command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(item, JsonOptions));
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<UnifiedTimelineItem>> GetCachedTimelineAsync(
        TimelineRequest request,
        CancellationToken cancellationToken)
    {
        await schemaInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var command = connection.CreateCommand();
        var sourceFilter = request.Sources is null || request.Sources.Count == 0
            ? string.Empty
            : $"WHERE SourceType IN ({string.Join(",", request.Sources.Select(source => (int)source))})";
        command.CommandText = $"""
            SELECT Json
            FROM TimelineItems
            {sourceFilter}
            ORDER BY PublishedAt DESC
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$limit", request.Limit);

        var items = new List<UnifiedTimelineItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var item = JsonSerializer.Deserialize<UnifiedTimelineItem>(reader.GetString(0), JsonOptions);
            if (item is not null)
            {
                items.Add(item);
            }
        }

        return items;
    }

    public async Task ClearAsync(CancellationToken cancellationToken)
    {
        await schemaInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM TimelineItems; DELETE FROM MediaAttachments;";
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
