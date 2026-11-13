using Notes.Api.Data.Entities;

namespace Notes.Api.Dtos;

/// <summary>
/// Body of POST /api/notes/{id}/shares (design 5). Both fields are strings and nullable: the service validates.
/// <see cref="Role"/> is "Viewer" or "Editor", case-insensitive (developer decision, task 05).
/// </summary>
public sealed record ShareCreateRequest(string? UserName, string? Role);

/// <summary>One share of a note (design 5): the recipient and the recipient's role.</summary>
public sealed record ShareInfo(Guid UserId, string UserName, ShareRole Role);
