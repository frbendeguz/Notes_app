using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Notes.Api;
using Notes.Api.Data;
using Notes.Api.Data.Entities;

namespace Notes.Tests;

/// <summary>
/// Proves the data-model constraints at the database level. Every arrange / act / assert step uses a
/// fresh DbContext, so the result comes from SQLite (indexes, foreign keys), not from EF's change tracker.
/// </summary>
public sealed class SchemaTests(TestApiFactory factory) : IClassFixture<TestApiFactory>, IDisposable
{
    // AC8
    [Fact]
    public async Task DeleteFolder_FolderHasNote_NoteRemainsWithNullFolderId()
    {
        var owner = await AddUserAsync();
        var folder = await AddFolderAsync(owner.Id, "Mappa");
        var note = await AddNoteAsync(owner.Id, folder.Id);

        await using (var db = NewContext())
        {
            db.Folders.Remove(await db.Folders.SingleAsync(f => f.Id == folder.Id));
            await db.SaveChangesAsync();
        }

        await using var check = NewContext();
        Assert.False(await check.Folders.AnyAsync(f => f.Id == folder.Id));
        var remaining = await check.Notes.SingleOrDefaultAsync(n => n.Id == note.Id);
        Assert.NotNull(remaining);
        Assert.Null(remaining.FolderId);
    }

    // AC5
    [Fact]
    public async Task AddShare_SameNoteAndUserTwice_ThrowsDbUpdateExceptionAndNoDuplicate()
    {
        var owner = await AddUserAsync();
        var recipient = await AddUserAsync();
        var note = await AddNoteAsync(owner.Id);
        await AddShareAsync(note.Id, recipient.Id, ShareRole.Viewer);

        await Assert.ThrowsAsync<DbUpdateException>(() => AddShareAsync(note.Id, recipient.Id, ShareRole.Editor));

        await using var check = NewContext();
        var shares = await check.NoteShares.Where(s => s.NoteId == note.Id).ToListAsync();
        var share = Assert.Single(shares);
        Assert.Equal(ShareRole.Viewer, share.Role);
    }

    // AC8
    [Theory]
    [InlineData("Árvíztűrő", "árvíztűrő")]
    [InlineData("Mappa", " MAPPA ")]
    public async Task AddFolder_SameNormalizedNameForSameOwner_ThrowsAndNoDuplicate(string first, string second)
    {
        var owner = await AddUserAsync();
        await AddFolderAsync(owner.Id, first);

        await Assert.ThrowsAsync<DbUpdateException>(() => AddFolderAsync(owner.Id, second));

        await using var check = NewContext();
        Assert.Equal(1, await check.Folders.CountAsync(f => f.OwnerId == owner.Id));
    }

    // AC8
    [Fact]
    public async Task AddFolder_SameNameForDifferentOwners_Succeeds()
    {
        var owner1 = await AddUserAsync();
        var owner2 = await AddUserAsync();

        await AddFolderAsync(owner1.Id, "Árvíztűrő");
        await AddFolderAsync(owner2.Id, "Árvíztűrő");

        await using var check = NewContext();
        Assert.Equal(1, await check.Folders.CountAsync(f => f.OwnerId == owner1.Id));
        Assert.Equal(1, await check.Folders.CountAsync(f => f.OwnerId == owner2.Id));
    }

    [Fact]
    public async Task AddUser_SameNormalizedUserNameTwice_ThrowsAndNoDuplicate()
    {
        var name = UniqueName();
        await AddUserAsync(name.ToLowerInvariant());

        await Assert.ThrowsAsync<DbUpdateException>(() => AddUserAsync(name.ToUpperInvariant()));

        await using var check = NewContext();
        Assert.Equal(1, await check.Users.CountAsync(u => u.NormalizedUserName == Text.Normalize(name)));
    }

