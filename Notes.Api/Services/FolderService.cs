using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Notes.Api.Data;
using Notes.Api.Data.Entities;
using Notes.Api.Dtos;
using Notes.Api.Errors;

namespace Notes.Api.Services;

/// <summary>
/// Folders (design 2, 5, 6; D2, D4, V7 – no rename). Ownership is decided by <see cref="NoteAccessService"/>
/// (<see cref="NoteAccessService.OwnedFolders"/>); a foreign folder is reported exactly like a missing one (404).
/// </summary>
public sealed class FolderService(AppDbContext db, NoteAccessService access)
{
    public const string NameRequired = "A mappa nevének megadása kötelező.";
    public static readonly string NameTooLong = $"A mappa neve legfeljebb {Limits.FolderNameMax} karakter lehet.";
    public const string DuplicateName = "Ilyen nevű mappád már van.";

    /// <summary>SQLite extended result code of a UNIQUE constraint violation.</summary>
    private const int SqliteUniqueViolation = 2067;

    /// <summary>
    /// Hungarian alphabetical order, case-insensitive (developer decision, task 04): "alma" and "Ábel" come with the
    /// A's, "cs" after "c". SQLite's BINARY collation would put lower-case and accented initials after "Z", so the
    /// (small, per-user) list is sorted in memory.
    /// </summary>
    private static readonly StringComparer NameOrder =
        StringComparer.Create(CultureInfo.GetCultureInfo("hu-HU"), ignoreCase: true);

    /// <summary>The caller's folders in Hungarian alphabetical order with their note counts.</summary>
    public async Task<IReadOnlyList<FolderSummary>> ListAsync(Guid userId, CancellationToken ct)
    {
        var folders = await access.OwnedFolders(userId)
            .Select(f => new FolderSummary(f.Id, f.Name, f.Notes.Count))
            .ToListAsync(ct);
        return folders.OrderBy(f => f.Name, NameOrder).ToList();
    }

    public async Task<FolderSummary> CreateAsync(Guid userId, FolderCreateRequest request, CancellationToken ct)
    {
        var name = Validate(request);
        var normalized = Text.Normalize(name);

        // D2: per-user uniqueness on the normalized name (case and accents, e.g. "Árvíztűrő" = "árvíztűrő").
        if (await access.OwnedFolders(userId).AnyAsync(f => f.NormalizedName == normalized, ct))
        {
            throw new ConflictException(DuplicateName);
        }

        var folder = new Folder { Id = Guid.NewGuid(), OwnerId = userId, Name = name, NormalizedName = normalized };
        db.Folders.Add(folder);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteUniqueViolation })
        {
            // A concurrent request created the same name between the check and the insert: the unique index decides.
            throw new ConflictException(DuplicateName);
        }

        return new FolderSummary(folder.Id, folder.Name, 0);
    }

    /// <summary>Deletes an own folder; its notes stay and become folderless (D4, database <c>SET NULL</c>).</summary>
    public async Task DeleteAsync(Guid userId, Guid folderId, CancellationToken ct)
    {
        var deleted = await access.OwnedFolders(userId).Where(f => f.Id == folderId).ExecuteDeleteAsync(ct);
        if (deleted == 0) throw new NotFoundException(NoteAccessService.FolderNotFoundMessage);
    }

    /// <summary>Design 6: name trimmed, 1–FolderNameMax (UTF-16 length, V3); the trimmed value is stored.</summary>
    private static string Validate(FolderCreateRequest request)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrEmpty(name)) throw Invalid(NameRequired);
        if (name.Length > Limits.FolderNameMax) throw Invalid(NameTooLong);
        return name;
    }

    private static ValidationFailedException Invalid(string message) =>
        new(new Dictionary<string, string[]> { ["name"] = [message] });
}
