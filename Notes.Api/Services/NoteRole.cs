namespace Notes.Api.Services;

/// <summary>
/// The caller's role on one note (design 4). <see cref="Owner"/> comes from <c>Note.OwnerId</c>,
/// <see cref="Editor"/> / <see cref="Viewer"/> from a <c>NoteShare</c> row, <see cref="None"/> means a stranger
/// or a non-existent note. Serialized as a string in JSON ("Owner", "Editor", "Viewer").
/// </summary>
public enum NoteRole
{
    None = 0,
    Viewer = 1,
    Editor = 2,
    Owner = 3,
}
