namespace Notes.Api.Data.Entities;

public class User
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = "";
    public string NormalizedUserName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateTime CreatedAt { get; set; }

    public List<Folder> Folders { get; set; } = [];
    public List<Note> OwnedNotes { get; set; } = [];
    public List<NoteShare> SharesReceived { get; set; } = [];
}
