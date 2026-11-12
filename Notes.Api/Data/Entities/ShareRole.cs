namespace Notes.Api.Data.Entities;

/// <summary>Role of a share recipient. The owner is <c>Note.OwnerId</c>, never a share row (design D3).</summary>
public enum ShareRole
{
    Viewer = 0,
    Editor = 1,
}
