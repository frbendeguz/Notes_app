namespace Notes.Api;

/// <summary>Length limits – the single source of truth (design D8).</summary>
public static class Limits
{
    public const int TitleMax = 100;
    public const int ContentMax = 10_000;
    public const int FolderNameMax = 50;
    public const int UserNameMin = 3;
    public const int UserNameMax = 32;
    public const int PasswordMin = 8;
    public const int PasswordMax = 128;
}
