using System.Globalization;
using System.Text.Json;
using JsonDiffPlatform.Api.Models;
using JsonCompareOptions = JsonDiffPlatform.Api.Models.CompareOptions;
using Microsoft.Data.Sqlite;

namespace JsonDiffPlatform.Api.Services;

public sealed class ProfileStore
{
    private readonly SqliteStore _database;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = false };

    public ProfileStore(SqliteStore database)
    {
        _database = database;
    }

    public async Task<List<CompareProfile>> GetAllAsync(CancellationToken cancellationToken)
    {
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            await using var connection = await _database.OpenAsync(cancellationToken);
            var profiles = await ReadUnsafeAsync(connection, cancellationToken);
            if (profiles.Count == 0)
            {
                profiles.Add(new CompareProfile { Name = "默认规则", Description = "平台内置默认比较规则" });
                await UpsertUnsafeAsync(connection, profiles[0], cancellationToken);
            }

            return profiles.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<CompareProfile?> FindAsync(string name, CancellationToken cancellationToken)
    {
        var profiles = await GetAllAsync(cancellationToken);
        return profiles.FirstOrDefault(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<CompareProfile> SaveAsync(ProfileRequest request, CancellationToken cancellationToken)
    {
        var profile = new CompareProfile
        {
            Name = string.IsNullOrWhiteSpace(request.Name) ? "未命名规则" : request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Options = request.Options ?? new JsonCompareOptions(),
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _mutex.WaitAsync(cancellationToken);
        try
        {
            await using var connection = await _database.OpenAsync(cancellationToken);
            await UpsertUnsafeAsync(connection, profile, cancellationToken);
            return profile;
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<bool> DeleteAsync(string name, CancellationToken cancellationToken)
    {
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            await using var connection = await _database.OpenAsync(cancellationToken);
            await using var countCommand = connection.CreateCommand();
            countCommand.CommandText = "SELECT COUNT(*) FROM profiles;";
            var count = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
            if (count <= 1)
            {
                return false;
            }

            await using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM profiles WHERE name = $name;";
            command.Parameters.AddWithValue("$name", name);
            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }
        finally
        {
            _mutex.Release();
        }
    }

    private async Task<List<CompareProfile>> ReadUnsafeAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name, description, updated_at, options_json FROM profiles ORDER BY name COLLATE NOCASE;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var profiles = new List<CompareProfile>();
        while (await reader.ReadAsync(cancellationToken))
        {
            profiles.Add(new CompareProfile
            {
                Name = reader.GetString(0),
                Description = reader.GetString(1),
                UpdatedAt = ParseDate(reader.GetString(2)),
                Options = Deserialize<JsonCompareOptions>(reader.GetString(3)) ?? new JsonCompareOptions()
            });
        }

        return profiles;
    }

    private async Task UpsertUnsafeAsync(SqliteConnection connection, CompareProfile profile, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO profiles (name, description, updated_at, options_json)
            VALUES ($name, $description, $updatedAt, $optionsJson)
            ON CONFLICT(name) DO UPDATE SET
                description = excluded.description,
                updated_at = excluded.updated_at,
                options_json = excluded.options_json;
            """;
        command.Parameters.AddWithValue("$name", profile.Name);
        command.Parameters.AddWithValue("$description", profile.Description);
        command.Parameters.AddWithValue("$updatedAt", profile.UpdatedAt.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$optionsJson", JsonSerializer.Serialize(profile.Options, _jsonOptions));
        await command.ExecuteNonQueryAsync(cancellationToken);
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
