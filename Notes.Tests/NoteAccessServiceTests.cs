using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Notes.Api;
using Notes.Api.Data;
using Notes.Api.Data.Entities;
using Notes.Api.Errors;
using Notes.Api.Services;

namespace Notes.Tests;

/// <summary>
/// Unit tests of the access decision (design 4) without HTTP. Each test class instance gets its own
/// in-memory SQLite database; every step uses a fresh DbContext so the role comes from the database.
/// </summary>
public sealed class NoteAccessServiceTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public NoteAccessServiceTests()
    {
        _connection.Open();
        using var db = NewContext();
        db.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    public enum Caller { Owner, Editor, Viewer, Stranger, MissingNote }

    [Theory]
    [InlineData(Caller.Owner, NoteRole.Owner)]
    [InlineData(Caller.Editor, NoteRole.Editor)]
    [InlineData(Caller.Viewer, NoteRole.Viewer)]
    [InlineData(Caller.Stranger, NoteRole.None)]
    [InlineData(Caller.MissingNote, NoteRole.None)]
    public async Task GetRole_Caller_ReturnsRoleFromDatabase(Caller caller, NoteRole expected)
    {
        var (userId, noteId) = await ArrangeAsync(caller);

        await using var db = NewContext();
        var role = await new NoteAccessService(db).GetRoleAsync(userId, noteId, CancellationToken.None);

        Assert.Equal(expected, role);
    }

    [Fact]
    public async Task GetRole_ShareRevoked_ReturnsNoneOnNextCall()
    {
        var (userId, noteId) = await ArrangeAsync(Caller.Editor);
        await using (var db = NewContext())
        {
            await db.NoteShares.Where(s => s.NoteId == noteId && s.UserId == userId).ExecuteDeleteAsync();
        }

        await using var check = NewContext();
        Assert.Equal(NoteRole.None, await new NoteAccessService(check).GetRoleAsync(userId, noteId, CancellationToken.None));
    }

    // AC6: design 4 matrix. null = allowed, otherwise the expected exception type.
    [Theory]
    [InlineData(Caller.Owner, NoteOperation.Read, null)]
    [InlineData(Caller.Owner, NoteOperation.Edit, null)]
    [InlineData(Caller.Owner, NoteOperation.Delete, null)]
    [InlineData(Caller.Editor, NoteOperation.Read, null)]
    [InlineData(Caller.Editor, NoteOperation.Edit, null)]
    [InlineData(Caller.Editor, NoteOperation.Delete, typeof(ForbiddenException))]
    [InlineData(Caller.Viewer, NoteOperation.Read, null)]
    [InlineData(Caller.Viewer, NoteOperation.Edit, typeof(ForbiddenException))]
    [InlineData(Caller.Viewer, NoteOperation.Delete, typeof(ForbiddenException))]
    [InlineData(Caller.Stranger, NoteOperation.Read, typeof(NotFoundException))]
    [InlineData(Caller.Stranger, NoteOperation.Edit, typeof(NotFoundException))]
    [InlineData(Caller.Stranger, NoteOperation.Delete, typeof(NotFoundException))]
    [InlineData(Caller.MissingNote, NoteOperation.Read, typeof(NotFoundException))]
    [InlineData(Caller.MissingNote, NoteOperation.Edit, typeof(NotFoundException))]
    [InlineData(Caller.MissingNote, NoteOperation.Delete, typeof(NotFoundException))]
    [InlineData(Caller.Owner, NoteOperation.MoveToFolder, null)]
    [InlineData(Caller.Editor, NoteOperation.MoveToFolder, typeof(ForbiddenException))]
    [InlineData(Caller.Viewer, NoteOperation.MoveToFolder, typeof(ForbiddenException))]
    [InlineData(Caller.Stranger, NoteOperation.MoveToFolder, typeof(NotFoundException))]
    [InlineData(Caller.MissingNote, NoteOperation.MoveToFolder, typeof(NotFoundException))]
    [InlineData(Caller.Owner, NoteOperation.Share, null)]
    [InlineData(Caller.Editor, NoteOperation.Share, typeof(ForbiddenException))]
    [InlineData(Caller.Viewer, NoteOperation.Share, typeof(ForbiddenException))]
    [InlineData(Caller.Stranger, NoteOperation.Share, typeof(NotFoundException))]
    [InlineData(Caller.MissingNote, NoteOperation.Share, typeof(NotFoundException))]
    public async Task EnsureCan_CallerAndOperation_FollowsMatrix(Caller caller, NoteOperation operation, Type? expectedException)
    {
        var (userId, noteId) = await ArrangeAsync(caller);
        await using var db = NewContext();
        var service = new NoteAccessService(db);

        if (expectedException is null)
        {
            var role = await service.EnsureCanAsync(userId, noteId, operation, CancellationToken.None);
            Assert.NotEqual(NoteRole.None, role);
        }
        else
        {
            await Assert.ThrowsAsync(expectedException,
                () => service.EnsureCanAsync(userId, noteId, operation, CancellationToken.None));
        }
    }

    [Fact]
    public async Task OwnedNotes_OwnerWithSharedAndForeignNotes_ReturnsOnlyOwned()
    {
        var owner = await AddUserAsync();
        var other = await AddUserAsync();
        var own = await AddNoteAsync(owner);
        var foreignShared = await AddNoteAsync(other);
        await AddShareAsync(foreignShared, owner, ShareRole.Editor);

        await using var db = NewContext();
        var ids = await new NoteAccessService(db).OwnedNotes(owner).Select(n => n.Id).ToListAsync();

        Assert.Equal([own], ids);
    }

    [Fact]
    public async Task SharedNotes_UserWithSharesOwnAndForeignNotes_ReturnsOnlySharedWithRoleFromShare()
    {
        var me = await AddUserAsync();
        var other = await AddUserAsync();
        await AddNoteAsync(me);
        await AddNoteAsync(other);
        var asEditor = await AddNoteAsync(other);
        var asViewer = await AddNoteAsync(other);
        await AddShareAsync(asEditor, me, ShareRole.Editor);
        await AddShareAsync(asViewer, me, ShareRole.Viewer);

        await using var db = NewContext();
        var shared = await new NoteAccessService(db).SharedNotes(me)
            .Select(s => new { s.Note.Id, s.Role })
            .ToListAsync();

        Assert.Equal(2, shared.Count);
        Assert.Contains(new { Id = asEditor, Role = NoteRole.Editor }, shared);
        Assert.Contains(new { Id = asViewer, Role = NoteRole.Viewer }, shared);
    }

    /// <summary>Returns the calling user's id and the note id for the given caller kind.</summary>
    private async Task<(Guid UserId, Guid NoteId)> ArrangeAsync(Caller caller)
    {
        var owner = await AddUserAsync();
        var note = await AddNoteAsync(owner);
        switch (caller)
        {
            case Caller.Owner:
                return (owner, note);
            case Caller.Editor:
            case Caller.Viewer:
                var recipient = await AddUserAsync();
                await AddShareAsync(note, recipient, caller == Caller.Editor ? ShareRole.Editor : ShareRole.Viewer);
                return (recipient, note);
            case Caller.Stranger:
                return (await AddUserAsync(), note);
            default:
                return (owner, Guid.NewGuid());
        }
    }

    private AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);

    private async Task<Guid> AddUserAsync()
    {
        var userName = AuthHelper.UniqueUserName();
        await using var db = NewContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            NormalizedUserName = Text.Normalize(userName),
            PasswordHash = "not-a-real-hash",
            CreatedAt = DateTime.UtcNow,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    private async Task<Guid> AddNoteAsync(Guid ownerId)
    {
        await using var db = NewContext();
        var note = new Note
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Title = "Jegyzet",
            Content = "",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Notes.Add(note);
        await db.SaveChangesAsync();
        return note.Id;
    }

    private async Task AddShareAsync(Guid noteId, Guid userId, ShareRole role)
    {
        await using var db = NewContext();
        db.NoteShares.Add(new NoteShare { Id = Guid.NewGuid(), NoteId = noteId, UserId = userId, Role = role });
        await db.SaveChangesAsync();
    }
}
