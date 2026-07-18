using Microsoft.Data.Sqlite;

namespace FediverseHub.Infrastructure.Persistence;

public sealed class SqliteConnectionFactory
{
    public SqliteConnectionFactory(string? dataDirectory = null)
    {
        SQLitePCL.Batteries_V2.Init();
        var directory = dataDirectory ??
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FediverseHub");
        Directory.CreateDirectory(directory);
        DatabasePath = Path.Combine(directory, "fediversehub.db");
    }

    public string DatabasePath { get; }

    public SqliteConnection CreateConnection() =>
        new($"Data Source={DatabasePath};Cache=Shared");
}
