using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Notes.Api.Data;
using Notes.Api.Data.Entities;
using Notes.Api.Dtos;
using Notes.Api.Errors;

namespace Notes.Api.Services;

/// <summary>
/// Sharing a note by user name, listing and revoking its shares (design 4, 5; V4, V10, V18).
/// Every operation is owner-only: the access decision is <see cref="NoteAccessService.EnsureCanAsync"/> with
/// <see cref="NoteOperation.Share"/>; this service only manages the share rows of a note already checked.
/// There is no role change (V4): revoke and share again.
/// </summary>
public sealed class ShareService(AppDbContext db, NoteAccessService access)
{
    public const string UserNameRequired = "A felhasználónév megadása kötelező.";
    public const string RoleInvalid = "A szerep csak „Viewer” (olvasó) vagy „Editor” (szerkesztő) lehet.";
    public const string RecipientNotFound = "Nincs ilyen nevű felhasználó.";
    public const string SelfShare = "A jegyzetet saját magaddal nem oszthatod meg.";
    public const string AlreadyShared = "A jegyzet már meg van osztva ezzel a felhasználóval.";
    public const string ShareNotFound = "A megosztás nem található.";

    /// <summary>SQLite extended result code of a UNIQUE constraint violation.</summary>
    private const int SqliteUniqueViolation = 2067;

    /// <summary>The shares of an own note, ordered by user name (user names are ASCII only, so ordinal order is fine).</summary>
    public async Task<IReadOnlyList<ShareInfo>> ListAsync(Guid userId, Guid noteId, CancellationToken ct)
    {
        await access.EnsureCanAsync(userId, noteId, NoteOperation.Share, ct);

        return await db.NoteShares
            .Where(s => s.NoteId == noteId)
            .OrderBy(s => s.User.NormalizedUserName)
            .Select(s => new ShareInfo(s.UserId, s.User.UserName, s.Role))
            .ToListAsync(ct);
    }

    /// <summary>
    /// Order (developer decision, task 05): 1. request validation (400), 2. owner check (403/404, V18),
    /// 3. recipient by normalized user name (404), 4. self (400), 5. repeated share (409, V10).
    /// So an editor, viewer or stranger never learns whether a user name exists.
    /// </summary>
    public async Task<ShareInfo> CreateAsync(Guid userId, Guid noteId, ShareCreateRequest request, CancellationToken ct)
    {
        var (userName, role) = Validate(request);
        await access.EnsureCanAsync(userId, noteId, NoteOperation.Share, ct);

        var normalized = Text.Normalize(userName);
        var recipient = await db.Users
            .Where(u => u.NormalizedUserName == normalized)
            .Select(u => new { u.Id, u.UserName })
            .SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException(RecipientNotFound);

        if (recipient.Id == userId)
        {
            throw new ValidationFailedException(new Dictionary<string, string[]> { ["userName"] = [SelfShare] });
        }

        if (await db.NoteShares.AnyAsync(s => s.NoteId == noteId && s.UserId == recipient.Id, ct))
        {
            throw new ConflictException(AlreadyShared);
        }

        db.NoteShares.Add(new NoteShare { Id = Guid.NewGuid(), NoteId = noteId, UserId = recipient.Id, Role = role });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteUniqueViolation })
        {
            // A concurrent request shared the note with the same user between the check and the insert.
            throw new ConflictException(AlreadyShared);
        }

        return new ShareInfo(recipient.Id, recipient.UserName, role);
    }

    /// <summary>Revokes a share of an own note; the note itself stays with the owner. Missing share → 404.</summary>
    public async Task DeleteAsync(Guid userId, Guid noteId, Guid recipientId, CancellationToken ct)
    {
        await access.EnsureCanAsync(userId, noteId, NoteOperation.Share, ct);

        var deleted = await db.NoteShares
            .Where(s => s.NoteId == noteId && s.UserId == recipientId)
            .ExecuteDeleteAsync(ct);
        if (deleted == 0) throw new NotFoundException(ShareNotFound);
    }

    /// <summary>
    /// User name: required (trimmed, non-empty); its format is not checked – an impossible name is simply not found
    /// (404, developer decision). Role: "Viewer" or "Editor", case-insensitive (developer decision); anything else,
    /// including a numeric value, → 400.
    /// </summary>
    private static (string UserName, ShareRole Role) Validate(ShareCreateRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        var userName = request.UserName?.Trim();
        if (string.IsNullOrEmpty(userName)) errors["userName"] = [UserNameRequired];

        ShareRole? role =
            string.Equals(request.Role, nameof(ShareRole.Viewer), StringComparison.OrdinalIgnoreCase) ? ShareRole.Viewer
            : string.Equals(request.Role, nameof(ShareRole.Editor), StringComparison.OrdinalIgnoreCase) ? ShareRole.Editor
            : null;
        if (role is null) errors["role"] = [RoleInvalid];

        if (errors.Count > 0) throw new ValidationFailedException(errors);
        return (userName!, role!.Value);
    }
}
