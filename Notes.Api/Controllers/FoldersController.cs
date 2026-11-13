using Microsoft.AspNetCore.Mvc;
using Notes.Api.Dtos;
using Notes.Api.Services;

namespace Notes.Api.Controllers;

/// <summary>Folders (design 5). Thin: validation and ownership are decided in the services.</summary>
[ApiController]
[Route("api/folders")]
public sealed class FoldersController(FolderService folderService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FolderSummary>>> List(CancellationToken ct) =>
        Ok(await folderService.ListAsync(User.GetUserId(), ct));

    [HttpPost]
    public async Task<ActionResult<FolderSummary>> Create(FolderCreateRequest request, CancellationToken ct)
    {
        var folder = await folderService.CreateAsync(User.GetUserId(), request, ct);
        // Developer decision (task 04): Location identifies the folder, although there is no GET for a single folder.
        return Created($"/api/folders/{folder.Id}", folder);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await folderService.DeleteAsync(User.GetUserId(), id, ct);
        return NoContent();
    }
}
