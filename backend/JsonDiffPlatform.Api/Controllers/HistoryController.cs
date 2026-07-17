using JsonDiffPlatform.Api.Models;
using JsonDiffPlatform.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace JsonDiffPlatform.Api.Controllers;

[ApiController]
[Route("api/history")]
public sealed class HistoryController : ControllerBase
{
    private readonly HistoryStore _historyStore;

    public HistoryController(HistoryStore historyStore)
    {
        _historyStore = historyStore;
    }

    [HttpGet]
    public Task<HistoryQueryResponse> Query([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? keyword = null, CancellationToken cancellationToken = default)
        => _historyStore.QueryAsync(page, pageSize, keyword, cancellationToken);

    [HttpGet("{id}")]
    public async Task<ActionResult<HistoryRecord>> Get(string id, CancellationToken cancellationToken)
    {
        var record = await _historyStore.FindAsync(id, cancellationToken);
        return record is null ? NotFound(new { message = "历史记录不存在。" }) : Ok(record);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        return await _historyStore.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound(new { message = "历史记录不存在。" });
    }
}

