using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using JsonDiffPlatform.Api.Models;
using JsonCompareOptions = JsonDiffPlatform.Api.Models.CompareOptions;

namespace JsonDiffPlatform.Api.Services;

public sealed class JsonComparisonService
{
    private const int MaxDifferences = 5000;
    private static readonly JsonSerializerOptions CompactJson = new() { WriteIndented = false };

    public CompareJsonResponse Compare(string oldJson, string newJson, JsonCompareOptions? suppliedOptions = null)
    {
        if (string.IsNullOrWhiteSpace(oldJson) || string.IsNullOrWhiteSpace(newJson))
        {
            throw new ArgumentException("两份 JSON 均不能为空。", nameof(oldJson));
        }

        var options = suppliedOptions ?? new JsonCompareOptions();
        var oldRoot = JsonNode.Parse(oldJson);
        var newRoot = JsonNode.Parse(newJson);
        var differences = new List<JsonDifference>();
        var stopwatch = Stopwatch.StartNew();

        CompareNode(oldRoot, true, newRoot, true, "$", options, differences);

        stopwatch.Stop();
        var summary = new DifferenceSummary
        {
            Total = differences.Count,
            Added = differences.Count(item => item.Kind == "Added"),
            Removed = differences.Count(item => item.Kind == "Removed"),
            Changed = differences.Count(item => item.Kind == "Changed"),
            TypeChanged = differences.Count(item => item.Kind == "TypeChanged")
        };

        return new CompareJsonResponse
        {
            Id = Guid.NewGuid().ToString("N"),
            IsEqual = differences.Count == 0,
            DurationMs = Math.Max(1, stopwatch.ElapsedMilliseconds),
            Summary = summary,
            Differences = differences,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static void CompareNode(
        JsonNode? oldNode,
        bool oldExists,
        JsonNode? newNode,
        bool newExists,
        string path,
        JsonCompareOptions options,
        List<JsonDifference> differences)
    {
        if (differences.Count >= MaxDifferences || !ShouldComparePath(path, options))
        {
            return;
        }

        if (!oldExists || !newExists)
        {
            if (!options.CompareKeys)
            {
                return;
            }

            if (options.NullStrategy.Equals("ignore", StringComparison.OrdinalIgnoreCase)
                && (IsNullLike(oldNode, oldExists) || IsNullLike(newNode, newExists)))
            {
                return;
            }

            if (options.NullStrategy.Equals("missing-as-null", StringComparison.OrdinalIgnoreCase)
                && IsNullLike(oldNode, oldExists) && IsNullLike(newNode, newExists))
            {
                return;
            }

            AddDifference(
                differences,
                !oldExists ? "Added" : "Removed",
                path,
                oldNode,
                newNode,
                !oldExists ? "missing" : GetTypeName(oldNode),
                !newExists ? "missing" : GetTypeName(newNode),
                !oldExists ? "新响应新增字段" : "新响应缺少字段");
            return;
        }

        if (oldNode is null || newNode is null)
        {
            if (options.NullStrategy.Equals("ignore", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (oldNode is null && newNode is null)
            {
                return;
            }

            AddDifference(differences, "Changed", path, oldNode, newNode, GetTypeName(oldNode), GetTypeName(newNode), "Null 值发生变化");
            return;
        }

        var oldType = GetTypeName(oldNode);
        var newType = GetTypeName(newNode);
        if (options.CompareTypes && !oldType.Equals(newType, StringComparison.Ordinal))
        {
            AddDifference(differences, "TypeChanged", path, oldNode, newNode, oldType, newType, "字段类型发生变化");
            return;
        }

        if (oldNode is JsonObject oldObject && newNode is JsonObject newObject)
        {
            CompareObject(oldObject, newObject, path, options, differences);
            return;
        }

        if (oldNode is JsonArray oldArray && newNode is JsonArray newArray)
        {
            CompareArray(oldArray, newArray, path, options, differences);
            return;
        }

        if (options.CompareValues && !AreValuesEqual(oldNode, newNode, options))
        {
            AddDifference(differences, "Changed", path, oldNode, newNode, oldType, newType, "字段值发生变化");
        }
    }

    private static void CompareObject(
        JsonObject oldObject,
        JsonObject newObject,
        string path,
        JsonCompareOptions options,
        List<JsonDifference> differences)
    {
        var matchedNewKeys = new HashSet<string>(options.CaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);

        foreach (var oldProperty in oldObject)
        {
            var mappedName = FindMappedName(oldProperty.Key, options);
            var exists = TryFindProperty(newObject, mappedName, options.CaseSensitive, out var actualNewName, out var newValue);
            var childPath = AppendPropertyPath(path, actualNewName ?? mappedName);
            if (exists)
            {
                matchedNewKeys.Add(actualNewName!);
            }

            // 字段映射只改变匹配关系，差异路径仍展示目标响应中的字段位置，便于定位。
            CompareNode(oldProperty.Value, true, newValue, exists, childPath, options, differences);
        }

        if (!options.CompareKeys)
        {
            return;
        }

        foreach (var newProperty in newObject)
        {
            if (matchedNewKeys.Contains(newProperty.Key))
            {
                continue;
            }

            var childPath = AppendPropertyPath(path, newProperty.Key);
            CompareNode(null, false, newProperty.Value, true, childPath, options, differences);
        }
    }

    private static void CompareArray(
        JsonArray oldArray,
        JsonArray newArray,
        string path,
        JsonCompareOptions options,
        List<JsonDifference> differences)
    {
        if (!string.IsNullOrWhiteSpace(options.ArrayKey))
        {
            CompareKeyedArray(oldArray, newArray, path, options, differences);
            return;
        }

        var oldItems = oldArray.Select((node, index) => (Node: node, Index: index)).ToList();
        var newItems = newArray.Select((node, index) => (Node: node, Index: index)).ToList();
        if (options.IgnoreArrayOrder)
        {
            oldItems = oldItems.OrderBy(item => CanonicalJson(item.Node), StringComparer.Ordinal).ToList();
            newItems = newItems.OrderBy(item => CanonicalJson(item.Node), StringComparer.Ordinal).ToList();
        }

        var length = Math.Max(oldItems.Count, newItems.Count);
        for (var index = 0; index < length; index++)
        {
            var oldItem = index < oldItems.Count ? oldItems[index].Node : null;
            var newItem = index < newItems.Count ? newItems[index].Node : null;
            CompareNode(oldItem, index < oldItems.Count, newItem, index < newItems.Count, $"{path}[{index}]", options, differences);
        }
    }

    private static void CompareKeyedArray(
        JsonArray oldArray,
        JsonArray newArray,
        string path,
        JsonCompareOptions options,
        List<JsonDifference> differences)
    {
        var keyName = options.ArrayKey!.Trim();
        var newByKey = new Dictionary<string, (JsonNode? Node, int Index)>(StringComparer.Ordinal);
        var newWithoutKey = new Queue<(JsonNode? Node, int Index)>();
        for (var index = 0; index < newArray.Count; index++)
        {
            var item = newArray[index];
            var key = ReadArrayKey(item, keyName, options.CaseSensitive);
            if (key is null || newByKey.ContainsKey(key))
            {
                newWithoutKey.Enqueue((item, index));
            }
            else
            {
                newByKey[key] = (item, index);
            }
        }

        var matchedKeys = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < oldArray.Count; index++)
        {
            var oldItem = oldArray[index];
            var key = ReadArrayKey(oldItem, keyName, options.CaseSensitive);
            if (key is not null && newByKey.TryGetValue(key, out var keyedNew))
            {
                matchedKeys.Add(key);
                CompareNode(oldItem, true, keyedNew.Node, true, $"{path}[{keyName}={key}]", options, differences);
                continue;
            }

            if (key is null && newWithoutKey.Count > 0)
            {
                var fallback = newWithoutKey.Dequeue();
                CompareNode(oldItem, true, fallback.Node, true, $"{path}[{fallback.Index}]", options, differences);
                continue;
            }

            CompareNode(oldItem, true, null, false, $"{path}[{keyName}={key ?? index.ToString(CultureInfo.InvariantCulture)}]", options, differences);
        }

        foreach (var pair in newByKey)
        {
            if (matchedKeys.Contains(pair.Key))
            {
                continue;
            }

            CompareNode(null, false, pair.Value.Node, true, $"{path}[{keyName}={pair.Key}]", options, differences);
        }

        while (newWithoutKey.Count > 0)
        {
            var fallback = newWithoutKey.Dequeue();
            CompareNode(null, false, fallback.Node, true, $"{path}[{fallback.Index}]", options, differences);
        }
    }

    private static bool AreValuesEqual(JsonNode oldNode, JsonNode newNode, JsonCompareOptions options)
    {
        if (TryGetNumber(oldNode, out var oldNumber) && TryGetNumber(newNode, out var newNumber))
        {
            var tolerance = Math.Max(Math.Abs(options.NumericTolerance), Math.Abs(options.FloatEpsilon));
            return Math.Abs(oldNumber - newNumber) <= tolerance;
        }

        return string.Equals(CanonicalJson(oldNode), CanonicalJson(newNode), StringComparison.Ordinal);
    }

    private static bool TryGetNumber(JsonNode node, out decimal number)
    {
        number = 0;
        if (node is not JsonValue value)
        {
            return false;
        }

        if (value.TryGetValue<decimal>(out number))
        {
            return true;
        }

        if (value.TryGetValue<double>(out var doubleValue) && doubleValue is not (double.NaN or double.PositiveInfinity or double.NegativeInfinity))
        {
            number = Convert.ToDecimal(doubleValue, CultureInfo.InvariantCulture);
            return true;
        }

        return false;
    }

    private static string? ReadArrayKey(JsonNode? node, string keyName, bool caseSensitive)
    {
        if (node is not JsonObject obj || !TryFindProperty(obj, keyName, caseSensitive, out _, out var keyNode) || keyNode is null)
        {
            return null;
        }

        return keyNode is JsonValue value && value.TryGetValue<string>(out var stringValue)
            ? stringValue
            : CanonicalJson(keyNode).Trim('"');
    }

    private static string FindMappedName(string name, JsonCompareOptions options)
    {
        var mapping = options.Mappings.FirstOrDefault(item =>
            item.From.Equals(name, options.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrWhiteSpace(mapping?.To) ? name : mapping.To;
    }

    private static bool TryFindProperty(JsonObject obj, string name, bool caseSensitive, out string? actualName, out JsonNode? value)
    {
        if (obj.TryGetPropertyValue(name, out value))
        {
            actualName = name;
            return true;
        }

        if (!caseSensitive)
        {
            foreach (var property in obj)
            {
                if (property.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    actualName = property.Key;
                    value = property.Value;
                    return true;
                }
            }
        }

        actualName = null;
        value = null;
        return false;
    }

    private static bool ShouldComparePath(string path, JsonCompareOptions options)
    {
        if (options.IgnorePaths.Any(pattern => PathMatcher.Matches(pattern, path)))
        {
            return false;
        }

        return options.WhitelistPaths.Count == 0
            || options.WhitelistPaths.Any(pattern => PathMatcher.Matches(pattern, path) || PathMatcher.IsAncestor(pattern, path));
    }

    private static bool IsNullLike(JsonNode? node, bool exists) => !exists || node is null || GetTypeName(node) == "null";

    private static string GetTypeName(JsonNode? node)
    {
        if (node is null)
        {
            return "null";
        }

        return node switch
        {
            JsonObject => "object",
            JsonArray => "array",
            JsonValue value => value.GetValueKind() switch
            {
                JsonValueKind.String => "string",
                JsonValueKind.Number => "number",
                JsonValueKind.True or JsonValueKind.False => "boolean",
                JsonValueKind.Null => "null",
                _ => "value"
            },
            _ => "value"
        };
    }

    private static string CanonicalJson(JsonNode? node) => node is null ? "null" : node.ToJsonString(CompactJson);

    private static string AppendPropertyPath(string path, string property)
    {
        return Regex.IsMatch(property, "^[A-Za-z_][A-Za-z0-9_]*$")
            ? $"{path}.{property}"
            : $"{path}['{property.Replace("'", "\\'")}']";
    }

    private static void AddDifference(
        List<JsonDifference> differences,
        string kind,
        string path,
        JsonNode? oldNode,
        JsonNode? newNode,
        string oldType,
        string newType,
        string message)
    {
        if (differences.Count >= MaxDifferences)
        {
            return;
        }

        differences.Add(new JsonDifference
        {
            Path = path,
            Kind = kind,
            OldValue = oldNode is null ? null : CanonicalJson(oldNode),
            NewValue = newNode is null ? null : CanonicalJson(newNode),
            OldType = oldType,
            NewType = newType,
            Message = message
        });
    }

    private static class PathMatcher
    {
        public static bool Matches(string? pattern, string path)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                return false;
            }

            pattern = Normalize(pattern);
            if (pattern.StartsWith("regex:", StringComparison.OrdinalIgnoreCase))
            {
                return Regex.IsMatch(path, pattern[6..], RegexOptions.CultureInvariant);
            }

            if (pattern.Length > 1 && pattern[0] == '/' && pattern[^1] == '/')
            {
                return Regex.IsMatch(path, pattern[1..^1], RegexOptions.CultureInvariant);
            }

            var patternParts = ToSegments(pattern);
            var pathParts = ToSegments(path);
            if (patternParts.Length != pathParts.Length)
            {
                return false;
            }

            return patternParts.Select((part, index) => (part, pathPart: pathParts[index]))
                .All(pair => pair.part == "*" || pair.part.Equals(pair.pathPart, StringComparison.Ordinal));
        }

        public static bool IsAncestor(string? pattern, string path)
        {
            if (string.IsNullOrWhiteSpace(pattern) || pattern.StartsWith("regex:", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            pattern = Normalize(pattern);
            var patternParts = ToSegments(pattern);
            var pathParts = ToSegments(path);
            if (pathParts.Length > patternParts.Length)
            {
                return false;
            }

            return pathParts.Select((part, index) => (part, patternPart: patternParts[index]))
                .All(pair => pair.patternPart == "*" || pair.patternPart.Equals(pair.part, StringComparison.Ordinal));
        }

        private static string Normalize(string value)
        {
            value = value.Trim();
            return value.StartsWith("$", StringComparison.Ordinal) ? value : "$" + (value.StartsWith(".") ? value : "." + value);
        }

        private static string[] ToSegments(string value)
        {
            return Regex.Matches(value, @"(?:\.([A-Za-z0-9_*\-]+)|\[\s*['""]?([^'""]+)['""]?\])")
                .Select(match => match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value)
                .ToArray();
        }
    }
}
