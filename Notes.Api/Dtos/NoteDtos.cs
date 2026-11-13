using Notes.Api.Services;

namespace Notes.Api.Dtos;

/// <summary>Body of POST and PUT /api/notes. Fields are nullable: the service validates (design 5, 6).</summary>
public sealed record NoteWriteRequest(string? Title, string? Content);

/// <summary>List item (design 5). <see cref="Role"/> is the caller's role. The folderId field comes with task 04.</summary>
public sealed record NoteSummary(Guid Id, string Title, DateTime UpdatedAt, NoteRole Role, string OwnerUserName);

/// <summary><see cref="NoteSummary"/> + content and creation time (design 5).</summary>
public sealed record NoteDetail(
    Guid Id,
    string Title,
    DateTime UpdatedAt,
    NoteRole Role,
    string OwnerUserName,
    string Content,
    DateTime CreatedAt);