    [Fact]
    public async Task DeleteNote_NoteHasShares_SharesDeleted()
    {
        var owner = await AddUserAsync();
        var recipient1 = await AddUserAsync();
        var recipient2 = await AddUserAsync();
        var note = await AddNoteAsync(owner.Id);
        await AddShareAsync(note.Id, recipient1.Id, ShareRole.Viewer);
        await AddShareAsync(note.Id, recipient2.Id, ShareRole.Editor);

        await using (var db = NewContext())
        {
            db.Notes.Remove(await db.Notes.SingleAsync(n => n.Id == note.Id));
            await db.SaveChangesAsync();
        }

        await using var check = NewContext();
        Assert.False(await check.Notes.AnyAsync(n => n.Id == note.Id));
        Assert.False(await check.NoteShares.AnyAsync(s => s.NoteId == note.Id));
        Assert.True(await check.Users.AnyAsync(u => u.Id == recipient1.Id));
    }

    [Fact]
    public async Task DeleteUser_UserOwnsNotesAndFolders_NotesAndFoldersDeleted()
    {
        var owner = await AddUserAsync();
        var recipient = await AddUserAsync();
        var folder = await AddFolderAsync(owner.Id, "Mappa");
        var noteInFolder = await AddNoteAsync(owner.Id, folder.Id);
        var noteWithoutFolder = await AddNoteAsync(owner.Id);
        await AddShareAsync(noteInFolder.Id, recipient.Id, ShareRole.Viewer);

        await using (var db = NewContext())
        {
            db.Users.Remove(await db.Users.SingleAsync(u => u.Id == owner.Id));
            await db.SaveChangesAsync();
        }

        await using var check = NewContext();
        Assert.False(await check.Users.AnyAsync(u => u.Id == owner.Id));
        Assert.False(await check.Folders.AnyAsync(f => f.OwnerId == owner.Id));
        Assert.False(await check.Notes.AnyAsync(n => n.Id == noteInFolder.Id || n.Id == noteWithoutFolder.Id));
        Assert.False(await check.NoteShares.AnyAsync(s => s.NoteId == noteInFolder.Id));
        Assert.True(await check.Users.AnyAsync(u => u.Id == recipient.Id));
    }

    // --- helpers -------------------------------------------------------------------------------

    private readonly List<IServiceScope> _scopes = [];

    private AppDbContext NewContext()
    {
        var scope = factory.Services.CreateScope();
        _scopes.Add(scope);
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    public void Dispose()
    {
        foreach (var scope in _scopes)
        {
            scope.Dispose();
        }
    }

    private static string UniqueName() => "u" + Guid.NewGuid().ToString("N")[..12];

    private async Task<User> AddUserAsync(string? userName = null)
    {
        userName ??= UniqueName();
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            NormalizedUserName = Text.Normalize(userName),
            PasswordHash = "not-a-real-hash",
            CreatedAt = DateTime.UtcNow,
        };
        await using var db = NewContext();
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private async Task<Folder> AddFolderAsync(Guid ownerId, string name)
    {
        var folder = new Folder
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = name.Trim(),
            NormalizedName = Text.Normalize(name),
        };
        await using var db = NewContext();
        db.Folders.Add(folder);
        await db.SaveChangesAsync();
        return folder;
    }

    private async Task<Note> AddNoteAsync(Guid ownerId, Guid? folderId = null)
    {
        var now = DateTime.UtcNow;
        var note = new Note
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            FolderId = folderId,
            Title = "Jegyzet",
            Content = "Tartalom",
            CreatedAt = now,
            UpdatedAt = now,
        };
        await using var db = NewContext();
        db.Notes.Add(note);
        await db.SaveChangesAsync();
        return note;
    }

    private async Task AddShareAsync(Guid noteId, Guid userId, ShareRole role)
    {
        await using var db = NewContext();
        db.NoteShares.Add(new NoteShare { Id = Guid.NewGuid(), NoteId = noteId, UserId = userId, Role = role });
        await db.SaveChangesAsync();
    }
}
