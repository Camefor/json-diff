using JsonDiffPlatform.Api.Models;
using JsonDiffPlatform.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace JsonDiffPlatform.Api.Controllers;

[ApiController]
[Route("api/config/profile")]
public sealed class ConfigController : ControllerBase
{
    private readonly ProfileStore _profileStore;

    public ConfigController(ProfileStore profileStore)
    {
        _profileStore = profileStore;
    }

    [HttpGet]
    public Task<List<CompareProfile>> List(CancellationToken cancellationToken) => _profileStore.GetAllAsync(cancellationToken);

    [HttpGet("{name}")]
    public async Task<ActionResult<CompareProfile>> Get(string name, CancellationToken cancellationToken)
    {
        var profile = await _profileStore.FindAsync(name, cancellationToken);
        return profile is null ? NotFound(new { message = "比较规则不存在。" }) : Ok(profile);
    }

    [HttpPost]
    public Task<CompareProfile> Save([FromBody] ProfileRequest request, CancellationToken cancellationToken)
        => _profileStore.SaveAsync(request, cancellationToken);

    [HttpDelete("{name}")]
    public async Task<IActionResult> Delete(string name, CancellationToken cancellationToken)
    {
        return await _profileStore.DeleteAsync(name, cancellationToken) ? NoContent() : BadRequest(new { message = "至少保留一个比较规则。" });
    }
}

