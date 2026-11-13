using Microsoft.AspNetCore.Mvc;
using Notes.Api.Dtos;
using Notes.Api.Services;

namespace Notes.Api.Controllers;

/// <summary>Note CRUD, the "shared with me" list and moving a note into a folder (design 5). Thin: access and validation are decided in the services.</summary>
[ApiController]
[Route("api/notes")]
public sealed class NotesController(NoteService noteService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NoteSummary>>> List(
        [FromQuery] Guid? folderId, [FromQuery] bool? unfiled, CancellationToken ct) =>
        Ok(await noteService.ListOwnAsync(User.GetUserId(), folderId, unfiled, ct));

    [HttpGet("shared")]
    public async Task<ActionResult<IReadOnlyList<NoteSummary>>> ListShared(CancellationToken ct) =>
        Ok(await noteService.ListSharedAsync(User.GetUserId(), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NoteDetail>> Get(Guid id, CancellationToken ct) =>
        Ok(await noteService.GetAsync(User.GetUserId(), id, ct));

    [HttpPost]
    public async Task<ActionResult<NoteDetail>> Create(NoteWriteRequest request, CancellationToken ct)
    {
        var note = await noteService.CreateAsync(User.GetUserId(), request, ct);
        return CreatedAtAction(nameof(Get), new { id = note.Id }, note);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<NoteDetail>> Update(Guid id, NoteWriteRequest request, CancellationToken ct) =>
        Ok(await noteService.UpdateAsync(User.GetUserId(), id, request, ct));

    [HttpPut("{id:guid}/folder")]
    public async Task<ActionResult<NoteDetail>> MoveToFolder(Guid id, NoteFolderRequest request, CancellationToken ct) =>
        Ok(await noteService.MoveToFolderAsync(User.GetUserId(), id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await noteService.DeleteAsync(User.GetUserId(), id, ct);
        return NoContent();
    }
}
