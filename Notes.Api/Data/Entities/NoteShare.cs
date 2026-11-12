namespace Notes.Api.Data.Entities;

public class NoteShare
{
    public Guid Id { get; set; }
    public Guid NoteId { get; set; }
    public Guid UserId { get; set; }
    public ShareRole Role { get; set; }

    public Note Note { get; set; } = null!;
    public User User { get; set; } = null!;
}
