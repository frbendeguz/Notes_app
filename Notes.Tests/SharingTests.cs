using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Notes.Api.Data;
using Notes.Api.Data.Entities;
using Notes.Api.Dtos;
using Notes.Api.Services;

namespace Notes.Tests;

/// <summary>
/// Integration tests of sharing, revoking and the "shared with me" list (task 05, design 4, 5; V1, V4, V10, V18, V19).
/// </summary>
public sealed class SharingTests(TestApiFactory factory) : IClassFixture<TestApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public enum Caller { Editor, Viewer, Stranger, MissingNote }

    public enum ShareEndpoint { List, Create, Delete }

    // ---------- Share ----------

    // AC5
    [Theory]
    [InlineData("Viewer", ShareRole.Viewer, NoteRole.Viewer)]
    [InlineData("Editor", ShareRole.Editor, NoteRole.Editor)]
    public async Task ShareNote_ExistingUser_Returns201AndRecipientSeesItInSharedList(
        string role, ShareRole expectedShareRole, NoteRole expectedNoteRole)
    {
        var owner = await AuthHelper.CreateUserAsync(factory);
        var recipient = await AuthHelper.CreateUserAsync(factory);
        var note = await CreateNoteAsync(owner, "Megosztott", "tartalom");

        var response = await ShareAsync(owner.Client, note.Id, recipient.UserName, role);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var share = (await response.Content.ReadFromJsonAsync<ShareInfo>(Json))!;
        Assert.Equal(new ShareInfo(recipient.Id, recipient.UserName, expectedShareRole), share);
        Assert.Equal($"/api/notes/{note.Id}/shares/{recipient.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal(role, (await ReadJsonAsync(response))["role"]!.GetValue<string>());

        var summary = Assert.Single(await ListSharedAsync(recipient.Client));
        Assert.Equal(note.Id, summary.Id);
        Assert.Equal("Megosztott", summary.Title);
        Assert.Equal(expectedNoteRole, summary.Role);
        Assert.Equal(owner.UserName, summary.OwnerUserName);
        Assert.Null(summary.FolderId);

        var detail = await recipient.Client.GetFromJsonAsync<NoteDetail>($"/api/notes/{note.Id}", Json);
        Assert.Equal("tartalom", detail!.Content);
        Assert.Equal(expectedNoteRole, detail.Role);
    }

    // AC5: the recipient is found by the normalized user name; the role is case-insensitive (developer decision).
    [Theory]
    [InlineData("viewer", ShareRole.Viewer)]
    [InlineData("EDITOR", ShareRole.Editor)]
    public async Task ShareNote_UserNameInOtherCaseWithSpacesAndLowerCaseRole_Returns201(string role, ShareRole expected)
    {
        var owner = await AuthHelper.CreateUserAsync(factory);
        var recipient = await AuthHelper.CreateUserAsync(factory);
        var note = await CreateNoteAsync(owner, "Megosztott", "");

        var response = await ShareAsync(owner.Client, note.Id, "  " + recipient.UserName.ToUpperInvariant() + " ", role);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var share = (await response.Content.ReadFromJsonAsync<ShareInfo>(Json))!;
        Assert.Equal(recipient.Id, share.UserId);
        Assert.Equal(recipient.UserName, share.UserName);
        Assert.Equal(expected, share.Role);
        Assert.Equal(1, await CountSharesAsync(note.Id));
    }

    // AC5
    [Theory]
    [InlineData("nincsilyenfelhasznalo")]
    [InlineData("a!")] // impossible user name: not validated, simply not found (developer decision)
    public async Task ShareNote_UnknownUser_Returns404AndNoShareCreated(string userName)
    {
        var owner = await AuthHelper.CreateUserAsync(factory);
        var note = await CreateNoteAsync(owner, "Megosztott", "");

        var response = await ShareAsync(owner.Client, note.Id, userName + Guid.NewGuid().ToString("N")[..8], "Viewer");

        await AssertProblemAsync(response, HttpStatusCode.NotFound, ShareService.RecipientNotFound);
        Assert.Equal(0, await CountSharesAsync(note.Id));
    }

    // AC5, V10
    [Fact]
    public async Task ShareNote_WithSelfInOtherCase_Returns400AndNoShareCreated()
    {
        var owner = await AuthHelper.CreateUserAsync(factory);
        var note = await CreateNoteAsync(owner, "Megosztott", "");

        var response = await ShareAsync(owner.Client, note.Id, owner.UserName.ToUpperInvariant(), "Editor");

        await AssertValidationErrorAsync(response, "userName", ShareService.SelfShare);
        Assert.Equal(0, await CountSharesAsync(note.Id));
    }

    // AC5, V10, V4: a repeated share is rejected even with another role (no role change, no duplicate).
    [Theory]
    [InlineData("Viewer")]
    [InlineData("Editor")]
    public async Task ShareNote_Repeated_Returns409AndShareCountUnchanged(string secondRole)
    {
        var owner = await AuthHelper.CreateUserAsync(factory);
        var recipient = await AuthHelper.CreateUserAsync(factory);
        var note = await CreateNoteAsync(owner, "Megosztott", "");
        Assert.Equal(HttpStatusCode.Created, (await ShareAsync(owner.Client, note.Id, recipient.UserName, "Viewer")).StatusCode);

        var response = await ShareAsync(owner.Client, note.Id, recipient.UserName, secondRole);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, ShareService.AlreadyShared);
        Assert.Equal(1, await CountSharesAsync(note.Id));
        var share = Assert.Single(await ListSharesAsync(owner.Client, note.Id));
        Assert.Equal(ShareRole.Viewer, share.Role);
    }

    // AC5
    [Theory]
    [InlineData("Owner")]
    [InlineData("Admin")]
    [InlineData("1")]
    [InlineData("")]
    [InlineData(null)]
    public async Task ShareNote_InvalidRole_Returns400AndNoShareCreated(string? role)
    {
        var owner = await AuthHelper.CreateUserAsync(factory);
        var recipient = await AuthHelper.CreateUserAsync(factory);
        var note = await CreateNoteAsync(owner, "Megosztott", "");

        var response = await ShareAsync(owner.Client, note.Id, recipient.UserName, role);

        await AssertValidationErrorAsync(response, "role", ShareService.RoleInvalid);
        Assert.Equal(0, await CountSharesAsync(note.Id));
    }

    [Fact]
    public async Task ShareNote_NumericRole_Returns400AndNoShareCreated()
    {
        var owner = await AuthHelper.CreateUserAsync(factory);
        var recipient = await AuthHelper.CreateUserAsync(factory);
        var note = await CreateNoteAsync(owner, "Megosztott", "");

        var response = await owner.Client.PostAsJsonAsync($"/api/notes/{note.Id}/shares",
            new { userName = recipient.UserName, role = 1 });

        // Model binding fails before the service: the generic format error (Program.cs), still a ProblemDetails 400.
        await AssertValidationErrorAsync(response, "$.role", "A kérés formátuma érvénytelen.");
        Assert.Equal(0, await CountSharesAsync(note.Id));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task ShareNote_MissingUserName_Returns400AndNoShareCreated(string? userName)
    {
        var owner = await AuthHelper.CreateUserAsync(factory);
        var note = await CreateNoteAsync(owner, "Megosztott", "");

        var response = await ShareAsync(owner.Client, note.Id, userName, "Viewer");

        await AssertValidationErrorAsync(response, "userName", ShareService.UserNameRequired);
        Assert.Equal(0, await CountSharesAsync(note.Id));
    }

    // The no-duplicate rule is per note, not global.
    [Fact]
    public async Task ShareNote_SameRecipientOnAnotherNote_Returns201()
    {
        var owner = await AuthHelper.CreateUserAsync(factory);
        var recipient = await AuthHelper.CreateUserAsync(factory);
        var first = await CreateNoteAsync(owner, "Első", "");
        var second = await CreateNoteAsync(owner, "Második", "");
        Assert.Equal(HttpStatusCode.Created, (await ShareAsync(owner.Client, first.Id, recipient.UserName, "Viewer")).StatusCode);

        var response = await ShareAsync(owner.Client, second.Id, recipient.UserName, "Editor");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var shared = await ListSharedAsync(recipient.Client);
        Assert.Equal(2, shared.Count);
        Assert.Equal(NoteRole.Viewer, shared.Single(s => s.Id == first.Id).Role);
        Assert.Equal(NoteRole.Editor, shared.Single(s => s.Id == second.Id).Role);
    }

    // ---------- List shares ----------

    [Fact]
    public async Task ListShares_Owner_ReturnsAllSharesOrderedByUserName()
    {
        var owner = await AuthHelper.CreateUserAsync(factory);
        var prefix = AuthHelper.UniqueUserName();
        var b = await AuthHelper.CreateUserAsync(factory, prefix + "B");
        var a = await AuthHelper.CreateUserAsync(factory, prefix + "a");
        var note = await CreateNoteAsync(owner, "Megosztott", "");
        await ShareAsync(owner.Client, note.Id, b.UserName, "Editor");
        await ShareAsync(owner.Client, note.Id, a.UserName, "Viewer");

        var shares = await ListSharesAsync(owner.Client, note.Id);

        Assert.Equal(
            [new ShareInfo(a.Id, a.UserName, ShareRole.Viewer), new ShareInfo(b.Id, b.UserName, ShareRole.Editor)],
            shares);
    }

    [Fact]
    public async Task ListShares_NoShares_ReturnsEmptyList()
    {
        var owner = await AuthHelper.CreateUserAsync(factory);
        var note = await CreateNoteAsync(owner, "Nincs megosztva", "");

        Assert.Empty(await ListSharesAsync(owner.Client, note.Id));
    }

    // ---------- Access matrix on the share endpoints ----------

    // AC6: editor and viewer get 403, a stranger and a missing note 404 (V1); nothing changes.
    [Theory]
    [InlineData(Caller.Editor, ShareEndpoint.List, HttpStatusCode.Forbidden)]
    [InlineData(Caller.Editor, ShareEndpoint.Create, HttpStatusCode.Forbidden)]
    [InlineData(Caller.Editor, ShareEndpoint.Delete, HttpStatusCode.Forbidden)]
    [InlineData(Caller.Viewer, ShareEndpoint.List, HttpStatusCode.Forbidden)]
    [InlineData(Caller.Viewer, ShareEndpoint.Create, HttpStatusCode.Forbidden)]
    [InlineData(Caller.Viewer, ShareEndpoint.Delete, HttpStatusCode.Forbidden)]
    [InlineData(Caller.Stranger, ShareEndpoint.List, HttpStatusCode.NotFound)]
    [InlineData(Caller.Stranger, ShareEndpoint.Create, HttpStatusCode.NotFound)]
    [InlineData(Caller.Stranger, ShareEndpoint.Delete, HttpStatusCode.NotFound)]
    [InlineData(Caller.MissingNote, ShareEndpoint.List, HttpStatusCode.NotFound)]
    [InlineData(Caller.MissingNote, ShareEndpoint.Create, HttpStatusCode.NotFound)]
    [InlineData(Caller.MissingNote, ShareEndpoint.Delete, HttpStatusCode.NotFound)]
    public async Task ShareEndpoints_NotOwner_RejectedAndSharesUnchanged(Caller caller, ShareEndpoint endpoint, HttpStatusCode expected)
    {
        var owner = await AuthHelper.CreateUserAsync(factory);
        var existing = await AuthHelper.CreateUserAsync(factory);
        var target = await AuthHelper.CreateUserAsync(factory);
        var note = await CreateNoteAsync(owner, "Megosztott", "");
        await ShareAsync(owner.Client, note.Id, existing.UserName, "Viewer");
        var (client, noteId) = await ArrangeCallerAsync(caller, owner, note.Id);
        var sharesBefore = await CountSharesAsync(note.Id);

        var response = endpoint switch
        {
            ShareEndpoint.List => await client.GetAsync($"/api/notes/{noteId}/shares"),
            ShareEndpoint.Create => await ShareAsync(client, noteId, target.UserName, "Editor"),
            _ => await client.DeleteAsync($"/api/notes/{noteId}/shares/{existing.Id}"),
        };

        await AssertProblemAsync(response, expected,
            expected == HttpStatusCode.NotFound ? NoteAccessService.NoteNotFoundMessage : NoteAccessService.ForbiddenMessage);
        Assert.Equal(sharesBefore, await CountSharesAsync(note.Id));
        Assert.Contains(existing.Id, (await ListSharesAsync(owner.Client, note.Id)).Select(s => s.UserId));
    }

    // AC6: a non-owner does not learn whether a user name exists – the unknown name also gets 403/404, not the recipient 404.
    [Theory]
    [InlineData(Caller.Editor, HttpStatusCode.Forbidden)]
    [InlineData(Caller.Stranger, HttpStatusCode.NotFound)]
    public async Task ShareNote_NotOwnerWithUnknownUser_GetsAccessErrorNotRecipientError(Caller caller, HttpStatusCode expected)
    {
        var owner = await AuthHelper.CreateUserAsync(factory);
        var note = await CreateNoteAsync(owner, "Megosztott", "");
        var (client, noteId) = await ArrangeCallerAsync(caller, owner, note.Id);
        var sharesBefore = await CountSharesAsync(note.Id);

        var response = await ShareAsync(client, noteId, "nincs" + Guid.NewGuid().ToString("N")[..12], "Viewer");

        await AssertProblemAsync(response, expected,
            expected == HttpStatusCode.NotFound ? NoteAccessService.NoteNotFoundMessage : NoteAccessService.ForbiddenMessage);
        Assert.Equal(sharesBefore, await CountSharesAsync(note.Id));
    }

    // V18: validation runs before the access check.
    [Fact]
    public async Task ShareNote_ViewerWithInvalidRole_Returns400()
    {
        var owner = await AuthHelper.CreateUserAsync(factory);
        var note = await CreateNoteAsync(owner, "Megosztott", "");
        var (client, noteId) = await ArrangeCallerAsync(Caller.Viewer, owner, note.Id);

        var sharesBefore = await CountSharesAsync(note.Id);

        var response = await ShareAsync(client, noteId, owner.UserName, "Owner");

        await AssertValidationErrorAsync(response, "role", ShareService.RoleInvalid);
        Assert.Equal(sharesBefore, await CountSharesAsync(note.Id));
    }

    // ---------- Access matrix on the note itself via real shares ----------

    // AC6
    [Fact]
    public async Task Editor_UpdatesNote_Returns200ButDeleteIs403AndNoteKept()
    {
        var owner = await AuthHelper.CreateUserAsync(factory);
        var note = await CreateNoteAsync(owner, "Eredeti", "eredeti");
        var (editor, _) = await ArrangeCallerAsync(Caller.Editor, owner, note.Id);

        var update = await editor.PutAsJsonAsync($"/api/notes/{note.Id}", new NoteWriteRequest("Szerkesztve", "új"));
        var delete = await editor.DeleteAsync($"/api/notes/{note.Id}");

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = (await update.Content.ReadFromJsonAsync<NoteDetail>(Json))!;
        Assert.Equal(NoteRole.Editor, updated.Role);
        await AssertProblemAsync(delete, HttpStatusCode.Forbidden, NoteAccessService.ForbiddenMessage);
        var stored = await LoadNoteAsync(note.Id);
        Assert.NotNull(stored);
        Assert.Equal("Szerkesztve", stored.Title);
        Assert.Equal("új", stored.Content);
    }

    // AC6
    [Fact]
    public async Task Viewer_UpdateAndDelete_Return403AndNoteUnchanged()
    {
        var owner = await AuthHelper.CreateUserAsync(factory);
        var note = await CreateNoteAsync(owner, "Eredeti", "eredeti");
        var (viewer, _) = await ArrangeCallerAsync(Caller.Viewer, owner, note.Id);

        var update = await viewer.PutAsJsonAsync($"/api/notes/{note.Id}", new NoteWriteRequest("Olvasó írta", "x"));
        var delete = await viewer.DeleteAsync($"/api/notes/{note.Id}");

        await AssertProblemAsync(update, HttpStatusCode.Forbidden, NoteAccessService.ForbiddenMessage);
        await AssertProblemAsync(delete, HttpStatusCode.Forbidden, NoteAccessService.ForbiddenMessage);
        var stored = await LoadNoteAsync(note.Id);
        Assert.NotNull(stored);
        Assert.Equal("Eredeti", stored.Title);
        Assert.Equal("eredeti", stored.Content);
        Assert.Equal(note.UpdatedAt, stored.UpdatedAt);
    }

    // AC6
    [Fact]
    public async Task Stranger_NoteAndSharedList_Returns404AndNotListed()
    {
        var owner = await AuthHelper.CreateUserAsync(factory);
        var recipient = await AuthHelper.CreateUserAsync(factory);
        var stranger = await AuthHelper.CreateUserAsync(factory);
        var note = await CreateNoteAsync(owner, "Megosztott", "");
        await ShareAsync(owner.Client, note.Id, recipient.UserName, "Editor");

        await AssertProblemAsync(await stranger.Client.GetAsync($"/api/notes/{note.Id}"),
            HttpStatusCode.NotFound, NoteAccessService.NoteNotFoundMessage);
        Assert.Empty(await ListSharedAsync(stranger.Client));
    }

    // ---------- Revoke ----------

    // AC7
    [Theory]
    [InlineData("Viewer")]
    [InlineData("Editor")]
    public async Task RevokeShare_Owner_Returns204RecipientLosesAccessOwnerKeepsNote(string role)
    {
        var owner = await AuthHelper.CreateUserAsync(factory);
        var recipient = await AuthHelper.CreateUserAsync(factory);
        var note = await CreateNoteAsync(owner, "Megosztott", "tartalom");
        await ShareAsync(owner.Client, note.Id, recipient.UserName, role);
        Assert.Equal(HttpStatusCode.OK, (await recipient.Client.GetAsync($"/api/notes/{note.Id}")).StatusCode);

        var response = await owner.Client.DeleteAsync($"/api/notes/{note.Id}/shares/{recipient.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        await AssertProblemAsync(await recipient.Client.GetAsync($"/api/notes/{note.Id}"),
            HttpStatusCode.NotFound, NoteAccessService.NoteNotFoundMessage);
        Assert.Equal(HttpStatusCode.NotFound,
            (await recipient.Client.PutAsJsonAsync($"/api/notes/{note.Id}", new NoteWriteRequest("x", "y"))).StatusCode);
        Assert.Empty(await ListSharedAsync(recipient.Client));
        Assert.Empty(await ListSharesAsync(owner.Client, note.Id));

        var kept = await owner.Client.GetFromJsonAsync<NoteDetail>($"/api/notes/{note.Id}", Json);
        Assert.Equal("tartalom", kept!.Content);
        var ownList = await owner.Client.GetFromJsonAsync<List<NoteSummary>>("/api/notes", Json);
        Assert.Contains(note.Id, ownList!.Select(n => n.Id));
    }

    // AC7: only the given recipient's share is removed.
    [Fact]
    public async Task RevokeShare_OneOfTwoRecipients_OtherKeepsAccess()
    {
        var owner = await AuthHelper.CreateUserAsync(factory);
        var revoked = await AuthHelper.CreateUserAsync(factory);
        var kept = await AuthHelper.CreateUserAsync(factory);
        var note = await CreateNoteAsync(owner, "Megosztott", "");
        await ShareAsync(owner.Client, note.Id, revoked.UserName, "Viewer");
        await ShareAsync(owner.Client, note.Id, kept.UserName, "Viewer");

        await owner.Client.DeleteAsync($"/api/notes/{note.Id}/shares/{revoked.Id}");

        Assert.Equal(HttpStatusCode.OK, (await kept.Client.GetAsync($"/api/notes/{note.Id}")).StatusCode);
        Assert.Single(await ListSharedAsync(kept.Client));
    }

    [Fact]
    public async Task RevokeShare_NotShared_Returns404()
    {
        var owner = await AuthHelper.CreateUserAsync(factory);
        var other = await AuthHelper.CreateUserAsync(factory);
        var note = await CreateNoteAsync(owner, "Megosztott", "");

        await AssertProblemAsync(await owner.Client.DeleteAsync($"/api/notes/{note.Id}/shares/{other.Id}"),
            HttpStatusCode.NotFound, ShareService.ShareNotFound);
        await AssertProblemAsync(await owner.Client.DeleteAsync($"/api/notes/{note.Id}/shares/{Guid.NewGuid()}"),
            HttpStatusCode.NotFound, ShareService.ShareNotFound);
    }

    // V4: no role change – revoke and share again with the new role.
    [Fact]
    public async Task RevokeShare_ThenShareAgainWithOtherRole_RecipientGetsNewRole()
    {
        var owner = await AuthHelper.CreateUserAsync(factory);
        var recipient = await AuthHelper.CreateUserAsync(factory);
        var note = await CreateNoteAsync(owner, "Megosztott", "");
        await ShareAsync(owner.Client, note.Id, recipient.UserName, "Viewer");

        await owner.Client.DeleteAsync($"/api/notes/{note.Id}/shares/{recipient.Id}");
        var response = await ShareAsync(owner.Client, note.Id, recipient.UserName, "Editor");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(NoteRole.Editor, Assert.Single(await ListSharedAsync(recipient.Client)).Role);
        Assert.Equal(1, await CountSharesAsync(note.Id));
    }

    // ---------- Shared with me list ----------

    // AC1: own, foreign and shared notes are listed separately.
    [Fact]
    public async Task ListShared_OwnForeignAndSharedNotes_ReturnsOnlySharedOrderedByUpdatedAtDesc()
    {
        var me = await AuthHelper.CreateUserAsync(factory);
        var other = await AuthHelper.CreateUserAsync(factory);
        await CreateNoteAsync(me, "Saját", "");
        await CreateNoteAsync(other, "Nem megosztott", "");
        var older = await CreateNoteAsync(other, "Régebbi", "");
        var newer = await CreateNoteAsync(other, "Újabb", "");
        await ShareAsync(other.Client, newer.Id, me.UserName, "Viewer");
        await ShareAsync(other.Client, older.Id, me.UserName, "Editor");
        // Editing makes "Régebbi" the most recently updated one.
        Assert.Equal(HttpStatusCode.OK,
            (await other.Client.PutAsJsonAsync($"/api/notes/{older.Id}", new NoteWriteRequest("Régebbi", "frissítve"))).StatusCode);

        var shared = await ListSharedAsync(me.Client);

        Assert.Equal([older.Id, newer.Id], shared.Select(s => s.Id));
        Assert.Equal([NoteRole.Editor, NoteRole.Viewer], shared.Select(s => s.Role));
        Assert.All(shared, s => Assert.Equal(other.UserName, s.OwnerUserName));
        var own = await me.Client.GetFromJsonAsync<List<NoteSummary>>("/api/notes", Json);
        Assert.Equal(["Saját"], own!.Select(n => n.Title));
    }

    [Fact]
    public async Task ListShared_NoToken_Returns401()
    {
        var response = await factory.CreateClient().GetAsync("/api/notes/shared");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ShareEndpoints_NoToken_Return401()
    {
        var client = factory.CreateClient();
        var noteId = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/notes/{noteId}/shares")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await ShareAsync(client, noteId, "valaki", "Viewer")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.DeleteAsync($"/api/notes/{noteId}/shares/{Guid.NewGuid()}")).StatusCode);
    }

    // ---------- Folders stay private ----------

    // V24: the editor learns nothing about the owner's folder.
    [Fact]
    public async Task Editor_NoteInOwnersFolder_FolderIdNotLeaked()
    {
        var owner = await AuthHelper.CreateUserAsync(factory);
        var editor = await AuthHelper.CreateUserAsync(factory);
        var folderResponse = await owner.Client.PostAsJsonAsync("/api/folders", new FolderCreateRequest("Titkos mappa"));
        var folder = (await folderResponse.Content.ReadFromJsonAsync<FolderSummary>(Json))!;
        var created = await owner.Client.PostAsJsonAsync("/api/notes", new NoteWriteRequest("Mappában", "", folder.Id));
        var note = (await created.Content.ReadFromJsonAsync<NoteDetail>(Json))!;
        Assert.Equal(folder.Id, note.FolderId);
        await ShareAsync(owner.Client, note.Id, editor.UserName, "Editor");

        var sharedJson = JsonNode.Parse(await editor.Client.GetStringAsync("/api/notes/shared"))!.AsArray();
        var detailJson = JsonNode.Parse(await editor.Client.GetStringAsync($"/api/notes/{note.Id}"))!.AsObject();
        var update = await editor.Client.PutAsJsonAsync($"/api/notes/{note.Id}", new NoteWriteRequest("Szerkesztve", ""));
        var filter = await editor.Client.GetAsync($"/api/notes?folderId={folder.Id}");
        var editorFolders = await editor.Client.GetFromJsonAsync<List<FolderSummary>>("/api/folders", Json);

        var item = Assert.Single(sharedJson)!.AsObject();
        Assert.True(item.ContainsKey("folderId"));
        Assert.Null(item["folderId"]);
        Assert.True(detailJson.ContainsKey("folderId"));
        Assert.Null(detailJson["folderId"]);
        Assert.Null((await update.Content.ReadFromJsonAsync<NoteDetail>(Json))!.FolderId);
        await AssertProblemAsync(filter, HttpStatusCode.NotFound, NoteAccessService.FolderNotFoundMessage);
        Assert.DoesNotContain(folder.Id, editorFolders!.Select(f => f.Id));
        // The editor's save does not move the note out of the owner's folder.
        Assert.Equal(folder.Id, (await LoadNoteAsync(note.Id))!.FolderId);
    }

    // Regression (task 09): protects folder deletion (task 04, D4 SetNull) together with sharing (task 05)
    // against later changes (e.g. the change request).
    // Deleting the owner's folder must not remove the note or its shares: the recipient keeps access.
    [Fact]
    public async Task DeleteFolder_SharedNoteInFolder_RecipientKeepsAccess()
    {
        var owner = await AuthHelper.CreateUserAsync(factory);
        var editor = await AuthHelper.CreateUserAsync(factory);
        var folderResponse = await owner.Client.PostAsJsonAsync("/api/folders", new FolderCreateRequest("Törlendő mappa"));
        Assert.Equal(HttpStatusCode.Created, folderResponse.StatusCode);
        var folder = (await folderResponse.Content.ReadFromJsonAsync<FolderSummary>(Json))!;
        var created = await owner.Client.PostAsJsonAsync("/api/notes", new NoteWriteRequest("Mappában", "eredeti", folder.Id));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var note = (await created.Content.ReadFromJsonAsync<NoteDetail>(Json))!;
        Assert.Equal(folder.Id, note.FolderId);
        Assert.Equal(HttpStatusCode.Created, (await ShareAsync(owner.Client, note.Id, editor.UserName, "Editor")).StatusCode);

        var delete = await owner.Client.DeleteAsync($"/api/folders/{folder.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        var shared = Assert.Single(await ListSharedAsync(editor.Client));
        Assert.Equal((note.Id, NoteRole.Editor), (shared.Id, shared.Role));
        var update = await editor.Client.PutAsJsonAsync($"/api/notes/{note.Id}", new NoteWriteRequest("Mappa nélkül", "szerkesztve"));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal([new ShareInfo(editor.Id, editor.UserName, ShareRole.Editor)], await ListSharesAsync(owner.Client, note.Id));
        var ownerView = (await owner.Client.GetFromJsonAsync<NoteDetail>($"/api/notes/{note.Id}", Json))!;
        Assert.Equal(("Mappa nélkül", "szerkesztve"), (ownerView.Title, ownerView.Content));
        Assert.Null(ownerView.FolderId);
        var unfiled = await owner.Client.GetFromJsonAsync<List<NoteSummary>>("/api/notes?unfiled=true", Json);
        Assert.Contains(note.Id, unfiled!.Select(n => n.Id));
    }

    // ---------- Concurrent edits ----------

    // Spec: "last write wins" – two PUTs one after the other, the second one is stored.
    [Fact]
    public async Task UpdateNote_OwnerThenEditor_LastWriteWins()
    {
        var owner = await AuthHelper.CreateUserAsync(factory);
        var note = await CreateNoteAsync(owner, "Eredeti", "eredeti");
        var (editor, _) = await ArrangeCallerAsync(Caller.Editor, owner, note.Id);

        var first = await owner.Client.PutAsJsonAsync($"/api/notes/{note.Id}", new NoteWriteRequest("Tulajdonosé", "első"));
        var second = await editor.PutAsJsonAsync($"/api/notes/{note.Id}", new NoteWriteRequest("Szerkesztőé", "második"));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var ownerView = await owner.Client.GetFromJsonAsync<NoteDetail>($"/api/notes/{note.Id}", Json);
        var editorView = await editor.GetFromJsonAsync<NoteDetail>($"/api/notes/{note.Id}", Json);
        Assert.Equal(("Szerkesztőé", "második"), (ownerView!.Title, ownerView.Content));
        Assert.Equal(("Szerkesztőé", "második"), (editorView!.Title, editorView.Content));
    }

    // ---------- helpers ----------

    /// <summary>Client of the calling user and the note id to target; editor/viewer get a real share via the API.</summary>
    private async Task<(HttpClient Client, Guid NoteId)> ArrangeCallerAsync(Caller caller, AuthenticatedUser owner, Guid noteId)
    {
        switch (caller)
        {
            case Caller.Editor:
            case Caller.Viewer:
                var recipient = await AuthHelper.CreateUserAsync(factory);
                var response = await ShareAsync(owner.Client, noteId, recipient.UserName, caller.ToString());
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                return (recipient.Client, noteId);
            case Caller.Stranger:
                return ((await AuthHelper.CreateUserAsync(factory)).Client, noteId);
            default:
                return (owner.Client, Guid.NewGuid());
        }
    }

    private static Task<HttpResponseMessage> ShareAsync(HttpClient client, Guid noteId, string? userName, string? role) =>
        client.PostAsJsonAsync($"/api/notes/{noteId}/shares", new ShareCreateRequest(userName, role));

    private static async Task<NoteDetail> CreateNoteAsync(AuthenticatedUser user, string title, string content)
    {
        var response = await user.Client.PostAsJsonAsync("/api/notes", new NoteWriteRequest(title, content));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<NoteDetail>(Json))!;
    }

    private static async Task<List<NoteSummary>> ListSharedAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<List<NoteSummary>>("/api/notes/shared", Json))!;

    private static async Task<List<ShareInfo>> ListSharesAsync(HttpClient client, Guid noteId) =>
        (await client.GetFromJsonAsync<List<ShareInfo>>($"/api/notes/{noteId}/shares", Json))!;

    private async Task<int> CountSharesAsync(Guid noteId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.NoteShares.CountAsync(s => s.NoteId == noteId);
    }

    private async Task<Note?> LoadNoteAsync(Guid noteId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Notes.AsNoTracking().SingleOrDefaultAsync(n => n.Id == noteId);
    }

    private static async Task<JsonObject> ReadJsonAsync(HttpResponseMessage response) =>
        JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string detail)
    {
        Assert.Equal(status, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal((int)status, body["status"]!.GetValue<int>());
        Assert.Equal(detail, body["detail"]?.GetValue<string>());
    }

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
