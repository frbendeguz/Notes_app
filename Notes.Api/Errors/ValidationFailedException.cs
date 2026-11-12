namespace Notes.Api.Errors;

/// <summary>400: invalid input. <see cref="Errors"/> maps field names (camelCase) to user-facing messages.</summary>
public sealed class ValidationFailedException(IDictionary<string, string[]> errors)
    : Exception("A megadott adatok érvénytelenek.")
{
    public IDictionary<string, string[]> Errors { get; } = errors;
}
