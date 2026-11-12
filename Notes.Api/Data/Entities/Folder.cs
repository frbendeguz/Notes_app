namespace Notes.Api.Data.Entities;

public class Folder
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = "";
    public string NormalizedName { get; set; } = "";

    public User Owner { get; set; } = null!;
    public List<Note> Notes { get; set; } = [];
}
