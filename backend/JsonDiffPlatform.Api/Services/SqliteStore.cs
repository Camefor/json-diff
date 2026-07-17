using Microsoft.Data.Sqlite;

namespace JsonDiffPlatform.Api.Services;

public sealed class SqliteStore
{
    public string ConnectionString { get; }

    public SqliteStore(IConfiguration configuration)
    {
        var configuredPath = configuration["Storage:DatabasePath"] ?? "data/json-diff.db";
        var databasePath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(AppContext.BaseDirectory, configuredPath);
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        ConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS history (
                id TEXT NOT NULL PRIMARY KEY,
                name TEXT NOT NULL,
                source_type TEXT NOT NULL,
                created_at TEXT NOT NULL,
                old_json TEXT NOT NULL,
                new_json TEXT NOT NULL,
                options_json TEXT NOT NULL,
                result_json TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_history_created_at ON history(created_at DESC);
            CREATE TABLE IF NOT EXISTS profiles (
                name TEXT NOT NULL PRIMARY KEY,
                description TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                options_json TEXT NOT NULL
            );
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}

