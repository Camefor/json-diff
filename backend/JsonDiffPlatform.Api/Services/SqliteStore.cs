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
                result_json TEXT NOT NULL,
                old_request_json TEXT NOT NULL DEFAULT '{}',
                new_request_json TEXT NOT NULL DEFAULT '{}',
                history_key TEXT NULL
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

        // 老库兼容：缺失的请求快照列以 ALTER TABLE 补齐，避免破坏已有历史数据
        await EnsureColumnAsync(connection, "history", "old_request_json", "TEXT NOT NULL DEFAULT '{}'", cancellationToken);
        await EnsureColumnAsync(connection, "history", "new_request_json", "TEXT NOT NULL DEFAULT '{}'", cancellationToken);
        await EnsureColumnAsync(connection, "history", "history_key", "TEXT NULL", cancellationToken);

        // 仅对新写入的接口历史建立唯一约束；NULL 允许普通 JSON 和批量比较继续逐次保存。
        await using var indexCommand = connection.CreateCommand();
        indexCommand.CommandText = "CREATE UNIQUE INDEX IF NOT EXISTS ux_history_key ON history(history_key) WHERE history_key IS NOT NULL;";
        await indexCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task EnsureColumnAsync(SqliteConnection connection, string table, string column, string definition, CancellationToken cancellationToken)
    {
        await using var inspect = connection.CreateCommand();
        inspect.CommandText = $"PRAGMA table_info({table});";
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using (var reader = await inspect.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                existing.Add(reader.GetString(1));
            }
        }
        if (existing.Contains(column))
        {
            return;
        }
        await using var alter = connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition};";
        await alter.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}

