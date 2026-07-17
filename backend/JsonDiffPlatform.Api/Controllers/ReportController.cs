using JsonDiffPlatform.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace JsonDiffPlatform.Api.Controllers;

[ApiController]
[Route("api/report")]
public sealed class ReportController : ControllerBase
{
    private readonly HistoryStore _historyStore;
    private readonly ReportService _reportService;

    public ReportController(HistoryStore historyStore, ReportService reportService)
    {
        _historyStore = historyStore;
        _reportService = reportService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Download(string id, [FromQuery] string format = "html", CancellationToken cancellationToken = default)
    {
        var record = await _historyStore.FindAsync(id, cancellationToken);
        if (record is null)
        {
            return NotFound(new { message = "历史记录不存在。" });
        }

        var report = _reportService.Build(record, format);
        return File(report.Content, report.ContentType, report.FileName);
    }
}

