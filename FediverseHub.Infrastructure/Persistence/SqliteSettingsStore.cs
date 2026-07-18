using System.Text.Json;
using FediverseHub.Core.Domain;
using FediverseHub.Core.Interfaces;

namespace FediverseHub.Infrastructure.Persistence;

public sealed class SqliteSettingsStore(
    SqliteConnectionFactory connectionFactory,
    SqliteSchemaInitializer schemaInitializer) : ISettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken)
    {
        await schemaInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Json FROM AppSettings WHERE Id = 1;";
        var json = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;

        return string.IsNullOrWhiteSpace(json)
            ? new AppSettings()
            : JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        await schemaInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO AppSettings (Id, Json)
            VALUES (1, $json)
            ON CONFLICT(Id) DO UPDATE SET Json = excluded.Json;
            """;
        command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(settings, JsonOptions));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
