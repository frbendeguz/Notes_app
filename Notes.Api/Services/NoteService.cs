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

    /// <summary>The caller's own notes, most recently updated first.</summary>
    public async Task<IReadOnlyList<NoteSummary>> ListOwnAsync(Guid userId, CancellationToken ct) =>
        await access.OwnedNotes(userId)
            .OrderByDescending(n => n.UpdatedAt)
            .ThenByDescending(n => n.CreatedAt)
            .Select(n => new NoteSummary(n.Id, n.Title, n.UpdatedAt, NoteRole.Owner, n.Owner.UserName))
            .ToListAsync(ct);

    public async Task<NoteDetail> GetAsync(Guid userId, Guid noteId, CancellationToken ct)
    {
        var role = await access.EnsureCanAsync(userId, noteId, NoteOperation.Read, ct);
        var note = await LoadAsync(noteId, ct);
        return ToDetail(note, role);
    }

    public async Task<NoteDetail> CreateAsync(Guid userId, NoteWriteRequest request, CancellationToken ct)
    {
        var (title, content) = Validate(request);

        var now = DateTime.UtcNow;
        var note = new Note
        {
            Id = Guid.NewGuid(),
            OwnerId = userId,
            Title = title,
            Content = content,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Notes.Add(note);
        await db.SaveChangesAsync(ct);

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

    public async Task DeleteAsync(Guid userId, Guid noteId, CancellationToken ct)
    {
        await access.EnsureCanAsync(userId, noteId, NoteOperation.Delete, ct);

        // Shares go with the note (cascade, D5).
        await db.Notes.Where(n => n.Id == noteId).ExecuteDeleteAsync(ct);
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

    private static NoteDetail ToDetail(Note note, NoteRole role) =>
        new(note.Id, note.Title, note.UpdatedAt, role, note.Owner.UserName, note.Content, note.CreatedAt);
}
