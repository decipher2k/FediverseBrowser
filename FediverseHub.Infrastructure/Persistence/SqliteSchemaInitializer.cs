using Microsoft.Data.Sqlite;

namespace FediverseHub.Infrastructure.Persistence;

public sealed class SqliteSchemaInitializer(SqliteConnectionFactory connectionFactory)
{
    private bool _initialized;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
        {
            return;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized)
            {
                return;
            }

            await using var connection = connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var command = connection.CreateCommand();
            command.CommandText = """
                PRAGMA user_version = 1;

                CREATE TABLE IF NOT EXISTS AppSettings (
                    Id INTEGER PRIMARY KEY CHECK (Id = 1),
                    Json TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS TimelineItems (
                    Id TEXT NOT NULL,
                    SourceType INTEGER NOT NULL,
                    SourceInstance TEXT NOT NULL,
                    PublishedAt TEXT NOT NULL,
                    Json TEXT NOT NULL,
                    PRIMARY KEY (Id, SourceType)
                );

                CREATE TABLE IF NOT EXISTS MediaAttachments (
                    Id TEXT PRIMARY KEY,
                    TimelineItemId TEXT NOT NULL,
                    Url TEXT NOT NULL,
                    ContentType TEXT NULL,
                    AltText TEXT NULL
                );

                CREATE TABLE IF NOT EXISTS Accounts (
                    Id TEXT PRIMARY KEY,
                    SourceType INTEGER NOT NULL,
                    InstanceUrl TEXT NOT NULL,
                    Handle TEXT NOT NULL,
                    DisplayName TEXT NULL
                );

                CREATE TABLE IF NOT EXISTS RssFeeds (
                    Id TEXT PRIMARY KEY,
                    Json TEXT NOT NULL,
                    IsEnabled INTEGER NOT NULL
                );

                CREATE TABLE IF NOT EXISTS FollowedHashtags (
                    Hashtag TEXT PRIMARY KEY,
                    SourceType INTEGER NULL,
                    IsRemoteFollowed INTEGER NOT NULL DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS UserInterests (
                    InterestId TEXT PRIMARY KEY,
                    SelectedAt TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS InstanceConfig (
                    SourceType INTEGER PRIMARY KEY,
                    Json TEXT NOT NULL
                );
                """;

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            _initialized = true;
        }
        finally
        {
            _gate.Release();
        }
    }
}
