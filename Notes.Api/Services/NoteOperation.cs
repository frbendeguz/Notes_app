namespace Notes.Api.Services;

/// <summary>
/// Operations on a single note that need an access check (rows of the design 4 matrix).
/// <see cref="Share"/> covers sharing, revoking and listing the shares of a note (one row of the matrix).
/// </summary>
public enum NoteOperation
{
    Read,
    Edit,
    Delete,
    MoveToFolder,
    Share,
}
