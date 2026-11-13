using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Notes.Api;
using Notes.Api.Data;
using Notes.Api.Data.Entities;
using Notes.Api.Dtos;
using Notes.Api.Services;

namespace Notes.Tests;

/// <summary>Integration tests of folders and note-to-folder assignment (task 04, design 2, 4–6; D2, D4).</summary>
public sealed class FoldersTests(TestApiFactory factory) : IClassFixture<TestApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public enum Caller { Editor, Viewer, Stranger, MissingNote }

    public enum FolderUse { Delete, MoveTarget, CreateNoteTarget, ListFilter }

    // ---------- Create: name validation ----------

    // AC8
    [Theory]
    [InlineData(1)]
    [InlineData(Limits.FolderNameMax)]
    public async Task CreateFolder_BoundaryLength_Returns201AndStored(int length)
    {
        var user = await AuthHelper.CreateUserAsync(factory);
        var name = new string('m', length);

        var response = await user.Client.PostAsJsonAsync("/api/folders", new FolderCreateRequest(name));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<FolderSummary>(Json))!;
        Assert.Equal(name, created.Name);
        Assert.Equal(0, created.NoteCount);
        Assert.Equal($"/api/folders/{created.Id}", response.Headers.Location?.OriginalString);
        var stored = await LoadFolderAsync(created.Id);
        Assert.Equal(name, stored!.Name);
        Assert.Equal(Text.Normalize(name), stored.NormalizedName);
    }

    // AC8
    [Theory]
    [InlineData("TOO_LONG")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateFolder_InvalidName_Returns400AndNoFolderCreated(string? name)
    {
        var tooLong = name == "TOO_LONG";
        name = tooLong ? new string('m', Limits.FolderNameMax + 1) : name;
        var user = await AuthHelper.CreateUserAsync(factory);

        var response = await user.Client.PostAsJsonAsync("/api/folders", new FolderCreateRequest(name));

        await AssertValidationErrorAsync(response, "name", tooLong ? FolderService.NameTooLong : FolderService.NameRequired);
        Assert.Equal(0, await CountFoldersAsync(user.Id));
    }

    [Fact]
    public async Task CreateFolder_NameWithSurroundingSpaces_StoredTrimmed()
    {
        var user = await AuthHelper.CreateUserAsync(factory);

        var created = await CreateFolderAsync(user, "  Munka  ");

        Assert.Equal("Munka", created.Name);
        Assert.Equal("Munka", (await LoadFolderAsync(created.Id))!.Name);
    }

    // Trimming happens before the length check: 50 characters + spaces is still valid.
    [Fact]
    public async Task CreateFolder_MaxLengthPlusSurroundingSpaces_Returns201()
    {
        var user = await AuthHelper.CreateUserAsync(factory);
        var name = new string('m', Limits.FolderNameMax);

        var created = await CreateFolderAsync(user, $"  {name}  ");

        Assert.Equal(name, created.Name);
    }

    // ---------- Create: uniqueness (D2) ----------

    // AC8: case-insensitive, accented letters included.
    [Theory]
    [InlineData("Árvíztűrő", "árvíztűrő")]
    [InlineData("Árvíztűrő", "ÁRVÍZTŰRŐ")]
    [InlineData("Árvíztűrő", "  árvíztűrő ")]
    [InlineData("Munka", "Munka")]
    public async Task CreateFolder_DuplicateNameIgnoringCase_Returns409AndNoDuplicate(string first, string second)
    {
        var user = await AuthHelper.CreateUserAsync(factory);
        await CreateFolderAsync(user, first);

        var response = await user.Client.PostAsJsonAsync("/api/folders", new FolderCreateRequest(second));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal(409, body["status"]!.GetValue<int>());
        Assert.Equal(FolderService.DuplicateName, body["detail"]!.GetValue<string>());
        Assert.Equal(1, await CountFoldersAsync(user.Id));
    }

    // "Árvíztűrő" and "Arvizturo" are different names: only case is ignored, not accents.
    [Fact]
    public async Task CreateFolder_SameLettersWithoutAccents_Returns201()
    {
        var user = await AuthHelper.CreateUserAsync(factory);
        await CreateFolderAsync(user, "Árvíztűrő");

        await CreateFolderAsync(user, "Arvizturo");

        Assert.Equal(2, await CountFoldersAsync(user.Id));
    }

    // AC8: uniqueness is per user.
    [Fact]
    public async Task CreateFolder_SameNameAsOtherUsersFolder_Returns201()
    {
        var me = await AuthHelper.CreateUserAsync(factory);
        var other = await AuthHelper.CreateUserAsync(factory);
        await CreateFolderAsync(other, "Árvíztűrő");

        var mine = await CreateFolderAsync(me, "árvíztűrő");

        Assert.Equal("árvíztűrő", mine.Name);
        Assert.Equal(1, await CountFoldersAsync(me.Id));
        Assert.Equal(1, await CountFoldersAsync(other.Id));
    }

    // ---------- List ----------

    [Fact]
    public async Task ListFolders_OwnFolders_OrderedByNameWithNoteCountsAndNoForeignFolders()
    {
        var user = await AuthHelper.CreateUserAsync(factory);
        var other = await AuthHelper.CreateUserAsync(factory);
        var work = await CreateFolderAsync(user, "Munka");
        var home = await CreateFolderAsync(user, "Otthon");
        var archive = await CreateFolderAsync(user, "Archívum");
        var foreign = await CreateFolderAsync(other, "Idegen");
        await CreateNoteAsync(user, "A", work.Id);
        await CreateNoteAsync(user, "B", work.Id);
        await CreateNoteAsync(user, "C", home.Id);
        await CreateNoteAsync(user, "D", null);
        await CreateNoteAsync(other, "E", foreign.Id);

        var folders = await ListFoldersAsync(user.Client);

        Assert.Equal(
            [new FolderSummary(archive.Id, "Archívum", 0), new FolderSummary(work.Id, "Munka", 2),
             new FolderSummary(home.Id, "Otthon", 1)],
            folders);
    }

    // ---------- Note into folder, filters ----------

    // AC8
    [Fact]
    public async Task MoveNote_IntoFolderAndOutWithNull_FiltersFollow()
    {
        var user = await AuthHelper.CreateUserAsync(factory);
        var folder = await CreateFolderAsync(user, "Munka");
        var note = await CreateNoteAsync(user, "Jegyzet", null);
        var other = await CreateNoteAsync(user, "Másik", null);
        Assert.Null(note.FolderId);

        var moved = await MoveAsync(user.Client, note.Id, folder.Id);

        Assert.Equal(HttpStatusCode.OK, moved.StatusCode);
        var detail = (await moved.Content.ReadFromJsonAsync<NoteDetail>(Json))!;
        Assert.Equal(folder.Id, detail.FolderId);
        Assert.Equal(folder.Id, (await LoadNoteAsync(note.Id))!.FolderId);
        Assert.Equal([note.Id], (await ListNotesAsync(user.Client, $"?folderId={folder.Id}")).Select(n => n.Id));
        Assert.Equal([other.Id], (await ListNotesAsync(user.Client, "?unfiled=true")).Select(n => n.Id));
        var all = await ListNotesAsync(user.Client, "");
        Assert.Equal(folder.Id, all.Single(n => n.Id == note.Id).FolderId);
        Assert.Null(all.Single(n => n.Id == other.Id).FolderId);

        var removed = await MoveAsync(user.Client, note.Id, null);

        Assert.Equal(HttpStatusCode.OK, removed.StatusCode);
        Assert.Null((await removed.Content.ReadFromJsonAsync<NoteDetail>(Json))!.FolderId);
        Assert.Null((await LoadNoteAsync(note.Id))!.FolderId);
        Assert.Empty(await ListNotesAsync(user.Client, $"?folderId={folder.Id}"));
        Assert.Equal(new[] { note.Id, other.Id }.Order(),
            (await ListNotesAsync(user.Client, "?unfiled=true")).Select(n => n.Id).Order());
    }

    // AC8: a note is in at most one folder.
    [Fact]
    public async Task MoveNote_ToAnotherFolder_VisibleOnlyInNewFolder()
    {
        var user = await AuthHelper.CreateUserAsync(factory);
        var first = await CreateFolderAsync(user, "Első");
        var second = await CreateFolderAsync(user, "Második");
        var note = await CreateNoteAsync(user, "Vándor", first.Id);

        var response = await MoveAsync(user.Client, note.Id, second.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(await ListNotesAsync(user.Client, $"?folderId={first.Id}"));
        Assert.Equal([note.Id], (await ListNotesAsync(user.Client, $"?folderId={second.Id}")).Select(n => n.Id));
        var counts = (await ListFoldersAsync(user.Client)).ToDictionary(f => f.Id, f => f.NoteCount);
        Assert.Equal(0, counts[first.Id]);
        Assert.Equal(1, counts[second.Id]);
    }

    // Developer decision (task 04): moving does not change UpdatedAt – the content stays the same.
    [Fact]
    public async Task MoveNote_Owner_UpdatedAtUnchanged()
    {
        var user = await AuthHelper.CreateUserAsync(factory);
        var folder = await CreateFolderAsync(user, "Munka");
        var note = await CreateNoteAsync(user, "Jegyzet", null);

        var detail = (await (await MoveAsync(user.Client, note.Id, folder.Id)).Content.ReadFromJsonAsync<NoteDetail>(Json))!;

        Assert.Equal(note.UpdatedAt, detail.UpdatedAt);
        Assert.Equal(note.UpdatedAt, (await LoadNoteAsync(note.Id))!.UpdatedAt);
    }

    [Fact]
    public async Task CreateNote_WithOwnFolder_Returns201InFolder()
    {
        var user = await AuthHelper.CreateUserAsync(factory);
        var folder = await CreateFolderAsync(user, "Munka");

        var created = await CreateNoteAsync(user, "Mappában", folder.Id);

        Assert.Equal(folder.Id, created.FolderId);
        Assert.Equal(folder.Id, (await LoadNoteAsync(created.Id))!.FolderId);
        Assert.Equal([created.Id], (await ListNotesAsync(user.Client, $"?folderId={folder.Id}")).Select(n => n.Id));
    }

    // V18: validation first – an invalid title with a foreign folder is 400, and nothing is created.
    [Fact]
    public async Task CreateNote_InvalidTitleAndForeignFolder_Returns400AndNoNoteCreated()
    {
        var user = await AuthHelper.CreateUserAsync(factory);
        var other = await AuthHelper.CreateUserAsync(factory);
        var foreign = await CreateFolderAsync(other, "Idegen");

        var response = await user.Client.PostAsJsonAsync("/api/notes", new NoteWriteRequest("  ", "", foreign.Id));

        await AssertValidationErrorAsync(response, "title", NoteService.TitleRequired);
        Assert.Equal(0, await CountNotesAsync(user.Id));
    }

    // ---------- Delete ----------

    // AC8, D4
    [Fact]
    public async Task DeleteFolder_NonEmpty_Returns204AndNotesKeptFolderless()
    {
        var user = await AuthHelper.CreateUserAsync(factory);
        var folder = await CreateFolderAsync(user, "Törlendő");
        var kept = await CreateFolderAsync(user, "Marad");
        var a = await CreateNoteAsync(user, "A", folder.Id);
        var b = await CreateNoteAsync(user, "B", folder.Id);
        var c = await CreateNoteAsync(user, "C", kept.Id);

        var response = await user.Client.DeleteAsync($"/api/folders/{folder.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(await LoadFolderAsync(folder.Id));
        Assert.Equal([kept.Id], (await ListFoldersAsync(user.Client)).Select(f => f.Id));
        var notes = await ListNotesAsync(user.Client, "");
        Assert.Equal(3, notes.Count);
        Assert.Null(notes.Single(n => n.Id == a.Id).FolderId);
        Assert.Null(notes.Single(n => n.Id == b.Id).FolderId);
        Assert.Equal(kept.Id, notes.Single(n => n.Id == c.Id).FolderId);
        Assert.Equal(new[] { a.Id, b.Id }.Order(), (await ListNotesAsync(user.Client, "?unfiled=true")).Select(n => n.Id).Order());
        Assert.Equal("A", (await LoadNoteAsync(a.Id))!.Title);
    }

    // ---------- Foreign / missing folder ----------

    // V1: a foreign folder is answered exactly like a missing one (404, same body), and nothing changes.
    [Theory]
    [InlineData(FolderUse.Delete)]
    [InlineData(FolderUse.MoveTarget)]
    [InlineData(FolderUse.CreateNoteTarget)]
    [InlineData(FolderUse.ListFilter)]
    public async Task FolderUse_ForeignVsMissingFolder_Identical404AndNothingChanged(FolderUse use)
    {
        var user = await AuthHelper.CreateUserAsync(factory);
        var other = await AuthHelper.CreateUserAsync(factory);
        var foreign = await CreateFolderAsync(other, "Idegen");
        var foreignNote = await CreateNoteAsync(other, "Idegen jegyzet", foreign.Id);
        var myNote = await CreateNoteAsync(user, "Saját", null);

        var foreignResponse = await UseFolderAsync(user.Client, use, foreign.Id, myNote.Id);
        var missingResponse = await UseFolderAsync(user.Client, use, Guid.NewGuid(), myNote.Id);

        Assert.Equal(HttpStatusCode.NotFound, foreignResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
        var foreignBody = await ReadJsonAsync(foreignResponse);
        var missingBody = await ReadJsonAsync(missingResponse);
        Assert.Equal(NoteAccessService.FolderNotFoundMessage, foreignBody["detail"]!.GetValue<string>());
        foreignBody.Remove("traceId");
        missingBody.Remove("traceId");
        Assert.Equal(missingBody.ToJsonString(), foreignBody.ToJsonString());

        Assert.NotNull(await LoadFolderAsync(foreign.Id));
        Assert.Equal(foreign.Id, (await LoadNoteAsync(foreignNote.Id))!.FolderId);
        Assert.Null((await LoadNoteAsync(myNote.Id))!.FolderId);
        Assert.Equal(1, await CountNotesAsync(user.Id));
    }

    // ---------- Move: access matrix ----------

    // AC6, design 4: only the owner moves a note; the note's folder stays unchanged otherwise.
    // Developer decision (task 04): the note access is checked before the folder.
    [Theory]
    [InlineData(Caller.Editor, HttpStatusCode.Forbidden)]
    [InlineData(Caller.Viewer, HttpStatusCode.Forbidden)]
    [InlineData(Caller.Stranger, HttpStatusCode.NotFound)]
    [InlineData(Caller.MissingNote, HttpStatusCode.NotFound)]
    public async Task MoveNote_NotOwner_RejectedAndFolderUnchanged(Caller caller, HttpStatusCode expected)
    {
        var owner = await AuthHelper.CreateUserAsync(factory);
        var ownerFolder = await CreateFolderAsync(owner, "Tulajdonosé");
        var note = await CreateNoteAsync(owner, "Megosztott", ownerFolder.Id);
        var callerUser = await AuthHelper.CreateUserAsync(factory);
        var callerFolder = await CreateFolderAsync(callerUser, "Hívóé");
        if (caller is Caller.Editor or Caller.Viewer)
        {
            await AddShareAsync(note.Id, callerUser.Id, caller == Caller.Editor ? ShareRole.Editor : ShareRole.Viewer);
        }
        var target = caller == Caller.MissingNote ? Guid.NewGuid() : note.Id;

        // Into the caller's own folder, out of the folder (null) and into a missing folder: always the note-level answer.
        foreach (var folderId in new Guid?[] { callerFolder.Id, null, Guid.NewGuid() })
        {
            var response = await MoveAsync(callerUser.Client, target, folderId);

            Assert.Equal(expected, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(expected == HttpStatusCode.Forbidden
                    ? NoteAccessService.ForbiddenMessage
                    : NoteAccessService.NoteNotFoundMessage,
                body["detail"]!.GetValue<string>());
            Assert.Equal(ownerFolder.Id, (await LoadNoteAsync(note.Id))!.FolderId);
        }
    }

    // Folders are private: an editor or viewer sees folderId = null even if the note is in the owner's folder.
    [Theory]
    [InlineData(ShareRole.Editor)]
    [InlineData(ShareRole.Viewer)]
    public async Task GetNote_SharedNoteInOwnersFolder_FolderIdNullForRecipient(ShareRole role)
    {
        var owner = await AuthHelper.CreateUserAsync(factory);
        var recipient = await AuthHelper.CreateUserAsync(factory);
        var folder = await CreateFolderAsync(owner, "Privát");
        var note = await CreateNoteAsync(owner, "Megosztott", folder.Id);
        await AddShareAsync(note.Id, recipient.Id, role);

        var json = JsonNode.Parse(await recipient.Client.GetStringAsync($"/api/notes/{note.Id}"))!.AsObject();

        Assert.True(json.ContainsKey("folderId")); // the field is always present (developer decision)
        Assert.Null(json["folderId"]);
        if (role == ShareRole.Editor)
        {
            var put = await recipient.Client.PutAsJsonAsync($"/api/notes/{note.Id}", new NoteWriteRequest("Szerkesztve", ""));
            Assert.Null((await put.Content.ReadFromJsonAsync<NoteDetail>(Json))!.FolderId);
        }
        var ownerView = (await owner.Client.GetFromJsonAsync<NoteDetail>($"/api/notes/{note.Id}", Json))!;
        Assert.Equal(folder.Id, ownerView.FolderId);
    }

    // ---------- Filters ----------

    [Fact]
    public async Task ListNotes_FolderIdAndUnfiledTogether_Returns400()
    {
        var user = await AuthHelper.CreateUserAsync(factory);
        var folder = await CreateFolderAsync(user, "Munka");
        await CreateNoteAsync(user, "Jegyzet", folder.Id);

        var response = await user.Client.GetAsync($"/api/notes?folderId={folder.Id}&unfiled=true");

        await AssertValidationErrorAsync(response, "unfiled", NoteService.ConflictingFolderFilters);
    }

    // Developer decision (task 04): unfiled=false means no filter, also next to folderId.
    [Fact]
    public async Task ListNotes_UnfiledFalse_ActsAsNoFilter()
    {
        var user = await AuthHelper.CreateUserAsync(factory);
        var folder = await CreateFolderAsync(user, "Munka");
        var inFolder = await CreateNoteAsync(user, "Mappában", folder.Id);
        var unfiled = await CreateNoteAsync(user, "Mappa nélkül", null);

        var all = await ListNotesAsync(user.Client, "?unfiled=false");
        var inFolderOnly = await ListNotesAsync(user.Client, $"?folderId={folder.Id}&unfiled=false");

        Assert.Equal(new[] { inFolder.Id, unfiled.Id }.Order(), all.Select(n => n.Id).Order());
        Assert.Equal([inFolder.Id], inFolderOnly.Select(n => n.Id));
    }

    [Fact]
    public async Task ListNotes_FolderIdNotGuid_Returns400()
    {
        var user = await AuthHelper.CreateUserAsync(factory);

        var response = await user.Client.GetAsync("/api/notes?folderId=nem-guid");

        // Model binding error: the same ProblemDetails shape as the service validation (Program.cs).
        await AssertValidationErrorAsync(response, "folderId", "A kérés formátuma érvénytelen.");
    }

    [Fact]
    public async Task FolderEndpoints_NoToken_Return401()
    {
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/folders")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/folders", new FolderCreateRequest("Munka"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.DeleteAsync($"/api/folders/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PutAsJsonAsync($"/api/notes/{Guid.NewGuid()}/folder", new NoteFolderRequest(null))).StatusCode);
    }

    // ---------- helpers ----------

    private static Task<HttpResponseMessage> UseFolderAsync(HttpClient client, FolderUse use, Guid folderId, Guid myNoteId) =>
        use switch
        {
            FolderUse.Delete => client.DeleteAsync($"/api/folders/{folderId}"),
            FolderUse.MoveTarget => MoveAsync(client, myNoteId, folderId),
            FolderUse.CreateNoteTarget => client.PostAsJsonAsync("/api/notes", new NoteWriteRequest("Új", "", folderId)),
            _ => client.GetAsync($"/api/notes?folderId={folderId}"),
        };

    private static Task<HttpResponseMessage> MoveAsync(HttpClient client, Guid noteId, Guid? folderId) =>
        client.PutAsJsonAsync($"/api/notes/{noteId}/folder", new NoteFolderRequest(folderId));

    private static async Task<FolderSummary> CreateFolderAsync(AuthenticatedUser user, string name)
    {
        var response = await user.Client.PostAsJsonAsync("/api/folders", new FolderCreateRequest(name));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<FolderSummary>(Json))!;
    }

    private static async Task<NoteDetail> CreateNoteAsync(AuthenticatedUser user, string title, Guid? folderId)
    {
        var response = await user.Client.PostAsJsonAsync("/api/notes", new NoteWriteRequest(title, "", folderId));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<NoteDetail>(Json))!;
    }

    private static async Task<List<FolderSummary>> ListFoldersAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<List<FolderSummary>>("/api/folders", Json))!;

    private static async Task<List<NoteSummary>> ListNotesAsync(HttpClient client, string query) =>
        (await client.GetFromJsonAsync<List<NoteSummary>>($"/api/notes{query}", Json))!;

    private async Task AddShareAsync(Guid noteId, Guid userId, ShareRole role)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.NoteShares.Add(new NoteShare { Id = Guid.NewGuid(), NoteId = noteId, UserId = userId, Role = role });
        await db.SaveChangesAsync();
    }

    private async Task<Folder?> LoadFolderAsync(Guid folderId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Folders.AsNoTracking().SingleOrDefaultAsync(f => f.Id == folderId);
    }

    private async Task<Note?> LoadNoteAsync(Guid noteId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Notes.AsNoTracking().SingleOrDefaultAsync(n => n.Id == noteId);
    }

    private async Task<int> CountFoldersAsync(Guid ownerId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Folders.CountAsync(f => f.OwnerId == ownerId);
    }

    private async Task<int> CountNotesAsync(Guid ownerId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Notes.CountAsync(n => n.OwnerId == ownerId);
    }

    private static async Task<JsonObject> ReadJsonAsync(HttpResponseMessage response) =>
        JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();

    private static async Task AssertValidationErrorAsync(HttpResponseMessage response, string field, string message)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal(400, body["status"]!.GetValue<int>());
        Assert.False(string.IsNullOrEmpty(body["detail"]?.GetValue<string>()));
        var messages = body["errors"]![field]!.AsArray().Select(m => m!.GetValue<string>());
        Assert.Contains(message, messages);
    }
}
