namespace Notes.Api.Data.Entities;

public class Note
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public Guid? FolderId { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User Owner { get; set; } = null!;
    public Folder? Folder { get; set; }
    public List<NoteShare> Shares { get; set; } = [];
}
