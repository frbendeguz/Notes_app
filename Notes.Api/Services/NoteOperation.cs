namespace Notes.Api.Services;

/// <summary>
/// Operations on a single note that need an access check (rows of the design 4 matrix).
/// Sharing is added by its own task (05).
/// </summary>
public enum NoteOperation
{
    Read,
    Edit,
    Delete,
    MoveToFolder,
}
