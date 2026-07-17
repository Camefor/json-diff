using System.Text.Json;
using JsonDiffPlatform.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.WriteIndented = false;
});
builder.Services.AddHttpClient("interface", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("InterfaceJsonComparePlatform/1.0");
});
builder.Services.AddCors(options => options.AddPolicy("frontend", policy =>
{
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
}));
builder.Services.AddSingleton<JsonComparisonService>();
builder.Services.AddSingleton<SqliteStore>();
builder.Services.AddSingleton<HistoryStore>();
builder.Services.AddSingleton<ProfileStore>();
builder.Services.AddSingleton<ReportService>();

var dataDirectory = Path.Combine(AppContext.BaseDirectory, "data");
Directory.CreateDirectory(dataDirectory);

var app = builder.Build();
var database = app.Services.GetRequiredService<SqliteStore>();
await database.InitializeAsync(CancellationToken.None);

app.UseCors("frontend");
app.MapControllers();

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    service = "interface-json-compare-api",
    version = "1.0.0",
    timestamp = DateTimeOffset.UtcNow
}));

// 保留轻量 OpenAPI 描述，避免运行时强制依赖第三方 Swagger 包。
app.MapGet("/openapi/v1.json", () => Results.Json(new
{
    openapi = "3.0.3",
    info = new { title = "Interface JSON Compare Platform API", version = "1.0.0" },
    paths = new Dictionary<string, object>
    {
        ["/api/compare/json"] = new { post = new { summary = "比较两份 JSON" } },
        ["/api/compare/interface"] = new { post = new { summary = "调用新旧接口并比较响应" } },
        ["/api/compare/batch"] = new { post = new { summary = "批量比较 JSON" } },
        ["/api/history"] = new { get = new { summary = "查询历史记录" } }
    }
}));

app.Run();

