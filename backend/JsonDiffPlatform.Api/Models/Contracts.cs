namespace JsonDiffPlatform.Api.Models;

public sealed class CompareJsonRequest
{
    public string OldJson { get; set; } = "{}";
    public string NewJson { get; set; } = "{}";
    public string? Name { get; set; }
    public CompareOptions Options { get; set; } = new();
}

public sealed class CompareOptions
{
    public bool CompareKeys { get; set; } = true;
    public bool CompareValues { get; set; } = true;
    public bool CompareTypes { get; set; } = true;
    public string NullStrategy { get; set; } = "strict";
    public decimal NumericTolerance { get; set; }
    public decimal FloatEpsilon { get; set; } = 0.000001m;
    public bool IgnoreArrayOrder { get; set; }
    public string ArrayKey { get; set; } = string.Empty;
    public bool CaseSensitive { get; set; } = true;
    public List<string> IgnorePaths { get; set; } = [];
    public List<string> WhitelistPaths { get; set; } = [];
    public List<FieldMapping> Mappings { get; set; } = [];
}

public sealed class FieldMapping
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
}

public sealed class CompareJsonResponse
{
    public string Id { get; set; } = string.Empty;
    public bool IsEqual { get; set; }
    public long DurationMs { get; set; }
    public DifferenceSummary Summary { get; set; } = new();
    public List<JsonDifference> Differences { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class DifferenceSummary
{
    public int Total { get; set; }
    public int Added { get; set; }
    public int Removed { get; set; }
    public int Changed { get; set; }
    public int TypeChanged { get; set; }
    public int Ignored { get; set; }
}

public sealed class JsonDifference
{
    public string Path { get; set; } = "$";
    public string Kind { get; set; } = "Changed";
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string OldType { get; set; } = "missing";
    public string NewType { get; set; } = "missing";
    public string Message { get; set; } = string.Empty;
}

public sealed class InterfaceCompareRequest
{
    public string? Name { get; set; }
    public InterfaceRequest OldRequest { get; set; } = new();
    public InterfaceRequest NewRequest { get; set; } = new();
    public CompareOptions Options { get; set; } = new();
}

public sealed class InterfaceRequest
{
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Query { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string? Body { get; set; }
}

public sealed class InterfaceCompareResponse
{
    public string Id { get; set; } = string.Empty;
    public CompareJsonResponse Result { get; set; } = new();
    public InterfaceResponseMeta OldResponse { get; set; } = new();
    public InterfaceResponseMeta NewResponse { get; set; } = new();
}

public sealed class InterfaceResponseMeta
{
    public int StatusCode { get; set; }
    public long DurationMs { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public sealed class BatchCompareRequest
{
    public List<BatchCompareItemRequest> Items { get; set; } = [];
    public CompareOptions Options { get; set; } = new();
}

public sealed class BatchCompareItemRequest
{
    public string? Id { get; set; }
    public string Name { get; set; } = "未命名任务";
    public string OldJson { get; set; } = "{}";
    public string NewJson { get; set; } = "{}";
    public CompareOptions? Options { get; set; }
}

public sealed class BatchCompareResponse
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int Total { get; set; }
    public int Equal { get; set; }
    public int Different { get; set; }
    public List<BatchCompareItemResponse> Items { get; set; } = [];
}

public sealed class BatchCompareItemResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsEqual { get; set; }
    public string? Error { get; set; }
    public CompareJsonResponse? Result { get; set; }
}

public sealed class HistoryRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "JSON 比较";
    public string SourceType { get; set; } = "json";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string OldJson { get; set; } = "{}";
    public string NewJson { get; set; } = "{}";
    public CompareOptions Options { get; set; } = new();
    public CompareJsonResponse Result { get; set; } = new();
    // 仅 SourceType=interface 时有值；用于「在接口比较中打开」时回填请求配置
    public InterfaceRequest? OldRequest { get; set; }
    public InterfaceRequest? NewRequest { get; set; }
}

public sealed class HistorySummary
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SourceType { get; set; } = "json";
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsEqual { get; set; }
    public long DurationMs { get; set; }
    public DifferenceSummary Summary { get; set; } = new();
    // 仅 SourceType=interface 时有值，列表展示用
    public string OldUrl { get; set; } = string.Empty;
    public string NewUrl { get; set; } = string.Empty;
}

public sealed class HistoryQueryResponse
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<HistorySummary> Items { get; set; } = [];
}

public sealed class CompareProfile
{
    public string Name { get; set; } = "默认规则";
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public CompareOptions Options { get; set; } = new();
}

public sealed class ProfileRequest
{
    public string Name { get; set; } = "默认规则";
    public string Description { get; set; } = string.Empty;
    public CompareOptions Options { get; set; } = new();
}
