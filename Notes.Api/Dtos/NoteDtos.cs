using Notes.Api.Services;

namespace Notes.Api.Dtos;

/// <summary>
/// Body of POST and PUT /api/notes. Fields are nullable: the service validates (design 5, 6).
/// <see cref="FolderId"/> is used only by POST (optional, own folder); PUT ignores it – moving a note
/// between folders is PUT /api/notes/{id}/folder.
/// </summary>
public sealed record NoteWriteRequest(string? Title, string? Content, Guid? FolderId = null);

/// <summary>
/// List item (design 5). <see cref="Role"/> is the caller's role. <see cref="FolderId"/> is filled only for the
/// owner (folders are private); for editors and viewers it is always <c>null</c> (the field is still present).
/// </summary>
public sealed record NoteSummary(Guid Id, string Title, DateTime UpdatedAt, NoteRole Role, string OwnerUserName, Guid? FolderId);

/// <summary><see cref="NoteSummary"/> + content and creation time (design 5).</summary>
public sealed record NoteDetail(
    Guid Id,
    string Title,
    DateTime UpdatedAt,
    NoteRole Role,
    string OwnerUserName,
    Guid? FolderId,
    string Content,
    DateTime CreatedAt);
