using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Notes.Api.Data;
using Notes.Api.Data.Entities;
using Notes.Api.Dtos;
using Notes.Api.Errors;

namespace Notes.Api.Services;

/// <summary>
/// Note CRUD (design 5, 6; V2, V3, V8). Access decisions are delegated to <see cref="NoteAccessService"/>;
/// validation runs before any database access, so a rejected edit leaves the stored note unchanged.
/// Concurrent edits: last write wins, no versioning (spec).
/// </summary>
public sealed class NoteService(AppDbContext db, NoteAccessService access)
{
    public const string TitleRequired = "A cím megadása kötelező.";
    public static readonly string TitleTooLong = $"A cím legfeljebb {Limits.TitleMax} karakter lehet.";
    public const string ContentRequired = "A szöveg megadása kötelező.";
    public static readonly string ContentTooLong = $"A szöveg legfeljebb {Limits.ContentMax} karakter lehet.";
    /// <summary>SQLite extended result code of a FOREIGN KEY constraint violation.</summary>
    private const int SqliteForeignKeyViolation = 787;

    public const string ConflictingFolderFilters = "A mappa szerinti és a mappa nélküli szűrés egyszerre nem adható meg.";

    /// <summary>
    /// The caller's own notes, most recently updated first. Optional filters (design 5): <paramref name="folderId"/>
    /// (own folder, otherwise 404) or <paramref name="unfiled"/> = true; both together → 400.
    /// <c>unfiled=false</c> means no filter (developer decision, task 04).
    /// </summary>
    public async Task<IReadOnlyList<NoteSummary>> ListOwnAsync(Guid userId, Guid? folderId, bool? unfiled, CancellationToken ct)
    {
        var onlyUnfiled = unfiled == true;
        if (folderId is not null && onlyUnfiled)
        {
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["unfiled"] = [ConflictingFolderFilters],
            });
        }

        var notes = access.OwnedNotes(userId);
        if (folderId is { } id)
        {
            await access.EnsureOwnFolderAsync(userId, id, ct);
            notes = notes.Where(n => n.FolderId == id);
        }
        else if (onlyUnfiled)
        {
            notes = notes.Where(n => n.FolderId == null);
        }

        // The caller owns every listed note, so the folderId is shown (it is hidden only from non-owners).
        return await notes
            .OrderByDescending(n => n.UpdatedAt)
            .ThenByDescending(n => n.CreatedAt)
            .Select(n => new NoteSummary(n.Id, n.Title, n.UpdatedAt, NoteRole.Owner, n.Owner.UserName, n.FolderId))
            .ToListAsync(ct);
    }

    public async Task<NoteDetail> GetAsync(Guid userId, Guid noteId, CancellationToken ct)
    {
        var role = await access.EnsureCanAsync(userId, noteId, NoteOperation.Read, ct);
        var note = await LoadAsync(noteId, ct);
        return ToDetail(note, role);
    }

    public async Task<NoteDetail> CreateAsync(Guid userId, NoteWriteRequest request, CancellationToken ct)
    {
        var (title, content) = Validate(request);
        if (request.FolderId is { } folderId) await access.EnsureOwnFolderAsync(userId, folderId, ct);

        var now = DateTime.UtcNow;
        var note = new Note
        {
            Id = Guid.NewGuid(),
            OwnerId = userId,
            FolderId = request.FolderId,
            Title = title,
            Content = content,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Notes.Add(note);
        await SaveWithFolderAsync(ct);

        await db.Entry(note).Reference(n => n.Owner).LoadAsync(ct);
        return ToDetail(note, NoteRole.Owner);
    }

    public async Task<NoteDetail> UpdateAsync(Guid userId, Guid noteId, NoteWriteRequest request, CancellationToken ct)
    {
        var (title, content) = Validate(request);
        var role = await access.EnsureCanAsync(userId, noteId, NoteOperation.Edit, ct);

        var note = await LoadAsync(noteId, ct);
        note.Title = title;
        note.Content = content;
        note.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return ToDetail(note, role);
    }

    /// <summary>
    /// PUT /api/notes/{id}/folder: owner only (design 4); target must be an own folder, <c>null</c> = folderless.
    /// Order (developer decision, task 04): note access first, then the folder – so an editor/viewer gets 403 and
    /// learns nothing about the folder. <c>UpdatedAt</c> is NOT changed: the content of the note stays the same.
    /// A note has a single nullable FolderId, so it is in at most one folder (AC8).
    /// </summary>
    public async Task<NoteDetail> MoveToFolderAsync(Guid userId, Guid noteId, NoteFolderRequest request, CancellationToken ct)
    {
        var role = await access.EnsureCanAsync(userId, noteId, NoteOperation.MoveToFolder, ct);
        if (request.FolderId is { } folderId) await access.EnsureOwnFolderAsync(userId, folderId, ct);

        var note = await LoadAsync(noteId, ct);
        note.FolderId = request.FolderId;
        await SaveWithFolderAsync(ct);

        return ToDetail(note, role);
    }

    public async Task DeleteAsync(Guid userId, Guid noteId, CancellationToken ct)
    {
        await access.EnsureCanAsync(userId, noteId, NoteOperation.Delete, ct);

        // Shares go with the note (cascade, D5).
        await db.Notes.Where(n => n.Id == noteId).ExecuteDeleteAsync(ct);
    }

    /// <summary>
    /// Saves a note whose folder was already checked. If the folder was deleted concurrently in between, the foreign
    /// key fails: that is answered as the usual folder 404 instead of a 500.
    /// </summary>
    private async Task SaveWithFolderAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteForeignKeyViolation })
        {
            throw new NotFoundException(NoteAccessService.FolderNotFoundMessage);
        }
    }

    /// <summary>Design 6: title trimmed, 1–TitleMax; content not trimmed, 0–ContentMax, null rejected. Length = UTF-16 units (V3).</summary>
    private static (string Title, string Content) Validate(NoteWriteRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        var title = request.Title?.Trim();
        if (string.IsNullOrEmpty(title)) errors["title"] = [TitleRequired];
        else if (title.Length > Limits.TitleMax) errors["title"] = [TitleTooLong];

        var content = request.Content;
        if (content is null) errors["content"] = [ContentRequired];
        else if (content.Length > Limits.ContentMax) errors["content"] = [ContentTooLong];

        if (errors.Count > 0) throw new ValidationFailedException(errors);
        return (title!, content!);
    }

    /// <summary>Loads a note whose access was already checked; if it vanished meanwhile, the answer is the usual 404.</summary>
    private async Task<Note> LoadAsync(Guid noteId, CancellationToken ct) =>
        await db.Notes.Include(n => n.Owner).SingleOrDefaultAsync(n => n.Id == noteId, ct)
            ?? throw new NotFoundException(NoteAccessService.NoteNotFoundMessage);

    /// <summary>The folderId is shown only to the owner; for editors and viewers it is null (folders are private).</summary>
    private static NoteDetail ToDetail(Note note, NoteRole role) =>
        new(note.Id, note.Title, note.UpdatedAt, role, note.Owner.UserName,
            role == NoteRole.Owner ? note.FolderId : null, note.Content, note.CreatedAt);
}
