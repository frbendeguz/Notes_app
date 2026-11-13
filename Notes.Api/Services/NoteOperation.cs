namespace Notes.Api.Services;

/// <summary>
/// Operations on a single note that need an access check (rows of the design 4 matrix).
/// Sharing and moving into a folder are added by their own tasks (04, 05).
/// </summary>
public enum NoteOperation
{
    Read,
    Edit,
    Delete,
}
