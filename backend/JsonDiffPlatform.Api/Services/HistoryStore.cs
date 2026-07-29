using System.Globalization;
using System.Text.Json;
using JsonDiffPlatform.Api.Models;
using JsonCompareOptions = JsonDiffPlatform.Api.Models.CompareOptions;
using Microsoft.Data.Sqlite;

namespace JsonDiffPlatform.Api.Services;

public sealed class HistoryStore
{
    private const int MaxHistory = 200;
    private readonly SqliteStore _database;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = false };

    public HistoryStore(SqliteStore database)
    {
        _database = database;
    }

    public async Task<HistoryQueryResponse> QueryAsync(int page, int pageSize, string? keyword, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            await using var connection = await _database.OpenAsync(cancellationToken);
            await using var countCommand = connection.CreateCommand();
            countCommand.CommandText = "SELECT COUNT(*) FROM history WHERE ($keyword = '' OR instr(lower(name), lower($keyword)) > 0);";
            countCommand.Parameters.AddWithValue("$keyword", keyword?.Trim() ?? string.Empty);
            var total = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT id, name, source_type, created_at, result_json, old_request_json, new_request_json FROM history WHERE ($keyword = '' OR instr(lower(name), lower($keyword)) > 0) ORDER BY created_at DESC LIMIT $pageSize OFFSET $offset;";
            command.Parameters.AddWithValue("$keyword", keyword?.Trim() ?? string.Empty);
            command.Parameters.AddWithValue("$pageSize", pageSize);
            command.Parameters.AddWithValue("$offset", (page - 1) * pageSize);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var items = new List<HistorySummary>();
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(ReadSummary(reader));
            }

            return new HistoryQueryResponse { Total = total, Page = page, PageSize = pageSize, Items = items };
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<HistoryRecord?> FindAsync(string id, CancellationToken cancellationToken)
    {
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            await using var connection = await _database.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT id, name, source_type, created_at, old_json, new_json, options_json, result_json, old_request_json, new_request_json FROM history WHERE id = $id LIMIT 1;";
            command.Parameters.AddWithValue("$id", id);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? ReadRecord(reader) : null;
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task SaveAsync(HistoryRecord record, CancellationToken cancellationToken)
    {
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            await using var connection = await _database.OpenAsync(cancellationToken);
            // 使用事务保证历史记录和容量裁剪同时完成，避免并发比较时产生半条记录。
            await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO history (id, name, source_type, created_at, old_json, new_json, options_json, result_json, old_request_json, new_request_json)
                VALUES ($id, $name, $sourceType, $createdAt, $oldJson, $newJson, $optionsJson, $resultJson, $oldRequestJson, $newRequestJson)
                ON CONFLICT(id) DO UPDATE SET
                    name = excluded.name,
                    source_type = excluded.source_type,
                    created_at = excluded.created_at,
                    old_json = excluded.old_json,
                    new_json = excluded.new_json,
                    options_json = excluded.options_json,
                    result_json = excluded.result_json,
                    old_request_json = excluded.old_request_json,
                    new_request_json = excluded.new_request_json;
                DELETE FROM history WHERE id IN (
                    SELECT id FROM history ORDER BY created_at DESC LIMIT -1 OFFSET 200
                );
                """;
            command.Parameters.AddWithValue("$id", record.Id);
            command.Parameters.AddWithValue("$name", record.Name);
            command.Parameters.AddWithValue("$sourceType", record.SourceType);
            command.Parameters.AddWithValue("$createdAt", record.CreatedAt.ToString("O", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$oldJson", record.OldJson);
            command.Parameters.AddWithValue("$newJson", record.NewJson);
            command.Parameters.AddWithValue("$optionsJson", JsonSerializer.Serialize(record.Options, _jsonOptions));
            command.Parameters.AddWithValue("$resultJson", JsonSerializer.Serialize(record.Result, _jsonOptions));
            command.Parameters.AddWithValue("$oldRequestJson", JsonSerializer.Serialize(record.OldRequest, _jsonOptions));
            command.Parameters.AddWithValue("$newRequestJson", JsonSerializer.Serialize(record.NewRequest, _jsonOptions));
            await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            await using var connection = await _database.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM history WHERE id = $id;";
            command.Parameters.AddWithValue("$id", id);
            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }
        finally
        {
            _mutex.Release();
        }
    }

    private HistorySummary ReadSummary(SqliteDataReader reader)
    {
        var result = Deserialize<CompareJsonResponse>(reader.GetString(4)) ?? new CompareJsonResponse();
        var oldRequest = Deserialize<InterfaceRequest>(reader.GetString(5));
        var newRequest = Deserialize<InterfaceRequest>(reader.GetString(6));
        return new HistorySummary
        {
            Id = reader.GetString(0),
            Name = reader.GetString(1),
            SourceType = reader.GetString(2),
            CreatedAt = ParseDate(reader.GetString(3)),
            IsEqual = result.IsEqual,
            DurationMs = result.DurationMs,
            Summary = result.Summary,
            OldUrl = oldRequest?.Url ?? string.Empty,
            NewUrl = newRequest?.Url ?? string.Empty
        };
    }

    private HistoryRecord ReadRecord(SqliteDataReader reader)
    {
        return new HistoryRecord
        {
            Id = reader.GetString(0),
            Name = reader.GetString(1),
            SourceType = reader.GetString(2),
            CreatedAt = ParseDate(reader.GetString(3)),
            OldJson = reader.GetString(4),
            NewJson = reader.GetString(5),
            Options = Deserialize<JsonCompareOptions>(reader.GetString(6)) ?? new JsonCompareOptions(),
            Result = Deserialize<CompareJsonResponse>(reader.GetString(7)) ?? new CompareJsonResponse(),
            OldRequest = Deserialize<InterfaceRequest>(reader.GetString(8)),
            NewRequest = Deserialize<InterfaceRequest>(reader.GetString(9))
        };
    }

    private T? Deserialize<T>(string value)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(value, _jsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private static DateTimeOffset ParseDate(string value) => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date) ? date : DateTimeOffset.UtcNow;
}
