namespace Notes.Api.Dtos;

/// <summary>Body of POST /api/folders. Nullable: the service validates (design 5, 6).</summary>
public sealed record FolderCreateRequest(string? Name);

/// <summary>Item of GET /api/folders (design 5): the folder and the number of notes in it.</summary>
public sealed record FolderSummary(Guid Id, string Name, int NoteCount);

/// <summary>Body of PUT /api/notes/{id}/folder. <c>null</c> (or missing) = the note becomes folderless.</summary>
public sealed record NoteFolderRequest(Guid? FolderId);
