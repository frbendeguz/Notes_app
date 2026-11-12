namespace Notes.Api;

public static class Text
{
    /// <summary>
    /// Normalized form used for case-insensitive uniqueness (design D2): trimmed, upper-case invariant.
    /// Handles accented letters too (e.g. "árvíztűrő" → "ÁRVÍZTŰRŐ").
    /// </summary>
    public static string Normalize(string s) => s.Trim().ToUpperInvariant();
}
