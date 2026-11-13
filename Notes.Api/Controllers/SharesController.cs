using Microsoft.AspNetCore.Mvc;
using Notes.Api.Dtos;
using Notes.Api.Services;

namespace Notes.Api.Controllers;

/// <summary>Shares of one note (design 5). Thin: validation and the owner-only check are in the services.</summary>
[ApiController]
[Route("api/notes/{id:guid}/shares")]
public sealed class SharesController(ShareService shareService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ShareInfo>>> List(Guid id, CancellationToken ct) =>
        Ok(await shareService.ListAsync(User.GetUserId(), id, ct));

    [HttpPost]
    public async Task<ActionResult<ShareInfo>> Create(Guid id, ShareCreateRequest request, CancellationToken ct)
    {
        var share = await shareService.CreateAsync(User.GetUserId(), id, request, ct);
        // Developer decision (task 05): Location identifies the share, although only DELETE exists on it (like V26).
        return Created($"/api/notes/{id}/shares/{share.UserId}", share);
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> Delete(Guid id, Guid userId, CancellationToken ct)
    {
        await shareService.DeleteAsync(User.GetUserId(), id, userId, ct);
        return NoContent();
    }
}
