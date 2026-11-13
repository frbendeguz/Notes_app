using Microsoft.EntityFrameworkCore;
using Notes.Api.Data;
using Notes.Api.Data.Entities;
using Notes.Api.Errors;

namespace Notes.Api.Services;

/// <summary>
/// The ONLY place where note access is decided (design 4). The role is always read from the database,
/// so a revoked share takes effect on the next request; the token never carries a role.
/// </summary>
public sealed class NoteAccessService(AppDbContext db)
{
    public const string NoteNotFoundMessage = "A jegyzet nem található.";
    public const string ForbiddenMessage = "Nincs jogosultságod ehhez a művelethez.";
    public const string FolderNotFoundMessage = "A mappa nem található.";

    /// <summary>Owner by <c>Note.OwnerId</c>, otherwise the share role; <see cref="NoteRole.None"/> for a stranger or a missing note.</summary>
    public async Task<NoteRole> GetRoleAsync(Guid userId, Guid noteId, CancellationToken ct)
    {
        var access = await db.Notes
            .Where(n => n.Id == noteId)
            .Select(n => new
            {
                n.OwnerId,
                ShareRole = n.Shares.Where(s => s.UserId == userId).Select(s => (ShareRole?)s.Role).FirstOrDefault(),
            })
            .SingleOrDefaultAsync(ct);

        if (access is null) return NoteRole.None;
        if (access.OwnerId == userId) return NoteRole.Owner;
        return access.ShareRole switch
        {
            ShareRole.Editor => NoteRole.Editor,
            ShareRole.Viewer => NoteRole.Viewer,
            _ => NoteRole.None,
        };
    }

    /// <summary>
    /// Enforces the matrix for one operation and returns the caller's role.
    /// Stranger or missing note → <see cref="NotFoundException"/> (same response, V1);
    /// visible note but too weak role → <see cref="ForbiddenException"/>.
    /// </summary>
    public async Task<NoteRole> EnsureCanAsync(Guid userId, Guid noteId, NoteOperation operation, CancellationToken ct)
    {
        var role = await GetRoleAsync(userId, noteId, ct);
        if (role == NoteRole.None) throw new NotFoundException(NoteNotFoundMessage);
        if (!IsAllowed(role, operation)) throw new ForbiddenException(ForbiddenMessage);
        return role;
    }

    /// <summary>The design 4 matrix for a role that can see the note.</summary>
    public static bool IsAllowed(NoteRole role, NoteOperation operation) => operation switch
    {
        NoteOperation.Read => role is NoteRole.Owner or NoteRole.Editor or NoteRole.Viewer,
        NoteOperation.Edit => role is NoteRole.Owner or NoteRole.Editor,
        NoteOperation.Delete => role is NoteRole.Owner,
        NoteOperation.MoveToFolder => role is NoteRole.Owner,
        NoteOperation.Share => role is NoteRole.Owner,
        _ => false,
    };

    /// <summary>The notes the user owns ("saját jegyzetek"); the caller's role on each of them is <see cref="NoteRole.Owner"/>.</summary>
    public IQueryable<Note> OwnedNotes(Guid userId) => db.Notes.Where(n => n.OwnerId == userId);

    /// <summary>
    /// The notes shared with the user ("Velem megosztott") with the caller's role on each, read from the share row
    /// (V19). Notes the user owns never appear here: the owner is <c>Note.OwnerId</c>, never a share row (D3).
    /// </summary>
    public IQueryable<SharedNote> SharedNotes(Guid userId) =>
        db.NoteShares
            .Where(s => s.UserId == userId && s.Note.OwnerId != userId)
            .Select(s => new SharedNote
            {
                Note = s.Note,
                Role = s.Role == ShareRole.Editor ? NoteRole.Editor : NoteRole.Viewer,
            });

    /// <summary>The user's own folders. Folders are private: only the owner sees or uses them (design 4, V19).</summary>
    public IQueryable<Folder> OwnedFolders(Guid userId) => db.Folders.Where(f => f.OwnerId == userId);

    /// <summary>
    /// Ensures that the folder exists and belongs to the user. A foreign and a missing folder give the same
    /// <see cref="NotFoundException"/> (same response, V1).
    /// </summary>
    public async Task EnsureOwnFolderAsync(Guid userId, Guid folderId, CancellationToken ct)
    {
        if (!await OwnedFolders(userId).AnyAsync(f => f.Id == folderId, ct))
        {
            throw new NotFoundException(FolderNotFoundMessage);
        }
    }
}

/// <summary>A note shared with the caller and the caller's role on it (<see cref="NoteAccessService.SharedNotes"/>).</summary>
public sealed class SharedNote
{
    public required Note Note { get; init; }
    public required NoteRole Role { get; init; }
}
