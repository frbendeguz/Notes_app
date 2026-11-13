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
        _ => false,
    };

    /// <summary>The notes the user owns ("saját jegyzetek"); the caller's role on each of them is <see cref="NoteRole.Owner"/>.</summary>
    public IQueryable<Note> OwnedNotes(Guid userId) => db.Notes.Where(n => n.OwnerId == userId);
}
