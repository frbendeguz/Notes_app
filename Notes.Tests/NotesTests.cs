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

/// <summary>Integration tests of note CRUD and the access matrix (task 03, design 4–6).</summary>
public sealed class NotesTests(TestApiFactory factory) : IClassFixture<TestApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public enum Caller { Owner, Editor, Viewer, Stranger, MissingNote }

    public enum Operation { Read, Edit, Delete }

    // ---------- Create, list, read back ----------

    // AC2
    [Fact]
    public async Task CreateNote_Valid_AppearsInListAlsoAfterNewLogin()
    {
        var user = await AuthHelper.CreateUserAsync(factory);

        var response = await user.Client.PostAsJsonAsync("/api/notes", new NoteWriteRequest("Bevásárlás", "tej, kenyér"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<NoteDetail>(Json))!;
        Assert.Equal($"/api/notes/{created.Id}", response.Headers.Location?.AbsolutePath);
        Assert.Equal("Bevásárlás", created.Title);
        Assert.Equal("tej, kenyér", created.Content);
        Assert.Equal(NoteRole.Owner, created.Role);
        Assert.Equal(user.UserName, created.OwnerUserName);

        var list = await ListAsync(user.Client);
        var summary = Assert.Single(list);
        Assert.Equal(created.Id, summary.Id);
        Assert.Equal("Bevásárlás", summary.Title);

        // "another device": a new HttpClient with a new login
        var otherDevice = await LoginAsync(user);
        var listOnOtherDevice = await ListAsync(otherDevice);
        Assert.Equal(list, listOnOtherDevice);
        var detail = (await otherDevice.GetFromJsonAsync<NoteDetail>($"/api/notes/{created.Id}", Json))!;
        Assert.Equal(created, detail);
    }

    [Fact]
    public async Task CreateNote_Timestamps_SerializedAsUtcAndUnchangedAfterReadBack()
    {
        var user = await AuthHelper.CreateUserAsync(factory);
        var before = DateTime.UtcNow;

        var response = await user.Client.PostAsJsonAsync("/api/notes", new NoteWriteRequest("Idő", ""));
        var after = DateTime.UtcNow;

        var createdJson = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        var id = createdJson["id"]!.GetValue<Guid>();
        var readJson = JsonNode.Parse(await user.Client.GetStringAsync($"/api/notes/{id}"))!;
        var listJson = JsonNode.Parse(await user.Client.GetStringAsync("/api/notes"))!.AsArray().Single()!;

        foreach (var node in new[] { createdJson["createdAt"], createdJson["updatedAt"], readJson["createdAt"],
                     readJson["updatedAt"], listJson["updatedAt"] })
        {
            Assert.EndsWith("Z", node!.GetValue<string>());
        }

        var createdAt = createdJson["createdAt"]!.GetValue<DateTime>();
        Assert.Equal(DateTimeKind.Utc, createdAt.Kind);
        Assert.InRange(createdAt, before, after);
        Assert.Equal(createdAt, readJson["createdAt"]!.GetValue<DateTime>());
        Assert.Equal(createdJson["updatedAt"]!.GetValue<DateTime>(), readJson["updatedAt"]!.GetValue<DateTime>());
        Assert.Equal(createdJson["updatedAt"]!.GetValue<DateTime>(), listJson["updatedAt"]!.GetValue<DateTime>());
    }

    // V2
    [Fact]
    public async Task CreateNote_TitleWithSurroundingSpaces_StoredTrimmedContentKept()
    {
        var user = await AuthHelper.CreateUserAsync(factory);

        var created = await CreateNoteAsync(user, "  Cím  ", "  szöveg  ");

        Assert.Equal("Cím", created.Title);
        Assert.Equal("  szöveg  ", created.Content);
        var stored = await LoadNoteAsync(created.Id);
        Assert.Equal("Cím", stored!.Title);
        Assert.Equal("  szöveg  ", stored.Content);
    }

    // AC3
    [Theory]
    [InlineData(1, 0)]
    [InlineData(Limits.TitleMax, 0)]
    [InlineData(1, Limits.ContentMax)]
    [InlineData(Limits.TitleMax, Limits.ContentMax)]
    public async Task CreateNote_BoundaryLengths_Returns201AndStored(int titleLength, int contentLength)
    {
        var user = await AuthHelper.CreateUserAsync(factory);

        var response = await user.Client.PostAsJsonAsync("/api/notes",
            new NoteWriteRequest(new string('c', titleLength), new string('s', contentLength)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<NoteDetail>(Json))!;
        var stored = await LoadNoteAsync(created.Id);
        Assert.Equal(titleLength, stored!.Title.Length);
        Assert.Equal(contentLength, stored.Content.Length);
    }

    // AC3, AC4
    [Theory]
    [InlineData(Limits.TitleMax + 1, 0, "title")]
    [InlineData(1, Limits.ContentMax + 1, "content")]
    public async Task CreateNote_TooLong_Returns400AndNoNoteCreated(int titleLength, int contentLength, string field)
    {
        var user = await AuthHelper.CreateUserAsync(factory);

        var response = await user.Client.PostAsJsonAsync("/api/notes",
            new NoteWriteRequest(new string('c', titleLength), new string('s', contentLength)));

        var expected = field == "title" ? NoteService.TitleTooLong : NoteService.ContentTooLong;
        await AssertValidationErrorAsync(response, field, expected);
        Assert.Equal(0, await CountNotesAsync(user.Id));
    }

    // AC4
    [Theory]
    [InlineData("", "szöveg", "title")]
    [InlineData("   ", "szöveg", "title")]
    [InlineData(null, "szöveg", "title")]
    [InlineData("Cím", null, "content")]
    public async Task CreateNote_MissingOrBlankField_Returns400AndNoNoteCreated(string? title, string? content, string field)
    {
        var user = await AuthHelper.CreateUserAsync(factory);

        var response = await user.Client.PostAsJsonAsync("/api/notes", new NoteWriteRequest(title, content));

        var expected = field == "title" ? NoteService.TitleRequired : NoteService.ContentRequired;
        await AssertValidationErrorAsync(response, field, expected);
        Assert.Equal(0, await CountNotesAsync(user.Id));
    }

    // AC1
    [Fact]
    public async Task ListNotes_OtherUsersAndSharedNotes_ReturnsOnlyOwnNotes()
    {
        var me = await AuthHelper.CreateUserAsync(factory);
        var other = await AuthHelper.CreateUserAsync(factory);
        var mine = await CreateNoteAsync(me, "Saját", "");
        await CreateNoteAsync(other, "Másé", "");
        var sharedWithMe = await CreateNoteAsync(other, "Velem megosztott", "");
        await AddShareAsync(sharedWithMe.Id, me.Id, ShareRole.Editor);

        var list = await ListAsync(me.Client);

        var summary = Assert.Single(list);
        Assert.Equal(mine.Id, summary.Id);
        Assert.Equal(NoteRole.Owner, summary.Role);
        Assert.Equal(me.UserName, summary.OwnerUserName);
    }

    [Fact]
    public async Task ListNotes_AfterUpdate_OrderedByUpdatedAtDescending()
    {
        var user = await AuthHelper.CreateUserAsync(factory);
        var a = await CreateNoteAsync(user, "A", "");
        var b = await CreateNoteAsync(user, "B", "");
        Assert.Equal([b.Id, a.Id], (await ListAsync(user.Client)).Select(n => n.Id));

        var put = await user.Client.PutAsJsonAsync($"/api/notes/{a.Id}", new NoteWriteRequest("A2", ""));
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        Assert.Equal([a.Id, b.Id], (await ListAsync(user.Client)).Select(n => n.Id));
    }

    // ---------- Update ----------

    [Fact]
    public async Task UpdateNote_Valid_Returns200AndUpdatedAtAdvancesCreatedAtKept()
    {
        var user = await AuthHelper.CreateUserAsync(factory);
        var created = await CreateNoteAsync(user, "Régi", "régi szöveg");

        var response = await user.Client.PutAsJsonAsync($"/api/notes/{created.Id}", new NoteWriteRequest(" Új ", ""));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<NoteDetail>(Json))!;
        Assert.Equal("Új", updated.Title);
        Assert.Equal("", updated.Content); // V8: empty content allowed
        Assert.Equal(created.CreatedAt, updated.CreatedAt);
        Assert.True(updated.UpdatedAt > created.UpdatedAt);
        var stored = await LoadNoteAsync(created.Id);
        Assert.Equal("Új", stored!.Title);
        Assert.Equal("", stored.Content);
        Assert.Equal(updated.UpdatedAt, stored.UpdatedAt);
    }

    // AC4
    [Theory]
    [InlineData("", "új", "title")]
    [InlineData("   ", "új", "title")]
    [InlineData(null, "új", "title")]
    [InlineData("Új", null, "content")]
    [InlineData("TOO_LONG_TITLE", "új", "title")]
    [InlineData("Új", "TOO_LONG_CONTENT", "content")]
    public async Task UpdateNote_InvalidInput_Returns400AndNoteUnchanged(string? title, string? content, string field)
    {
        title = title == "TOO_LONG_TITLE" ? new string('c', Limits.TitleMax + 1) : title;
        content = content == "TOO_LONG_CONTENT" ? new string('s', Limits.ContentMax + 1) : content;
        var user = await AuthHelper.CreateUserAsync(factory);
        var created = await CreateNoteAsync(user, "Eredeti", "eredeti szöveg");
        var before = await LoadNoteAsync(created.Id);

        var response = await user.Client.PutAsJsonAsync($"/api/notes/{created.Id}", new NoteWriteRequest(title, content));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.True(body["errors"]![field] is JsonArray { Count: > 0 });
        await AssertNoteUnchangedAsync(before!);
    }

    // Developer decision (task 03): validation runs before the access check, so a viewer's invalid PUT is 400, not 403.
    [Fact]
    public async Task UpdateNote_ViewerWithInvalidBody_Returns400AndNoteUnchanged()
    {
        var owner = await AuthHelper.CreateUserAsync(factory);
        var viewer = await AuthHelper.CreateUserAsync(factory);
        var note = await CreateNoteAsync(owner, "Eredeti", "eredeti szöveg");
        await AddShareAsync(note.Id, viewer.Id, ShareRole.Viewer);
        var before = await LoadNoteAsync(note.Id);

        var response = await viewer.Client.PutAsJsonAsync($"/api/notes/{note.Id}", new NoteWriteRequest("   ", "új"));

        await AssertValidationErrorAsync(response, "title", NoteService.TitleRequired);
        await AssertNoteUnchangedAsync(before!);
    }

    // ---------- Access matrix ----------

    // AC6: design 4 matrix; the share is written directly into the database (sharing endpoints: task 05).
    [Theory]
    [InlineData(Operation.Read, Caller.Owner, HttpStatusCode.OK)]
    [InlineData(Operation.Read, Caller.Editor, HttpStatusCode.OK)]
    [InlineData(Operation.Read, Caller.Viewer, HttpStatusCode.OK)]
    [InlineData(Operation.Read, Caller.Stranger, HttpStatusCode.NotFound)]
    [InlineData(Operation.Read, Caller.MissingNote, HttpStatusCode.NotFound)]
    [InlineData(Operation.Edit, Caller.Owner, HttpStatusCode.OK)]
    [InlineData(Operation.Edit, Caller.Editor, HttpStatusCode.OK)]
    [InlineData(Operation.Edit, Caller.Viewer, HttpStatusCode.Forbidden)]
    [InlineData(Operation.Edit, Caller.Stranger, HttpStatusCode.NotFound)]
    [InlineData(Operation.Edit, Caller.MissingNote, HttpStatusCode.NotFound)]
    [InlineData(Operation.Delete, Caller.Owner, HttpStatusCode.NoContent)]
    [InlineData(Operation.Delete, Caller.Editor, HttpStatusCode.Forbidden)]
    [InlineData(Operation.Delete, Caller.Viewer, HttpStatusCode.Forbidden)]
    [InlineData(Operation.Delete, Caller.Stranger, HttpStatusCode.NotFound)]
    [InlineData(Operation.Delete, Caller.MissingNote, HttpStatusCode.NotFound)]
    public async Task NoteOperation_CallerRole_FollowsMatrix(Operation operation, Caller caller, HttpStatusCode expected)
    {
        var (client, noteId, owner) = await ArrangeCallerAsync(caller);
        var before = await LoadNoteAsync(noteId);

        var response = await SendAsync(client, operation, noteId);

        Assert.Equal(expected, response.StatusCode);
        if (expected is HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
        {
            var body = await ReadJsonAsync(response);
            Assert.Equal((int)expected, body["status"]!.GetValue<int>());
            Assert.Equal(expected == HttpStatusCode.Forbidden
                    ? NoteAccessService.ForbiddenMessage
                    : NoteAccessService.NoteNotFoundMessage,
                body["detail"]!.GetValue<string>());
            if (before is not null) await AssertNoteUnchangedAsync(before);
            return;
        }

        switch (operation)
        {
            case Operation.Read:
                var detail = (await response.Content.ReadFromJsonAsync<NoteDetail>(Json))!;
                Assert.Equal(ExpectedRole(caller), detail.Role);
                Assert.Equal(owner.UserName, detail.OwnerUserName);
                Assert.Equal(before!.Content, detail.Content);
                break;
            case Operation.Edit:
                var updated = (await response.Content.ReadFromJsonAsync<NoteDetail>(Json))!;
                Assert.Equal(ExpectedRole(caller), updated.Role);
                Assert.Equal("Módosítva", (await LoadNoteAsync(noteId))!.Title);
                break;
            case Operation.Delete:
                Assert.Null(await LoadNoteAsync(noteId));
                break;
        }
    }

    // AC6, V1: a stranger cannot tell a foreign note from a missing one.
    [Theory]
    [InlineData(Operation.Read)]
    [InlineData(Operation.Edit)]
    [InlineData(Operation.Delete)]
    public async Task NoteOperation_ForeignVsMissingNote_IdenticalNotFoundBody(Operation operation)
    {
        var owner = await AuthHelper.CreateUserAsync(factory);
        var stranger = await AuthHelper.CreateUserAsync(factory);
        var note = await CreateNoteAsync(owner, "Titkos", "");

        var foreign = await SendAsync(stranger.Client, operation, note.Id);
        var missing = await SendAsync(stranger.Client, operation, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        var foreignBody = await ReadJsonAsync(foreign);
        var missingBody = await ReadJsonAsync(missing);
        // traceId differs per request by nature; everything else must be identical.
        foreignBody.Remove("traceId");
        missingBody.Remove("traceId");
        Assert.Equal(missingBody.ToJsonString(), foreignBody.ToJsonString());
    }

    // AC6: the role is read from the database on every request, not cached.
    [Fact]
    public async Task GetNote_ShareRemovedFromDatabase_Returns404OnNextRequest()
    {
        var owner = await AuthHelper.CreateUserAsync(factory);
        var recipient = await AuthHelper.CreateUserAsync(factory);
        var note = await CreateNoteAsync(owner, "Megosztott", "");
        await AddShareAsync(note.Id, recipient.Id, ShareRole.Viewer);
        Assert.Equal(HttpStatusCode.OK, (await recipient.Client.GetAsync($"/api/notes/{note.Id}")).StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.NoteShares.Where(s => s.NoteId == note.Id).ExecuteDeleteAsync();
        }

        Assert.Equal(HttpStatusCode.NotFound, (await recipient.Client.GetAsync($"/api/notes/{note.Id}")).StatusCode);
    }

    // ---------- Delete ----------

    [Fact]
    public async Task DeleteNote_Owner_Returns204ThenNoteIs404AndSharesRemoved()
    {
        var owner = await AuthHelper.CreateUserAsync(factory);
        var recipient = await AuthHelper.CreateUserAsync(factory);
        var note = await CreateNoteAsync(owner, "Törlendő", "");
        await AddShareAsync(note.Id, recipient.Id, ShareRole.Editor);

        var response = await owner.Client.DeleteAsync($"/api/notes/{note.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.Client.GetAsync($"/api/notes/{note.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await recipient.Client.GetAsync($"/api/notes/{note.Id}")).StatusCode);
        Assert.Empty(await ListAsync(owner.Client));
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.NoteShares.AnyAsync(s => s.NoteId == note.Id));
    }

    [Fact]
    public async Task NotesEndpoints_NoToken_Return401()
    {
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/notes")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/notes", new NoteWriteRequest("Cím", ""))).StatusCode);
    }

    // ---------- helpers ----------

    private static NoteRole ExpectedRole(Caller caller) => caller switch
    {
        Caller.Owner => NoteRole.Owner,
        Caller.Editor => NoteRole.Editor,
        Caller.Viewer => NoteRole.Viewer,
        _ => NoteRole.None,
    };

    /// <summary>Client of the calling user, the target note id and the note's owner.</summary>
    private async Task<(HttpClient Client, Guid NoteId, AuthenticatedUser Owner)> ArrangeCallerAsync(Caller caller)
    {
        var owner = await AuthHelper.CreateUserAsync(factory);
        var note = await CreateNoteAsync(owner, "Eredeti", "eredeti szöveg");
        switch (caller)
        {
            case Caller.Owner:
                return (owner.Client, note.Id, owner);
            case Caller.Editor:
            case Caller.Viewer:
                var recipient = await AuthHelper.CreateUserAsync(factory);
                await AddShareAsync(note.Id, recipient.Id, caller == Caller.Editor ? ShareRole.Editor : ShareRole.Viewer);
                return (recipient.Client, note.Id, owner);
            case Caller.Stranger:
                return ((await AuthHelper.CreateUserAsync(factory)).Client, note.Id, owner);
            default:
                return (owner.Client, Guid.NewGuid(), owner);
        }
    }

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, Operation operation, Guid noteId) => operation switch
    {
        Operation.Read => client.GetAsync($"/api/notes/{noteId}"),
        Operation.Edit => client.PutAsJsonAsync($"/api/notes/{noteId}", new NoteWriteRequest("Módosítva", "új szöveg")),
        _ => client.DeleteAsync($"/api/notes/{noteId}"),
    };

    private static async Task<NoteDetail> CreateNoteAsync(AuthenticatedUser user, string title, string content)
    {
        var response = await user.Client.PostAsJsonAsync("/api/notes", new NoteWriteRequest(title, content));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<NoteDetail>(Json))!;
    }

    private static async Task<List<NoteSummary>> ListAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<List<NoteSummary>>("/api/notes", Json))!;

    private async Task<HttpClient> LoginAsync(AuthenticatedUser user)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new AuthRequest(user.UserName, user.Password));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new("Bearer", auth.Token);
        return client;
    }

    private async Task AddShareAsync(Guid noteId, Guid userId, ShareRole role)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.NoteShares.Add(new NoteShare { Id = Guid.NewGuid(), NoteId = noteId, UserId = userId, Role = role });
        await db.SaveChangesAsync();
    }

    private async Task<Note?> LoadNoteAsync(Guid noteId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Notes.AsNoTracking().SingleOrDefaultAsync(n => n.Id == noteId);
    }

    private async Task<int> CountNotesAsync(Guid ownerId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Notes.CountAsync(n => n.OwnerId == ownerId);
    }

    private async Task AssertNoteUnchangedAsync(Note before)
    {
        var after = await LoadNoteAsync(before.Id);
        Assert.NotNull(after);
        Assert.Equal(before.Title, after.Title);
        Assert.Equal(before.Content, after.Content);
        Assert.Equal(before.UpdatedAt, after.UpdatedAt);
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
