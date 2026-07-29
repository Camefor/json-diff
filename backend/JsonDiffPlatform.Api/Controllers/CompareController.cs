using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using JsonDiffPlatform.Api.Models;
using JsonDiffPlatform.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace JsonDiffPlatform.Api.Controllers;

[ApiController]
[Route("api/compare")]
public sealed class CompareController : ControllerBase
{
    private readonly JsonComparisonService _comparisonService;
    private readonly HistoryStore _historyStore;
    private readonly IHttpClientFactory _httpClientFactory;

    public CompareController(JsonComparisonService comparisonService, HistoryStore historyStore, IHttpClientFactory httpClientFactory)
    {
        _comparisonService = comparisonService;
        _historyStore = historyStore;
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost("json")]
    [RequestSizeLimit(60_000_000)]
    public async Task<ActionResult<CompareJsonResponse>> CompareJson([FromBody] CompareJsonRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OldJson) || string.IsNullOrWhiteSpace(request.NewJson))
        {
            return BadRequest(new { message = "基准响应与目标响应均不能为空。" });
        }

        try
        {
            var result = _comparisonService.Compare(request.OldJson, request.NewJson, request.Options);
            await SaveHistoryAsync(request.Name ?? "JSON 比较", "json", request.OldJson, request.NewJson, request.Options, result, cancellationToken);
            return Ok(result);
        }
        catch (JsonException exception)
        {
            return BadRequest(new { message = "JSON 格式无效。", detail = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("interface")]
    public async Task<ActionResult<InterfaceCompareResponse>> CompareInterface([FromBody] InterfaceCompareRequest request, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(request.OldRequest.Url, UriKind.Absolute, out _) || !Uri.TryCreate(request.NewRequest.Url, UriKind.Absolute, out _))
        {
            return BadRequest(new { message = "旧接口和新接口 URL 必须是有效的绝对地址。" });
        }

        try
        {
            var oldResponse = await ExecuteRequestAsync(request.OldRequest, cancellationToken);
            var newResponse = await ExecuteRequestAsync(request.NewRequest, cancellationToken);
            var result = _comparisonService.Compare(oldResponse.Body, newResponse.Body, request.Options);
            await SaveHistoryAsync(request.Name ?? "接口比较", "interface", oldResponse.Body, newResponse.Body, request.Options, result, cancellationToken, request.OldRequest, request.NewRequest);

            return Ok(new InterfaceCompareResponse
            {
                Id = result.Id,
                Result = result,
                OldResponse = oldResponse.Meta,
                NewResponse = newResponse.Meta
            });
        }
        catch (HttpRequestException exception)
        {
            return BadRequest(new { message = "接口请求失败。", detail = exception.Message });
        }
        catch (JsonException exception)
        {
            return BadRequest(new { message = "接口返回不是有效 JSON。", detail = exception.Message });
        }
    }

    [HttpPost("batch")]
    [RequestSizeLimit(60_000_000)]
    public async Task<ActionResult<BatchCompareResponse>> CompareBatch([FromBody] BatchCompareRequest request, CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0)
        {
            return BadRequest(new { message = "至少添加一条批量比较任务。" });
        }

        var items = request.Items.Take(100).ToList();
        var results = await Task.WhenAll(items.Select(async item =>
        {
            try
            {
                var options = item.Options ?? request.Options;
                var result = _comparisonService.Compare(item.OldJson, item.NewJson, options);
                await SaveHistoryAsync(item.Name, "batch", item.OldJson, item.NewJson, options, result, cancellationToken);
                return new BatchCompareItemResponse
                {
                    Id = item.Id ?? result.Id,
                    Name = item.Name,
                    IsEqual = result.IsEqual,
                    Result = result
                };
            }
            catch (Exception exception) when (exception is JsonException or ArgumentException)
            {
                return new BatchCompareItemResponse
                {
                    Id = item.Id ?? Guid.NewGuid().ToString("N"),
                    Name = item.Name,
                    IsEqual = false,
                    Error = exception.Message
                };
            }
        }));

        return Ok(new BatchCompareResponse
        {
            Total = results.Length,
            Equal = results.Count(item => item.Error is null && item.IsEqual),
            Different = results.Count(item => item.Error is not null || !item.IsEqual),
            Items = results.ToList()
        });
    }

    private async Task SaveHistoryAsync(
        string name,
        string sourceType,
        string oldJson,
        string newJson,
        CompareOptions options,
        CompareJsonResponse result,
        CancellationToken cancellationToken,
        InterfaceRequest? oldRequest = null,
        InterfaceRequest? newRequest = null)
    {
        await _historyStore.SaveAsync(new HistoryRecord
        {
            Id = result.Id,
            Name = string.IsNullOrWhiteSpace(name) ? "JSON 比较" : name,
            SourceType = sourceType,
            CreatedAt = result.CreatedAt,
            OldJson = oldJson,
            NewJson = newJson,
            Options = options,
            Result = result,
            // 仅接口比较场景下携带请求快照，前端据此回填到「接口比较」页面
            OldRequest = oldRequest,
            NewRequest = newRequest
        }, cancellationToken);
    }

    private async Task<ExecutedResponse> ExecuteRequestAsync(InterfaceRequest definition, CancellationToken cancellationToken)
    {
        var uri = BuildUri(definition);
        using var message = new HttpRequestMessage(new HttpMethod(definition.Method.ToUpperInvariant()), uri);
        if (!string.IsNullOrWhiteSpace(definition.Body) && !HttpMethod.Get.Method.Equals(message.Method.Method, StringComparison.OrdinalIgnoreCase)
            && !HttpMethod.Head.Method.Equals(message.Method.Method, StringComparison.OrdinalIgnoreCase))
        {
            message.Content = new StringContent(definition.Body);
            message.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }

        foreach (var header in definition.Headers)
        {
            if (!message.Headers.TryAddWithoutValidation(header.Key, header.Value) && message.Content is not null)
            {
                message.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        var client = _httpClientFactory.CreateClient("interface");
        var stopwatch = Stopwatch.StartNew();
        using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        stopwatch.Stop();

        return new ExecutedResponse
        {
            Body = body,
            Meta = new InterfaceResponseMeta
            {
                StatusCode = (int)response.StatusCode,
                DurationMs = Math.Max(1, stopwatch.ElapsedMilliseconds),
                ContentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty,
                Url = uri.ToString()
            }
        };
    }

    private static Uri BuildUri(InterfaceRequest definition)
    {
        var builder = new UriBuilder(definition.Url);
        var existingQuery = builder.Query.TrimStart('?');
        var additionalQuery = string.Join("&", definition.Query
            .Where(item => !string.IsNullOrWhiteSpace(item.Key))
            .Select(item => $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value ?? string.Empty)}"));
        builder.Query = string.Join("&", new[] { existingQuery, additionalQuery }.Where(value => !string.IsNullOrWhiteSpace(value)));
        return builder.Uri;
    }

    private sealed class ExecutedResponse
    {
        public string Body { get; init; } = string.Empty;
        public InterfaceResponseMeta Meta { get; init; } = new();
    }
}
